using Newtonsoft.Json;
using System;

namespace WebAPI.Models
{
    public class Subscription
    {
        [JsonProperty("resource-name")]
        public string ResourceName { get; set; }

        [JsonProperty("creation-datetime")]
        public DateTime CreationDatetime { get; set; }

        [JsonProperty("evt")]
        public int Evt { get; set; } // 1-creation, 2-deletion

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }

        [JsonIgnore]
        public string ParentContainerName { get; set; }

        [JsonIgnore]
        public string ParentAppName { get; set; }
    }
}