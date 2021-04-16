using NetSockets.Models;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetSockets
{
    public abstract class WebSocketHandler
    {
        protected ConnectionManager _connect { get; }

        protected WebSocketHandler(ConnectionManager connectionManager)
        {
            _connect = connectionManager;
        }

        public virtual async Task OnConnected(WebSocket socket, Key key)
        {
            await _connect.AddSocketAsync(socket, key);
        }

        public virtual async Task OnDisconnected(WebSocket socket)
        {
            await _connect.RemoveSocket(_connect.GetKeyBySocket(socket));
        }

        private string SendConvertJson(int action, string message)
        {
            var res = new SocketModel { Action = action, Text = message };
            return JsonConvert.SerializeObject(res);
        }

        protected async Task SendMessageAsync(WebSocket socket, int action, string message)
        {
            try
            {
                message = SendConvertJson(action, message);

                if (socket.State != WebSocketState.Open)
                    return;
                await socket.SendAsync(Encoding.UTF8.GetBytes(message),
                                       messageType: WebSocketMessageType.Text,
                                       endOfMessage: true,
                                       cancellationToken: CancellationToken.None);
            }
            catch
            {
                // ignored
            }
        }

        protected async Task SendMessageAsync(Key socketId, int action, string message)
        {
            await SendMessageAsync(_connect.GetSocketById(socketId), action, message);
        }

        protected async Task SendMessageAsync(string socketId, int action, string message)
        {
            await SendMessageAsync(_connect.GetSocketById(socketId), action, message);
        }

        public async Task SendMessageToAllAsync(int action, string message)
        {
            foreach (var pair in _connect.GetAll())
            {
                if (pair.Value.State == WebSocketState.Open)
                    await SendMessageAsync(pair.Value, action, message);
            }
        }

        //TODO - decide if exposing the message string is better than exposing the result and buffer
        public abstract Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer);
    }
}