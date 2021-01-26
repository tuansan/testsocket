using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NetSockets
{
    public class ConnectionManager
    {
        private ConcurrentDictionary<string, WebSocket> _sockets = new ConcurrentDictionary<string, WebSocket>();

        public WebSocket GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public ConcurrentDictionary<string, WebSocket> GetAll()
        {
            return _sockets;
        }

        public IList<string> GetAllLaiXe()
        {
            return _sockets.Where(p => int.Parse(p.Key) > 10).Take(20).Select(s => s.Key).ToList();
        }

        public string GetId(WebSocket socket)
        {
            return _sockets.LastOrDefault(p => p.Value == socket).Key ?? string.Empty;
        }

        public async Task AddSocketAsync(WebSocket socket, string key)
        {
            if (!_sockets.TryAdd(key, socket))
                await RemoveSocket(key);
        }

        public void UpdateKey(string key, string newKey)
        {
            _sockets.TryRemove(key, out WebSocket socket);
            _sockets.TryAdd(newKey, socket);
        }

        public async Task RemoveSocket(string id)
        {
            if (_sockets.TryRemove(id, out WebSocket socket))
                await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                        statusDescription: "Closed by the ConnectionManager",
                                        cancellationToken: CancellationToken.None);
        }
    }
}