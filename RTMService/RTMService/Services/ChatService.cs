using MongoDB.Driver;
using MongoDB.Driver.Builders;
using Newtonsoft.Json;
using RTMService.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Services
{
    public class ChatService : IChatService
    {
        MongoClient _client;
        MongoServer _server;
        IMongoCollection<Workspace> _dbWorkSpace;
        IMongoCollection<Channel> _dbChannel;
        IMongoCollection<User> _dbUser;
        IMongoCollection<Message> _dbMessage;
        IMongoCollection<OneToOneChannelInfo> _dbOneToOne;

        public ChatService()
        {
            _client = new MongoClient("mongodb://localhost:27017");
            _server = _client.GetServer();
            _dbWorkSpace = _client.GetDatabase("AllWorkspace").GetCollection<Workspace>("Workspace");
            _dbChannel = _client.GetDatabase("AllChannels").GetCollection<Channel>("Channel");
            _dbUser = _client.GetDatabase("AllUsers").GetCollection<User>("User");
            _dbMessage = _client.GetDatabase("AllMessages").GetCollection<Message>("Message");
            _dbOneToOne = _client.GetDatabase("OneToOneTable").GetCollection<OneToOneChannelInfo>("OneToOne");

        }

        public async Task<IEnumerable<Workspace>> GetAllWorkspacesAsync()
        {
            return await _dbWorkSpace.Find(_ => true).ToListAsync();
        }



        public async Task<Workspace> GetWorkspaceById(string id)
        {

            return await _dbWorkSpace.Find(w => w.WorkspaceId == id).FirstOrDefaultAsync();
        }

        public async Task<Workspace> GetWorkspaceByName(string workspaceName)
        {
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            var stringifiedWorkspace = cache.StringGetAsync($"{workspaceName}");
            if (stringifiedWorkspace.Result.HasValue)
            {
                var workspaceObject = JsonConvert.DeserializeObject<Workspace>(stringifiedWorkspace.Result);

                return workspaceObject;
            }



            var Workspace = _dbWorkSpace.Find(w => w.WorkspaceName == workspaceName).FirstOrDefaultAsync();
            string jsonString = JsonConvert.SerializeObject(Workspace.Result);
            await cache.StringSetAsync($"{workspaceName}", jsonString, TimeSpan.FromMinutes(1));

            return await Workspace;



        }

        public async Task<Workspace> CreateWorkspace(WorkspaceView workSpace)
        {
            Workspace newWorkspace = new Workspace
            {
                WorkspaceId = workSpace.Id,
                WorkspaceName = workSpace.WorkspaceName
            };

            await _dbWorkSpace.InsertOneAsync(newWorkspace);
            //creating default channels
            foreach (var channel in workSpace.Channels)
            {
                Channel newChannel = new Channel
                {
                    ChannelName = channel.ChannelName,
                    //Admin = user,
                    WorkspaceId = newWorkspace.WorkspaceId
                };
                // newChannel.Users.Add(user);
                await CreateDefaultChannel(newChannel, workSpace.WorkspaceName);
            }

            return await GetWorkspaceById(newWorkspace.WorkspaceId);
        }
        public async Task DeleteWorkspace(string id)
        {

            await _dbWorkSpace.DeleteOneAsync(w => w.WorkspaceId == id);
        }

        public async Task<Channel> CreateChannel(Channel channel, string workspaceName)
        {
            var searchedWorkspace = GetWorkspaceByName(workspaceName).Result;
            channel.WorkspaceId = searchedWorkspace.WorkspaceId;
            await _dbChannel.InsertOneAsync(channel);
            var result = GetWorkspaceById(searchedWorkspace.WorkspaceId).Result;
            result.Channels.Add(channel);
            result.WorkspaceId = searchedWorkspace.WorkspaceId;
            var filter = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == result.WorkspaceId);
            await _dbWorkSpace.ReplaceOneAsync(filter, result);
            /////Storing in cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            string jsonString = JsonConvert.SerializeObject(result);
            await cache.StringSetAsync($"{workspaceName}", jsonString);
            string jsonStringChannel = JsonConvert.SerializeObject(channel);
            await cache.StringSetAsync($"{channel.ChannelId}", jsonStringChannel);
            ///////////
            return channel;
        }
        public async Task<Channel> CreateDefaultChannel(Channel channel, string workspaceName)
        {

            var searchedWorkspace = GetWorkspaceByName(workspaceName).Result;
            channel.WorkspaceId = searchedWorkspace.WorkspaceId;
            await _dbChannel.InsertOneAsync(channel);
            var result = GetWorkspaceById(searchedWorkspace.WorkspaceId).Result;
            result.DefaultChannels.Add(channel);
            result.WorkspaceId = searchedWorkspace.WorkspaceId;
            var filter = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == result.WorkspaceId);
            await _dbWorkSpace.ReplaceOneAsync(filter, result);
            /////Storing in cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            string jsonString = JsonConvert.SerializeObject(result);
            await cache.StringSetAsync($"{workspaceName}", jsonString);
            ///////////
            return channel;
        }
        public async Task<Channel> CreateOneToOneChannel(Channel channel, string workspaceName)
        {

            var searchedWorkspace = GetWorkspaceByName(workspaceName).Result;
            channel.WorkspaceId = searchedWorkspace.WorkspaceId;
            await _dbChannel.InsertOneAsync(channel);
            //storing in cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            //changed
            string jsonString = JsonConvert.SerializeObject(channel);
            await cache.StringSetAsync($"{channel.ChannelId}", jsonString);
            return channel;
        }
        public async Task<Channel> GetChannelById(string channelId)
        {
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            var stringifiedChannel = cache.StringGetAsync($"{channelId}");
            if (stringifiedChannel.Result.HasValue)
            {
                var channelObject = JsonConvert.DeserializeObject<Channel>(stringifiedChannel.Result);

                return channelObject;
            }



            var Channel = _dbChannel.Find(w => w.ChannelId == channelId).FirstOrDefaultAsync();
            string jsonString = JsonConvert.SerializeObject(Channel.Result);
            await cache.StringSetAsync($"{channelId}", jsonString);

            return await Channel;
            //changed
            //return await _dbChannel.Find(w => w.ChannelId == channelId).FirstOrDefaultAsync();
        }
        public async Task<User> AddUserToChannel(User newUser, string channelId)
        {

            // add user to channel and updating channel
            var resultChannel = GetChannelById(channelId).Result;
            resultChannel.Users.Add(newUser);
            resultChannel.ChannelId = channelId;
            var filter = new FilterDefinitionBuilder<Channel>().Where(r => r.ChannelId == resultChannel.ChannelId);
            var update = Builders<Channel>.Update
                .Set(r => r.ChannelId, resultChannel.ChannelId)
                .Set(r => r.Users, resultChannel.Users);
            await _dbChannel.UpdateOneAsync(filter, update);

            // update channel in workspace
            var resultWorkspace = GetWorkspaceById(resultChannel.WorkspaceId).Result;
            resultWorkspace.Channels.First(i => i.ChannelId == channelId).Users.Add(newUser);
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == resultChannel.WorkspaceId);
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.Channels, resultWorkspace.Channels)
                .Set(r => r.WorkspaceId, resultChannel.WorkspaceId);
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);
            /////Storing in cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            string jsonStringWorkspace = JsonConvert.SerializeObject(resultWorkspace);
            await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonStringWorkspace);
            // storing channel in cache
            string jsonStringChannel = JsonConvert.SerializeObject(resultChannel);
            await cache.StringSetAsync($"{resultChannel.ChannelId}", jsonStringChannel);
            ///////////
            return newUser;

        }
        public async Task<User> AddUserToDefaultChannel(User newUser, string channelId)
        {

            // add user to default channel and updating channel
            var resultChannel = GetChannelById(channelId).Result;
            resultChannel.Users.Add(newUser);
            //resultChannel.Admin = newUser;
            resultChannel.ChannelId = channelId;
            var filter = new FilterDefinitionBuilder<Channel>().Where(r => r.ChannelId == resultChannel.ChannelId);
            var update = Builders<Channel>.Update
                .Set(r => r.ChannelId, resultChannel.ChannelId)
                .Set(r => r.Users, resultChannel.Users);
            await _dbChannel.UpdateOneAsync(filter, update);

            var resultWorkspace = GetWorkspaceById(resultChannel.WorkspaceId).Result;
            resultWorkspace.DefaultChannels.First(i => i.ChannelId == channelId).Users.Add(newUser);
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == resultChannel.WorkspaceId);
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.DefaultChannels, resultWorkspace.DefaultChannels)
                .Set(r => r.WorkspaceId, resultChannel.WorkspaceId);
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);
            /////Storing in cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            string jsonStringWorkspace = JsonConvert.SerializeObject(resultWorkspace);
            await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonStringWorkspace);
            // storing channel in cache
            string jsonStringChannel = JsonConvert.SerializeObject(resultChannel);
            await cache.StringSetAsync($"{resultChannel.ChannelId}", jsonStringChannel);
            ///////////
            return newUser;

        }
        //changed
        public async Task<List<Message>> GetLastNMessagesOfChannel(string channelId, int N)
        {
            var listOfMessages = await  _dbMessage.Find(m => m.ChannelId==channelId).ToListAsync();
            var sortedMessages = listOfMessages.OrderBy(m => m.Timestamp).ToList();
            var list = sortedMessages.Skip(sortedMessages.Count() - N).Take(2).ToList();
            return list;
            //return channel.Messages.Skip(channel.Messages.Count() - N).Take(3).ToList();
        }
        public async Task<Message> AddMessageToChannel(Message message, string channelId, string senderMail)
        {


            var resultChannel = GetChannelById(channelId).Result;
            //var resultWorkspace = GetWorkspaceById(resultChannel.WorkspaceId).Result;
            //var resultSender = GetUserByEmail(senderMail, resultWorkspace.WorkspaceName);
            Message newMessage = message;
            await _dbMessage.InsertOneAsync(newMessage);
           
            resultChannel.Messages.Add(newMessage);

            var lastmessages = resultChannel.Messages.Skip(Math.Max(0, resultChannel.Messages.Count() - 5)).ToList();
            Channel cacheChannel = new Channel()
            {
                Messages = lastmessages,
                ChannelId = resultChannel.ChannelId,
                Users = resultChannel.Users,
                WorkspaceId = resultChannel.WorkspaceId,
                ChannelName = resultChannel.ChannelName,
                Admin = resultChannel.Admin
            };
            //this is the culprit
            //cacheChannel.Messages = lastmessages;
            ///////////////////////////
            resultChannel.ChannelId = channelId;
            var filter = new FilterDefinitionBuilder<Channel>().Where(r => r.ChannelId == resultChannel.ChannelId);
            var update = Builders<Channel>.Update
                .Set(r => r.ChannelId, resultChannel.ChannelId)
                .Set(r => r.Messages, resultChannel.Messages);
            await _dbChannel.UpdateOneAsync(filter, update);
            /////Storing in cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            //changed
            // storing channel in cache
            string jsonStringChannel = JsonConvert.SerializeObject(cacheChannel);
            await cache.StringSetAsync($"{resultChannel.ChannelId}", jsonStringChannel);
            return newMessage;

        }

        public async Task DeleteChannel(string channelId)
        {
            var channelresult = GetChannelById(channelId).Result;
            await _dbChannel.DeleteOneAsync(w => w.ChannelId == channelId);
            var workspace = GetWorkspaceById(channelresult.WorkspaceId).Result;
            var channelToDelete = workspace.Channels.Find(c => c.ChannelId == channelId);
            var defaultChannelToDelete = workspace.DefaultChannels.Find(c => c.ChannelId == channelId);
            workspace.Channels.Remove(channelToDelete);
            workspace.DefaultChannels.Remove(defaultChannelToDelete);
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == channelresult.WorkspaceId);
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.DefaultChannels, workspace.DefaultChannels)
                .Set(r => r.Channels, workspace.Channels)
                .Set(r => r.WorkspaceId, workspace.WorkspaceId);
            /////Storing in cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            string jsonString = JsonConvert.SerializeObject(workspace);
            await cache.StringSetAsync($"{workspace.WorkspaceName}", jsonString);
            ///////////
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);

        }
        public async Task DeleteUserFromChannel(string emailId, string channelId)
        {
            var channel = GetChannelById(channelId).Result;

            var resultUser = channel.Users.Find(u => u.EmailId == emailId);//GetUserByEmail(emailId);

            channel.Users.Remove(resultUser);
            channel.ChannelId = channelId;
            var filter = new FilterDefinitionBuilder<Channel>().Where(r => r.ChannelId == channel.ChannelId);
            var update = Builders<Channel>.Update
                .Set(r => r.ChannelId, channel.ChannelId)
                .Set(r => r.Users, channel.Users);
            await _dbChannel.UpdateOneAsync(filter, update);

            var resultWorkspace = GetWorkspaceById(channel.WorkspaceId).Result;
            resultWorkspace.WorkspaceId = channel.WorkspaceId;
            var userToDelete = resultWorkspace.Channels.Find(c => c.ChannelId == channelId).Users.Find(u => u.EmailId == emailId);
            resultWorkspace.Channels.Find(c => c.ChannelId == channelId).Users.Remove(userToDelete);
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == resultWorkspace.WorkspaceId);
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.DefaultChannels, resultWorkspace.DefaultChannels)
                .Set(r => r.Channels, resultWorkspace.Channels)
                .Set(r => r.WorkspaceId, resultWorkspace.WorkspaceId);
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);
            /////Storing in cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            //changed
            string jsonString = JsonConvert.SerializeObject(resultWorkspace);
            await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonString);
            string jsonStringChannel = JsonConvert.SerializeObject(channel);
            await cache.StringSetAsync($"{channel.ChannelId}", jsonStringChannel);
            ///////////
        }

        public List<User> GetAllUsersInWorkspace(string workspaceName)
        {
            var resultWorkspace = GetWorkspaceByName(workspaceName).Result;
            return resultWorkspace.Users;
        }

        public async Task<User> AddUserToWorkspace(UserAccountView newuser, string workspaceName)
        {
            User user = new User
            {
                UserId = newuser.Id,
                EmailId = newuser.EmailId,
                FirstName = newuser.FirstName,
                LastName = newuser.LastName
            };
            await _dbUser.InsertOneAsync(user);

            var resultWorkspace = GetWorkspaceByName(workspaceName).Result;
            resultWorkspace.Users.Add(user);
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == resultWorkspace.WorkspaceId);
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.Users, resultWorkspace.Users)
                .Set(r => r.WorkspaceId, resultWorkspace.WorkspaceId);
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);

            var listOfDefaultChannels = resultWorkspace.DefaultChannels;
            foreach (var defaultChannel in listOfDefaultChannels)
            {
                await AddUserToDefaultChannel(user, defaultChannel.ChannelId);
            }
            /////Storing in cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            string jsonString = JsonConvert.SerializeObject(resultWorkspace);
            await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonString);
            ///////////
            return user;
        }
        public async Task<string> GetChannelIdForOneToOneChat(string senderMail, string receiverMail, string workspaceId)
        {
            try
            {
                var entry = await _dbOneToOne.
                Find(o => o.WorkspaceId == workspaceId &&
                o.Users.Any(u => u == senderMail) &&
                o.Users.Any(u => u == receiverMail)).
                FirstOrDefaultAsync();
                return entry.ChannelId;
            }
            catch
            {
                return null;
            }
        }
        public async Task<Channel> GetChannelForOneToOneChat(string senderMail, string receiverMail, string workspaceName)
        {
            var workspace = GetWorkspaceByName(workspaceName).Result;
            var channelId = GetChannelIdForOneToOneChat(senderMail, receiverMail, workspace.WorkspaceId).Result;
            //changed
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            var stringifiedChannel = cache.StringGetAsync($"{channelId}");
            if (stringifiedChannel.Result.HasValue)
            {
                var ChannelObject = JsonConvert.DeserializeObject<Channel>(stringifiedChannel.Result);

                return ChannelObject;
            }
            /////////
            var oneToOneChannel = GetChannelById(channelId);
            if (oneToOneChannel.Result != null)
            {
                return oneToOneChannel.Result;
            }
            else
            {
                var sender = GetUserByEmail(senderMail, workspaceName);
                var receiver = GetUserByEmail(receiverMail, workspaceName);
                Channel newOneToOneChannel = new Channel
                {
                    ChannelName = "",

                };
                newOneToOneChannel.Users.Add(sender);
                newOneToOneChannel.Users.Add(receiver);
                oneToOneChannel = CreateOneToOneChannel(newOneToOneChannel, workspaceName);

                OneToOneChannelInfo entryOfPersonalChannel = new OneToOneChannelInfo
                {
                    ChannelId = oneToOneChannel.Result.ChannelId,
                    WorkspaceId = workspace.WorkspaceId
                };
                entryOfPersonalChannel.Users.Add(senderMail);
                entryOfPersonalChannel.Users.Add(receiverMail);
                await _dbOneToOne.InsertOneAsync(entryOfPersonalChannel);
                return oneToOneChannel.Result;
            }
        }


        public async Task<List<Channel>> GetAllUserChannelsInWorkSpace(string workSpaceName, string emailId)
        {
            var workspace = GetWorkspaceByName(workSpaceName).Result;
            return await _dbChannel.Find(p => (p.WorkspaceId == workspace.WorkspaceId) && (p.ChannelName != "") && (p.Users.Any(u => u.EmailId == emailId))).ToListAsync();

        }

        public async Task<List<Channel>> GetAllChannelsInWorkspace(string workSpaceName)
        {

            var workspace = GetWorkspaceByName(workSpaceName).Result;
            var result = Query<Channel>.Where(p => (p.WorkspaceId == workspace.WorkspaceId) && (p.ChannelName != ""));
            return await _dbChannel.Find(p => (p.WorkspaceId == workspace.WorkspaceId) && (p.ChannelName != "")).ToListAsync();


        }

        public User GetUserByEmail(string emailId, string workspaceName)
        {
            var resultWorkspace = GetWorkspaceByName(workspaceName).Result;
            var user = resultWorkspace.Users.Find(u => u.EmailId == emailId);
            return user;
        }


    }
}