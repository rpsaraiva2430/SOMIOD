using System;

namespace WebAPI.Models
{
    public class Application
    {
        public int Id { get; set; }
        public string ResourceName { get; set; }
        public DateTime CreationDatetime { get; set; }
    }
}