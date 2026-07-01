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
        public string BodyLanguage { get; set; } = "JSON";
        public string BodyFileName { get; set; } = "";
        public long BodyFileSize { get; set; }
        public List<FormParamModel> FormParams { get; set; } = new();
        public AuthConfig Auth { get; set; } = new();
    }

    public class AuthConfig
    {
        public string Type { get; set; } = "none"; // none, bearer, basic, apikey, oauth2
        public string BearerToken { get; set; } = "";
        public string BasicUsername { get; set; } = "";
        public string BasicPassword { get; set; } = "";

        // API Key
        public string ApiKeyName { get; set; } = "";
        public string ApiKeyValue { get; set; } = "";
        public string ApiKeyLocation { get; set; } = "header"; // header | query

        // OAuth 2.0
        public string OAuthGrantType { get; set; } = "client_credentials";
        public string OAuthTokenUrl { get; set; } = "";
        public string OAuthClientId { get; set; } = "";
        public string OAuthClientSecret { get; set; } = "";
        public string OAuthScope { get; set; } = "";
        public string OAuthAccessToken { get; set; } = "";
    }
}
