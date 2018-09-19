using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class UserAccountView
    {
        public UserAccountView()
        {
            this.Workspaces = new List<WorkspaceView>();
            this.UserWorkspaces = new List<UserWorkspaceView>();
        }
        public string Id { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EmailId { get; set; }

        public string Password { get; set; }

        public bool IsVerified { get; set; }

        public List<WorkspaceView> Workspaces { get; set; }
        public List<UserWorkspaceView> UserWorkspaces { get; set; }
    }
}
