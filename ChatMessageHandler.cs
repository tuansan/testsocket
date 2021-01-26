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
        private readonly IDistributedCache _cache;
        private readonly Random _random = new Random();

        public ChatMessageHandler(ConnectionManager webSocketConnectionManager, IDistributedCache cache) : base(webSocketConnectionManager)
        {
            _cache = cache;
        }

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public override async Task OnDisconnected(WebSocket socket)
        {
            var socketId = WebSocketConnectionManager.GetId(socket);
            await base.OnDisconnected(socket);
            if (!string.IsNullOrEmpty(socketId))
            {
                await SendMessageToAllAsync($"{socketId}: Disconnected");
            }
            string nguoidatxe = _cache.GetString("TaiXe#" + socketId);
            if (!string.IsNullOrEmpty(nguoidatxe))
            {
                await NextTaiXeAsync(nguoidatxe);
            }
        }

        public override async Task ReceiveAsync(WebSocket socket, WebSocketReceiveResult result, byte[] buffer)
        {
            try
            {
                var socketId = WebSocketConnectionManager.GetId(socket);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var param = JsonConvert.DeserializeObject<SocketModel>(message);
                string res = string.Empty;
                string taixe = string.Empty;
                string nguoidat = string.Empty;

                switch (param.Action)
                {
                    case "DatXe":
                        var list = WebSocketConnectionManager.GetAllLaiXe();
                        var check = _cache.GetString("NguoiDat#" + socketId);
                        if (string.IsNullOrEmpty(check))
                            check = _cache.GetString("NguoiDatDangChay#" + socketId);
                        if (string.IsNullOrEmpty(check))
                            await DatXeAsync(list, socketId);
                        break;

                    case "KetThuc":
                        nguoidat = _cache.GetString("TaiXeDangChay#" + socketId);
                        await _cache.RemoveAsync("TaiXeDangChay#" + socketId);
                        await _cache.RemoveAsync("NguoiDatDangChay#" + nguoidat);
                        break;

                    case "Ok":
                        nguoidat = _cache.GetString("TaiXe#" + socketId);
                        taixe = _cache.GetString("NguoiDat#" + nguoidat);
                        if (taixe.Equals(socketId))
                        {
                            var socketnguoidat = WebSocketConnectionManager.GetSocketById(nguoidat);
                            if (socketnguoidat == null)
                            {
                                await SendMessageAsync(socket, "Chat||" + "Nguoi dat da disconnected");
                            }
                            else
                            {
                                await SendMessageAsync(socketnguoidat, "Chat||" + taixe + " da Ok");
                                await SendMessageAsync(socket, "BatDau||" + "Da xac nhan don " + nguoidat);
                                await _cache.RemoveAsync("NguoiDat#" + nguoidat);
                                await _cache.RemoveAsync("TaiXe#" + taixe);
                                await _cache.SetStringAsync("TaiXeDangChay#" + taixe, nguoidat);
                                await _cache.SetStringAsync("NguoiDatDangChay#" + nguoidat, taixe);
                                await SendMessageToAllAsync("---------------------------------");
                            }
                        }
                        break;

                    case "Cancel":
                        nguoidat = _cache.GetString("TaiXe#" + socketId);
                        taixe = _cache.GetString("NguoiDat#" + nguoidat);
                        if (taixe.Equals(socketId))
                        {
                            await NextTaiXeAsync(nguoidat);
                            await SendMessageAsync(socket, "---------------------------------");
                        }
                        break;

                    default:
                        res = "Chat||" + socketId + ": " + param.Text;
                        await SendMessageToAllAsync(res);
                        break;
                }
            }
            catch { }
        }

        public async Task NextTaiXeAsync(string key)
        {
            string ListTaiXe = _cache.GetString("ListTaiXe#" + key);
            if (string.IsNullOrEmpty(ListTaiXe))
            {
                await KhongCoTaiXe(key);
            }
            else
            {
                var list = ListTaiXe.Split(",").ToList();
                var taixe = _cache.GetString("NguoiDat#" + key);
                await _cache.RemoveAsync("TaiXe#" + taixe);
                await DatXeAsync(list, key);
            }
        }

        private async Task DatXeAsync(IList<string> list, string key)
        {
            string taixe = string.Empty;
            for (int i = 0; i < list.Count; i++)
            {
                string tx = _cache.GetString("TaiXe#" + list[i]);
                if (string.IsNullOrEmpty(tx))
                {
                    tx = _cache.GetString("TaiXeDangChay#" + list[i]);
                }
                if (string.IsNullOrEmpty(tx))
                {
                    var laixe = WebSocketConnectionManager.GetSocketById(list[i]);
                    if (laixe == null) continue;
                    taixe = list[i];
                    await RemoveItemListTaiXe(list, taixe, key);
                    await SendMessageAsync(laixe, "DatXe||");
                    await _cache.SetStringAsync("TaiXe#" + taixe, key);
                    await _cache.SetStringAsync("NguoiDat#" + key, taixe);
                    break;
                }
            }
            if (string.IsNullOrEmpty(taixe))
            {
                await KhongCoTaiXe(key);
            }
        }

        private async Task KhongCoTaiXe(string key)
        {
            await SendMessageAsync(key, "Chat|| Khong con tai xe");
            string taixe = _cache.GetString("NguoiDat#" + key);
            await _cache.RemoveAsync("ListTaiXe#" + key);
            await _cache.RemoveAsync("NguoiDat#" + key);
            await _cache.RemoveAsync("TaiXe#" + taixe);
        }

        private async Task RemoveItemListTaiXe(IList<string> list, string item, string key)
        {
            int index = list.IndexOf(item);
            if (index > -1)
            {
                list.RemoveAt(index);
                await _cache.SetStringAsync("ListTaiXe#" + key, string.Join(",", list));
            }
        }
    }
}