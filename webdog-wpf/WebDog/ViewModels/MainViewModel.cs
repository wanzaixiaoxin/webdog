using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WebDog.Models;
using WebDog.Services;
using WebDog.Views;

namespace WebDog.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly HttpService _httpService = new();
        private readonly WsService _wsService = new();
        private readonly StorageService _storage = new();
        private readonly OAuthService _oauthService = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private CancellationTokenSource _httpCts;

        // ---- Protocol ----
        private string _protocol = "http";
        public string Protocol
        {
            get => _protocol;
            set
            {
                if (SetProperty(ref _protocol, value))
                {
                    OnPropertyChanged(nameof(IsHttpMode));
                    OnPropertyChanged(nameof(IsWsMode));
                }
            }
        }
        public bool IsHttpMode => _protocol == "http";
        public bool IsWsMode => _protocol == "ws";

        // ---- Request ----
        public string[] Methods { get; } = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };

        private string _method = "GET";
        public string Method { get => _method; set => SetProperty(ref _method, value); }

        private string _url = "";
        public string Url { get => _url; set => SetProperty(ref _url, value); }

        public ObservableCollection<KeyValuePairModel> Params { get; } = new();
        public ObservableCollection<KeyValuePairModel> Headers { get; } = new();
        public ObservableCollection<FormParamModel> FormParams { get; } = new();

        private string _bodyType = "json";
        public string BodyType { get => _bodyType; set => SetProperty(ref _bodyType, value); }

        private string _body = "";
        public string Body { get => _body; set => SetProperty(ref _body, value); }

        // ---- Body language selector for raw editor ----
        public string[] BodyLanguages { get; } = { "JSON", "XML", "HTML", "Text", "JavaScript" };

        private string _bodyLanguage = "JSON";
        public string BodyLanguage { get => _bodyLanguage; set => SetProperty(ref _bodyLanguage, value); }

        private string _bodyFileName = "";
        public string BodyFileName { get => _bodyFileName; set => SetProperty(ref _bodyFileName, value); }

        private long _bodyFileSize;
        public long BodyFileSize { get => _bodyFileSize; set => SetProperty(ref _bodyFileSize, value); }

        private bool _bodyWordWrap = true;
        public bool BodyWordWrap { get => _bodyWordWrap; set => SetProperty(ref _bodyWordWrap, value); }

        // ---- Auth ----
        private string _authType = "none";
        public string AuthType { get => _authType; set => SetProperty(ref _authType, value); }

        private string _bearerToken = "";
        public string BearerToken { get => _bearerToken; set => SetProperty(ref _bearerToken, value); }

        private string _basicUsername = "";
        public string BasicUsername { get => _basicUsername; set => SetProperty(ref _basicUsername, value); }

        private string _basicPassword = "";
        public string BasicPassword { get => _basicPassword; set => SetProperty(ref _basicPassword, value); }

        // ---- API Key Auth ----
        private string _apiKeyName = "";
        public string ApiKeyName { get => _apiKeyName; set => SetProperty(ref _apiKeyName, value); }

        private string _apiKeyValue = "";
        public string ApiKeyValue { get => _apiKeyValue; set => SetProperty(ref _apiKeyValue, value); }

        private string _apiKeyLocation = "header";
        public string ApiKeyLocation { get => _apiKeyLocation; set => SetProperty(ref _apiKeyLocation, value); }

        // ---- OAuth 2.0 ----
        private string _oauthTokenUrl = "";
        public string OAuthTokenUrl { get => _oauthTokenUrl; set => SetProperty(ref _oauthTokenUrl, value); }

        private string _oauthClientId = "";
        public string OAuthClientId { get => _oauthClientId; set => SetProperty(ref _oauthClientId, value); }

        private string _oauthClientSecret = "";
        public string OAuthClientSecret { get => _oauthClientSecret; set => SetProperty(ref _oauthClientSecret, value); }

        private string _oauthScope = "";
        public string OAuthScope { get => _oauthScope; set => SetProperty(ref _oauthScope, value); }

        private string _oauthAccessToken = "";
        public string OAuthAccessToken { get => _oauthAccessToken; set => SetProperty(ref _oauthAccessToken, value); }

        private string _oauthTokenStatus = "";
        public string OAuthTokenStatus { get => _oauthTokenStatus; set => SetProperty(ref _oauthTokenStatus, value); }

        // ---- Response ----
        private ResponseData _response;
        public ResponseData Response
        {
            get => _response;
            set
            {
                if (SetProperty(ref _response, value))
                {
                    UpdateResponseSummary();
                }
            }
        }

        // Derived response summary fields
        public string ResponseContentType { get; private set; }
        public string ResponseMethod { get; private set; }
        public string ResponseUrl { get; private set; }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _errorMessage;
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        private string _responseView = "pretty";
        public string ResponseView
        {
            get => _responseView;
            set
            {
                if (SetProperty(ref _responseView, value))
                {
                    UpdateDisplayedResponse();
                }
            }
        }

        public bool IsImageResponse =>
            ResponseContentType != null && (
                ResponseContentType.StartsWith("image/") ||
                ResponseContentType == "application/octet-stream"
            );

        public bool IsHtmlResponse =>
            ResponseContentType != null && ResponseContentType.Contains("html");

        private string _displayedResponseBody = "";
        public string DisplayedResponseBody { get => _displayedResponseBody; set => SetProperty(ref _displayedResponseBody, value); }

        // JSON tree (built when switching to Tree view on a JSON response)
        private JsonNodeModel _jsonTree;
        public JsonNodeModel JsonTree { get => _jsonTree; set => SetProperty(ref _jsonTree, value); }

        public bool IsJsonResponse => SyntaxHighlighter.Detect(ResponseContentType) == SyntaxHighlighter.Language.Json;

        private string _responseSearch = "";
        public string ResponseSearch
        {
            get => _responseSearch;
            set
            {
                if (SetProperty(ref _responseSearch, value))
                {
                    UpdateSearchMatches();
                }
            }
        }

        public int SearchMatchCount { get; private set; }
        public string SearchStatus => SearchMatchCount > 0 ? $"{SearchMatchCount} match(es)" : "";

        public ObservableCollection<HeaderDisplayItem> ResponseHeaders { get; } = new();
        public ObservableCollection<CookieItem> ResponseCookies { get; } = new();

        // ---- Tabs ----
        private int _requestTabIndex;
        public int RequestTabIndex { get => _requestTabIndex; set => SetProperty(ref _requestTabIndex, value); }

        private int _responseTabIndex;
        public int ResponseTabIndex { get => _responseTabIndex; set => SetProperty(ref _responseTabIndex, value); }

        // ---- WebSocket ----
        private string _wsUrl = "";
        public string WsUrl { get => _wsUrl; set => SetProperty(ref _wsUrl, value); }

        private string _wsScheme = "ws";
        public string WsScheme { get => _wsScheme; set => SetProperty(ref _wsScheme, value); }

        private bool _wsConnected;
        public bool WsConnected { get => _wsConnected; set => SetProperty(ref _wsConnected, value); }

        public ObservableCollection<WsMessage> WsMessages { get; } = new();

        private string _wsInput = "";
        public string WsInput { get => _wsInput; set => SetProperty(ref _wsInput, value); }

        // ---- WS connection parameters ----
        private string _wsSubprotocols = "";
        public string WsSubprotocols { get => _wsSubprotocols; set => SetProperty(ref _wsSubprotocols, value); }

        public ObservableCollection<KeyValuePairModel> WsHeaders { get; } = new();

        // Message type for sending: Text or Binary
        private string _wsMessageType = "text"; // text | binary
        public string WsMessageType { get => _wsMessageType; set => SetProperty(ref _wsMessageType, value); }

        // ---- History ----
        public ObservableCollection<HistoryItem> History { get; } = new();
        private bool _showHistory;
        public bool ShowHistory { get => _showHistory; set => SetProperty(ref _showHistory, value); }

        private string _historySearch = "";
        public string HistorySearch { get => _historySearch; set { SetProperty(ref _historySearch, value); OnPropertyChanged(nameof(FilteredHistory)); } }

        public IEnumerable<HistoryItem> FilteredHistory =>
            string.IsNullOrWhiteSpace(_historySearch)
                ? History
                : History.Where(h => (h.Url?.Contains(_historySearch) ?? false) || (h.Method?.Contains(_historySearch) ?? false));

        // ---- Env ----
        public ObservableCollection<EnvVariable> EnvVars { get; } = new();
        private bool _showEnv;
        public bool ShowEnv { get => _showEnv; set => SetProperty(ref _showEnv, value); }

        // ---- Last request snapshot (for retry) ----
        private string _lastUrl = "";
        private string _lastMethod = "";
        private AuthConfig _lastAuth = new();

        // ---- Commands ----
        public ICommand SendRequestCommand { get; }
        public ICommand CancelRequestCommand { get; }
        public ICommand RetryRequestCommand { get; }
        public ICommand ConnectWsCommand { get; }
        public ICommand SendWsMessageCommand { get; }
        public ICommand ClearWsMessagesCommand { get; }
        public ICommand AddWsHeaderCommand { get; }
        public ICommand RemoveWsHeaderCommand { get; }
        public ICommand SetWsMessageTypeCommand { get; }
        public ICommand FormatWsJsonCommand { get; }
        public ICommand SelectHistoryCommand { get; }
        public ICommand DeleteHistoryCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand AddParamCommand { get; }
        public ICommand RemoveParamCommand { get; }
        public ICommand AddHeaderCommand { get; }
        public ICommand RemoveHeaderCommand { get; }
        public ICommand AddFormParamCommand { get; }
        public ICommand RemoveFormParamCommand { get; }
        public ICommand BulkEditParamsCommand { get; }
        public ICommand BulkEditHeadersCommand { get; }
        public ICommand BulkEditFormParamsCommand { get; }
        public ICommand AddEnvVarCommand { get; }
        public ICommand RemoveEnvVarCommand { get; }
        public ICommand CopyResponseCommand { get; }
        public ICommand CopyAsCurlCommand { get; }
        public ICommand CopyCodeCommand { get; }
        public ICommand SaveResponseCommand { get; }
        public ICommand FormatJsonCommand { get; }
        public ICommand ToggleBodyWordWrapCommand { get; }
        public ICommand ToggleHistoryCommand { get; }
        public ICommand ToggleEnvCommand { get; }
        public ICommand SetProtocolCommand { get; }
        public ICommand SetResponseViewCommand { get; }
        public ICommand SetBodyTypeCommand { get; }
        public ICommand SetAuthTypeCommand { get; }
        public ICommand GetOAuthTokenCommand { get; }
        public ICommand SetApiKeyLocationCommand { get; }

        private string _codeLanguage = "csharp";
        public string CodeLanguage { get => _codeLanguage; set => SetProperty(ref _codeLanguage, value); }
        public string[] CodeLanguages { get; } = { "csharp", "python", "javascript" };

        public ObservableCollection<TimingSegment> ResponseTimingSegments { get; } = new();

        public class TimingSegment
        {
            public string Label { get; set; }
            public long Value { get; set; }
            public System.Windows.Media.Brush Color { get; set; }
        }

        public MainViewModel()
        {
            SendRequestCommand = new RelayCommand(async () => await SendRequestAsync(), () => !IsLoading && !string.IsNullOrWhiteSpace(Url));
            CancelRequestCommand = new RelayCommand(CancelRequest, () => IsLoading);
            RetryRequestCommand = new RelayCommand(async () => await RetryAsync(), () => !IsLoading && !string.IsNullOrWhiteSpace(_lastUrl));
            ConnectWsCommand = new RelayCommand(async () =>
            {
                if (WsConnected) DisconnectWs();
                else await ConnectWsAsync();
            }, () => WsConnected || !string.IsNullOrWhiteSpace(WsUrl));
            SendWsMessageCommand = new RelayCommand(async () => await SendWsAsync(), () => WsConnected && !string.IsNullOrWhiteSpace(WsInput));
            ClearWsMessagesCommand = new RelayCommand(() => WsMessages.Clear());
            AddWsHeaderCommand = new RelayCommand(() => WsHeaders.Add(new KeyValuePairModel()));
            RemoveWsHeaderCommand = new RelayCommand<KeyValuePairModel>(h => WsHeaders.Remove(h));
            SetWsMessageTypeCommand = new RelayCommand<string>(t => WsMessageType = t);
            FormatWsJsonCommand = new RelayCommand(() =>
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<object>(WsInput);
                    WsInput = JsonSerializer.Serialize(obj, _jsonOptions);
                }
                catch { }
            });
            SelectHistoryCommand = new RelayCommand<HistoryItem>(SelectHistory);
            DeleteHistoryCommand = new RelayCommand<HistoryItem>(DeleteHistory);
            ClearHistoryCommand = new RelayCommand(() =>
            {
                if (History.Count == 0) return;
                var ok = MessageBox.Show($"Clear all {History.Count} history item(s)? This cannot be undone.",
                    "Clear History", MessageBoxButton.OKCancel, MessageBoxImage.Warning) == MessageBoxResult.OK;
                if (!ok) return;
                History.Clear();
                _storage.SaveHistory(new List<HistoryItem>());
            });
            AddParamCommand = new RelayCommand(() => Params.Add(new KeyValuePairModel()));
            RemoveParamCommand = new RelayCommand<KeyValuePairModel>(p => Params.Remove(p));
            AddHeaderCommand = new RelayCommand(() => Headers.Add(new KeyValuePairModel()));
            RemoveHeaderCommand = new RelayCommand<KeyValuePairModel>(h => Headers.Remove(h));
            AddFormParamCommand = new RelayCommand(() => FormParams.Add(new FormParamModel()));
            RemoveFormParamCommand = new RelayCommand<FormParamModel>(p => FormParams.Remove(p));
            BulkEditParamsCommand = new RelayCommand(() => BulkEditKv("Edit Query Parameters", Params));
            BulkEditHeadersCommand = new RelayCommand(() => BulkEditKv("Edit Headers", Headers));
            BulkEditFormParamsCommand = new RelayCommand(() =>
            {
                var current = string.Join("\n", FormParams.Where(p => !string.IsNullOrWhiteSpace(p.Key))
                    .Select(p => $"{p.Key}={p.Value}"));
                var dialog = new BulkEditDialog("Edit Form Parameters", current);
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true)
                {
                    FormParams.Clear();
                    foreach (var line in dialog.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var trimmed = line.Trim();
                        var idx = trimmed.IndexOfAny(new[] { '=', ':' });
                        if (idx > 0)
                            FormParams.Add(new FormParamModel { Key = trimmed[..idx].Trim(), Value = trimmed[(idx + 1)..].Trim() });
                        else if (!string.IsNullOrWhiteSpace(trimmed))
                            FormParams.Add(new FormParamModel { Key = trimmed });
                    }
                    if (FormParams.Count == 0)
                        FormParams.Add(new FormParamModel());
                }
            });
            AddEnvVarCommand = new RelayCommand(() => EnvVars.Add(new EnvVariable()));
            RemoveEnvVarCommand = new RelayCommand<EnvVariable>(v => EnvVars.Remove(v));
            CopyResponseCommand = new RelayCommand(() =>
            {
                var text = ResponseView == "raw" && Response?.RawBody != null ? Response.RawBody : Response?.Body;
                if (text != null) Clipboard.SetText(text);
            });
            CopyAsCurlCommand = new RelayCommand(() =>
            {
                var curl = CurlService.Generate(Method, Url, Headers.ToList(), BodyType, Body, GetCurrentAuth());
                Clipboard.SetText(curl);
            });
            CopyCodeCommand = new RelayCommand(() =>
            {
                var auth = GetCurrentAuth();
                var code = CodeLanguage switch
                {
                    "python" => CodeSnippetService.GeneratePython(Method, Url, Headers.ToList(), BodyType, Body, auth),
                    "javascript" => CodeSnippetService.GenerateJavaScriptFetch(Method, Url, Headers.ToList(), BodyType, Body, auth),
                    _ => CodeSnippetService.GenerateCSharp(Method, Url, Headers.ToList(), BodyType, Body, auth),
                };
                Clipboard.SetText(code);
            });
            SaveResponseCommand = new RelayCommand(() =>
            {
                if (Response == null || string.IsNullOrEmpty(Response.RawBody)) return;
                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = "response",
                    DefaultExt = SuggestExtension(ResponseContentType),
                    Filter = BuildSaveFilter(ResponseContentType),
                };
                if (dlg.ShowDialog() == true)
                {
                    try { System.IO.File.WriteAllText(dlg.FileName, Response.RawBody); }
                    catch (Exception ex) { ErrorMessage = ex.Message; }
                }
            }, () => Response != null && !string.IsNullOrEmpty(Response.RawBody));
            FormatJsonCommand = new RelayCommand(() =>
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<object>(Body);
                    Body = JsonSerializer.Serialize(obj, _jsonOptions);
                }
                catch { }
            });
            ToggleBodyWordWrapCommand = new RelayCommand(() => BodyWordWrap = !BodyWordWrap);
            ToggleHistoryCommand = new RelayCommand(() => ShowHistory = !ShowHistory);
            ToggleEnvCommand = new RelayCommand(() => ShowEnv = !ShowEnv);
            SetProtocolCommand = new RelayCommand<string>(p => Protocol = p);
            SetResponseViewCommand = new RelayCommand<string>(v => ResponseView = v);
            SetBodyTypeCommand = new RelayCommand<string>(t =>
            {
                BodyType = t;
                if (t == "none") Body = "";
                if (t == "binary") { BodyFileName = ""; BodyFileSize = 0; Body = ""; }
            });
            SetAuthTypeCommand = new RelayCommand<string>(t =>
            {
                AuthType = t;
                if (t != "none")
                {
                    var toRemove = Headers.Where(h => h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)).ToList();
                    foreach (var h in toRemove) Headers.Remove(h);
                }
            });
            GetOAuthTokenCommand = new RelayCommand(async () => await GetOAuthTokenAsync(),
                () => AuthType == "oauth2" && !string.IsNullOrWhiteSpace(OAuthTokenUrl)
                    && !string.IsNullOrWhiteSpace(OAuthClientId) && !string.IsNullOrWhiteSpace(OAuthClientSecret));
            SetApiKeyLocationCommand = new RelayCommand<string>(loc => ApiKeyLocation = loc);

            _wsService.OnMessage += msg =>
                Application.Current?.Dispatcher?.Invoke(() => WsMessages.Add(msg));
            _wsService.OnDisconnected += () =>
                Application.Current?.Dispatcher?.Invoke(() => WsConnected = false);

            foreach (var h in _storage.LoadHistory()) History.Add(h);
            foreach (var v in _storage.LoadEnvVars()) EnvVars.Add(v);

            // Auto-save env vars on any change
            EnvVars.CollectionChanged += (_, __) => SaveEnvVars();
            void AttachEnvVar(EnvVariable ev)
            {
                ev.PropertyChanged += (_, __) => SaveEnvVars();
            }
            foreach (var v in EnvVars) AttachEnvVar(v);
            EnvVars.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                    foreach (EnvVariable item in e.NewItems) AttachEnvVar(item);
                if (e.OldItems != null)
                    foreach (EnvVariable item in e.OldItems)
                        item.PropertyChanged -= (_, __) => SaveEnvVars();
            };

            if (Params.Count == 0) Params.Add(new KeyValuePairModel());
            if (Headers.Count == 0) Headers.Add(new KeyValuePairModel());
            if (FormParams.Count == 0) FormParams.Add(new FormParamModel());
            if (WsHeaders.Count == 0) WsHeaders.Add(new KeyValuePairModel());

            // Sync Params → URL query string whenever a param changes
            Params.CollectionChanged += (_, __) => SyncParamsToUrl();
            foreach (var p in Params) p.PropertyChanged += ParamItem_PropertyChanged;
            Params.CollectionChanged += (_, e) =>
            {
                if (e.NewItems != null)
                    foreach (KeyValuePairModel item in e.NewItems)
                        item.PropertyChanged += ParamItem_PropertyChanged;
                if (e.OldItems != null)
                    foreach (KeyValuePairModel item in e.OldItems)
                        item.PropertyChanged -= ParamItem_PropertyChanged;
            };
        }

        private void ParamItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(KeyValuePairModel.Key) ||
                e.PropertyName == nameof(KeyValuePairModel.Value) ||
                e.PropertyName == nameof(KeyValuePairModel.Enabled))
            {
                SyncParamsToUrl();
            }
        }

        private bool _suppressUrlSync;
        private void SyncParamsToUrl()
        {
            if (_suppressUrlSync) return;
            try
            {
                _suppressUrlSync = true;
                if (string.IsNullOrWhiteSpace(Url)) return;
                var uri = new Uri(Url.StartsWith("http") ? Url : $"http://{Url}");
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                query.Clear();
                foreach (var p in Params.Where(p => p.Enabled && !string.IsNullOrWhiteSpace(p.Key)))
                    query[p.Key] = p.Value;
                var builder = new UriBuilder(uri) { Query = query.ToString() };
                Url = builder.Uri.GetLeftPart(UriPartial.Path);
                if (!string.IsNullOrEmpty(builder.Uri.Query) && builder.Uri.Query != "?")
                    Url += builder.Uri.Query;
            }
            catch { }
            finally { _suppressUrlSync = false; }
        }

        private string ApplyEnv(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            foreach (var v in EnvVars.Where(e => e.Enabled && !string.IsNullOrWhiteSpace(e.Key)))
                text = text.Replace($"{{{{{v.Key}}}}}", v.Value);
            return text;
        }

        private AuthConfig GetCurrentAuth() => new()
        {
            Type = AuthType,
            BearerToken = ApplyEnv(BearerToken),
            BasicUsername = ApplyEnv(BasicUsername),
            BasicPassword = ApplyEnv(BasicPassword),
            ApiKeyName = ApplyEnv(ApiKeyName),
            ApiKeyValue = ApplyEnv(ApiKeyValue),
            ApiKeyLocation = ApiKeyLocation,
            OAuthGrantType = "client_credentials",
            OAuthTokenUrl = ApplyEnv(OAuthTokenUrl),
            OAuthClientId = ApplyEnv(OAuthClientId),
            OAuthClientSecret = ApplyEnv(OAuthClientSecret),
            OAuthScope = ApplyEnv(OAuthScope),
            OAuthAccessToken = ApplyEnv(OAuthAccessToken),
        };

        private async Task GetOAuthTokenAsync()
        {
            try
            {
                OAuthTokenStatus = "Requesting token...";
                var auth = GetCurrentAuth();
                var result = await _oauthService.GetTokenAsync(auth);
                OAuthAccessToken = result.AccessToken;
                OAuthTokenStatus = result.ExpiresIn > 0
                    ? $"Token obtained (expires in {result.ExpiresIn}s)"
                    : "Token obtained";
            }
            catch (Exception ex)
            {
                OAuthTokenStatus = $"Failed: {ex.Message}";
            }
        }

        private async Task SendRequestAsync()
        {
            IsLoading = true;
            ErrorMessage = null;
            Response = null;
            _httpCts = new CancellationTokenSource();

            try
            {
                var envUrl = ApplyEnv(Url);
                var envParams = Params.Select(p => new KeyValuePairModel { Id = p.Id, Key = ApplyEnv(p.Key), Value = ApplyEnv(p.Value), Enabled = p.Enabled }).ToList();
                var envHeaders = Headers.Select(h => new KeyValuePairModel { Id = h.Id, Key = ApplyEnv(h.Key), Value = ApplyEnv(h.Value), Enabled = h.Enabled }).ToList();
                var envBody = ApplyEnv(Body);
                var auth = GetCurrentAuth();

                // Save snapshot for retry
                _lastUrl = Url;
                _lastMethod = Method;
                _lastAuth = new AuthConfig
                {
                    Type = AuthType, BearerToken = BearerToken, BasicUsername = BasicUsername, BasicPassword = BasicPassword,
                    ApiKeyName = ApiKeyName, ApiKeyValue = ApiKeyValue, ApiKeyLocation = ApiKeyLocation,
                    OAuthTokenUrl = OAuthTokenUrl, OAuthClientId = OAuthClientId, OAuthClientSecret = OAuthClientSecret,
                    OAuthScope = OAuthScope, OAuthAccessToken = OAuthAccessToken,
                };

                // Resolve Content-Type from body type + language selector
                var contentType = BodyType switch
                {
                    "json" => "application/json",
                    "raw" or "text" => BodyLanguage switch
                    {
                        "XML" => "application/xml",
                        "HTML" => "text/html",
                        "JavaScript" => "application/javascript",
                        _ => "text/plain",
                    },
                    _ => null
                };

                var res = await _httpService.SendAsync(Method, envUrl, envParams, envHeaders, BodyType, envBody, auth, FormParams.ToList(), contentType, _httpCts.Token);

                // DEBUG: Write response to temp file for verification
                try
                {
                    var debugPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "webdog_response.txt");
                    var debugLines = new List<string>
                    {
                        $"Status: {res.Status} {res.StatusText}",
                        $"Body length: {res.Body?.Length ?? 0}",
                        $"RawBody length: {res.RawBody?.Length ?? 0}",
                        $"Headers count: {res.Headers?.Count ?? 0}",
                        $"Size: {res.Size}",
                        $"-----HEADERS-----",
                    };
                    if (res.Headers != null)
                        foreach (var h in res.Headers)
                            debugLines.Add($"  {h.Key}: {h.Value}");
                    debugLines.Add("-----BODY(first 1000 chars)-----");
                    debugLines.Add(res.Body?.Length > 1000 ? res.Body[..1000] : res.Body ?? "(null)");
                    System.IO.File.WriteAllText(debugPath, string.Join("\n", debugLines));
                    ErrorMessage = $"Debug written to: {debugPath} | BodyLen={res.Body?.Length ?? 0} Headers={res.Headers?.Count ?? 0}";
                }
                catch { }

                // Format JSON for pretty view
                if (!string.IsNullOrEmpty(res.Body))
                {
                    try
                    {
                        var jsonDoc = JsonDocument.Parse(res.Body);
                        res.Body = JsonSerializer.Serialize(jsonDoc.RootElement, new JsonSerializerOptions { WriteIndented = true });
                    }
                    catch { /* not JSON, keep raw */ }
                }

                Response = res;
                UpdateDisplayedResponse();

                // Update cookies
                ResponseCookies.Clear();
                foreach (var c in res.Cookies) ResponseCookies.Add(c);

                var item = new HistoryItem
                {
                    Method = Method, Url = envUrl, Status = res.Status, Time = res.Time,
                    Timestamp = DateTime.UtcNow,
                    Request = new RequestConfig
                    {
                        Method = Method, Url = Url, Protocol = Protocol,
                        Params = Params.Select(p => new KeyValuePairModel { Id = p.Id, Key = p.Key, Value = p.Value, Enabled = p.Enabled }).ToList(),
                        Headers = Headers.Select(h => new KeyValuePairModel { Id = h.Id, Key = h.Key, Value = h.Value, Enabled = h.Enabled }).ToList(),
                        BodyType = BodyType, Body = Body,
                        Auth = new AuthConfig
                        {
                            Type = AuthType, BearerToken = BearerToken, BasicUsername = BasicUsername, BasicPassword = BasicPassword,
                            ApiKeyName = ApiKeyName, ApiKeyValue = ApiKeyValue, ApiKeyLocation = ApiKeyLocation,
                            OAuthTokenUrl = OAuthTokenUrl, OAuthClientId = OAuthClientId, OAuthClientSecret = OAuthClientSecret,
                            OAuthScope = OAuthScope, OAuthAccessToken = OAuthAccessToken,
                        },
                    },
                    Response = res,
                };
                History.Insert(0, item);
                if (History.Count > 200) History.RemoveAt(History.Count - 1);
                _storage.SaveHistory(History.ToList());
            }
            catch (OperationCanceledException) { ErrorMessage = null; }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                var item = new HistoryItem
                {
                    Method = Method, Url = ApplyEnv(Url), Timestamp = DateTime.UtcNow,
                    Request = new RequestConfig
                    {
                        Method = Method, Url = Url, Protocol = Protocol,
                        Params = Params.Select(p => new KeyValuePairModel { Id = p.Id, Key = p.Key, Value = p.Value, Enabled = p.Enabled }).ToList(),
                        Headers = Headers.Select(h => new KeyValuePairModel { Id = h.Id, Key = h.Key, Value = h.Value, Enabled = h.Enabled }).ToList(),
                        BodyType = BodyType, Body = Body,
                        Auth = new AuthConfig
                        {
                            Type = AuthType, BearerToken = BearerToken, BasicUsername = BasicUsername, BasicPassword = BasicPassword,
                            ApiKeyName = ApiKeyName, ApiKeyValue = ApiKeyValue, ApiKeyLocation = ApiKeyLocation,
                            OAuthTokenUrl = OAuthTokenUrl, OAuthClientId = OAuthClientId, OAuthClientSecret = OAuthClientSecret,
                            OAuthScope = OAuthScope, OAuthAccessToken = OAuthAccessToken,
                        },
                    },
                };
                History.Insert(0, item);
                _storage.SaveHistory(History.ToList());
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelRequest()
        {
            _httpCts?.Cancel();
            IsLoading = false;
        }

        private async Task RetryAsync()
        {
            if (string.IsNullOrWhiteSpace(_lastUrl)) return;
            // Restore the last request snapshot so Retry replays exactly the previous call
            Url = _lastUrl;
            Method = _lastMethod;
            if (_lastAuth != null)
            {
                AuthType = _lastAuth.Type ?? "none";
                BearerToken = _lastAuth.BearerToken ?? "";
                BasicUsername = _lastAuth.BasicUsername ?? "";
                BasicPassword = _lastAuth.BasicPassword ?? "";
                ApiKeyName = _lastAuth.ApiKeyName ?? "";
                ApiKeyValue = _lastAuth.ApiKeyValue ?? "";
                ApiKeyLocation = _lastAuth.ApiKeyLocation ?? "header";
                OAuthTokenUrl = _lastAuth.OAuthTokenUrl ?? "";
                OAuthClientId = _lastAuth.OAuthClientId ?? "";
                OAuthClientSecret = _lastAuth.OAuthClientSecret ?? "";
                OAuthScope = _lastAuth.OAuthScope ?? "";
                OAuthAccessToken = _lastAuth.OAuthAccessToken ?? "";
            }
            await SendRequestAsync();
        }

        private void UpdateDisplayedResponse()
        {
            if (Response == null)
            {
                DisplayedResponseBody = "";
                JsonTree = null;
                return;
            }

            if (ResponseView == "tree")
            {
                // Build JSON tree if possible, else fall back to pretty text.
                if (IsJsonResponse && !string.IsNullOrWhiteSpace(Response.Body))
                {
                    try { JsonTree = JsonNodeModel.BuildFromJson(Response.Body); }
                    catch { JsonTree = null; ResponseView = "pretty"; }
                }
                else
                {
                    JsonTree = null;
                    ResponseView = "pretty";
                }
            }
            else
            {
                JsonTree = null;
            }

            DisplayedResponseBody = ResponseView == "raw" ? Response.RawBody : Response.Body;
            UpdateSearchMatches();
        }

        private void UpdateSearchMatches()
        {
            if (string.IsNullOrWhiteSpace(ResponseSearch) || string.IsNullOrEmpty(DisplayedResponseBody))
                SearchMatchCount = 0;
            else
            {
                int count = 0, idx = 0;
                var body = DisplayedResponseBody;
                while ((idx = body.IndexOf(ResponseSearch, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
                {
                    count++;
                    idx += ResponseSearch.Length;
                }
                SearchMatchCount = count;
            }
            OnPropertyChanged(nameof(SearchMatchCount));
            OnPropertyChanged(nameof(SearchStatus));
        }

        private void UpdateResponseSummary()
        {
            ResponseHeaders.Clear();
            if (Response == null)
            {
                ResponseContentType = null;
                ResponseMethod = null;
                ResponseUrl = null;
            }
            else
            {
                // Extract Content-Type from response headers (strip charset etc.)
                ResponseContentType = null;
                if (Response.Headers != null)
                {
                    foreach (var kv in Response.Headers)
                    {
                        ResponseHeaders.Add(new HeaderDisplayItem { Name = kv.Key, Value = kv.Value });
                        if (kv.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                        {
                            var ct = kv.Value ?? "";
                            var sep = ct.IndexOf(';');
                            ResponseContentType = sep >= 0 ? ct[..sep].Trim() : ct.Trim();
                        }
                    }
                }
                ResponseMethod = Method;
                ResponseUrl = Url;
            }
            OnPropertyChanged(nameof(ResponseContentType));
            OnPropertyChanged(nameof(ResponseMethod));
            OnPropertyChanged(nameof(ResponseUrl));
            OnPropertyChanged(nameof(IsJsonResponse));
            RefreshTimingSegments();
        }

        private void RefreshTimingSegments()
        {
            ResponseTimingSegments.Clear();
            var t = Response?.Timing;
            if (t == null) return;
            System.Windows.Media.Brush B(string hex)
            {
                var b = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
                b.Freeze();
                return b;
            }
            ResponseTimingSegments.Add(new TimingSegment { Label = "DNS", Value = t.DnsMs, Color = B("#60A5FA") });
            ResponseTimingSegments.Add(new TimingSegment { Label = "Connect", Value = t.ConnectMs, Color = B("#34D399") });
            ResponseTimingSegments.Add(new TimingSegment { Label = "TLS", Value = t.TlsMs, Color = B("#A78BFA") });
            ResponseTimingSegments.Add(new TimingSegment { Label = "TTFB", Value = t.TtfbMs, Color = B("#FBBF24") });
            ResponseTimingSegments.Add(new TimingSegment { Label = "Transfer", Value = t.TransferMs, Color = B("#2DD4BF") });
            ResponseTimingSegments.Add(new TimingSegment { Label = "Total", Value = t.TotalMs, Color = B("#F87171") });
        }

        // ---- WebSocket ----
        private string BuildWsUrl()
        {
            var raw = ApplyEnv(WsUrl)?.Trim() ?? "";
            if (raw.StartsWith("ws://", StringComparison.OrdinalIgnoreCase) ||
                raw.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
                return raw;
            return $"{WsScheme}://{raw}";
        }

        private async Task ConnectWsAsync()
        {
            try
            {
                var subprotocols = string.IsNullOrWhiteSpace(WsSubprotocols)
                    ? null
                    : WsSubprotocols.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var headers = WsHeaders.Where(h => h.Enabled && !string.IsNullOrWhiteSpace(h.Key)).ToList();
                await _wsService.ConnectAsync(BuildWsUrl(), subprotocols, headers);
                WsConnected = true;
            }
            catch (Exception ex)
            {
                WsMessages.Add(new WsMessage { Type = "error", Data = ex.Message });
            }
        }

        private void DisconnectWs()
        {
            _wsService.Disconnect();
            WsConnected = false;
        }

        private async Task SendWsAsync()
        {
            var isBinary = WsMessageType == "binary";
            var payload = ApplyEnv(WsInput);
            await _wsService.SendAsync(payload, isBinary);
            WsMessages.Add(new WsMessage { Type = "sent", Data = payload, Size = Encoding.UTF8.GetByteCount(payload) });
            WsInput = "";
            OnPropertyChanged(nameof(WsInput));
        }

        // ---- History ----
        private void SelectHistory(HistoryItem item)
        {
            if (item == null) return;
            Protocol = item.Request.Protocol ?? "http";
            Method = item.Request.Method;
            Url = item.Request.Url;
            BodyType = item.Request.BodyType;
            Body = item.Request.Body;

            // Restore auth
            if (item.Request.Auth != null)
            {
                AuthType = item.Request.Auth.Type ?? "none";
                BearerToken = item.Request.Auth.BearerToken ?? "";
                BasicUsername = item.Request.Auth.BasicUsername ?? "";
                BasicPassword = item.Request.Auth.BasicPassword ?? "";
                ApiKeyName = item.Request.Auth.ApiKeyName ?? "";
                ApiKeyValue = item.Request.Auth.ApiKeyValue ?? "";
                ApiKeyLocation = item.Request.Auth.ApiKeyLocation ?? "header";
                OAuthTokenUrl = item.Request.Auth.OAuthTokenUrl ?? "";
                OAuthClientId = item.Request.Auth.OAuthClientId ?? "";
                OAuthClientSecret = item.Request.Auth.OAuthClientSecret ?? "";
                OAuthScope = item.Request.Auth.OAuthScope ?? "";
                OAuthAccessToken = item.Request.Auth.OAuthAccessToken ?? "";
            }
            else
            {
                AuthType = "none";
                BearerToken = "";
                BasicUsername = "";
                BasicPassword = "";
            }

            Params.Clear();
            foreach (var p in item.Request.Params) Params.Add(new KeyValuePairModel { Id = p.Id, Key = p.Key, Value = p.Value, Enabled = p.Enabled });
            if (Params.Count == 0) Params.Add(new KeyValuePairModel());

            Headers.Clear();
            foreach (var h in item.Request.Headers) Headers.Add(new KeyValuePairModel { Id = h.Id, Key = h.Key, Value = h.Value, Enabled = h.Enabled });
            if (Headers.Count == 0) Headers.Add(new KeyValuePairModel());

            Response = item.Response;
            ResponseCookies.Clear();
            if (item.Response?.Cookies != null)
                foreach (var c in item.Response.Cookies) ResponseCookies.Add(c);
            ErrorMessage = null;
            UpdateDisplayedResponse();
        }

        private void DeleteHistory(HistoryItem item)
        {
            if (item != null)
            {
                History.Remove(item);
                _storage.SaveHistory(History.ToList());
            }
        }

        public void SaveEnvVars()
        {
            _storage.SaveEnvVars(EnvVars.ToList());
        }

        private void BulkEditKv(string title, ObservableCollection<KeyValuePairModel> collection)
        {
            var current = string.Join("\n", collection.Where(p => !string.IsNullOrWhiteSpace(p.Key))
                .Select(p => $"{p.Key}={p.Value}"));
            var dialog = new BulkEditDialog(title, current);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                collection.Clear();
                foreach (var line in dialog.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = line.Trim();
                    var idx = trimmed.IndexOfAny(new[] { '=', ':' });
                    if (idx > 0)
                        collection.Add(new KeyValuePairModel { Key = trimmed[..idx].Trim(), Value = trimmed[(idx + 1)..].Trim() });
                    else if (!string.IsNullOrWhiteSpace(trimmed))
                        collection.Add(new KeyValuePairModel { Key = trimmed });
                }
                if (collection.Count == 0)
                    collection.Add(new KeyValuePairModel());
            }
        }

        private static string SuggestExtension(string contentType)
        {
            var lang = SyntaxHighlighter.Detect(contentType);
            return lang switch
            {
                SyntaxHighlighter.Language.Json => ".json",
                SyntaxHighlighter.Language.Xml => ".xml",
                SyntaxHighlighter.Language.Html => ".html",
                _ => ".txt"
            };
        }

        private static string BuildSaveFilter(string contentType)
        {
            var ext = SuggestExtension(contentType).TrimStart('.');
            return $"{ext} files (*.{ext})|*.{ext}|All files (*.*)|*.*";
        }
    }
}
