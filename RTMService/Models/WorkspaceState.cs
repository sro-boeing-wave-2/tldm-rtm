using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class WorkspaceState
    {
        public WorkspaceState()
        {
            this.ListOfChannelState = new List<ChannelState>();
        }
           
        public string WorkspaceName { get; set; }
        public List<ChannelState> ListOfChannelState { get; set; }
    }
}
