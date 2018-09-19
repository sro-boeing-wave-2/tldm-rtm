using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class Channel
    {
        public Channel()
        {
            this.Users = new List<User>();
            this.Messages = new List<Message>();
            this.Admin = new User();
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ChannelId { get; set; }

        [BsonElement("channelName")]
        public string ChannelName { get; set; }

        [BsonElement("users")]
        public List<User> Users { get; set; }

        [BsonElement("admin")]
        public User Admin { get; set; }

        [BsonElement("messages")]
        public List<Message> Messages { get; set; }

        [BsonElement("workspaceId")]
        public string WorkspaceId { get; set; }
    }
}
