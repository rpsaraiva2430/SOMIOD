using System;

namespace WebAPI.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string ResourceName { get; set; }
        public int Evt { get; set; } //1-creation, 2-delçetion
        public string Endpoint { get; set; }
        public DateTime CreationDatetime { get; set; }
        public int ContainerId { get; set; }
    }
}