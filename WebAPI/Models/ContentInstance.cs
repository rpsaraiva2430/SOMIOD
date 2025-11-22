using Newtonsoft.Json;
using System;

namespace WebAPI.Models
{
    public class ContentInstance
    {
        [JsonProperty("resource-name")]
        public string ResourceName { get; set; }

        [JsonProperty("creation-datetime")]
        public DateTime CreationDatetime { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("content-type")]
        public string ContentType { get; set; }

        [JsonIgnore]
        public string ParentContainerName { get; set; }

        [JsonIgnore]
        public string ParentAppName { get; set; }
    }
}