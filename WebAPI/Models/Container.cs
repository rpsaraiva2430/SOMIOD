using System;

namespace WebAPI.Models
{
    public class Container
    {
        public int Id { get; set; }
        public string ResourceName { get; set; }
        public DateTime CreationDatetime { get; set; }
        public int ApplicationId { get; set; }
    }
}