using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class RolesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}