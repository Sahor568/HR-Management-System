using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class DepartmentsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}