using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class Workspace
    {
        public Workspace()
        {
            this.Channels = new List<Channel>();
            this.DefaultChannels = new List<Channel>();
            this.Users = new List<User>();
        }

        // Todo: To be removed later
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        
        public string WorkspaceId { get; set; }

        [BsonElement("workspaceName")]
        public string WorkspaceName { get; set; }

        [BsonElement("channels")]
        public List<Channel> Channels { get; set; }

        [BsonElement("defaultChannels")]
        public List<Channel> DefaultChannels { get; set; }

        [BsonElement("users")]
        public List<User> Users { get; set; }
    }
}
