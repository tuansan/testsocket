using Microsoft.Extensions.Caching.Distributed;
using NetSockets.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace NetSockets
{
    public class ChatMessageHandler : WebSocketHandler
    {
        private readonly IDistributedCache _cachessss;
        private readonly Random _random = new Random();

        public ChatMessageHandler(ConnectionManager webSocketConnectionManager) : base(webSocketConnectionManager)
        {
        }

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var key = WebSocketConnectionManager.GetId(socket);
            await base.OnDisconnected(socket);
            if (key != null)
            {
                await SendMessageToAllAsync($"{key.Name}: Disconnected");
            }
            if (key.Role == (int)ENVaiTro.TaiXe && key.Status != (int)ENTrangThaiTaiXe.RANH)
            {
                await NextTaiXeAsync(WebSocketConnectionManager.GetKeyById(key.TargetId));
            }
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            try
            {
                var user = WebSocketConnectionManager.GetId(socket);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var param = JsonConvert.DeserializeObject<SocketModel>(message);
                string res = string.Empty;
                Key khach = new Key();
                Key taixe = new Key();

                switch (param.Action)
                {
                    case "DatXe":
                        khach = user;

                        var list = WebSocketConnectionManager.GetAllLaiXe();
                        if (khach.Status == (int)ENTrangThaiKhach.RANH)
                        {
                            khach.TaiXes = list.Select(s => s.Id).ToList();
                            khach.Status = (int)ENTrangThaiKhach.DANG_YEUCAU;
                            WebSocketConnectionManager.UpdateKey(khach);
                            await DatXeAsync(khach);
                        }
                        break;

                    case "KetThuc":
                        taixe = user;
                        khach = WebSocketConnectionManager.GetKeyById(taixe.TargetId);

                        taixe.Status = (int)ENTrangThaiTaiXe.RANH;
                        khach.Status = (int)ENTrangThaiKhach.RANH;

                        WebSocketConnectionManager.UpdateKey(taixe);
                        WebSocketConnectionManager.UpdateKey(khach);

                        await SendMessageToAllAsync("---------------------------------");
                        break;

                    case "Ok":
                        taixe = user;
                        khach = WebSocketConnectionManager.GetKeyById(taixe.TargetId);

                        if (khach.TargetId.Equals(taixe.Id))
                        {
                            var socketnguoidat = WebSocketConnectionManager.GetSocketById(khach);
                            if (socketnguoidat == null)
                            {
                                await SendMessageAsync(socket, "Chat||" + "Nguoi dat da disconnected");
                            }
                            else
                            {
                                await SendMessageAsync(socketnguoidat, "Chat||" + taixe.Name + " da Ok");
                                await SendMessageAsync(socket, "BatDau||" + "Da xac nhan don " + khach.Name);

                                taixe.Status = (int)ENTrangThaiTaiXe.DANG_CHAY;
                                khach.Status = (int)ENTrangThaiKhach.DANG_CHAY;

                                WebSocketConnectionManager.UpdateKey(taixe);
                                WebSocketConnectionManager.UpdateKey(khach);
                                await SendMessageToAllAsync("---------------------------------");
                            }
                        }
                        break;

                    case "Cancel":
                        taixe = user;
                        khach = WebSocketConnectionManager.GetKeyById(taixe.TargetId);
                        if (khach.TargetId.Equals(taixe.Id))
                        {
                            await NextTaiXeAsync(khach);
                            await SendMessageAsync(socket, "---------------------------------");
                        }
                        break;

                    default:
                        res = "Chat||" + user.Name + ": " + param.Text;
                        await SendMessageToAllAsync(res);
                        break;
                }
            }
            catch { }
        }

        public async Task NextTaiXeAsync(Key khach)
        {
            if (khach.TaiXes.Count == 0)
            {
                await KhongCoTaiXe(khach);
            }
            else
            {
                var taixe = WebSocketConnectionManager.GetKeyById(khach.TargetId);
                taixe.TargetId = string.Empty;
                taixe.Status = (int)ENTrangThaiTaiXe.RANH;
                WebSocketConnectionManager.UpdateKey(taixe);
                await DatXeAsync(khach);
            }
        }

        private async Task DatXeAsync(Key khach)
        {
            foreach (var item in khach.TaiXes)
            {
                var taixe = WebSocketConnectionManager.GetKeyById(item);
                if(taixe.Status == (int)ENTrangThaiTaiXe.RANH)
                {
                    var laixe = WebSocketConnectionManager.GetSocketById(taixe);
                    if (laixe == null) continue;

                    await SendMessageAsync(laixe, "DatXe||");

                    khach.TaiXes.Remove(item);
                    khach.TargetId = item;

                    taixe.Status = (int)ENTrangThaiTaiXe.DANG_XACNHAN;
                    taixe.TargetId = khach.Id;

                    WebSocketConnectionManager.UpdateKey(taixe);
                    WebSocketConnectionManager.UpdateKey(khach);
                    return;
                }
            }
            
            await KhongCoTaiXe(khach);
        }

        private async Task KhongCoTaiXe(Key khach)
        {
            await SendMessageAsync(khach, "Chat|| Khong con tai xe");
            Key taixe = WebSocketConnectionManager.GetKeyById(khach.TargetId);
            if (taixe != null)
            {
                taixe.TargetId = string.Empty;
                taixe.Status = (int)ENTrangThaiTaiXe.RANH;
                WebSocketConnectionManager.UpdateKey(taixe);
            }
            khach.TaiXes = null;
            khach.TargetId = string.Empty;
            khach.Status = (int)ENTrangThaiKhach.RANH;
            WebSocketConnectionManager.UpdateKey(khach);
        }
    }
}