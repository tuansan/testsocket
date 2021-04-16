using Microsoft.AspNetCore.Http;
using NetSockets.Models;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetSockets
{
    public class ManagerMiddleware
    {
        private WebSocketHandler _webSocketHandler { get; }

        public ManagerMiddleware(WebSocketHandler webSocketHandler)
        {
            _webSocketHandler = webSocketHandler;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
                return;
            using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
            string id = context.Request.Query["id"].ToString();
            string name = context.Request.Query["name"].ToString();
            if (string.IsNullOrEmpty(name))
                name = id;
            int local = int.Parse(id);
            await _webSocketHandler.OnConnected(socket, new Key
            {
                Id = id,
                Name = name,
                Local = local * 123 + "",
                Role = local > 10 ? (int)ENVaiTro.TaiXe : (int)ENVaiTro.Khach
            });
            await Receive(socket, async (result, buffer) =>
            {
                switch (result.MessageType)
                {
                    case WebSocketMessageType.Text:
                        await _webSocketHandler.ReceiveAsync(socket, result, buffer);
                        return;

                    case WebSocketMessageType.Close:
                        await _webSocketHandler.OnDisconnected(socket);
                        return;

                    case WebSocketMessageType.Binary:
                        break;
                }
            });
        }

        private static async Task Receive(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                                                       cancellationToken: CancellationToken.None);

                handleMessage(result, buffer);
            }
        }
    }
}