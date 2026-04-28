using Management.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Management.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Example API endpoint with role-based authorization
        [Authorize(Roles = "Admin")]
        [Route("api/admin-only")]
        public IActionResult AdminOnly()
        {
            return Json(new { message = "This endpoint is only accessible to Admin users" });
        }

        [Authorize(Roles = "HR,Admin")]
        [Route("api/hr-admin")]
        public IActionResult HrOrAdmin()
        {
            return Json(new { message = "This endpoint is accessible to HR and Admin users" });
        }

        [Authorize(Roles = "Employee,HR,Admin")]
        [Route("api/authenticated")]
        public IActionResult Authenticated()
        {
            return Json(new { message = "This endpoint is accessible to all authenticated users" });
        }
    }
}
