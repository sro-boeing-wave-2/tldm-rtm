using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RTMService.Models
{
    public class UserState
    {
        public UserState()
        {
            this.ListOfWorkspaceState = new List<WorkspaceState>();
        }
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string EmailId { get; set; }
        public List<WorkspaceState> ListOfWorkspaceState { get; set; }
    }
}
