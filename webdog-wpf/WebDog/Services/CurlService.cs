using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebDog.Models;

namespace WebDog.Services
{
    public static class CurlService
    {
        public static string Generate(string method, string url, List<KeyValuePairModel> headers,
            string bodyType, string body, AuthConfig auth)
        {
            var sb = new StringBuilder();
            sb.Append("curl -X ");
            sb.Append(method);

            sb.Append(" \\\n  '");
            sb.Append(url);
            sb.Append("'");

            // Auth
            if (auth != null)
            {
                if (auth.Type == "bearer" && !string.IsNullOrWhiteSpace(auth.BearerToken))
                {
                    sb.Append(" \\\n  -H 'Authorization: Bearer ");
                    sb.Append(auth.BearerToken);
                    sb.Append("'");
                }
                else if (auth.Type == "basic" && !string.IsNullOrWhiteSpace(auth.BasicUsername))
                {
                    sb.Append(" \\\n  -u '");
                    sb.Append(auth.BasicUsername);
                    sb.Append(":");
                    sb.Append(auth.BasicPassword);
                    sb.Append("'");
                }
            }

            // Headers
            foreach (var h in headers.Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key)))
            {
                sb.Append(" \\\n  -H '");
                sb.Append(h.Key);
                sb.Append(": ");
                sb.Append(h.Value);
                sb.Append("'");
            }

            // Body
            if (method != "GET" && method != "HEAD" && bodyType != "none" && !string.IsNullOrWhiteSpace(body))
            {
                sb.Append(" \\\n  -d '");
                sb.Append(body.Replace("'", "\\'"));
                sb.Append("'");
            }

            return sb.ToString();
        }
    }
}
