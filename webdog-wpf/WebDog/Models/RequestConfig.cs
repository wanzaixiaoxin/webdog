using System.Collections.Generic;

namespace WebDog.Models
{
    public class RequestConfig
    {
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = "";
        public string Protocol { get; set; } = "http";
        public List<KeyValuePairModel> Params { get; set; } = new();
        public List<KeyValuePairModel> Headers { get; set; } = new();
        public string BodyType { get; set; } = "json";
        public string Body { get; set; } = "";
    }
}
