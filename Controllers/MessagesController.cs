using Microsoft.AspNetCore.Mvc;
using System;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace NetSockets.Controllers
{
    public class MessagesController : Controller
    {
        private MessageHandler _chatMessageHandler { get; set; }

        public MessagesController(MessageHandler chatMessageHandler)
        {
            _chatMessageHandler = chatMessageHandler;
        }

        [HttpGet]
        public async Task SendMessage([FromQueryAttribute] string message)
        {
            await _chatMessageHandler.SendMessageToAllAsync(0, message);
        }
    }
}
