using RTMService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Services
{
    public interface IChatService
    {
        // workspace related task
        Task<Workspace> CreateWorkspace(WorkspaceView workspace);
        Task DeleteWorkspace(string workspaceName);
        Task<IEnumerable<Workspace>> GetAllWorkspacesAsync();
        Task<Workspace> GetWorkspaceById(string id);
        Task<Workspace> GetWorkspaceByName(string workspaceName);


        ////// not implemented
        ////Task<Workspace> UpdateWorkspaceByName(string workspaceName);
        //////

        ////// channel related task
        Task<Channel> CreateChannel(Channel channel, string workspaceName);
        Task<Channel> CreateDefaultChannel(Channel channel, string workspaceName);
        Task<Channel> GetChannelById(string channelId);
        Task<List<Channel>> GetAllUserChannelsInWorkSpace(string workSpaceName, string emailId);
        Task<List<Channel>> GetAllChannelsInWorkspace(string workSpaceName);
        Task<string> GetChannelIdForOneToOneChat(string senderMail, string receiverMail, string workspaceId);
        Task<Channel> GetChannelForOneToOneChat(string senderMail, string receiverMail, string workspaceName);
        ////Channel GetGeneralChannelIdByWorkSpaceName(string workSpaceName);
        Task DeleteChannel(string channelId);
        ////Task<Channel> UpdateChannel(Channel channel);
        ////Task<List<Channel>> GetChannelByuserIDandWorkspaceName(int userId, string workspaceName);
        Task<User> AddUserToChannel(User user, string channelId);
        Task<User> AddUserToDefaultChannel(User newUser, string channelId);
        Task DeleteUserFromChannel(string emailId, string channelId);
        ////Task<List<Message>> GetMessagesInChannel(int channelId, string workspaceName);

        ////// user related task
        List<User> GetAllUsersInWorkspace(string workspaceName);
        Task<User> AddUserToWorkspace(UserAccountView user, string workspaceName);  
        User GetUserByEmail(string emailId, string workspaceName);
        ////Task DeleteUserFromWorkspace(string workspaceName, int userId);
        ////Task UpdateUserInWorkspace(User user);

        //////Message related task
        Task<List<Message>> GetLastNMessagesOfChannel(string channelId, int N);
        Task<Message> AddMessageToChannel(Message message, string channelId, string senderMail);
        ////Task DeleteMessageInChannel(string workspaceName, int channelId, int messageId);

    }
}
