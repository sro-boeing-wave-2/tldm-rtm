using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class UserStateView
    {
        public string Id { get; set; }
        public string EmailId { get; set; }
        public bool IsJoined { get; set; }
        public string Otp { get; set; }
    }
}
