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

            // adding default bot for interspace communication 
            UserAccountView newUser = new UserAccountView
            {
                EmailId = "entre.bot@gmail.com",
                FirstName = "Bot",
                LastName = "User",
                Id= "60681125-e117-4bb2-9287-eb840c4cg672"
            };
            await AddUserToWorkspace(newUser, workSpace.WorkspaceName);

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

            // convert channel object to string
            string jsonStringChannel = JsonConvert.SerializeObject(channel);

            // update key value pair of channel in cache
            await cache.StringSetAsync($"{channel.ChannelId}", jsonStringChannel);
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

            //convert channel object in to string
            string jsonString = JsonConvert.SerializeObject(channel);

            // set key value pair of channel in cache
            await cache.StringSetAsync($"{channel.ChannelId}", jsonString);

            ///////////////Notification Work/////////////////////

            //get user state for first user
            var stringifiedUserState = cache.StringGetAsync($"{channel.Users[0].EmailId}");

            // convert it to object
            var userstate = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);

            //create a new channel state
            ChannelState channelState = new ChannelState()
            {
                channelId = channel.ChannelId,
                UnreadMessageCount = 0
            };

            // add it to its current workspace state of user
            userstate.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).ListOfChannelState.Add(channelState);

            // convert the user state to string
            string jsonStringUserState = JsonConvert.SerializeObject(userstate);

            // update user state in cache
            await cache.StringSetAsync($"{userstate.EmailId}", jsonStringUserState);
            
            //updating in mongo
            var filterUserState = new FilterDefinitionBuilder<UserState>().Where(r => r.EmailId == channel.Users[0].EmailId);

            var updateUserState = Builders<UserState>.Update
                .Set(r => r.ListOfWorkspaceState, userstate.ListOfWorkspaceState);

            await _dbNotificationUserState.UpdateOneAsync(filterUserState, updateUserState);
            /////
            //get user state for second user
            var stringifiedUserState1 = cache.StringGetAsync($"{channel.Users[1].EmailId}");

            // convert it to object
            var userstate1 = JsonConvert.DeserializeObject<UserState>(stringifiedUserState1.Result);

            //create a new channel state
            ChannelState channelState1 = new ChannelState()
            {
                channelId = channel.ChannelId,
                UnreadMessageCount = 0
            };

            // add it to its current workspace state of user
            userstate1.ListOfWorkspaceState.Find(w => w.WorkspaceName == workspaceName).ListOfChannelState.Add(channelState1);

            // convert the user state to string
            string jsonStringUserState1 = JsonConvert.SerializeObject(userstate1);

            // update user state in cache
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
        // get a channel by channel ID
        public async Task<Channel> GetChannelById(string channelId)
        {
            // return null if channel id is null
            if(channelId== null)
            {
                return null;
            }

            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            // get channel from cache by id
            var stringifiedChannel = cache.StringGetAsync($"{channelId}");

            // convert it into object
            var channelObject = JsonConvert.DeserializeObject<Channel>(stringifiedChannel.Result);

            // return channel only if correct channel is obtained
            if(channelObject.ChannelId == channelId)
            {
                return channelObject;
            }

            // else search in mongo collection
            var Channel = _dbChannel.Find(w => w.ChannelId == channelId).FirstOrDefaultAsync();

            // convert it into string
            string jsonString = JsonConvert.SerializeObject(Channel.Result);

            // store in cache after cache miss
            await cache.StringSetAsync($"{channelId}", jsonString);

            return await Channel;
        }
        // add user to channel
        public async Task<User> AddUserToChannel(User newUser, string channelId)
        {

            // get channel by id
            var resultChannel = GetChannelById(channelId).Result;

            // add user to channel
            resultChannel.Users.Add(newUser);

            // for safety 
            resultChannel.ChannelId = channelId;

            // apply filter 
            var filter = new FilterDefinitionBuilder<Channel>().Where(r => r.ChannelId == resultChannel.ChannelId);

            // update definition
            var update = Builders<Channel>.Update
                .Set(r => r.ChannelId, resultChannel.ChannelId)
                .Set(r => r.Users, resultChannel.Users);

            // update document in collection
            await _dbChannel.UpdateOneAsync(filter, update);

            // search workspace by id to update workpace
            var resultWorkspace = GetWorkspaceById(resultChannel.WorkspaceId).Result;

            // add new user to channel in workspace 
            resultWorkspace.Channels.First(i => i.ChannelId == channelId).Users.Add(newUser);

            // filter for workpsace
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == resultChannel.WorkspaceId);

            // update definition
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.Channels, resultWorkspace.Channels)
                .Set(r => r.WorkspaceId, resultChannel.WorkspaceId);

            // update workpsace document in collection 
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);

            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            //convert workpsace object to string to update in cache
            string jsonStringWorkspace = JsonConvert.SerializeObject(resultWorkspace);

            // update in cache 
            await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonStringWorkspace);

            // storing channel in cache
            // convert channel object to string
            string jsonStringChannel = JsonConvert.SerializeObject(resultChannel);

            // update in cache
            await cache.StringSetAsync($"{resultChannel.ChannelId}", jsonStringChannel);
            ///////////
            ///////////////Notification Work/////////////////////

            //get user state from cache 
            var stringifiedUserState = cache.StringGetAsync($"{newUser.EmailId}");

            //convert it to user state object
            var userstate = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);

            // create new channel state 
            ChannelState channel = new ChannelState()
            {
                channelId = channelId,
                UnreadMessageCount = 0
            };

            // add it to current workpsace state of user
            userstate.ListOfWorkspaceState.Find(w => w.WorkspaceName == resultWorkspace.WorkspaceName).ListOfChannelState.Add(channel);

            // convert it back to string
            string jsonStringUserState = JsonConvert.SerializeObject(userstate);

            // update it in cache 
            await cache.StringSetAsync($"{userstate.EmailId}", jsonStringUserState);

            //updating in mongo
            var filterUserState = new FilterDefinitionBuilder<UserState>().Where(r => r.EmailId == newUser.EmailId);

            var updateUserState = Builders<UserState>.Update
                .Set(r => r.ListOfWorkspaceState, userstate.ListOfWorkspaceState);

            await _dbNotificationUserState.UpdateOneAsync(filterUserState, updateUserState);
            ////////////////////////////////////////////////////////

            return newUser;

        }
        // adding user to default channel
        public async Task<User> AddUserToDefaultChannel(User newUser, string channelId)
        {

            // get channel by channel id 
            var resultChannel = GetChannelById(channelId).Result;

            //add user to channel
            resultChannel.Users.Add(newUser);

            // for safety
            resultChannel.ChannelId = channelId;

            // filter for channel
            var filter = new FilterDefinitionBuilder<Channel>().Where(r => r.ChannelId == resultChannel.ChannelId);

            // update definition
            var update = Builders<Channel>.Update
                .Set(r => r.ChannelId, resultChannel.ChannelId)
                .Set(r => r.Users, resultChannel.Users);

            // update in channel collection
            await _dbChannel.UpdateOneAsync(filter, update);

            // get workpsace by id for updation
            var resultWorkspace = GetWorkspaceById(resultChannel.WorkspaceId).Result;

            //add user to default channel in workpace 
            resultWorkspace.DefaultChannels.First(i => i.ChannelId == channelId).Users.Add(newUser);

            // filter for workspace 
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == resultChannel.WorkspaceId);

            //update definition
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.DefaultChannels, resultWorkspace.DefaultChannels)
                .Set(r => r.WorkspaceId, resultChannel.WorkspaceId);

            // update in workpsace collection
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);

            //// get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            // convert workspace object to string for updation in cache 
            string jsonStringWorkspace = JsonConvert.SerializeObject(resultWorkspace);

            // update cache 
            await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonStringWorkspace);

            // storing channel in cache
            // convert channel object to string for updation in cache 
            string jsonStringChannel = JsonConvert.SerializeObject(resultChannel);

            //update cache 
            await cache.StringSetAsync($"{resultChannel.ChannelId}", jsonStringChannel);
            
            ///////////
            return newUser;

        }
        // get all user channels for bot to communicate in them
        public async Task<List<string>> GetAllUserChannels(string emailId)
        {
            //find list of channels from mongo 
            var listOfChannels = await _dbChannel.Find(p => (p.ChannelName != "") && (p.Users.Any(u => u.EmailId == emailId))).ToListAsync();

            // create a new list of channel id strings
            List<string> listOfChannelIds = new List<string>();

            // populate it 
            foreach (var channel in listOfChannels)
            {
                listOfChannelIds.Add(channel.ChannelId);
            }
            return listOfChannelIds;
        }
        // get workspace name by channel id
        public async Task<string> GetWorkspaceNameByChannelId(string channelId)
        {
            // get channel by id
            var channel = GetChannelById(channelId).Result;

            //search workspace by workspace id in channel in mongo 
            var workspace = await _dbWorkSpace.Find(w => w.WorkspaceId == channel.WorkspaceId).FirstOrDefaultAsync();

            return workspace.WorkspaceName;
        }
        // get older messages of a channel 
        // total count of messages - N + 10 messages
        public async Task<List<Message>> GetLastNMessagesOfChannel(string channelId, int N)
        {
            // load messages from mongo collection
            var listOfMessages = await  _dbMessage.Find(m => m.ChannelId==channelId).ToListAsync();

            // order messages by time stamp
            var sortedMessages = listOfMessages.OrderBy(m => m.Timestamp).ToList();

            // return list of 10 messages only when n is less then total message count
            if (N < sortedMessages.Count())
            {
                var list = sortedMessages.Skip(sortedMessages.Count() - N).Take(10).ToList();
                return list;
            }
            // else return empty list
            else
            {
                List<Message> emptyList = new List<Message>() { };
                return emptyList;
            }
            
        }
        // add a message to channel
        public async Task<Message> AddMessageToChannel(Message message, string channelId, string senderMail)
        {
            // get channel by id in which message is sent
            var resultChannel = GetChannelById(channelId).Result;

            // get workspace of that channel by id
            var resultWorkspace = GetWorkspaceById(resultChannel.WorkspaceId).Result;

            // no need to do this !!!!!!!!!!!!!!!!!!!
            // no need to do this !!!!!!!!!!!!!!!!!!!
            Message newMessage = message;

            // insert into message collection
            await _dbMessage.InsertOneAsync(newMessage);
           
            // add message to channel
            resultChannel.Messages.Add(newMessage);

            //take latest 50 messages from that channel to store in cache
            var lastmessages = resultChannel.Messages.Skip(Math.Max(0, resultChannel.Messages.Count() - 50)).ToList();

            // create the same channel for cache with latest 50 messages
            Channel cacheChannel = new Channel()
            {
                Messages = lastmessages,
                ChannelId = resultChannel.ChannelId,
                Users = resultChannel.Users,
                WorkspaceId = resultChannel.WorkspaceId,
                ChannelName = resultChannel.ChannelName,
                Admin = resultChannel.Admin
            };

            // for safety
            resultChannel.ChannelId = channelId;

            // updation in mongo
            var filter = new FilterDefinitionBuilder<Channel>().Where(r => r.ChannelId == resultChannel.ChannelId);

            var update = Builders<Channel>.Update
                .Set(r => r.ChannelId, resultChannel.ChannelId)
                .Set(r => r.Messages, resultChannel.Messages);

            await _dbChannel.UpdateOneAsync(filter, update);

            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            // storing channel in cache
            string jsonStringChannel = JsonConvert.SerializeObject(cacheChannel);

            //update channel in cache 
            await cache.StringSetAsync($"{resultChannel.ChannelId}", jsonStringChannel);

            //////////////////Notification Work/////////////////////////////////////

            // update every user state for all users in channel 
            // notify each user that a message has been sent
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
        // delete a channel by id 
        public async Task DeleteChannel(string channelId)
        {
            //USER STATE NOT UPDATED AFTER DELETING CHANNEL !!!!!!!!!!!!!!!!!!!!!!

            // get the channel to be deleted
            var channelresult = GetChannelById(channelId).Result;

            // delete from mongo collection
            await _dbChannel.DeleteOneAsync(w => w.ChannelId == channelId);

            // get the workpace in which the channel exists
            var workspace = GetWorkspaceById(channelresult.WorkspaceId).Result;

            // find the channel to be deleted
            var channelToDelete = workspace.Channels.Find(c => c.ChannelId == channelId);

            // find the default channel to be deleted
            var defaultChannelToDelete = workspace.DefaultChannels.Find(c => c.ChannelId == channelId);

            // remove channel
            workspace.Channels.Remove(channelToDelete);

            // remove default channel
            workspace.DefaultChannels.Remove(defaultChannelToDelete);

            // filter for updation workpsace in collection
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == channelresult.WorkspaceId);

            // update defintion
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.DefaultChannels, workspace.DefaultChannels)
                .Set(r => r.Channels, workspace.Channels)
                .Set(r => r.WorkspaceId, workspace.WorkspaceId);
            
            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            // convert the workspace object to string for cache updating
            string jsonString = JsonConvert.SerializeObject(workspace);

            // update cache 
            await cache.StringSetAsync($"{workspace.WorkspaceName}", jsonString);

            // update mongo collection
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);

           

        }
        // remove user from channel 
        public async Task DeleteUserFromChannel(string emailId, string channelId)
        {
            // get the channel by id
            var channel = GetChannelById(channelId).Result;

            // find user to be removed
            var resultUser = channel.Users.Find(u => u.EmailId == emailId);

            // remove user from channel
            channel.Users.Remove(resultUser);

            // for safety
            channel.ChannelId = channelId;

            // filter for updation of channel in mongo collection
            var filter = new FilterDefinitionBuilder<Channel>().Where(r => r.ChannelId == channel.ChannelId);

            // update definition for channel 
            var update = Builders<Channel>.Update
                .Set(r => r.ChannelId, channel.ChannelId)
                .Set(r => r.Users, channel.Users);

            // update in mongo collection
            await _dbChannel.UpdateOneAsync(filter, update);

            // find workpsace to update it 
            var resultWorkspace = GetWorkspaceById(channel.WorkspaceId).Result;

            // for safety
            resultWorkspace.WorkspaceId = channel.WorkspaceId;

            // first check if channel is default and delete user from it 
            try
            {
                var userToDelete = resultWorkspace.DefaultChannels.Find(c => c.ChannelId == channelId).Users.Find(u => u.EmailId == emailId);
                resultWorkspace.DefaultChannels.Find(c => c.ChannelId == channelId).Users.Remove(userToDelete);
            }
            // else remove from normal channel 
            catch
            {
                var userToDelete = resultWorkspace.Channels.Find(c => c.ChannelId == channelId).Users.Find(u => u.EmailId == emailId);
                resultWorkspace.Channels.Find(c => c.ChannelId == channelId).Users.Remove(userToDelete);
            }
            
            // filter for workspace updation 
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == resultWorkspace.WorkspaceId);

            // update defintion for workpace 
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.DefaultChannels, resultWorkspace.DefaultChannels)
                .Set(r => r.Channels, resultWorkspace.Channels)
                .Set(r => r.WorkspaceId, resultWorkspace.WorkspaceId);

            // update in mongo collection
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);

            //// get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            
            // convert workspace object to string for updating cache 
            string jsonString = JsonConvert.SerializeObject(resultWorkspace);

            //update workspace in cache 
            await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonString);

            // convert channel object to string for updating cache
            string jsonStringChannel = JsonConvert.SerializeObject(channel);

            // update channel in cache 
            await cache.StringSetAsync($"{channel.ChannelId}", jsonStringChannel);
            ///////////
            ///////////////Notification Work///////////////////////
            // get user state from the cache 
            var stringifiedUserState = cache.StringGetAsync($"{emailId}");

            // convert the result string to user state object 
            var userstate = JsonConvert.DeserializeObject<UserState>(stringifiedUserState.Result);

            string workspaceName = GetWorkspaceNameByChannelId(channelId).Result;
           
            // find channel state of user from channel id 
            var channelState =  userstate.ListOfWorkspaceState.
                Find(w => w.WorkspaceName == workspaceName).
                ListOfChannelState.
                Find(c => c.channelId == channelId);

            // delete channel state for the user state
            userstate.ListOfWorkspaceState.
                Find(w => w.WorkspaceName == workspaceName).
                ListOfChannelState.Remove(channelState);

            //convert the object back to string
            string jsonStringUserState = JsonConvert.SerializeObject(userstate);

            // update the user state inside cache
            await cache.StringSetAsync($"{emailId}", jsonStringUserState);
            ///////////////////////////////////////////////////////
        }
        // get all users of a workspace 
        public List<User> GetAllUsersInWorkspace(string workspaceName)
        {
            // get workspace by name 
            var resultWorkspace = GetWorkspaceByName(workspaceName).Result;

            return resultWorkspace.Users;
        }
        // add user to workspace 
        public async Task<User> AddUserToWorkspace(UserAccountView newuser, string workspaceName)
        {
            // make a user object from user account view 
            User user = new User
            {
                UserId = newuser.Id,
                EmailId = newuser.EmailId,
                FirstName = newuser.FirstName,
                LastName = newuser.LastName
            };

            //insert a new document in mongo collection of user 
            await _dbUser.InsertOneAsync(user);

            // get workspace by name 
            var resultWorkspace = GetWorkspaceByName(workspaceName).Result;

            // add user to workspace 
            resultWorkspace.Users.Add(user);

            // filter to update workpsace in monog collection 
            var filterWorkspace = new FilterDefinitionBuilder<Workspace>().Where(r => r.WorkspaceId == resultWorkspace.WorkspaceId);

            //update definition for workspace 
            var updateWorkspace = Builders<Workspace>.Update
                .Set(r => r.Users, resultWorkspace.Users)
                .Set(r => r.WorkspaceId, resultWorkspace.WorkspaceId);

            // update in mongo collection
            await _dbWorkSpace.UpdateOneAsync(filterWorkspace, updateWorkspace);

            // get list of default channel names given by user
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
                // add it to the list of default channels
                listOfDefaultChannelState.Add(channel);
            }

            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            // CHECK THESE COMMENTED LINES ONCE AGAIN !!!!!!!!!!!!!!!!
            //string jsonString = JsonConvert.SerializeObject(resultWorkspace);
            //await cache.StringSetAsync($"{resultWorkspace.WorkspaceName}", jsonString);

            ///////////////Notification Work/////////////////////

            //get user state by email of new user
            var userstate = await GetUserStateByEmailId(user.EmailId);

            // add workspace state to user state if user state already exists
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

                // update in cache 
                string jsonStringUserState = JsonConvert.SerializeObject(userstate);
                await cache.StringSetAsync($"{userstate.EmailId}", jsonStringUserState);
            }
            //else create a new user state 
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

                // create a mongo document
                await CreateNotificationStateOfUser(newUserState);

                // add it in cache 
                string jsonStringUserState = JsonConvert.SerializeObject(newUserState);
                await cache.StringSetAsync($"{newUserState.EmailId}", jsonStringUserState);

            }
            ////////////////////////////////////////////////////////
            return user;
        }
        // get channel ID for one to one channel 
        public async Task<string> GetChannelIdForOneToOneChat(string senderMail, string receiverMail, string workspaceId)
        {
            // search in mongo collection
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
        // get channel for one to one chat
        public async Task<Channel> GetChannelForOneToOneChat(string senderMail, string receiverMail, string workspaceName)
        {
            // get workspace by name 
            var workspace = GetWorkspaceByName(workspaceName).Result;

            // get channel id of if channel exist in that channel 
            var channelId = GetChannelIdForOneToOneChat(senderMail, receiverMail, workspace.WorkspaceId).Result;
            
            // get redis database and call it cache
            var cache = RedisConnectorHelper.Connection.GetDatabase();

            // search in cache if channel exists
            var stringifiedChannel = cache.StringGetAsync($"{channelId}");
            
            // and return that channel 
            if (stringifiedChannel.Result.HasValue && channelId!=null)
            {
                var ChannelObject = JsonConvert.DeserializeObject<Channel>(stringifiedChannel.Result);

                return ChannelObject;
            }

            // this also searches in cache 
            //NO NEED TO DO THIS AGAIN  !!!!!!!!!!!!!!!!!!!!
            // ONLY SEARCH IN MONGO NOW !!!!
            var oneToOneChannel = GetChannelById(channelId);

            // if successful return result
            if (oneToOneChannel.Result !=null)
            {
                return oneToOneChannel.Result;
            }

            //else create a new channel
            else
            {
                // get sender object by mail id 
                var sender = GetUserByEmail(senderMail, workspaceName);

                // get receiver object by mail id
                var receiver = GetUserByEmail(receiverMail, workspaceName);

                // create new channel 
                Channel newOneToOneChannel = new Channel
                {
                    ChannelName = "",

                };

                //add both user and sender in channel
                newOneToOneChannel.Users.Add(sender);
                newOneToOneChannel.Users.Add(receiver);

                // create a new document in mongo
                oneToOneChannel = CreateOneToOneChannel(newOneToOneChannel, workspaceName);

                //create new entry for one to one table 
                OneToOneChannelInfo entryOfPersonalChannel = new OneToOneChannelInfo
                {
                    ChannelId = oneToOneChannel.Result.ChannelId,
                    WorkspaceId = workspace.WorkspaceId
                };
                entryOfPersonalChannel.Users.Add(senderMail);
                entryOfPersonalChannel.Users.Add(receiverMail);

                // insert new document
                await _dbOneToOne.InsertOneAsync(entryOfPersonalChannel);
                return oneToOneChannel.Result;
            }
        }

        // get all channels in workpsace user is part of 
        public async Task<List<Channel>> GetAllUserChannelsInWorkSpace(string workSpaceName, string emailId)
        {
            // get workspace by name
            var workspace = GetWorkspaceByName(workSpaceName).Result;

            return await _dbChannel.Find(p => (p.WorkspaceId == workspace.WorkspaceId) && (p.ChannelName != "") && (p.Users.Any(u => u.EmailId == emailId))).ToListAsync();
        }
        // get all channels in a workspace 
        public async Task<List<Channel>> GetAllChannelsInWorkspace(string workSpaceName)
        {
            // seacrh workspace by name 
            var workspace = GetWorkspaceByName(workSpaceName).Result;

            // Check this logic AGAIN !!!!!!!!!!!!!
            var result = Query<Channel>.Where(p => (p.WorkspaceId == workspace.WorkspaceId) && (p.ChannelName != ""));
            return await _dbChannel.Find(p => (p.WorkspaceId == workspace.WorkspaceId) && (p.ChannelName != "")).ToListAsync();

        }
        // search user by email in a workspace 
        public User GetUserByEmail(string emailId, string workspaceName)
        {
            // search workspace by name 
            var resultWorkspace = GetWorkspaceByName(workspaceName).Result;

            // find the user in workspace 
            var user = resultWorkspace.Users.Find(u => u.EmailId == emailId);

            return user;
        }


    }
}