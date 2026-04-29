using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class HolidaysController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}