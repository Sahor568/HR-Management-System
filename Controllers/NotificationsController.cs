using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class NotificationsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
