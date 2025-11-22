using Newtonsoft.Json;
using System;

namespace WebAPI.Models
{
    public class Container
    {
        [JsonProperty("resource-name")]
        public string ResourceName { get; set; }

        [JsonProperty("creation-datetime")]
        public string CreationDatetime { get; set; }

        // Ignoramos no JSON de resposta porque já está no URL
        [JsonIgnore]
        public string ParentAppName { get; set; }
    }
}