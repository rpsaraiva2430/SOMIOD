using Newtonsoft.Json;
using System;

namespace WebAPI.Models
{
    public class Application
    {
        [JsonProperty("resource-name")]
        public string ResourceName { get; set; }

        [JsonProperty("creation-datetime")]
        public string CreationDatetime { get; set; }
    }
}