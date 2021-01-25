using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace NetSockets.Controllers
{
    public class MessagesController : Controller
    {
        private ChatMessageHandler _chatMessageHandler { get; set; }

        public MessagesController(ChatMessageHandler chatMessageHandler)
        {
            _chatMessageHandler = chatMessageHandler;
        }

        [HttpGet]
        public async Task SendMessage([FromQueryAttribute] string message)
        {
            await _chatMessageHandler.SendMessageToAllAsync(message);
        }
    }
}
