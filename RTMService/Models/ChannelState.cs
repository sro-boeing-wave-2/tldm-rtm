using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class ChannelState
    {
        public ChannelState()
        {
            this.ListOfUsers = new List<string>();
        }
        public string channelId { get; set; }
        public int UnreadMessageCount { get; set; }
        public DateTime LastTimestamp { get; set; }
        public List<string> ListOfUsers { get; set; }
    }
}
