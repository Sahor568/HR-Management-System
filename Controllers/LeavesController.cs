using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class LeavesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}