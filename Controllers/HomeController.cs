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

        public IActionResult Index(int id = 0)
        {
            return View(id);
        }

        public IActionResult Noti(string host)
        {
            return View();
        }
    }
}
