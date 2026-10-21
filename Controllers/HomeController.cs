using Giahuy0790.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Giahuy0790.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("kids")]
        public IActionResult Kids()
        {
            return View();
        }

        [HttpGet("tracking")]
        [HttpGet("gps")]
        public IActionResult Tracking()
        {
            return View();
        }

        [HttpGet("parental-control")]
        public IActionResult ParentalControl()
        {
            return View();
        }

        [HttpGet("activities")]
        public IActionResult Activities()
        {
            return View();
        }

        [HttpGet("screen-time")]
        [HttpGet("screentime")]
        public IActionResult ScreenTime()
        {
            return View();
        }

        [HttpGet("join-family")]
        public IActionResult JoinFamily()
        {
            return View();
        }

        [HttpGet("privacy")]
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
