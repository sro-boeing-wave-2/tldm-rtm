using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using RTMService.Controllers;
using RTMService.Models;
using RTMService.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ChatUnitTesting
{
    public class RTMUnitTest
    {
        public ObjectId Id1 = ObjectId.GenerateNewId();
        public WorkspaceView Postworkspace = new WorkspaceView()
        {
            Id = "5b71298a6a2e663634872c65",
            WorkspaceName = "dummyWorkspace1",
            PictureUrl = "",
            Bots = new List<BotView> { },
            Channels = new List<ChannelView> { },
            UsersState = new List<UserStateView> { },
            UserWorkspaces = new List<UserWorkspaceView> { }
        };
        public Message msg = new Message()
        {
            MessageId = "one",
            MessageBody = "msgbody",
            Timestamp = DateTime.Now,
            IsStarred = true,
            Sender = new User { }
        };
        public User newuser = new User()
        {
            Id = "5b8932523f0c56095c70d82g",
            UserId = "123qwe",
            FirstName = "rahul",
            LastName = "verma",
            EmailId = "rv@gmail.com"
        };
        public Channel channel1 = new Channel()
        {
            ChannelId = "5b8932523f0c56095c70d82d",
            ChannelName = "firstchannel",
            Users = new List<User> { },
            Admin = new User { },
            Messages = new List<Message> { },
            WorkspaceId = null
        };


        public class FakeChatService : IChatService
        {
            public async Task<List<Message>> GetLastNMessagesOfChannel(string channelId, int N)
            {
                List<Message> list = new List<Message>();
                return list;
                //return channel.Messages.Skip(channel.Messages.Count() - N).Take(3).ToList();
            }
            public async Task<Workspace> CreateWorkspace(WorkspaceView workSpace)
            {
                Workspace workspace1 = new Workspace()
                {
                    Id = workSpace.Id,
                    WorkspaceId = "123qwe",
                    WorkspaceName = workSpace.WorkspaceName,
                    Channels = new List<Channel> { },
                    DefaultChannels = new List<Channel> { },
                    Users = new List<User> { }
                };

                return (workspace1);
            }
            public async Task<IEnumerable<Workspace>> GetAllWorkspacesAsync()
            {
                List<Workspace> allworkspace = new List<Workspace>
                                 {
                                new Workspace(){
                                WorkspaceId = "5b71298a6a2e663634872a65",
                                WorkspaceName = "dummyWorkspace",
                                Channels = new List<Channel> { },
                                Users = new List<User> { }
                                }
                            };
                return allworkspace;

            }

            public async Task<Channel> CreateDefaultChannel(Channel channel, string workspaceName)
            {
                var workspace = GetWorkspaceByName(workspaceName).Result;
                var ch = channel;
                workspace.Channels.Add(ch);
                return ch;
            }

            public async Task DeleteWorkspace(string workspaceName)
            {
                Workspace workspace1 = new Workspace()
                {
                    WorkspaceId = "5b71298a6a2e663634872a65",
                    WorkspaceName = "dummyWorkspace",
                    Channels = new List<Channel> { },
                    Users = new List<User> { }
                };
            }

            public async Task<Workspace> GetWorkspaceById(string id)
            {
                List<Workspace> workspaces = new List<Workspace> {
                    new Workspace(){
                        Id = "5b71298a6a2e663634872e65",
                        WorkspaceId="123zxc",
                        WorkspaceName="first",
                        Channels=new List<Channel>{ },
                        DefaultChannels=new List<Channel>{ },
                        Users=new List<User>{
                            new User(){
                                Id="6c71298a6a2e663634872e61",
                                UserId="456",
                                FirstName="stack",
                                LastName="Route",
                                EmailId="sr@gmail.com"
                            }
                        }
                    },
                    new Workspace(){
                        Id = "5b71298a6a2e663634872f65",
                        WorkspaceId="123zxc",
                        WorkspaceName="second",
                        Channels=new List<Channel>{ },
                        DefaultChannels=new List<Channel>{ },
                        Users=new List<User>{ }
                    }
                };
                var workspace1 = workspaces.Find(u => u.Id == id);
                return (workspace1);
            }
            public async Task<Workspace> GetWorkspaceByName(string workspaceName)
            {
                Workspace workspace1 = new Workspace()
                {
                    WorkspaceId = "5b71298a6a2e663634872a65",
                    WorkspaceName = "dummyWorkspace",
                    Channels = new List<Channel> { },
                    Users = new List<User> {
                        new User()
                        {
                            Id="5b8932523f0c56095c70d82g",
                            UserId="123qwe",
                            FirstName="rahul",
                            LastName="verma",
                            EmailId="rv@gmail.com"
                        }
                    }
                };
                return (workspace1);
            }
            public async Task<Channel> CreateChannel(Channel channel, string workspaceId)
            {
                Channel channel1 = new Channel()
                {
                    ChannelId = "5b8932523f0c56095c70d82d",
                    ChannelName = "firstchannel",
                    Users = new List<User> { },
                    Admin = new User { },
                    Messages = new List<Message> { },
                    WorkspaceId = null
                };
                return (channel1);
            }
            public async Task<Channel> GetChannelById(string channelId)
            {
                Channel channel1 = new Channel()
                {
                    ChannelId = "5b8932523f0c56095c70d82d",
                    ChannelName = "firstchannel",
                    Users = new List<User> { },
                    Admin = new User { },
                    Messages = new List<Message> { },
                    WorkspaceId = "5b71298a6a2e663634872e65"
                };
                return (channel1);
            }
            public async Task<List<Channel>> GetAllUserChannelsInWorkSpace(string workSpaceName, string emailid)
            {
                List<Channel> allchannels = new List<Channel> {
                                new Channel()
                                {
                                     ChannelId = "5b8932523f0c56095c70d82d",
                                ChannelName = "firstchannel",
                                Users = new List<User> { },
                                Admin = new User { },
                                Messages = new List<Message> { },
                                WorkspaceId = null
                                },
                                 new Channel()
                                {
                                     ChannelId = "5b8932523f0c56095c70d82d",
                                ChannelName = "firstchannel",
                                Users = new List<User> { },
                                Admin = new User { },
                                Messages = new List<Message> { },
                                WorkspaceId = null
                                }
                            };
                return (allchannels);
            }
            public async Task<List<Channel>> GetAllChannelsInWorkspace(string workSpaceName)
            {
                List<Channel> allchannels = new List<Channel> {
                                new Channel()
                                {
                                     ChannelId = "5b8932523f0c56095c70d82d",
                                ChannelName = "firstchannel",
                                Users = new List<User> { },
                                Admin = new User { },
                                Messages = new List<Message> { },
                                WorkspaceId = null
                                },
                                 new Channel()
                                {
                                     ChannelId = "5b8932523f0c56095c70d82d",
                                ChannelName = "thirdchannel",
                                Users = new List<User> { },
                                Admin = new User { },
                                Messages = new List<Message> { },
                                WorkspaceId = null
                                }
                            };
                return (allchannels);
            }
            public async Task DeleteChannel(string channelId)
            {
                Channel channel1 = new Channel()
                {
                    ChannelId = "5b8932523f0c56095c70d82d",
                    ChannelName = "firstchannel",
                    Users = new List<User> { },
                    Admin = new User { },
                    Messages = new List<Message> { },
                    WorkspaceId = null
                };
            }
            //            /// //////////////Check
            public async Task<User> AddUserToChannel(User user, string channelId)
            {
                User user1 = user;
                var channel = GetChannelById(channelId).Result;
                channel.Users.Add(user1);
                return user1;
            }
            public List<User> GetAllUsersInWorkspace(string workspaceName)
            {
                List<User> allusers = new List<User>() {
                                new User(){
                            Id="5b8932523f0c56095c70d90q",
                            UserId="123qwe",
                            FirstName="rahul",
                            LastName="verma",
                            EmailId="rv@gmail.com"
                                }
                            };
                return allusers;
            }
            public User GetUserById(string emailid)
            {
                User user1 = new User()
                {
                    UserId = null,
                    FirstName = null,
                    LastName = null,
                    EmailId = null
                };
                return user1;
            }

            public async Task<Message> AddMessageToChannel(Message msg, string channelId, string senderMail)
            {
                Channel channel1 = new Channel()
                {
                    ChannelId = "5b8932523f0c56095c70d82d",
                    ChannelName = "firstchannel",
                    Users = new List<User> { },
                    Admin = new User { },
                    Messages = new List<Message> { },
                    WorkspaceId = null
                };
                channel1.Messages.Add(msg);
                return msg;
            }

            public User GetUserByEmail(string emailId, string workspaceName)
            {
                var workspace = GetWorkspaceByName("dummyWorkspace").Result;
                var user = workspace.Users.Find(u => u.EmailId == emailId);
                return user;
            }

            public async Task<User> AddUserToWorkspace(UserAccountView newuser, string workspaceName)
            {
                var workspace = GetWorkspaceByName("dummyWorkspace").Result;
                User user = new User()
                {
                    Id = "5b71298a6a2e663634872a34",
                    UserId = newuser.Id,
                    FirstName = newuser.FirstName,
                    LastName = newuser.LastName,
                    EmailId = newuser.EmailId
                };
                workspace.Users.Add(user);
                return user;
            }

            public async Task DeleteUserFromChannel(string emailId, string channelId)
            {
                Workspace workspace1 = new Workspace()
                {
                    WorkspaceId = "5b71298a6a2e663634872a65",
                    WorkspaceName = "dummyWorkspace",
                    Channels = new List<Channel> { },
                    Users = new List<User> {
                        new User()
                        {
                            Id="5b8932523f0c56095c70d82g",
                            UserId="123qwe",
                            FirstName="rahul",
                            LastName="verma",
                            EmailId="rv@gmail.com"
                        }
                    }
                };
                workspace1.Users.RemoveAll(u => u.EmailId == emailId);
            }


            /// /////////////??????????check

            public async Task<User> AddUserToDefaultChannel(User newUser, string channelId)
            {
                var resultChannel = GetChannelById(channelId);

                return newUser;
            }

            public async Task<Channel> GetChannelForOneToOneChat(string senderMail, string receiverMail, string workspaceName)
            {
                var workspace = GetWorkspaceByName("dummyWorkspace").Result;
                Channel channel1 = new Channel()
                {
                    ChannelId = "5b8932523f0c56095c70d83e",
                    ChannelName = "firstchannel",
                    Users = new List<User> { },
                    Admin = new User { },
                    Messages = new List<Message> { },
                    WorkspaceId = null
                };
                workspace.Channels.Add(channel1);
                return channel1;
            }

            public async Task<string> GetChannelIdForOneToOneChat(string senderMail, string receiverMail, string workspaceId)
            {
                var workspace = GetWorkspaceByName("dummyWorkspace").Result;
                var channels = workspace.Channels.Find(u => u.ChannelId == "5b8932523f0c56095c70d82g");
                return channels.ChannelId;
            }

        }
        //        /// ////////////////////////////////////////////////
        [Fact]
        public void CreateWorkspace()
        {
            FakeChatService fakechat = new FakeChatService();
            ChatController _chatcontroller = new ChatController(fakechat);
            var result = _chatcontroller.CreateWorkspace(Postworkspace);
            var workspaceposted = result as ObjectResult;
            var workspace = workspaceposted.Value as Workspace;
            Assert.Equal("5b71298a6a2e663634872c65", workspace.Id);
        }
        [Fact]
        public void Getallworkspace()
        {
            FakeChatService fakechat = new FakeChatService();
            ChatController _chatcontroller = new ChatController(fakechat);
            var result = _chatcontroller.GetAllWorkspace();
            var workspaceposted = result as ObjectResult;
            var workspace = workspaceposted.Value as List<Workspace>;
            Assert.Single(workspace);
        }
        [Fact]
        public void GetWorkspaceByName()
        {
            FakeChatService fakechat = new FakeChatService();
            ChatController _chatcontroller = new ChatController(fakechat);
            var result = _chatcontroller.GetWorkspaceByName("dummyWorkspace");
            var workspaceposted = result as ObjectResult;
            var workspace = workspaceposted.Value as Workspace;
            Assert.Equal("5b71298a6a2e663634872a65", workspace.WorkspaceId);
        }

        [Fact]
        public void deleteaworkspace()
        {
            FakeChatService fakechat = new FakeChatService();
            ChatController _chatcontroller = new ChatController(fakechat);
            var result = _chatcontroller.DeleteWorkspaceById("5b71298a6a2e663634872e65");
            var notePosted = result as NoContentResult;
            Assert.Equal(204, notePosted.StatusCode);
        }

        [Fact]
        public void deletechannel()
        {
            FakeChatService fakechat = new FakeChatService();
            ChatController _chatcontroller = new ChatController(fakechat);
            var result = _chatcontroller.DeleteChannelById("5b8932523f0c56095c70d82d");
            var notePosted = result as NoContentResult;
            Assert.Equal(204, notePosted.StatusCode);
        }
        [Fact]
        public void createchannelinworkspace()
        {
            var putworkspace = new Workspace()
            {
                WorkspaceId = "5b71298a6a2e663634872a65",
                WorkspaceName = "dummyWorkspace",
                Channels = new List<Channel> { },
                Users = new List<User> { }
            };
            Channel channel1 = new Channel()
            {
                ChannelId = "5b8932523f0c56095c70d82d",
                ChannelName = "firstchannel",
                Users = new List<User> { },
                Admin = new User { },
                Messages = new List<Message> { },
                WorkspaceId = null
            };
            FakeChatService fakechat = new FakeChatService();
            ChatController _chatcontroller = new ChatController(fakechat);
            var result = _chatcontroller.CreateChannelInWorkSpace(channel1, "5b71298a6a2e663634872a65");
            var notePosted = result as ObjectResult;
            var channel = notePosted.Value as Channel;
            Assert.Equal("firstchannel", channel.ChannelName);
        }
        [Fact]
        public void addusertoachannel()
        {
            Channel channel1 = new Channel()
            {
                ChannelId = "5b8932523f0c56095c70d82d",
                ChannelName = "firstchannel",
                Users = new List<User> { },
                Admin = new User { },
                Messages = new List<Message> { },
                WorkspaceId = "5b71298a6a2e663634872e65"
            };
            User user1 = new User()
            {
                Id = "6c71298a6a2e663634872e61",
                UserId = "456",
                FirstName = "stack",
                LastName = "Route",
                EmailId = "sr@gmail.com"
            };
            FakeChatService fakechat = new FakeChatService();
            ChatController _chatcontroller = new ChatController(fakechat);
            var result = _chatcontroller.AddUserToChannel(user1, "5b8932523f0c56095c70d82d");
            var notePosted = result as ObjectResult;
            Assert.Equal(user1, notePosted.Value);
        }
        [Fact]
        public void gettallusersinaworkspace()
        {
            FakeChatService fakechat = new FakeChatService();
            ChatController _chatcontroller = new ChatController(fakechat);
            var result = _chatcontroller.GetAllUsersInWorkspace("dummyWorkspace");
            var workspaceposted = result as ObjectResult;
            var user = workspaceposted.Value as List<User>;
            Assert.Single(user);
        }
        [Fact]
        public void getallchannelsinworkspace()
        {
            FakeChatService fakechat = new FakeChatService();
            ChatController _chatcontroller = new ChatController(fakechat);
            var result = _chatcontroller.GetAllChannelsInWorkSpace("dummyWorkspace");
            var listofchannels = result as ObjectResult;
            var allchannels = listofchannels.Value as List<Channel>;
            Assert.Equal(2, allchannels.Count);
        }

        //        [Fact]
        //        public void getuserbyid()
        //        {
        //            FakeChatService fakechat = new FakeChatService();
        //            ChatController _chatcontroller = new ChatController(fakechat);
        //            var result = _chatcontroller.GetUserById(null);
        //            var workspaceposted = result as OkObjectResult;
        //            var workspace = workspaceposted.Value as Workspace;
        //            Assert.Empty(workspace.Users);
        //        }
    }
}