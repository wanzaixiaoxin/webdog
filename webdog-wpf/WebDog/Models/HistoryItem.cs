namespace WebDog.Models
{
    public class HistoryItem
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N")[..8];
        public string Method { get; set; } = "GET";
        public string Url { get; set; } = "";
        public int? Status { get; set; }
        public long? Time { get; set; }
        public string Timestamp { get; set; } = System.DateTime.UtcNow.ToString("O");
        public RequestConfig Request { get; set; } = new();
        public ResponseData Response { get; set; }
    }
}
