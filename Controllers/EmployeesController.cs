using Microsoft.AspNetCore.Mvc;

namespace Management.Controllers
{
    public class EmployeesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}