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

namespace WebDog.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly HttpService _httpService = new();
        private readonly WsService _wsService = new();
        private readonly StorageService _storage = new();
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
        private CancellationTokenSource _httpCts;

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

        private string _method = "GET";
        public string Method { get => _method; set => SetProperty(ref _method, value); }

        private string _url = "";
        public string Url { get => _url; set => SetProperty(ref _url, value); }

        public ObservableCollection<KeyValuePairModel> Params { get; } = new();
        public ObservableCollection<KeyValuePairModel> Headers { get; } = new();

        private string _bodyType = "json";
        public string BodyType { get => _bodyType; set => SetProperty(ref _bodyType, value); }

        private string _body = "";
        public string Body { get => _body; set => SetProperty(ref _body, value); }

        private ResponseData _response;
        public ResponseData Response { get => _response; set => SetProperty(ref _response, value); }

        private bool _isLoading;
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

        private string _errorMessage;
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }

        private string _responseView = "pretty";
        public string ResponseView { get => _responseView; set => SetProperty(ref _responseView, value); }

        private string _wsUrl = "";
        public string WsUrl { get => _wsUrl; set => SetProperty(ref _wsUrl, value); }

        private bool _wsConnected;
        public bool WsConnected { get => _wsConnected; set => SetProperty(ref _wsConnected, value); }

        public ObservableCollection<WsMessage> WsMessages { get; } = new();

        private string _wsInput = "";
        public string WsInput { get => _wsInput; set => SetProperty(ref _wsInput, value); }

        public ObservableCollection<HistoryItem> History { get; } = new();
        private bool _showHistory;
        public bool ShowHistory { get => _showHistory; set => SetProperty(ref _showHistory, value); }

        private string _historySearch = "";
        public string HistorySearch { get => _historySearch; set { SetProperty(ref _historySearch, value); OnPropertyChanged(nameof(FilteredHistory)); } }

        public IEnumerable<HistoryItem> FilteredHistory =>
            string.IsNullOrWhiteSpace(_historySearch)
                ? History
                : History.Where(h => (h.Url?.Contains(_historySearch) ?? false) || (h.Method?.Contains(_historySearch) ?? false));

        public ObservableCollection<EnvVariable> EnvVars { get; } = new();
        private bool _showEnv;
        public bool ShowEnv { get => _showEnv; set => SetProperty(ref _showEnv, value); }

        public ICommand SendRequestCommand { get; }
        public ICommand ConnectWsCommand { get; }
        public ICommand SendWsMessageCommand { get; }
        public ICommand ClearWsMessagesCommand { get; }
        public ICommand SelectHistoryCommand { get; }
        public ICommand DeleteHistoryCommand { get; }
        public ICommand ClearHistoryCommand { get; }
        public ICommand AddParamCommand { get; }
        public ICommand RemoveParamCommand { get; }
        public ICommand AddHeaderCommand { get; }
        public ICommand RemoveHeaderCommand { get; }
        public ICommand AddEnvVarCommand { get; }
        public ICommand RemoveEnvVarCommand { get; }
        public ICommand CopyResponseCommand { get; }
        public ICommand FormatJsonCommand { get; }
        public ICommand ToggleHistoryCommand { get; }
        public ICommand ToggleEnvCommand { get; }
        public ICommand SetProtocolCommand { get; }
        public ICommand SetResponseViewCommand { get; }
        public ICommand SetBodyTypeCommand { get; }

        public MainViewModel()
        {
            SendRequestCommand = new RelayCommand(async () => await SendRequestAsync(), () => !IsLoading && !string.IsNullOrWhiteSpace(Url));
            ConnectWsCommand = new RelayCommand(async () =>
            {
                if (WsConnected) DisconnectWs();
                else await ConnectWsAsync();
            }, () => WsConnected || !string.IsNullOrWhiteSpace(WsUrl));
            SendWsMessageCommand = new RelayCommand(async () => await SendWsAsync(), () => WsConnected && !string.IsNullOrWhiteSpace(WsInput));
            ClearWsMessagesCommand = new RelayCommand(() => WsMessages.Clear());
            SelectHistoryCommand = new RelayCommand<HistoryItem>(SelectHistory);
            DeleteHistoryCommand = new RelayCommand<HistoryItem>(DeleteHistory);
            ClearHistoryCommand = new RelayCommand(() => { History.Clear(); _storage.SaveHistory(new List<HistoryItem>()); });
            AddParamCommand = new RelayCommand(() => Params.Add(new KeyValuePairModel()));
            RemoveParamCommand = new RelayCommand<KeyValuePairModel>(p => Params.Remove(p));
            AddHeaderCommand = new RelayCommand(() => Headers.Add(new KeyValuePairModel()));
            RemoveHeaderCommand = new RelayCommand<KeyValuePairModel>(h => Headers.Remove(h));
            AddEnvVarCommand = new RelayCommand(() => EnvVars.Add(new EnvVariable()));
            RemoveEnvVarCommand = new RelayCommand<EnvVariable>(v => EnvVars.Remove(v));
            CopyResponseCommand = new RelayCommand(() =>
            {
                if (Response?.Body != null)
                    Clipboard.SetText(Response.Body);
            });
            FormatJsonCommand = new RelayCommand(() =>
            {
                try
                {
                    var obj = JsonSerializer.Deserialize<object>(Body);
                    Body = JsonSerializer.Serialize(obj, _jsonOptions);
                    OnPropertyChanged(nameof(Body));
                }
                catch { }
            });
            ToggleHistoryCommand = new RelayCommand(() => ShowHistory = !ShowHistory);
            ToggleEnvCommand = new RelayCommand(() => ShowEnv = !ShowEnv);
            SetProtocolCommand = new RelayCommand<string>(p => Protocol = p);
            SetResponseViewCommand = new RelayCommand<string>(v => ResponseView = v);
            SetBodyTypeCommand = new RelayCommand<string>(t =>
            {
                BodyType = t;
                if (t == "none" || (t == "json" && BodyType != "json"))
                    Body = "";
            });

            _wsService.OnMessage += msg =>
                Application.Current?.Dispatcher?.Invoke(() => WsMessages.Add(msg));
            _wsService.OnDisconnected += () =>
                Application.Current?.Dispatcher?.Invoke(() => WsConnected = false);

            foreach (var h in _storage.LoadHistory()) History.Add(h);
            foreach (var v in _storage.LoadEnvVars()) EnvVars.Add(v);

            if (Params.Count == 0) Params.Add(new KeyValuePairModel());
            if (Headers.Count == 0) Headers.Add(new KeyValuePairModel());
        }

        private string ApplyEnv(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            foreach (var v in EnvVars.Where(e => e.Enabled && !string.IsNullOrWhiteSpace(e.Key)))
            {
                text = text.Replace($"{{{{{v.Key}}}}}", v.Value);
            }
            return text;
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
                var envParams = Params.Select(p => new KeyValuePairModel
                {
                    Id = p.Id, Key = ApplyEnv(p.Key), Value = ApplyEnv(p.Value), Enabled = p.Enabled
                }).ToList();
                var envHeaders = Headers.Select(h => new KeyValuePairModel
                {
                    Id = h.Id, Key = ApplyEnv(h.Key), Value = ApplyEnv(h.Value), Enabled = h.Enabled
                }).ToList();
                var envBody = ApplyEnv(Body);

                var res = await _httpService.SendAsync(Method, envUrl, envParams, envHeaders, BodyType, envBody, _httpCts.Token);
                Response = res;

                var item = new HistoryItem
                {
                    Method = Method,
                    Url = envUrl,
                    Status = res.Status,
                    Time = res.Time,
                    Timestamp = DateTime.UtcNow.ToString("O"),
                    Request = new RequestConfig
                    {
                        Method = Method, Url = Url, Protocol = Protocol,
                        Params = new List<KeyValuePairModel>(Params),
                        Headers = new List<KeyValuePairModel>(Headers),
                        BodyType = BodyType, Body = Body,
                    },
                    Response = res,
                };
                History.Insert(0, item);
                if (History.Count > 100) History.RemoveAt(History.Count - 1);
                _storage.SaveHistory(History.ToList());
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                var item = new HistoryItem
                {
                    Method = Method, Url = ApplyEnv(Url), Timestamp = DateTime.UtcNow.ToString("O"),
                    Request = new RequestConfig
                    {
                        Method = Method, Url = Url, Protocol = Protocol,
                        Params = new List<KeyValuePairModel>(Params),
                        Headers = new List<KeyValuePairModel>(Headers),
                        BodyType = BodyType, Body = Body,
                    }
                };
                History.Insert(0, item);
                _storage.SaveHistory(History.ToList());
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ConnectWsAsync()
        {
            try
            {
                await _wsService.ConnectAsync(ApplyEnv(WsUrl));
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
            await _wsService.SendAsync(ApplyEnv(WsInput));
            WsMessages.Add(new WsMessage { Type = "sent", Data = WsInput, Size = Encoding.UTF8.GetByteCount(WsInput) });
            WsInput = "";
            OnPropertyChanged(nameof(WsInput));
        }

        private void SelectHistory(HistoryItem item)
        {
            if (item == null) return;
            Protocol = item.Request.Protocol ?? "http";
            Method = item.Request.Method;
            Url = item.Request.Url;
            BodyType = item.Request.BodyType;
            Body = item.Request.Body;

            Params.Clear();
            foreach (var p in item.Request.Params) Params.Add(p);
            if (Params.Count == 0) Params.Add(new KeyValuePairModel());

            Headers.Clear();
            foreach (var h in item.Request.Headers) Headers.Add(h);
            if (Headers.Count == 0) Headers.Add(new KeyValuePairModel());

            Response = item.Response;
            ErrorMessage = null;
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
    }
}
