using Microsoft.Extensions.Caching.Distributed;
using NetSockets.Models;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace NetSockets
{
    public abstract class MessageHandler : WebSocketHandler
    {
        private readonly IDistributedCache _cache;
        private readonly Random _random = new Random();

        protected MessageHandler(ConnectionManager connectionManager, IDistributedCache cache) : base(connectionManager)
        {
            _cache = cache;
        }

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public override async Task OnConnected(WebSocket socket, Key key)
        {
            var old = _connect.GetKeyById(key.Id);

            if (old != null)
                await _connect.RemoveSocket(old);

            if (int.TryParse(key.Id, out int id))
            {
                if (id > 10)
                {
                    key.TargetId = _cache.GetString("TX#" + key.Id);
                }
                else
                {
                    key.TargetId = _cache.GetString("K#" + key.Id);
                }
            }
            if (!string.IsNullOrEmpty(key.TargetId))
            {
                key.Status = (int)ENTrangThaiUser.DANG_CHAY;
                Key target = _connect.GetKeyById(key.TargetId);
                await SendMessageAsync(socket, (int)ENActionSend.BAT_DAU, target.Name);
            }

            await base.OnConnected(socket, key);
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var key = _connect.GetKeyBySocket(socket);
            await base.OnDisconnected(socket);
            if (key != null)
            {
                await SendMessageToAllAsync((int)ENActionSend.CHAT, $"{key.Name}: Disconnected");
            }
            if (key is {Role: (int)ENVaiTro.TaiXe, Status: (int)ENTrangThaiUser.DANG_XACNHAN})
            {
                await NextTaiXeAsync(_connect.GetKeyById(key.TargetId));
            }
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            try
            {
                var user = _connect.GetKeyBySocket(socket);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var param = JsonConvert.DeserializeObject<SocketModel>(message);
                Key khach;
                Key taixe;
                if (user.Role == (int)ENVaiTro.Khach)
                {
                    khach = user;
                    taixe = _connect.GetKeyById(user.TargetId);
                }
                else
                {
                    taixe = user;
                    khach = _connect.GetKeyById(user.TargetId);
                }
                switch (param.Action)
                {
                    case (int)ENActionReceive.DAT_XE:
                        var list = _connect.GetAllLaiXe();
                        if (khach.Status == (int)ENTrangThaiUser.RANH)
                        {
                            khach.TaiXes = list.Select(s => s.Id).ToList();
                            khach.Status = (int)ENTrangThaiUser.DANG_XACNHAN;
                            _connect.UpdateKey(khach);
                            await DatXeAsync(khach);
                        }
                        break;

                    case (int)ENActionReceive.END:
                        taixe.Status = (int)ENTrangThaiUser.RANH;
                        taixe.TargetId = string.Empty;
                        khach.Status = (int)ENTrangThaiUser.RANH;
                        khach.TargetId = string.Empty;
                        khach.TaiXes = null;

                        _connect.UpdateKey(taixe);
                        _connect.UpdateKey(khach);

                        await SendMessageAsync(taixe, (int)ENActionSend.KET_THUC, khach.Name);
                        await SendMessageAsync(khach, (int)ENActionSend.KET_THUC, taixe.Name);
                        await _cache.RemoveAsync("TX#" + taixe.Id);
                        await _cache.RemoveAsync("K#" + khach.Id);
                        break;

                    case (int)ENActionReceive.OK:
                        if (khach.TargetId.Equals(taixe.Id))
                        {
                            var socketnguoidat = _connect.GetSocketById(khach);
                            if (socketnguoidat == null)
                            {
                                await SendMessageAsync(socket, (int)ENActionSend.POPUP, khach.Name + " đã hủy hoặc mất kết nối");
                            }
                            else
                            {
                                await SendMessageAsync(socketnguoidat, (int)ENActionSend.BAT_DAU, taixe.Name);
                                await SendMessageAsync(socket, (int)ENActionSend.BAT_DAU, khach.Name);

                                taixe.Status = (int)ENTrangThaiUser.DANG_CHAY;
                                khach.Status = (int)ENTrangThaiUser.DANG_CHAY;

                                _connect.UpdateKey(taixe);
                                _connect.UpdateKey(khach);

                                await _cache.SetStringAsync("TX#" + taixe.Id, khach.Id);
                                await _cache.SetStringAsync("K#" + khach.Id, taixe.Id);
                            }
                        }
                        break;

                    case (int)ENActionReceive.CANCEL:
                        if (taixe.Status == (int)ENTrangThaiUser.DANG_XACNHAN && khach.TargetId.Equals(taixe.Id))
                        {
                            await NextTaiXeAsync(khach);
                            await SendMessageAsync(socket, (int)ENActionSend.POPUP, "Đã từ trối");
                        }
                        break;

                    case (int)ENActionReceive.HUY_DAT:
                        if (taixe != null)
                        {
                            taixe.Status = (int)ENTrangThaiUser.RANH;
                            taixe.TargetId = string.Empty;
                            _connect.UpdateKey(taixe);
                        }

                        khach.Status = (int)ENTrangThaiUser.RANH;
                        khach.TargetId = string.Empty;
                        khach.TaiXes = null;

                        _connect.UpdateKey(khach);
                        break;

                    case (int)ENActionReceive.CHAT:
                        if (string.IsNullOrEmpty(user.TargetId))
                            await SendMessageToAllAsync((int)ENActionSend.CHAT, user.Name + ": " + param.Text);
                        else
                            await SendMessageAsync(user.TargetId, (int)ENActionSend.CHAT, user.Name + ": " + param.Text);
                        break;

                    default:
                        await SendMessageToAllAsync((int)ENActionSend.CHAT, user.Name + ": " + param.Text);
                        break;
                }
            }
            catch
            {
                // ignored
            }
        }

        private async Task NextTaiXeAsync(Key khach)
        {
            if (khach?.Status == (int)ENTrangThaiUser.DANG_XACNHAN)
            {
                if (khach.TaiXes.Count == 0)
                {
                    await KhongCoTaiXe(khach);
                }
                else
                {
                    var taixe = _connect.GetKeyById(khach.TargetId);
                    taixe.TargetId = string.Empty;
                    taixe.Status = (int)ENTrangThaiUser.RANH;
                    _connect.UpdateKey(taixe);
                    await DatXeAsync(khach);
                }
            }
        }

        private async Task DatXeAsync(Key khach)
        {
            foreach (var item in khach.TaiXes)
            {
                var taixe = _connect.GetKeyById(item);
                if (taixe.Status == (int)ENTrangThaiUser.RANH)
                {
                    var laixe = _connect.GetSocketById(taixe);
                    if (laixe == null) continue;

                    await SendMessageAsync(laixe, (int)ENActionSend.XAC_NHAN, khach.Name);

                    khach.TaiXes.Remove(item);
                    khach.TargetId = item;

                    taixe.Status = (int)ENTrangThaiUser.DANG_XACNHAN;
                    taixe.TargetId = khach.Id;

                    _connect.UpdateKey(taixe);
                    _connect.UpdateKey(khach);
                    return;
                }
            }

            await KhongCoTaiXe(khach);
        }

        private async Task KhongCoTaiXe(Key khach)
        {
            await SendMessageAsync(khach, (int)ENActionSend.POPUP, "Không có tài xế");
            Key taixe = _connect.GetKeyById(khach.TargetId);
            if (taixe != null)
            {
                taixe.TargetId = string.Empty;
                taixe.Status = (int)ENTrangThaiUser.RANH;
                _connect.UpdateKey(taixe);
            }
            khach.TaiXes = null;
            khach.TargetId = string.Empty;
            khach.Status = (int)ENTrangThaiUser.RANH;
            _connect.UpdateKey(khach);
        }
    }
}