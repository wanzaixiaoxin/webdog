using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebDog.Models;

namespace WebDog.Services
{
    public class WsService
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;

        public bool IsConnected => _ws?.State == WebSocketState.Open;

        public event Action<WsMessage> OnMessage;
        public event Action OnDisconnected;

        /// <summary>Connect with optional subprotocols and handshake headers.</summary>
        public async Task ConnectAsync(string url, IEnumerable<string> subprotocols = null, IEnumerable<KeyValuePairModel> headers = null)
        {
            Disconnect();
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();

            if (subprotocols != null)
            {
                foreach (var p in subprotocols)
                {
                    if (!string.IsNullOrWhiteSpace(p))
                        _ws.Options.AddSubProtocol(p.Trim());
                }
            }

            if (headers != null)
            {
                foreach (var h in headers)
                {
                    if (h.Enabled && !string.IsNullOrWhiteSpace(h.Key))
                        _ws.Options.SetRequestHeader(h.Key, h.Value ?? "");
                }
            }

            await _ws.ConnectAsync(new Uri(url), _cts.Token);
            var proto = string.IsNullOrEmpty(_ws.SubProtocol) ? "default" : _ws.SubProtocol;
            OnMessage?.Invoke(new WsMessage { Type = "info", Data = $"Connected to {url} (protocol: {proto})" });
            _ = ReceiveLoopAsync();
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            try { _ws?.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None).Wait(2000); } catch { }
            _ws?.Dispose();
            _ws = null;
            _cts?.Dispose();
            _cts = null;
        }

        /// <summary>Send a text or binary message.</summary>
        public async Task SendAsync(string message, bool isBinary = false)
        {
            if (_ws?.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                isBinary ? WebSocketMessageType.Binary : WebSocketMessageType.Text,
                true,
                _cts.Token);
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                while (_ws?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var buffer = new byte[8192];
                    using var messageStream = new System.IO.MemoryStream();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            OnMessage?.Invoke(new WsMessage
                            {
                                Type = "info",
                                Data = $"Connection closed (code: {(int?)result.CloseStatus}, reason: {result.CloseStatusDescription ?? "N/A"})"
                            });
                            OnDisconnected?.Invoke();
                            return;
                        }
                        messageStream.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    var data = Encoding.UTF8.GetString(messageStream.ToArray());
                    OnMessage?.Invoke(new WsMessage
                    {
                        Type = result.MessageType == WebSocketMessageType.Binary ? "binary" : "received",
                        Data = data,
                        Size = messageStream.Length,
                    });
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                OnMessage?.Invoke(new WsMessage { Type = "error", Data = ex.Message });
            }
            finally
            {
                OnDisconnected?.Invoke();
            }
        }
    }
}
