using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using RTMService.Models;
using RTMService.Services;
using System;
using System.Collections.Generic;
using System.Linq;//
using System.Threading.Tasks;

namespace RTMService.Hubs
{
    public class ChatHub : Hub
    {
        private IChatService iservice;

        // Dictionary for storing current connections
        static Dictionary<string, string> CurrentConnections = new Dictionary<string, string>();

        //constructor with dependency injection
        public ChatHub(IChatService c)
        {          
            iservice = c;
        }
        public void SendToAll(string name, string message)
        {
            Clients.All.SendAsync("sendToAll", name, message);
        }

        public void PrintId(string id)
        {
            Clients.All.SendAsync("printId", Context.ConnectionId);
        }

        public void JoinGroup(string name)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, name);
        }

        public void JoinChannel(string ChannelId)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, ChannelId);
        }

        public void LeaveGroup(string name)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, "foo");
        }         
        public void SendMessageToGroups(string sender, string message)
        {
            List<string> groups = new List<string>() { "foo" };
            Clients.Groups(groups).SendAsync("SendMessageToGroups", sender, message);
        }
        
        // Send workspace object and user notifications 
        public void SendWorkspaceObject(string workspaceName, string userMail)
        {
            var searchedWorkspace = iservice.GetWorkspaceByName(workspaceName).Result;
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            try
            {               
                var stringifiedUserState = cache.StringGetAsync($"{userMail}");
                var UserStateObject = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);
                var workspaceStateObject = UserStateObject.ListOfWorkspaceState.
                    Where(w => w.WorkspaceName == workspaceName).FirstOrDefault();
                Clients.Clients(Context.ConnectionId).SendAsync("ReceiveUpdatedWorkspace", searchedWorkspace, workspaceStateObject);
            }
            catch 
            {
                var UserStateObject = iservice.GetUserStateByEmailId(userMail).Result;
                var workspaceStateObject = UserStateObject.ListOfWorkspaceState.
                    Where(w => w.WorkspaceName == workspaceName).FirstOrDefault();
                Clients.Clients(Context.ConnectionId).SendAsync("ReceiveUpdatedWorkspace", searchedWorkspace, workspaceStateObject);
            }
            finally
            { }
            
        }
        // add and remove user from channel notification
        public void AddLeaveChannelNotification(string channelId, User user)
        {
            Clients.Group(channelId).SendAsync("ReceiveChannelNotification", user,channelId);
        }
        // send all user channels
        public void SendAllUserChannel(string emailId)
        {
            var listOfUserChannels = iservice.GetAllUserChannels(emailId).Result;
            Clients.All.SendAsync("ReceiveUserChannels", listOfUserChannels,emailId);
        }
        // send message in channel
        public void SendMessageInChannel(string sender, Message message, string channelId, string workspaceName)
        {
           
            try
            {
                //////////////////////Notification Work//////////////////////////////////////
                var cache = RedisConnectorHelper.Connection.GetDatabase();

                var stringifiedUserState = cache.StringGetAsync($"{sender}");

                var UserStateObject = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);
                UserStateObject.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).
                    ListOfChannelState.Find(c => c.channelId == channelId).UnreadMessageCount = 0;

                UserStateObject.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).
                        ListOfChannelState.Find(c => c.channelId == channelId).LastTimestamp = message.Timestamp;
                string jsonString = JsonConvert.SerializeObject(UserStateObject);
                cache.StringSetAsync($"{sender}", jsonString);

                ///////////////////////////////////////////////////////////////////////////////
                Groups.AddToGroupAsync(Context.ConnectionId, channelId);
                var newMessage = iservice.AddMessageToChannel(message, channelId, sender).Result;
                Clients.Group(channelId).SendAsync("SendMessageInChannel", sender, newMessage);
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }
           
        }
        
        public override Task OnConnectedAsync()
        {
            //Console.WriteLine("in on connected");
            string id = Context.ConnectionId;
            CurrentConnections.Add(id, "");
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            CurrentConnections.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
        public void SendToAllconnid(string emailId)
        {
            try
            {
                int i = 0;
                if (emailId == null && CurrentConnections.ContainsKey(emailId))
                { return; }
                else
                {
                    CurrentConnections[Context.ConnectionId] = emailId;
                }


                string[] arr = new string[CurrentConnections.Count];
                foreach (var item in CurrentConnections)
                {
                    arr[i] = item.Value;
                    i++;
                }
                Clients.All.SendAsync("sendToAllconnid", arr);
            }
            catch
            {

            }

        }

        public void GetNotificationsForChannelsInWorkspace(string workspaceName, string emailId, string channelId, DateTime LastTimeStamp)
        {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            try
            {
                var stringifiedUserState = cache.StringGetAsync($"{emailId}");

                var UserStateObject = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);
                if(UserStateObject.EmailId == emailId)
                {
                    UserStateObject.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).
                ListOfChannelState.Find(c => c.channelId == channelId).UnreadMessageCount = 0;

                    UserStateObject.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).
                            ListOfChannelState.Find(c => c.channelId == channelId).LastTimestamp = LastTimeStamp;

                    string jsonString = JsonConvert.SerializeObject(UserStateObject);
                    cache.StringSetAsync($"{emailId}", jsonString);
                }
                else
                {
                    UserStateObject = iservice.GetUserStateByEmailId(emailId).Result;
                    UserStateObject.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).
                    ListOfChannelState.Find(c => c.channelId == channelId).UnreadMessageCount = 0;

                    UserStateObject.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).
                            ListOfChannelState.Find(c => c.channelId == channelId).LastTimestamp = LastTimeStamp;

                    string jsonString = JsonConvert.SerializeObject(UserStateObject);
                    cache.StringSetAsync($"{emailId}", jsonString);
                }
                
                

            }
            catch
            {
                
            }

        }
        public void whoistyping(string channelId, string name)
        {
            //Groups.AddToGroupAsync(Context.ConnectionId, channelId);
            Clients.OthersInGroup(channelId).SendAsync("whoistyping", name + " is typing",channelId,name);
        }
    }
}