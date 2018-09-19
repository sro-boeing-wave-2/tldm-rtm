using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class OneToOneChannelInfo
    {
        public OneToOneChannelInfo()
        {
            this.Users = new List<string>();
        }
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("ChannelId")]
        public string ChannelId { get; set; }

        [BsonElement("Users")]
        public List<string> Users { get; set; }

        [BsonElement("WorkspaceId")]
        public string WorkspaceId { get; set; }
    }
}