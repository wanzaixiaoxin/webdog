using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using WebDog.Models;

namespace WebDog.Services
{
    public class HttpService
    {
        private readonly HttpClient _client = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        public async Task<ResponseData> SendAsync(
            string method,
            string url,
            List<KeyValuePairModel> parameters,
            List<KeyValuePairModel> headers,
            string bodyType,
            string body,
            CancellationToken ct = default)
        {
            var uri = BuildUri(url, parameters);
            var request = new HttpRequestMessage(new HttpMethod(method), uri);

            foreach (var h in headers.Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key)))
            {
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            bool hasBody = method != "GET" && method != "HEAD" && bodyType != "none" && !string.IsNullOrWhiteSpace(body);
            if (hasBody)
            {
                if (bodyType == "json")
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }
                else if (bodyType == "urlencoded")
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
                }
                else if (bodyType == "formdata")
                {
                    var form = new MultipartFormDataContent();
                    try
                    {
                        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body, _jsonOptions);
                        foreach (var kv in dict)
                        {
                            form.Add(new StringContent(kv.Value.ToString() ?? ""), kv.Key);
                        }
                    }
                    catch
                    {
                        foreach (var pair in body.Split('&'))
                        {
                            var parts = pair.Split(new[] { '=' }, 2);
                            if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
                            {
                                form.Add(new StringContent(parts.Length > 1 ? parts[1] : ""), parts[0]);
                            }
                        }
                    }
                    request.Content = form;
                }
                else
                {
                    request.Content = new StringContent(body, Encoding.UTF8);
                }
            }

            var start = DateTime.UtcNow;
            var response = await _client.SendAsync(request, ct);
            var elapsed = (long)(DateTime.UtcNow - start).TotalMilliseconds;

            var bodyText = await response.Content.ReadAsStringAsync();
            var size = Encoding.UTF8.GetByteCount(bodyText);

            var resHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in response.Headers)
            {
                resHeaders[h.Key] = string.Join(", ", h.Value);
            }
            foreach (var h in response.Content.Headers)
            {
                resHeaders[h.Key] = string.Join(", ", h.Value);
            }

            return new ResponseData
            {
                Status = (int)response.StatusCode,
                StatusText = response.ReasonPhrase,
                Headers = resHeaders,
                Body = bodyText,
                Time = elapsed,
                Size = size,
            };
        }

        private static Uri BuildUri(string url, List<KeyValuePairModel> parameters)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            foreach (var p in parameters.Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Key)))
            {
                query[p.Key] = p.Value;
            }
            builder.Query = query.ToString();
            return builder.Uri;
        }
    }
}
