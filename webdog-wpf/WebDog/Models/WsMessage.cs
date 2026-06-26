using System;

namespace WebDog.Models
{
    public class WsMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Type { get; set; } = "info"; // sent, received, info, error
        public string Data { get; set; } = "";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public long? Size { get; set; }
    }
}
