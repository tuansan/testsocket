using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetSockets.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetSockets.Controllers
{
    public class HomeController : Controller
    {
        private readonly Random _random = new Random();

        public int RandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        public IActionResult Index(int id = 0)
        {
            if (id == 0)
                id = RandomNumber(1, 10000);
            return View(id);
        }

        public IActionResult Noti(string host)
        {
            return View();
        }
    }
}
