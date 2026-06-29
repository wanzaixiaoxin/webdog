using System.Collections.Generic;

namespace WebDog.Models
{
    public class ResponseData
    {
        public int Status { get; set; }
        public string StatusText { get; set; } = "";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = "";
        public string RawBody { get; set; } = ""; // unformatted raw body
        public long Time { get; set; }
        public long Size { get; set; }
        public List<CookieItem> Cookies { get; set; } = new();
        public ResponseTiming Timing { get; set; } = new();
    }

    public class ResponseTiming
    {
        public long DnsMs { get; set; }
        public long ConnectMs { get; set; }
        public long TlsMs { get; set; }
        public long TtfbMs { get; set; }      // time to first byte (request sent -> headers received)
        public long TransferMs { get; set; }  // body download
        public long TotalMs { get; set; }
    }

    public class CookieItem
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public string Domain { get; set; } = "";
        public string Path { get; set; } = "";
        public string Expires { get; set; } = "";
        public int? MaxAge { get; set; }
        public bool HttpOnly { get; set; }
        public bool Secure { get; set; }
        public string SameSite { get; set; } = "";
    }

    public class HeaderDisplayItem
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
