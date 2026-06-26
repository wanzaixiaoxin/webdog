using System.Collections.Generic;

namespace WebDog.Models
{
    public class ResponseData
    {
        public int Status { get; set; }
        public string StatusText { get; set; } = "";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = "";
        public long Time { get; set; }
        public long Size { get; set; }
    }
}
