using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class WorkspaceView
    {
        public WorkspaceView()
        {
            this.UsersState = new List<UserStateView>();
            this.Channels = new List<ChannelView>();
            this.UserWorkspaces = new List<UserWorkspaceView>();
            this.Bots = new List<BotView>();
        }
        public string Id { get; set; }
        public string WorkspaceName { get; set; } 
        public string PictureUrl { get; set; }
        public List<BotView> Bots { get; set; }
        public List<ChannelView> Channels { get; set; }
        public List<UserStateView> UsersState { get; set; }
        public List<UserWorkspaceView> UserWorkspaces { get; set; }
    }
}
