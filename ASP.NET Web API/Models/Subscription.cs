namespace ASP.NET_Web_API.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string ResourceName { get; set; }
        public int Evt { get; set; } //1-creation, 2-delçetion
        public string Endpoint { get; set; }
        public string CreationDatetime { get; set; }
        public int ContainerId { get; set; }
    }
}