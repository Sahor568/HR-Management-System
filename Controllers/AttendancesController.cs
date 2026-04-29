using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class AttendancesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}