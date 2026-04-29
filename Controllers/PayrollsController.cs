using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class PayrollsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}