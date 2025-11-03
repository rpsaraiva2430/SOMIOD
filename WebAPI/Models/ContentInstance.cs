using System;

namespace WebAPI.Models
{
    public class ContentInstance
    {
        public int Id { get; set; }
        public string ResourceName { get; set; }
        public DateTime CreationDatetime { get; set; }
        public int ContainerId { get; set; }
        public string Content { get; set; }
        public string ContentType { get; set; }
    }
}