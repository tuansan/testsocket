using NetSockets.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace NetSockets
{
    public class ChatMessageHandler : WebSocketHandler
    {
        public ChatMessageHandler(ConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
        {
        }

        public override async Task OnConnected(WebSocket socket)
        {
            await base.OnConnected(socket);
            var socketId = WebSocketConnectionManager.GetId(socket);
            await SendMessageAsync(socketId, "Connected");
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);
            await base.OnDisconnected(socket);
            await SendMessageToAllAsync($"{socketId} Disconnected");
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);
            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var param = JsonConvert.DeserializeObject<SocketModel>(message);
            string res = string.Empty;
            switch (param.Action)
            {
                case "UpdateKey":
                    WebSocketConnectionManager.UpdateKey(socketId, param.Text);
                    break;
                default:
                    res = socketId+": "+ param.Text;
                    await SendMessageToAllAsync(res);
                    break;
            }
        }
    }
}
