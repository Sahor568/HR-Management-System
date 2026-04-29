using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}