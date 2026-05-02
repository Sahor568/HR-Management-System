using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    [Authorize]
    public class HRController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
