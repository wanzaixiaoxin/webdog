using System;
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

        public async Task ConnectAsync(string url)
        {
            Disconnect();
            _cts = new CancellationTokenSource();
            _ws = new ClientWebSocket();
            await _ws.ConnectAsync(new Uri(url), _cts.Token);
            OnMessage?.Invoke(new WsMessage { Type = "info", Data = $"Connected to {url}" });
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

        public async Task SendAsync(string message)
        {
            if (_ws?.State != WebSocketState.Open) return;
            var bytes = Encoding.UTF8.GetBytes(message);
            await _ws.SendAsync(
                new ArraySegment<byte>(bytes),
                WebSocketMessageType.Text,
                true,
                _cts.Token);
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[8192];
            try
            {
                while (_ws?.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
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

                    var data = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    OnMessage?.Invoke(new WsMessage
                    {
                        Type = "received",
                        Data = data,
                        Size = result.Count,
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
