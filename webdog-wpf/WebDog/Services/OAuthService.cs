using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebDog.Models;

namespace WebDog.Services
{
    public class OAuthService
    {
        private readonly HttpClient _client = new();

        public async Task<OAuthTokenResult> GetTokenAsync(AuthConfig auth, CancellationToken ct = default)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", auth.OAuthGrantType),
                new KeyValuePair<string, string>("client_id", auth.OAuthClientId),
                new KeyValuePair<string, string>("client_secret", auth.OAuthClientSecret),
            });

            if (!string.IsNullOrWhiteSpace(auth.OAuthScope))
            {
                content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", auth.OAuthGrantType),
                    new KeyValuePair<string, string>("client_id", auth.OAuthClientId),
                    new KeyValuePair<string, string>("client_secret", auth.OAuthClientSecret),
                    new KeyValuePair<string, string>("scope", auth.OAuthScope),
                });
            }

            var response = await _client.PostAsync(auth.OAuthTokenUrl, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OAuth token request failed ({response.StatusCode}): {body}");

            var json = JsonDocument.Parse(body);
            var accessToken = json.RootElement.TryGetProperty("access_token", out var at) ? at.GetString() : null;
            var expiresIn = json.RootElement.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 0;

            return new OAuthTokenResult
            {
                AccessToken = accessToken ?? "",
                ExpiresIn = expiresIn,
                TokenType = json.RootElement.TryGetProperty("token_type", out var tt) ? tt.GetString() : "bearer",
            };
        }
    }

    public class OAuthTokenResult
    {
        public string AccessToken { get; set; } = "";
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "bearer";
    }
}
