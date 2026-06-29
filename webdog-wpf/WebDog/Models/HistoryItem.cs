namespace WebDog.Models
{
    public class HistoryItem
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N")[..8];
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = "";
        public int? Status { get; set; }
        public long? Time { get; set; }
        public System.DateTime Timestamp { get; set; } = System.DateTime.UtcNow;
        public RequestConfig Request { get; set; } = new();
        public ResponseData Response { get; set; }
    }
}
