using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RTMService.Hubs;
using RTMService.Models;
using RTMService.Services;

namespace RTMService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        // using interface for chat service
        private IChatService iservice;

        //constructor for controller
        public ChatController(IChatService c)
        {
            //dependency injection inside constructor
            iservice = c;
        }

        // creating a workspace by posting a workspace view object 
        //Post Request
        [HttpPost]
        [Route("workspaces")]
        public IActionResult CreateWorkspace([FromBody] WorkspaceView workspace)
        {
            // before creating new workspace check if it already exists
            var searchedWorkspace = iservice.GetWorkspaceById(workspace.Id).Result;
            if (searchedWorkspace != null)
            {
                //if workspace already exists return error message
                return NotFound("Workspace already exists");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            Workspace newWorkspace = iservice.CreateWorkspace(workspace).Result;
            return new ObjectResult(newWorkspace);
        }

        // getting all the workspaces
        [HttpGet]
        [Route("workspaces")]
        public IActionResult GetAllWorkspace()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // calling chat service to get all workspaces
            var ListofWorkspace = iservice.GetAllWorkspacesAsync().Result;
            return new ObjectResult(ListofWorkspace);
        }

        // getting the workspace by workspace name
        [HttpGet]
        [Route("workspaces/{workspaceName}")]
        public IActionResult GetWorkspaceByName(string workspaceName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var Workspace = iservice.GetWorkspaceByName(workspaceName).Result;
            if (Workspace == null)
            {
                // if not found return error message
                return NotFound("No Workspcae Found");
            }
            return new ObjectResult(Workspace);
        }

        // // deleting a workspace by workspace id
        [HttpDelete]
        [Route("workspaces/{id}")]
        public IActionResult DeleteWorkspaceById(string id)
        {
            // check if workspace exists or not
            var workspaceToDelete = iservice.GetWorkspaceById(id).Result;
            if (workspaceToDelete == null)
            {
                // if workspace does not exist return error message
                return NotFound("Workspace trying to delete not found");
            }
            // call service to delete 
            iservice.DeleteWorkspace(workspaceToDelete.WorkspaceId);
            return NoContent();
        }

        // // deleting a channel by channel id
        [HttpDelete]
        [Route("workspaces/channels/{channelId}")]
        public IActionResult DeleteChannelById(string channelId)
        {
            //get the channel to be delete
            var ChannelToDelete = iservice.GetChannelById(channelId).Result;
            if (ChannelToDelete == null)
            {
                // if channel does not exist return error message
                return NotFound("Channel trying to delete not found");
            }
            // call service to delete
            iservice.DeleteChannel(ChannelToDelete.ChannelId);
            return NoContent();
        }

        // // deleting a user from channel by channel id and email id of user
        [HttpDelete]
        [Route("workspaces/channels/{channelId:length(24)}/{emailId}")]
        public IActionResult DeleteuserFromChannel(string channelId, string emailId)
        {
            // get the channel from which user needs to be deleted
            var ChannelToDelete = iservice.GetChannelById(channelId).Result;
            if (ChannelToDelete == null)
            {
                // return error message if channel does not exist
                return NotFound("Channel not found");
            }
            //call service to delete user from channel
            iservice.DeleteUserFromChannel(emailId, channelId);
            return NoContent();
        }

        // //// deleting a user from workspace
        //[HttpDelete]
        //[Route("workspaces/channels/{channelId:length(24)}/{emailId}")]
        //public IActionResult DeleteuserFromWorkspace(User user, string workspaceName)
        //{
        //    var searchedWorkspace = iservice.GetWorkspaceByName(workspaceName);
        //    if (searchedWorkspace == null)
        //    {
        //        return NotFound("Workspace not found");
        //    }

        //    iservice.DeleteUserFromChannel(emailId, channelId);
        //    return NoContent();
        //}


        // creating a channel inside workspasce by giving channel object
        [HttpPut]
        [Route("workspaces/{workspaceName}")]
        public IActionResult CreateChannelInWorkSpace([FromBody] Channel channel, string workspaceName)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call service to create channel 
            var newChannel = iservice.CreateChannel(channel, workspaceName).Result;
            return new ObjectResult(newChannel);
        }

        // // Adding a user to a channel by channel id
        [HttpPut]
        [Route("workspaces/channel/{channelId}")]
        public IActionResult AddUserToChannel([FromBody] User user, string ChannelId)
        {
            //first search for the channel in which user needs to be added
            var searchedChannel = iservice.GetChannelById(ChannelId).Result;
            // check if user is already inside channel
            var userAlreadyAddedInChannel = searchedChannel.Users.Find(u => u.UserId == user.UserId);
            if (userAlreadyAddedInChannel != null)
            {
                //return error message if user already inside channel
                return NotFound("User already added in Channel");
            }
            // search for the workspace in the channel exists
            var searchedWorkspace = iservice.GetWorkspaceById(searchedChannel.WorkspaceId).Result;
            // search if user is inside workspace or not
            var searchedUser = searchedWorkspace.Users.Find(u => u.UserId == user.UserId);
            if (searchedUser == null)
            {
                // return error if user is not onboarding workspace
                return NotFound("User is not added in Workspace. First complete onboarding process");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call service to add user to channel
            iservice.AddUserToChannel(user, ChannelId);
            return new ObjectResult(user);
        }

        // // getting all the users inside workspace
        [HttpGet]
        [Route("workspaces/user/{workspaceName}")]
        public IActionResult GetAllUsersInWorkspace(string workspaceName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call servie to get all users inside workspace
            var ListofUsers = iservice.GetAllUsersInWorkspace(workspaceName);
            return new ObjectResult(ListofUsers);
        }

        // // getting channel by ID
        [HttpGet]
        [Route("workspaces/channelId/{channelId}")]
        public IActionResult GetChannelById(string channelId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call service to get channel
            var channel = iservice.GetChannelById(channelId).Result;
            return new ObjectResult(channel);
        }

        // // getting last n messages of channel
        [HttpGet]
        [Route("workspaces/channel/messages/{channelId}/{N}")]
        public IActionResult GetLastNMessagesOfChannel(string channelId,int N)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call servie to get the messages
            var ListOfMessages = iservice.GetLastNMessagesOfChannel(channelId,N).Result;
            return new ObjectResult(ListOfMessages);
        }

        // Adding a user to a workspace
        [HttpPut]
        [Route("workspaces/user/{workspaceName}")]
        public IActionResult AddUserToWorkspace([FromBody] UserAccountView user, string workspaceName) // frombody workspace object or string name
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                // get the workspace in which user needs to be added
                var searchedWorkSpace = iservice.GetWorkspaceByName(workspaceName).Result;
                // check if user already added in workspace
                var userAlreadyInWorkspace = searchedWorkSpace.Users.Find(u => u.UserId == user.Id);
                if (userAlreadyInWorkspace != null)
                {
                    // return error message if already added 
                    return NotFound("User already added in Workspace");
                }
            }
            catch { }
            // call service to add user to workspace 
            var userAdded = iservice.AddUserToWorkspace(user, workspaceName).Result;
            return new ObjectResult(userAdded);
        }

        //// Adding a message to channel by channel id and email id of sender
        [HttpPut]
        [Route("workspaces/message/{channelId}/{senderMail}")]
        public IActionResult AddMessageToChannel([FromBody] Message message, string channelId, string senderMail) // frombody workspace object or string name
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call service to add message in channel
            var newMessage = iservice.AddMessageToChannel(message, channelId, senderMail).Result;
            return new ObjectResult(newMessage);
        }

        // getting all channels in a workspace by workspace name
        [HttpGet]
        [Route("workspaces/channels/{workspaceName}")]
        public IActionResult GetAllChannelsInWorkSpace(string workspaceName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call service to get all channels inside workspace
            List<Channel> channels = iservice.GetAllChannelsInWorkspace(workspaceName).Result;
            return new ObjectResult(channels);
        }

        // // getting all channels a user is part of in a workspace by workspace name and emailid
        [HttpGet]
        [Route("workspaces/{workspaceName}/{emailId}")]
        public IActionResult GetAllChannelsOfUserInWorkSpace(string workspaceName, string emailId)
        {
            // check if both the fields are given for input
            if (workspaceName == null || emailId == null)
            {
                return NotFound("Please enter both workspaceName and email id");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call service to get all user channels in workspace 
            List<Channel> channels = iservice.GetAllUserChannelsInWorkSpace(workspaceName, emailId).Result;
            return new ObjectResult(channels);
        }

        // // getting all channels of a user by emailid
        [HttpGet]
        [Route("workspaces/userchannels/{emailId}")]
        public IActionResult GetAllChannelsOfUser(string emailId)
        {
            if (emailId == null)
            {
                return NotFound("Please enter email id");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call service to get all user channels
            List<string> channelIds = iservice.GetAllUserChannels(emailId).Result;
            return new ObjectResult(channelIds);
        }

        // // get one to one channel of two users 
        [HttpGet]
        [Route("workspaces/onetoone/{workspaceName}/{senderMail}/{receiverMail}")]
        public IActionResult GetOneToOneChannel(string senderMail, string receiverMail, string workspaceName)
        {
            if (workspaceName == null || senderMail == null || receiverMail == null)
            {
                return NotFound("Please enter all fields");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call service to get on to one channel object
            Channel channel = iservice.GetChannelForOneToOneChat(senderMail, receiverMail, workspaceName).Result;
            return new ObjectResult(channel);
        }

        // // get user by email id and workspace name
        [HttpGet]
        [Route("workspaces/getuser/{workspaceName}/{userEmail}")]
        public IActionResult GetUserByEmail(string userEmail, string workspaceName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // call service to get user
            var user = iservice.GetUserByEmail(userEmail, workspaceName);
            return new ObjectResult(user);
        }


    }
}