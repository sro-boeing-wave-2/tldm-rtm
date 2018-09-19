using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class UserWorkspaceView
    {
        public string UserId { get; set; }
        public UserAccountView UserAccount { get; set; }
        public string WorkspaceId { get; set; }
        public WorkspaceView Workspace { get; set; }
    }
}
