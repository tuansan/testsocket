using NetSockets.Models;
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
        private ConcurrentDictionary<Key, WebSocket> _sockets = new ConcurrentDictionary<Key, WebSocket>();

        public WebSocket GetSocketById(string id)
        {
            return _sockets.FirstOrDefault(p => p.Key.Id == id).Value;
        }

        public WebSocket GetSocketById(Key id)
        {
            return _sockets.FirstOrDefault(p => p.Key == id).Value;
        }

        public ConcurrentDictionary<Key, WebSocket> GetAll()
        {
            return _sockets;
        }

        public IList<Key> GetAllLaiXe()
        {
            return _sockets.Where(p => p.Key.Role == (int)ENVaiTro.TaiXe).Take(20).Select(s => s.Key).ToList();
        }

        public Key GetKeyById(string id)
        {
            return _sockets.LastOrDefault(p => p.Key.Id == id).Key ?? null;
        }

        public Key GetKeyBySocket(WebSocket socket)
        {
            return _sockets.LastOrDefault(p => p.Value == socket).Key ?? null;
        }

        public async Task AddSocketAsync(WebSocket socket, Key key)
        {
            if (!_sockets.TryAdd(key, socket))
            {
                await RemoveSocket(key);
                _sockets.TryAdd(key, socket);
            }
        }

        public void UpdateKey(Key newKey)
        {
            if (_sockets.TryRemove(GetKeyById(newKey.Id), out WebSocket socket))
                _sockets.TryAdd(newKey, socket);
        }

        public async Task RemoveSocket(Key id)
        {
            if (_sockets.TryRemove(id, out WebSocket socket))
                await socket.CloseAsync(closeStatus: WebSocketCloseStatus.NormalClosure,
                                        statusDescription: "Closed by the ConnectionManager",
                                        cancellationToken: CancellationToken.None);
        }
    }
}