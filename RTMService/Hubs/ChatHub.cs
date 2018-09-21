﻿using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using RTMService.Models;
using RTMService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Hubs
{
    public class ChatHub : Hub
    {
        private IChatService iservice;


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
        public void SendWorkspaceObject(string workspaceName)
        {
            var searchedWorkspace = iservice.GetWorkspaceByName(workspaceName).Result;
            Clients.Clients(Context.ConnectionId).SendAsync("ReceiveUpdatedWorkspace", searchedWorkspace);
        }
        public void SendAllUserChannel(string emailId)
        {
            var listOfUserChannels = iservice.GetAllUserChannels(emailId).Result;
            Clients.All.SendAsync("ReceiveUserChannels", listOfUserChannels);
        }
        public void SendMessageInChannel(string sender, Message message, string channelId)
        //public void SendMessageInChannel(string sender, Message message, string channelId, string workspaceName)
        {
            ///////////////////////////////////////////////////////////////////////////
            //var cache = RedisConnectorHelper.Connection.GetDatabase();

            //var stringifiedUserState = cache.StringGetAsync($"{sender}");
            //if (stringifiedUserState.Result.HasValue)
            //{
            //    var UserStateObject = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);
            //    var workspaceStateObject = UserStateObject.ListOfWorkspaceState.
            //        Where(w => w.WorkspaceName == workspaceName).FirstOrDefault();
            //    workspaceStateObject.ListOfChannelState.Find(v => v.channelId == channelId).UnreadMessageCount++;
            //    workspaceStateObject.ListOfChannelState.Find(v => v.channelId == channelId).LastTimestamp = message.Timestamp;

            //    /*var nnn =*/ UserStateObject.ListOfWorkspaceState.Find(v => v.WorkspaceName == workspaceName) = workspaceStateObject;
            //    string jsonString = JsonConvert.SerializeObject(UserStateObject);
            //    cache.StringSetAsync($"{sender}", jsonString);
            //}
            ///////////////////////////////////////////////////////////////////////////////
            Groups.AddToGroupAsync(Context.ConnectionId, channelId);
            var newMessage = iservice.AddMessageToChannel(message, channelId, sender).Result;
            Clients.Group(channelId).SendAsync("SendMessageInChannel", sender, newMessage);
            //Clients.Client(Context.ConnectionId).SendAsync(channelId);
        }
        static Dictionary<string, string> CurrentConnections = new Dictionary<string, string>();
        public override Task OnConnectedAsync()
        {
            string id = Context.ConnectionId;
            CurrentConnections.Add(id, "");
            //SendToAllconnid(CurrentConnections.Count);
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

        //public WorkspaceState GetNotificationsForChannelsInWorkspace(string workspaceName, string emailId,string channelId, DateTime LastTimeStamp)
        //{
        //    try
        //    {
        //        var cache = RedisConnectorHelper.Connection.GetDatabase();

        //        var stringifiedUserState = cache.StringGetAsync($"{emailId}");

        //            var UserStateObject = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);
        //            var workspaceStateObject = UserStateObject.ListOfWorkspaceState.
        //                Where(w => w.WorkspaceName == workspaceName).FirstOrDefault();
        //            workspaceStateObject.ListOfChannelState.Find(v => v.channelId == channelId).UnreadMessageCount = 0;
        //            workspaceStateObject.ListOfChannelState.Find(v => v.channelId == channelId).LastTimestamp = LastTimeStamp;
        //            return workspaceStateObject;


        //    }
        //    catch
        //    {
        //        var cache = RedisConnectorHelper.Connection.GetDatabase();
        //        ChannelState channel = new ChannelState()
        //        {
        //            channelId = channelId,
        //            UnreadMessageCount = 0,
        //            LastTimestamp = LastTimeStamp
        //        };

        //        WorkspaceState newWorkspace = new WorkspaceState()
        //        {
        //            WorkspaceName = workspaceName,

        //        };
        //        newWorkspace.ListOfChannelState.Add(channel);
        //        UserState userState = new UserState()
        //        {
        //            EmailId = emailId,
        //        };
        //        userState.ListOfWorkspaceState.Add(newWorkspace);
        //        iservice.CreateNotificationStateOfUser(userState);
        //        string jsonString = JsonConvert.SerializeObject(userState);
        //        cache.StringSetAsync($"{emailId}", jsonString);
        //        return newWorkspace;

        //    }

        //}
    }
}