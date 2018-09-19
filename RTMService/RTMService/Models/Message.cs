using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class Message
    {
        public Message()
        {
            this.Sender = new User();
        }

        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string MessageId { get; set; }

        [BsonElement("messageBody")]
        public string MessageBody { get; set; }

        [BsonElement("timestamp")]
        public DateTime Timestamp { get; set; }

        [BsonElement("isStarred")]
        public bool IsStarred { get; set; }

        [BsonElement("channelId")]
        public string ChannelId { get; set; }

        [BsonElement("sender")]
        public User Sender { get; set; }
    }
}
