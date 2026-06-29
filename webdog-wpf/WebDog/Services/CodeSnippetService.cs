using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WebDog.Models;

namespace WebDog.Services
{
    public static class CodeSnippetService
    {
        public static string GenerateCSharp(string method, string url, List<KeyValuePairModel> headers,
            string bodyType, string body, AuthConfig auth)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Net.Http;");
            sb.AppendLine("using System.Net.Http.Headers;");
            sb.AppendLine("using System.Text;");
            sb.AppendLine();
            sb.AppendLine("var client = new HttpClient();");
            sb.AppendLine($"var request = new HttpRequestMessage(HttpMethod.{Capitalize(method)}, \"{EscapeCs(url)}\");");

            // Auth
            if (auth?.Type == "bearer" && !string.IsNullOrWhiteSpace(auth.BearerToken))
                sb.AppendLine($"request.Headers.Authorization = new AuthenticationHeaderValue(\"Bearer\", \"{EscapeCs(auth.BearerToken)}\");");
            else if (auth?.Type == "basic" && !string.IsNullOrWhiteSpace(auth.BasicUsername))
                sb.AppendLine($"request.Headers.Authorization = new AuthenticationHeaderValue(\"Basic\", Convert.ToBase64String(Encoding.UTF8.GetBytes(\"{EscapeCs(auth.BasicUsername)}:{EscapeCs(auth.BasicPassword)}\")));");

            // Headers
            foreach (var h in headers.Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key)))
            {
                if (h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && auth?.Type != "none") continue;
                sb.AppendLine($"request.Headers.TryAddWithoutValidation(\"{EscapeCs(h.Key)}\", \"{EscapeCs(h.Value)}\");");
            }

            // Body
            if (method != "GET" && method != "HEAD" && bodyType != "none" && !string.IsNullOrWhiteSpace(body))
            {
                var contentType = bodyType switch
                {
                    "json" => "application/json",
                    "urlencoded" => "application/x-www-form-urlencoded",
                    "text" => "text/plain",
                    _ => "application/octet-stream"
                };
                sb.AppendLine($"request.Content = new StringContent(\"{EscapeCs(body)}\", Encoding.UTF8, \"{contentType}\");");
            }

            sb.AppendLine();
            sb.AppendLine("var response = await client.SendAsync(request);");
            sb.AppendLine("var responseBody = await response.Content.ReadAsStringAsync();");
            return sb.ToString();
        }

        public static string GeneratePython(string method, string url, List<KeyValuePairModel> headers,
            string bodyType, string body, AuthConfig auth)
        {
            var sb = new StringBuilder();
            sb.AppendLine("import requests");
            sb.AppendLine();

            var headerDict = new Dictionary<string, string>();
            foreach (var h in headers.Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key)))
            {
                if (h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && auth?.Type != "none") continue;
                headerDict[h.Key] = h.Value;
            }

            sb.Append($"response = requests.{method.ToLowerInvariant()}(");
            sb.AppendLine($"    \"{EscapePy(url)}\"");

            // Auth
            if (auth?.Type == "bearer" && !string.IsNullOrWhiteSpace(auth.BearerToken))
                sb.AppendLine($"    headers={{\"Authorization\": \"Bearer {EscapePy(auth.BearerToken)}\"}},");
            else if (auth?.Type == "basic")
                sb.AppendLine($"    auth=(\"{EscapePy(auth?.BasicUsername)}\", \"{EscapePy(auth?.BasicPassword)}\"),");

            // Headers (only if no bearer auth was already added)
            if ((auth?.Type != "bearer") && headerDict.Count > 0)
            {
                sb.AppendLine("    headers={");
                foreach (var kv in headerDict)
                    sb.AppendLine($"        \"{EscapePy(kv.Key)}\": \"{EscapePy(kv.Value)}\",");
                sb.AppendLine("    },");
            }

            // Body
            if (method != "GET" && method != "HEAD" && bodyType != "none" && !string.IsNullOrWhiteSpace(body))
            {
                if (bodyType == "json")
                    sb.AppendLine($"    json=json.loads(r'''{body}'''),");
                else
                    sb.AppendLine($"    data=r'''{body}''',");
            }

            sb.AppendLine(")");
            sb.AppendLine("print(response.status_code)");
            sb.AppendLine("print(response.text)");
            return sb.ToString();
        }

        public static string GenerateJavaScriptFetch(string method, string url, List<KeyValuePairModel> headers,
            string bodyType, string body, AuthConfig auth)
        {
            var sb = new StringBuilder();
            sb.AppendLine("const response = await fetch(");
            sb.AppendLine($"    \"{EscapeJs(url)}\",");
            sb.AppendLine("    {");
            sb.AppendLine($"        method: \"{method.ToUpperInvariant()}\",");

            // Headers
            sb.AppendLine("        headers: {");
            if (auth?.Type == "bearer" && !string.IsNullOrWhiteSpace(auth.BearerToken))
                sb.AppendLine($"            \"Authorization\": \"Bearer {EscapeJs(auth.BearerToken)}\",");
            else if (auth?.Type == "basic" && !string.IsNullOrWhiteSpace(auth.BasicUsername))
                sb.AppendLine($"            \"Authorization\": \"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.BasicUsername}:{auth.BasicPassword}"))}\",");

            foreach (var h in headers.Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key)))
            {
                if (h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) && auth?.Type != "none") continue;
                sb.AppendLine($"            \"{EscapeJs(h.Key)}\": \"{EscapeJs(h.Value)}\",");
            }
            sb.AppendLine("        },");

            // Body
            if (method != "GET" && method != "HEAD" && bodyType != "none" && !string.IsNullOrWhiteSpace(body))
            {
                if (bodyType == "json")
                {
                    // JSON is a subset of JS, embed directly as an object literal.
                    sb.AppendLine($"        body: JSON.stringify({body}),");
                }
                else
                {
                    sb.AppendLine($"        body: \"{EscapeJs(body)}\",");
                }
            }

            sb.AppendLine("    }");
            sb.AppendLine(");");
            sb.AppendLine("const data = await response.text();");
            sb.AppendLine("console.log(response.status, data);");
            return sb.ToString();
        }

        private static string Capitalize(string m) => char.ToUpper(m[0]) + m[1..].ToLowerInvariant();
        private static string EscapeCs(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
        private static string EscapePy(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
        private static string EscapeJs(string s) => s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("'", "\\'") ?? "";
    }
}
