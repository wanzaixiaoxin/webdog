using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
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
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>Allow skipping SSL certificate validation (for self-signed certs).</summary>
        public bool SkipCertificateValidation { get; set; }

        /// <summary>Request timeout in seconds; &lt;=0 means infinite.</summary>
        public int TimeoutSeconds { get; set; } = 100;

        public async Task<ResponseData> SendAsync(
            string method,
            string url,
            List<KeyValuePairModel> parameters,
            List<KeyValuePairModel> headers,
            string bodyType,
            string body,
            AuthConfig auth,
            List<FormParamModel> formParams = null,
            string contentType = null,
            CancellationToken ct = default)
        {
            var uri = BuildUri(url, parameters);
            var timing = new ResponseTiming();
            var overall = Stopwatch.StartNew();

            using var handler = BuildHandler(timing);
            using var client = new HttpClient(handler);
            if (TimeoutSeconds > 0)
                client.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

            var request = new HttpRequestMessage(new HttpMethod(method), uri);

            foreach (var h in headers.Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key)))
            {
                // Skip Authorization if auth is configured (we'll set it ourselves)
                if (auth != null && auth.Type != "none" &&
                    h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    continue;
                request.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            // Apply auth
            if (auth != null)
            {
                if (auth.Type == "bearer" && !string.IsNullOrWhiteSpace(auth.BearerToken))
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {auth.BearerToken}");
                else if (auth.Type == "basic" && !string.IsNullOrWhiteSpace(auth.BasicUsername))
                {
                    var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{auth.BasicUsername}:{auth.BasicPassword}"));
                    request.Headers.TryAddWithoutValidation("Authorization", $"Basic {credentials}");
                }
                else if (auth.Type == "apikey" && !string.IsNullOrWhiteSpace(auth.ApiKeyName) && !string.IsNullOrWhiteSpace(auth.ApiKeyValue))
                {
                    if (auth.ApiKeyLocation == "header")
                        request.Headers.TryAddWithoutValidation(auth.ApiKeyName, auth.ApiKeyValue);
                    else
                        uri = AppendQueryParam(uri, auth.ApiKeyName, auth.ApiKeyValue);
                }
                else if (auth.Type == "oauth2" && !string.IsNullOrWhiteSpace(auth.OAuthAccessToken))
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {auth.OAuthAccessToken}");
            }

            bool hasBody = method != "GET" && method != "HEAD" && bodyType != "none";
            if (hasBody)
            {
                if (bodyType == "json")
                {
                    if (!string.IsNullOrWhiteSpace(body))
                        request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }
                else if (bodyType == "urlencoded")
                {
                    var pairs = (formParams ?? new List<FormParamModel>())
                        .Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Key)).ToList();
                    if (pairs.Count > 0)
                    {
                        var form = new FormUrlEncodedContent(
                            pairs.Select(p => new KeyValuePair<string, string>(p.Key, p.Value ?? "")));
                        request.Content = form;
                    }
                }
                else if (bodyType == "formdata")
                {
                    var pairs = (formParams ?? new List<FormParamModel>())
                        .Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Key)).ToList();
                    if (pairs.Count > 0)
                    {
                        var form = new MultipartFormDataContent();
                        foreach (var p in pairs)
                        {
                            if (p.ParamType == "file" && File.Exists(p.Value))
                            {
                                var fileBytes = await File.ReadAllBytesAsync(p.Value, ct);
                                var fileContent = new ByteArrayContent(fileBytes);
                                var fileName = !string.IsNullOrWhiteSpace(p.FileName) ? p.FileName : Path.GetFileName(p.Value);
                                form.Add(fileContent, p.Key, fileName);
                            }
                            else
                            {
                                form.Add(new StringContent(p.Value ?? ""), p.Key);
                            }
                        }
                        request.Content = form;
                    }
                }
                else if (bodyType == "binary" && !string.IsNullOrWhiteSpace(body) && File.Exists(body))
                {
                    var fileBytes = await File.ReadAllBytesAsync(body, ct);
                    request.Content = new ByteArrayContent(fileBytes);
                }
                else if (!string.IsNullOrWhiteSpace(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, contentType ?? "text/plain");
                }
            }

            // Use ResponseHeadersRead so we can split TTFB from transfer time.
            var sendSw = Stopwatch.StartNew();
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            timing.TtfbMs = sendSw.ElapsedMilliseconds;

            // Read raw bytes, decompress if needed (custom ConnectCallback may bypass AutomaticDecompression)
            var rawBytes = await response.Content.ReadAsByteArrayAsync(ct);
            var bodyBytes = DecompressIfNeeded(rawBytes, response);
            timing.TransferMs = sendSw.ElapsedMilliseconds - timing.TtfbMs;
            var size = rawBytes.Length;

            var bodyText = DecodeResponseBody(bodyBytes, response.Content.Headers.ContentType?.CharSet);

            var resHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var h in response.Headers)
                resHeaders[h.Key] = string.Join(", ", h.Value);
            foreach (var h in response.Content.Headers)
                resHeaders[h.Key] = string.Join(", ", h.Value);

            // Parse cookies
            var cookies = new List<CookieItem>();
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders))
            {
                foreach (var cookieHeader in setCookieHeaders)
                {
                    var parts = cookieHeader.Split(';');
                    if (parts.Length > 0)
                    {
                        var nameValue = parts[0].Trim();
                        var eqIdx = nameValue.IndexOf('=');
                        var cookie = new CookieItem();
                        if (eqIdx > 0)
                        {
                            cookie.Name = nameValue[..eqIdx].Trim();
                            cookie.Value = nameValue[(eqIdx + 1)..].Trim();
                        }
                        foreach (var part in parts.Skip(1))
                        {
                            var p = part.Trim();
                            if (p.StartsWith("Domain=", StringComparison.OrdinalIgnoreCase))
                                cookie.Domain = p[7..].Trim();
                            else if (p.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                                cookie.Path = p[5..].Trim();
                            else if (p.StartsWith("Expires=", StringComparison.OrdinalIgnoreCase))
                                cookie.Expires = p[8..].Trim();
                            else if (p.StartsWith("Max-Age=", StringComparison.OrdinalIgnoreCase))
                                cookie.MaxAge = int.TryParse(p[8..].Trim(), out var ma) ? ma : null;
                            else if (p.Equals("HttpOnly", StringComparison.OrdinalIgnoreCase))
                                cookie.HttpOnly = true;
                            else if (p.Equals("Secure", StringComparison.OrdinalIgnoreCase))
                                cookie.Secure = true;
                            else if (p.StartsWith("SameSite=", StringComparison.OrdinalIgnoreCase))
                                cookie.SameSite = p[9..].Trim();
                        }
                        cookies.Add(cookie);
                    }
                }
            }

            overall.Stop();
            timing.TotalMs = overall.ElapsedMilliseconds;
            if (timing.TtfbMs == 0) timing.TtfbMs = timing.TotalMs;

            return new ResponseData
            {
                Status = (int)response.StatusCode,
                StatusText = response.ReasonPhrase,
                Headers = resHeaders,
                Body = bodyText,
                RawBody = bodyText,
                Time = timing.TotalMs,
                Size = size,
                Cookies = cookies,
                Timing = timing,
            };
        }

        private SocketsHttpHandler BuildHandler(ResponseTiming timing)
        {
            var handler = new SocketsHttpHandler
            {
                // Custom connect callback lets us measure DNS / TCP / TLS phases.
                ConnectCallback = async (ctx, ct) => await ConnectAsync(ctx, timing, ct),
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.None,
            };

            if (SkipCertificateValidation)
            {
                handler.SslOptions = new SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                };
            }

            return handler;
        }

        private async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, ResponseTiming timing, CancellationToken ct)
        {
            var sw = Stopwatch.StartNew();
            IPAddress[] addresses = null;
            try
            {
                addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, ct);
            }
            catch { }
            timing.DnsMs = sw.ElapsedMilliseconds;

            Socket socket;
            if (addresses != null && addresses.Length > 0)
            {
                sw.Restart();
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                try
                {
                    await socket.ConnectAsync(addresses, context.DnsEndPoint.Port, ct);
                }
                catch
                {
                    socket.Dispose();
                    socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                    await socket.ConnectAsync(context.DnsEndPoint.Host, context.DnsEndPoint.Port, ct);
                }
            }
            else
            {
                sw.Restart();
                socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
                await socket.ConnectAsync(context.DnsEndPoint.Host, context.DnsEndPoint.Port, ct);
            }
            timing.ConnectMs = sw.ElapsedMilliseconds;

            var stream = new NetworkStream(socket, ownsSocket: true);

            // Detect TLS from the original request scheme (more reliable than port).
            var scheme = context.InitialRequestMessage?.RequestUri?.Scheme ?? "";
            var isTls = scheme.Equals("https", StringComparison.OrdinalIgnoreCase);
            if (isTls || context.DnsEndPoint.Port == 443)
            {
                sw.Restart();
                var sslStream = new SslStream(stream, leaveInnerStreamOpen: false);
                var sslOptions = new SslClientAuthenticationOptions
                {
                    TargetHost = context.DnsEndPoint.Host,
                };
                if (SkipCertificateValidation)
                {
                    sslOptions.RemoteCertificateValidationCallback = (_, _, _, _) => true;
                }
                await sslStream.AuthenticateAsClientAsync(sslOptions, ct);
                timing.TlsMs = sw.ElapsedMilliseconds;
                return sslStream;
            }

            return stream;
        }

        private static Uri BuildUri(string url, List<KeyValuePairModel> parameters)
        {
            var normalized = NormalizeUrl(url);
            var builder = new UriBuilder(normalized);
            var query = HttpUtility.ParseQueryString(builder.Query);
            foreach (var p in parameters.Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Key)))
                query[p.Key] = p.Value;
            builder.Query = query.ToString();
            return builder.Uri;
        }

        private static string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            var trimmed = url.Trim();
            if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return trimmed;
            return "http://" + trimmed;
        }

        private static Uri AppendQueryParam(Uri uri, string key, string value)
        {
            var builder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query[key] = value;
            builder.Query = query.ToString();
            return builder.Uri;
        }

        private static byte[] DecompressIfNeeded(byte[] bytes, HttpResponseMessage response)
        {
            var encoding = response.Content.Headers.ContentEncoding;
            if (encoding == null || encoding.Count == 0) return bytes;

            foreach (var enc in encoding)
            {
                try
                {
                    if (enc.Equals("gzip", StringComparison.OrdinalIgnoreCase) ||
                        enc.Equals("x-gzip", StringComparison.OrdinalIgnoreCase))
                    {
                        using var input = new MemoryStream(bytes);
                        using var gzip = new GZipStream(input, CompressionMode.Decompress);
                        using var output = new MemoryStream();
                        gzip.CopyTo(output);
                        bytes = output.ToArray();
                    }
                    else if (enc.Equals("deflate", StringComparison.OrdinalIgnoreCase))
                    {
                        using var input = new MemoryStream(bytes);
                        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
                        using var output = new MemoryStream();
                        deflate.CopyTo(output);
                        bytes = output.ToArray();
                    }
                    else if (enc.Equals("br", StringComparison.OrdinalIgnoreCase))
                    {
                        using var input = new MemoryStream(bytes);
                        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
                        using var output = new MemoryStream();
                        brotli.CopyTo(output);
                        bytes = output.ToArray();
                    }
                }
                catch { /* keep original bytes if decompression fails */ }
            }
            return bytes;
        }

        private static string DecodeResponseBody(byte[] bytes, string charset)
        {
            if (bytes == null || bytes.Length == 0) return "";

            // Try charset from Content-Type header first
            if (!string.IsNullOrWhiteSpace(charset))
            {
                try
                {
                    var enc = Encoding.GetEncoding(charset.Trim().Trim('"'));
                    return enc.GetString(bytes);
                }
                catch { }
            }

            // Try UTF-8 with BOM detection
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes);

            // Try UTF-8
            try
            {
                var utf8 = Encoding.UTF8.GetString(bytes);
                if (utf8.Length > 0 && utf8.IndexOf('\uFFFD') < 0)
                    return utf8;
            }
            catch { }

            // Try common encodings for Chinese/Asian content
            foreach (var name in new[] { "gb18030", "gbk", "gb2312", "big5", "shift_jis", "euc-kr", "windows-1252" })
            {
                try
                {
                    var enc = Encoding.GetEncoding(name);
                    return enc.GetString(bytes);
                }
                catch { }
            }

            // Last resort: Latin-1 preserves all byte values
            return Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        }
    }
}
