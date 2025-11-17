using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using TrainingRequestApp.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;

namespace TrainingRequestApp.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // เพิ่มฟังก์ชันเข้ารหัสรหัสผ่าน SHA256 ตาม VB.NET
        private string EncryptPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);

                // แปลง Byte Array เป็น String Hex
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hash)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // 🔹 GET: หน้า Login
        [HttpGet]
        public IActionResult Index()
        {
            Console.WriteLine("🔵 Login Page Loaded");
            return View("Login");
        }

        // 🔹 POST: ทำการล็อกอิน
        [HttpPost]
        [ValidateAntiForgeryToken] // ✅ ป้องกัน CSRF
        public IActionResult Authenticate(LoginViewModel model)
        {
            try
            {
                Console.WriteLine("🟢 Authenticate() called!");

                if (!ModelState.IsValid)
                {
                    Console.WriteLine("🔴 ModelState Invalid!");
                    ViewBag.ErrorMessage = "❌ Please enter a valid UserID and Password.";
                    return View("Login", model);
                }

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // ✅ ค้นหาผู้ใช้จากตาราง Employees ตาม VB.NET (เพิ่ม Email)
                    string query = "SELECT UserID, Name, lastname, Status, UPassword, account_permissions, Company, Email FROM Employees WHERE UserID = @UserID";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", model.UserID);
                        SqlDataReader reader = command.ExecuteReader();

                        if (!reader.Read())
                        {
                            Console.WriteLine("🔴 User not found: " + model.UserID);
                            ViewBag.ErrorMessage = "❌ Invalid UserID or password.";
                            return View("Login", model);
                        }

                        // ✅ อ่านค่าจากตาราง Employees ตาม VB.NET (รวม Email)
                        string userID = reader["UserID"].ToString();
                        string firstName = reader["Name"].ToString();
                        string lastName = reader["lastname"].ToString();
                        string status = reader["Status"].ToString();
                        string storedPassword = reader["UPassword"].ToString();
                        string permissions = reader["account_permissions"].ToString();
                        string company = reader["Company"].ToString();
                        string email = reader["Email"]?.ToString() ?? "";

                        // ✅ ถ้าไม่มี Email ใช้ชื่อ-นามสกุลแทน
                        string displayName = !string.IsNullOrWhiteSpace(email) ? email : firstName + " " + lastName;

                        // ✅ เก็บชื่อและนามสกุลใน PEmail
                        model.PEmail = firstName + " " + lastName;

                        reader.Close();

                        // 🔍 Debug Information
                        Console.WriteLine("🟡 Debug Info:");
                        Console.WriteLine("   - UserID Input: " + model.UserID);
                        Console.WriteLine("   - Password Input: " + model.Password);
                        Console.WriteLine("   - DB UserID: " + userID);
                        Console.WriteLine("   - DB Name: " + firstName + " " + lastName);
                        Console.WriteLine("   - DB Email: " + (string.IsNullOrWhiteSpace(email) ? "NULL/EMPTY" : email));
                        Console.WriteLine("   - Display Name: " + displayName);
                        Console.WriteLine("   - DB Status: " + status);
                        Console.WriteLine("   - DB UPassword: " + (string.IsNullOrEmpty(storedPassword) ? "NULL/EMPTY" : "EXISTS"));
                        Console.WriteLine("   - DB Permissions: " + permissions);
                        Console.WriteLine("   - DB Company: " + company);

                        // ✅ ตรวจสอบสถานะและรหัสผ่านตาม VB.NET Logic
                        if (status == "Active")
                        {
                            // ถ้าสถานะเป็น Active และมีรหัสผ่าน
                            if (!string.IsNullOrWhiteSpace(storedPassword))
                            {
                                // เข้ารหัสรหัสผ่านที่ผู้ใช้ป้อนเพื่อเปรียบเทียบ
                                string encryptedInputPassword = EncryptPassword(model.Password.Trim());

                                if (encryptedInputPassword == storedPassword.Trim())
                                {
                                    // รหัสผ่านถูกต้อง เข้าสู่ระบบสำเร็จ
                                    Console.WriteLine("🟢 Login Successful: " + model.UserID + " (" + displayName + ")");

                                    // ✅ ตั้งค่า Session (ใช้ Email จริงๆ)
                                    HttpContext.Session.SetString("UserEmail", displayName);
                                    HttpContext.Session.SetString("UserRole", permissions);
                                    HttpContext.Session.SetString("UserId", userID);
                                    HttpContext.Session.SetString("Company", company);

                                    // ✅ Remember Me (ถ้าถูกเลือก)
                                    if (model.RememberMe)
                                    {
                                        Console.WriteLine("🟢 Remember Me Enabled");
                                        Response.Cookies.Append("UserEmail", displayName, new CookieOptions
                                        {
                                            Expires = DateTime.Now.AddDays(30),
                                            HttpOnly = true,
                                            Secure = true,
                                            SameSite = SameSiteMode.Strict
                                        });
                                    }

                                    // ✅ Redirect ไปตามสิทธิ์ของผู้ใช้
                                    return permissions.Contains("Admin")
                                        ? RedirectToAction("Index", "Home")
                                        : RedirectToAction("UserDashboard", "Dashboard");
                                }
                                else
                                {
                                    // รหัสผ่านไม่ถูกต้อง
                                    Console.WriteLine("🔴 Invalid password for user: " + model.UserID);
                                    ViewBag.ErrorMessage = "❌ รหัสผ่านไม่ถูกต้อง กรุณาลองใหม่อีกครั้ง";
                                    return View("Login", model);
                                }
                            }
                            else
                            {
                                // มีรหัสผู้ใช้แต่ไม่มีรหัสผ่าน
                                ViewBag.ErrorMessage = "❌ ไม่มีรหัสผ่านในระบบ กรุณาติดต่อผู้ดูแลระบบเพื่อลงทะเบียน";
                                return View("Login", model);
                            }
                        }
                        else if (status == "Register/Reset Password")
                        {
                            // ถ้าสถานะเป็น Register/Reset Password ให้แจ้งให้ตั้งรหัสผ่านใหม่
                            ViewBag.ErrorMessage = "❌ บัญชีนี้ต้องตั้งรหัสผ่านใหม่ กรุณาติดต่อผู้ดูแลระบบ";
                            return View("Login", model);
                        }
                        else
                        {
                            // สถานะอื่นๆ
                            ViewBag.ErrorMessage = "❌ บัญชีผู้ใช้นี้ไม่สามารถเข้าสู่ระบบได้ (สถานะ: " + status + ") กรุณาติดต่อผู้ดูแลระบบ";
                            return View("Login", model);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("🔴 Error in Authenticate(): " + ex.Message);
                ViewBag.ErrorMessage = "❌ An error occurred while processing your request.";
                return View("Login");
            }
        }

        // 🔹 GET: ออกจากระบบ
        [HttpGet]
        public IActionResult Logout()
        {
            Console.WriteLine("🟠 Logout Called!");
            HttpContext.Session.Clear();
            Response.Cookies.Delete("UserEmail");
            return RedirectToAction("Index", "Login");
        }
    }
}
