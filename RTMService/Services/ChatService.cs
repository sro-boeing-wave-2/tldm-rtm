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
        IMongoCollection<UserState> _dbNotificationUserState;


        // constructor for chat service
        public ChatService()
        {
            // for running the application in local machine
           //_client = new MongoClient("mongodb://localhost:27017");
           
            // for running the application in docker
            _client = new MongoClient("mongodb://db/admindatabase");

            // starting Mongo Server 
            // Stop using this version as it is obsolete
            _server = _client.GetServer();
            
            // creating mongodb collection for all required models

            // Workspace Collection
            _dbWorkSpace = _client.GetDatabase("AllWorkspace").GetCollection<Workspace>("Workspace");
            // Channel Collection
            _dbChannel = _client.GetDatabase("AllChannels").GetCollection<Channel>("Channel");
            // User Collection
            _dbUser = _client.GetDatabase("AllUsers").GetCollection<User>("User");
            // Message Collection
            _dbMessage = _client.GetDatabase("AllMessages").GetCollection<Message>("Message");
            // One to One Collection
            _dbOneToOne = _client.GetDatabase("OneToOneTable").GetCollection<OneToOneChannelInfo>("OneToOne");
            // User State and Notification Collection
            _dbNotificationUserState = _client.GetDatabase("Notification").GetCollection<UserState>("UserState");
        }

        //find all workspaces from database
        public async Task<IEnumerable<Workspace>> GetAllWorkspacesAsync()
        {
            return await _dbWorkSpace.Find(_ => true).ToListAsync();
        }

        // search workspace by workspace id from database 
        public async Task<Workspace> GetWorkspaceById(string id)
        {
            return await _dbWorkSpace.Find(w => w.WorkspaceId == id).FirstOrDefaultAsync();
        }

        // search workspace by name 
        public async Task<Workspace> GetWorkspaceByName(string workspaceName)
        {
            // get redis database and call it cache 
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            // get workspace object as string value with key workspace name
            var stringifiedWorkspace = cache.StringGetAsync($"{workspaceName}");
            if (stringifiedWorkspace.Result.HasValue)
            {
                // if the value exists then convert string to workpsace object
                var workspaceObject = JsonConvert.DeserializeObject<Workspace>(stringifiedWorkspace.Result);
                return workspaceObject;
            }

            // find workspace from database
            var Workspace = _dbWorkSpace.Find(w => w.WorkspaceName == workspaceName).FirstOrDefaultAsync();

            // convert it into string
            string jsonString = JsonConvert.SerializeObject(Workspace.Result);

            // store it in cache
            await cache.StringSetAsync($"{workspaceName}", jsonString);

            return await Workspace;



        }
        // Insert a document for user in notification collection
        public async Task CreateNotificationStateOfUser(UserState userState)
        {
            await _dbNotificationUserState.InsertOneAsync(userState);
        }
        // get user state by email Id
        public async Task<UserState> GetUserStateByEmailId(string emailId)
        {
            return await _dbNotificationUserState.Find(w => w.EmailId == emailId).FirstOrDefaultAsync();
        }
        // Create a new Workspace using workspace view
        public async Task<Workspace> CreateWorkspace(WorkspaceView workSpace)
        {
            Workspace newWorkspace = new Workspace
            {
                WorkspaceId = workSpace.Id,
                WorkspaceName = workSpace.WorkspaceName
            };

            await _dbWorkSpace.InsertOneAsync(newWorkspace);

            //creating default channels in workspace 
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
            //foreach(var bot in workSpace.Bots)
            //{
            //    UserAccountView newUser = new UserAccountView
            //    {
            //        EmailId = bot.LogoUrl,
            //        FirstName = bot.Name,
            //        LastName = bot.Name
            //    };
            //   await AddUserToWorkspace(newUser, workSpace.WorkspaceName);
            //}

            return await GetWorkspaceById(newWorkspace.WorkspaceId);
        }
        // delete a workspace from collection by workspace Id
        public async Task DeleteWorkspace(string id)
        {
            await _dbWorkSpace.DeleteOneAsync(w => w.WorkspaceId == id);
            // update redis cache with deleted workpsace 
            // NOT Implemented!!!!!!!!!!!!
        }
        // Create a channel in workspace 
        public async Task<Channel> CreateChannel(Channel channel, string workspaceName)
        {
            // Search the workspace in which channel needs to be added 
            var searchedWorkspace = GetWorkspaceByName(workspaceName).Result;

            // update channel with the workspace id to complete the model
            channel.WorkspaceId = searchedWorkspace.WorkspaceId;

            // insert the document in mongo collection
            await _dbChannel.InsertOneAsync(channel);

            // search workspace by ID  (may not need this, CHECK!!!!!)
            var result = GetWorkspaceById(searchedWorkspace.WorkspaceId).Result;

            // add channel to channel list of workpsace 
            result.Channels.Add(channel);

            // make sure the workspace id is same 
            result.WorkspaceId = searchedWorkspace.WorkspaceId;

            //Apply the filer to the collection
            var filter = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == result.WorkspaceId);
           
            // replace the document in collection
            await _dbWorkSpace.ReplaceOneAsync(filter, result);

            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            // convert workspace object into string
            string jsonString = JsonConvert.SerializeObject(result);

            // update the key value pair of workpsace in cache
            await cache.StringSetAsync($"{workspaceName}", jsonString);

            // convert channel object to string
            string jsonStringChannel = JsonConvert.SerializeObject(channel);

            // update key value pair of channel in cache
            await cache.StringSetAsync($"{channel.ChannelId}", jsonStringChannel);

            ///////////////Notification Work/////////////////////
            // iterate over all users in a channel
            foreach(var user in channel.Users)
            {
                // get user state from the cache 
                var stringifiedUserState = cache.StringGetAsync($"{user.EmailId}");

                // convert the result string to user state object 
                var userstate = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);

                // create a new channel state 
                ChannelState channelState = new ChannelState()
                {
                    channelId = channel.ChannelId,
                    UnreadMessageCount = 0
                };

                // add new channel to the workspace inside the user state
                userstate.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).ListOfChannelState.Add(channelState);

                //convert the object back to string
                string jsonStringUserState = JsonConvert.SerializeObject(userstate);
                
                // update the user state inside cache
                await cache.StringSetAsync($"{userstate.EmailId}", jsonStringUserState);
                
                //updating in mongo the user state for safety and debugging
                var filterUserState = new FilterDefinitionBuilder<UserState>().Where(r => r.EmailId == user.EmailId);

                var updateUserState = Builders<UserState>.Update
                    .Set(r => r.ListOfWorkspaceState, userstate.ListOfWorkspaceState);

                await _dbNotificationUserState.UpdateOneAsync(filterUserState, updateUserState);

            }
            ////////////////////////////////////////////////////////
            return channel;
        }
        // Creating a default channel
        public async Task<Channel> CreateDefaultChannel(Channel channel, string workspaceName)
        {
            // search workspace by name
            var searchedWorkspace = GetWorkspaceByName(workspaceName).Result;

            // set the workspace id inside channel
            channel.WorkspaceId = searchedWorkspace.WorkspaceId;

            // insert the document inside channel collection
            await _dbChannel.InsertOneAsync(channel);

            //search workspace by id (may not need this , CHECK!!!!!!!)
            var result = GetWorkspaceById(searchedWorkspace.WorkspaceId).Result;

            // add default channel to workspace
            result.DefaultChannels.Add(channel);

            // // make sure the workspace id is same 
            result.WorkspaceId = searchedWorkspace.WorkspaceId;

            //Apply the filer to the collection
            var filter = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == result.WorkspaceId);

            // replace the document in mongo
            await _dbWorkSpace.ReplaceOneAsync(filter, result);
            
            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            // convert workspace object to string
            string jsonString = JsonConvert.SerializeObject(result);

            //update it in cache
            await cache.StringSetAsync($"{workspaceName}", jsonString);
            ///////////
            return channel;
        }
        // create a channel for on to one communication
        public async Task<Channel> CreateOneToOneChannel(Channel channel, string workspaceName)
        {
            // search workspace by name 
            var searchedWorkspace = GetWorkspaceByName(workspaceName).Result;

            // set workpsace id inside channel id
            channel.WorkspaceId = searchedWorkspace.WorkspaceId;

            // insert the channel in mongo collection
            await _dbChannel.InsertOneAsync(channel);

            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            //changed
            string jsonString = JsonConvert.SerializeObject(channel);
            await cache.StringSetAsync($"{channel.ChannelId}", jsonString);

            ///////////////Notification Work/////////////////////
            var stringifiedUserState = cache.StringGetAsync($"{channel.Users[0].EmailId}");

            var userstate = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);
            //var userstate = await GetUserStateByEmailId(channel.Users[0].EmailId);

            ChannelState channelState = new ChannelState()
            {
                channelId = channel.ChannelId,
                UnreadMessageCount = 0
            };

            userstate.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).ListOfChannelState.Add(channelState);

            string jsonStringUserState = JsonConvert.SerializeObject(userstate);

            await cache.StringSetAsync($"{userstate.EmailId}", jsonStringUserState);
            
            //updating in mongo

            var filterUserState = new FilterDefinitionBuilder<UserState>().Where(r => r.EmailId == channel.Users[0].EmailId);

            var updateUserState = Builders<UserState>.Update
                .Set(r => r.ListOfWorkspaceState, userstate.ListOfWorkspaceState);

            await _dbNotificationUserState.UpdateOneAsync(filterUserState, updateUserState);
            /////

            var stringifiedUserState1 = cache.StringGetAsync($"{channel.Users[1].EmailId}");

            var userstate1 = JsonConvert.DeserializeObject<UserState>(stringifiedUserState1.Result);
            //var userstate1 = await GetUserStateByEmailId(channel.Users[1].EmailId);

            ChannelState channelState1 = new ChannelState()
            {
                channelId = channel.ChannelId,
                UnreadMessageCount = 0
            };

            userstate1.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).ListOfChannelState.Add(channelState1);

            string jsonStringUserState1 = JsonConvert.SerializeObject(userstate1);

            await cache.StringSetAsync($"{userstate1.EmailId}", jsonStringUserState1);
            //updating in mongo

            var filterUserState1 = new FilterDefinitionBuilder<UserState>().Where(r => r.EmailId == channel.Users[1].EmailId);

            var updateUserState1 = Builders<UserState>.Update
                .Set(r => r.ListOfWorkspaceState, userstate1.ListOfWorkspaceState);

            await _dbNotificationUserState.UpdateOneAsync(filterUserState1, updateUserState1);
            /////
            ////////////////////////////////////////////////////////
            return channel;
        }
        public async Task<Channel> GetChannelById(string channelId)
        {
            // get redis database and call it cache
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
            //// get redis database and call it cache

            var cache = RedisConnectorHelper.Connection.GetDatabase();

            string jsonStringWorkspace = JsonConvert.SerializeObject(resultWorkspace);

            await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonStringWorkspace);
            // storing channel in cache

            string jsonStringChannel = JsonConvert.SerializeObject(resultChannel);

            await cache.StringSetAsync($"{resultChannel.ChannelId}", jsonStringChannel);
            ///////////
            ///////////////Notification Work/////////////////////
            var stringifiedUserState = cache.StringGetAsync($"{newUser.EmailId}");

            var userstate = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);
            //var userstate = await GetUserStateByEmailId(newUser.EmailId);

            ChannelState channel = new ChannelState()
            {
                channelId = channelId,
                UnreadMessageCount = 0
            };
            userstate.ListOfWorkspaceState.Find(w => w.WorkspaceName == resultWorkspace.WorkspaceName).ListOfChannelState.Add(channel);

            string jsonStringUserState = JsonConvert.SerializeObject(userstate);

            await cache.StringSetAsync($"{userstate.EmailId}", jsonStringUserState);
            //updating in mongo

            var filterUserState = new FilterDefinitionBuilder<UserState>().Where(r => r.EmailId == newUser.EmailId);

            var updateUserState = Builders<UserState>.Update
                .Set(r => r.ListOfWorkspaceState, userstate.ListOfWorkspaceState);

            await _dbNotificationUserState.UpdateOneAsync(filterUserState, updateUserState);
            ////////////////////////////////////////////////////////

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
            //// get redis database and call it cache

            var cache = RedisConnectorHelper.Connection.GetDatabase();


            string jsonStringWorkspace = JsonConvert.SerializeObject(resultWorkspace);

            await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonStringWorkspace);
            // storing channel in cache

            string jsonStringChannel = JsonConvert.SerializeObject(resultChannel);

            await cache.StringSetAsync($"{resultChannel.ChannelId}", jsonStringChannel);
            
            ///////////
            return newUser;

        }
        public async Task<List<string>> GetAllUserChannels(string emailId)
        {

            var listOfChannels = await _dbChannel.Find(p => (p.ChannelName != "") && (p.Users.Any(u => u.EmailId == emailId))).ToListAsync();

            List<string> listOfChannelIds = new List<string>();

            foreach (var channel in listOfChannels)
            {
                listOfChannelIds.Add(channel.ChannelId);
            }
            return listOfChannelIds;
        }
        //changed
        public async Task<List<Message>> GetLastNMessagesOfChannel(string channelId, int N)
        {
            var listOfMessages = await  _dbMessage.Find(m => m.ChannelId==channelId).ToListAsync();

            var sortedMessages = listOfMessages.OrderBy(m => m.Timestamp).ToList();

            if (N < sortedMessages.Count())
            {
                var list = sortedMessages.Skip(sortedMessages.Count() - N).Take(10).ToList();
                return list;
            }
            
            else
            {
                List<Message> emptyList = new List<Message>() { };
                return emptyList;
            }
            
            //return channel.Messages.Skip(channel.Messages.Count() - N).Take(3).ToList();
        }
        public async Task<Message> AddMessageToChannel(Message message, string channelId, string senderMail)
        {


            var resultChannel = GetChannelById(channelId).Result;

            var resultWorkspace = GetWorkspaceById(resultChannel.WorkspaceId).Result;
            //var resultSender = GetUserByEmail(senderMail, resultWorkspace.WorkspaceName);

            Message newMessage = message;

            await _dbMessage.InsertOneAsync(newMessage);
           
            resultChannel.Messages.Add(newMessage);

            var lastmessages = resultChannel.Messages.Skip(Math.Max(0, resultChannel.Messages.Count() - 50)).ToList();

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
            // get redis database and call it cache

            var cache = RedisConnectorHelper.Connection.GetDatabase();
            //changed
            // storing channel in cache

            string jsonStringChannel = JsonConvert.SerializeObject(cacheChannel);
            await cache.StringSetAsync($"{resultChannel.ChannelId}", jsonStringChannel);

            //////////////////Notification Work/////////////////////////////////////
           try
            {
                foreach (var user in resultChannel.Users)
                {
                    if (user.EmailId != senderMail)
                    {
                        var stringifiedUserState = cache.StringGetAsync($"{user.EmailId}");

                        var UserStateObject = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);
                        UserStateObject.ListOfWorkspaceState.Find(w => w.WorkspaceName == resultWorkspace.WorkspaceName).
                            ListOfChannelState.Find(c => c.channelId == channelId).UnreadMessageCount++;

                        string jsonString = JsonConvert.SerializeObject(UserStateObject);
                        await cache.StringSetAsync($"{user.EmailId}", jsonString);
                    }


                }
            }
            catch(Exception e)
            {
                Console.Write(e.Message);
            }
            
            
            //////////////////////////////////////////////////////////////////////////
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
            // get redis database and call it cache
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
            try
            {
                var userToDelete = resultWorkspace.DefaultChannels.Find(c => c.ChannelId == channelId).Users.Find(u => u.EmailId == emailId);
                resultWorkspace.DefaultChannels.Find(c => c.ChannelId == channelId).Users.Remove(userToDelete);
            }
            catch
            {
                var userToDelete = resultWorkspace.Channels.Find(c => c.ChannelId == channelId).Users.Find(u => u.EmailId == emailId);
                resultWorkspace.Channels.Find(c => c.ChannelId == channelId).Users.Remove(userToDelete);
            }
            
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == resultWorkspace.WorkspaceId);

            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.DefaultChannels, resultWorkspace.DefaultChannels)
                .Set(r => r.Channels, resultWorkspace.Channels)
                .Set(r => r.WorkspaceId, resultWorkspace.WorkspaceId);

            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);
            //// get redis database and call it cache

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
            //make a new list of channels of channel state
            List<ChannelState> listOfDefaultChannelState = new List<ChannelState>();
            foreach (var defaultChannel in listOfDefaultChannels)
            {
                await AddUserToDefaultChannel(user, defaultChannel.ChannelId);

                ChannelState channel = new ChannelState()
                {
                    channelId = defaultChannel.ChannelId,
                    UnreadMessageCount = 0,
                };
                // add it to the list
                listOfDefaultChannelState.Add(channel);
            }
            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            //string jsonString = JsonConvert.SerializeObject(resultWorkspace);
            //await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonString);

            ///////////////Notification Work/////////////////////

            var userstate = await GetUserStateByEmailId(user.EmailId);
            if(userstate !=null)
            {
                WorkspaceState newWorkspace = new WorkspaceState()
                {
                    WorkspaceName = workspaceName,

                };
                foreach (var channel in listOfDefaultChannelState)
                {
                    newWorkspace.ListOfChannelState.Add(channel);
                }
                userstate.ListOfWorkspaceState.Add(newWorkspace);
                string jsonStringUserState = JsonConvert.SerializeObject(userstate);
                await cache.StringSetAsync($"{userstate.EmailId}", jsonStringUserState);
            }
            else
            {
                WorkspaceState newWorkspace = new WorkspaceState()
                {
                    WorkspaceName = workspaceName,

                };
                foreach (var channel in listOfDefaultChannelState)
                {
                    newWorkspace.ListOfChannelState.Add(channel);
                }
                UserState newUserState = new UserState()
                {
                    EmailId =user.EmailId,
                };
                newUserState.ListOfWorkspaceState.Add(newWorkspace);
                await CreateNotificationStateOfUser(newUserState);
                string jsonStringUserState = JsonConvert.SerializeObject(newUserState);
                await cache.StringSetAsync($"{newUserState.EmailId}", jsonStringUserState);

            }


            ////////////////////////////////////////////////////////

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
            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            var stringifiedChannel = cache.StringGetAsync($"{channelId}");
            if (stringifiedChannel.Result.HasValue && channelId!=null)
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