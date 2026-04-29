using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class HRController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
