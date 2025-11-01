using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;

namespace TrainingRequestApp.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // ตรวจสอบว่าผู้ใช้ล็อกอินหรือไม่
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Index", "Login"); // ถ้าไม่มี Session ให้กลับไปหน้า Login
            }

            ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            Console.WriteLine("🟢 Redirected to Home/Dashboard");

            return View();
        }
        // Controllers/HomeController.cs - เพิ่ม Action ใหม่

        [HttpGet]
        public IActionResult MonthlyRequests()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Index", "Login");
            }

            ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserRole = HttpContext.Session.GetString("UserRole");

            return View();
        }
    }
}
