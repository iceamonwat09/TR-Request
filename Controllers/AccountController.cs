using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using TrainingRequestApp.Models;
using Microsoft.Extensions.Configuration;
using BCrypt.Net;
using System;
using Microsoft.AspNetCore.Builder;

namespace TrainingRequestApp.Controllers
{
    public class AccountController : Controller
    {


      


        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {



            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                Console.WriteLine("Connection String: " + connectionString);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("✅ Connected Successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Connection Error: " + ex.Message);
            }



            if (!ModelState.IsValid)
            {
                return View(user); // ถ้าข้อมูลไม่ถูกต้อง ให้กลับไปที่หน้า Register
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // ตรวจสอบว่า Account นี้มีอยู่ในระบบแล้วหรือไม่
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Account = @Account";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Account", user.Account);
                        int exists = (int)checkCommand.ExecuteScalar();

                        if (exists > 0)
                        {
                            ViewBag.ErrorMessage = "❌ Account already exists!";
                            return View(user);
                        }
                    }

                    // **ถ้าไม่มีบัญชีนี้ ให้บันทึกข้อมูลลงฐานข้อมูล**
                    string query = @"INSERT INTO Users 
                                    (EmployeeId, Account, PasswordHash, Prefix, FirstName, LastName, Department, Division, Gender, JD, Position, Status, Permission) 
                                    VALUES 
                                    (@EmployeeId, @Account, @PasswordHash, @Prefix, @FirstName, @LastName, @Department, @Division, @Gender, @JD, @Position, @Status, @Permission)";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeId", user.EmployeeId);
                        command.Parameters.AddWithValue("@Account", user.Account);

                        // ✅ **ใช้ user.Password และเข้ารหัสก่อนบันทึก**
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password);
                        command.Parameters.AddWithValue("@PasswordHash", hashedPassword);

                        command.Parameters.AddWithValue("@Prefix", user.Prefix ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@FirstName", user.FirstName);
                        command.Parameters.AddWithValue("@LastName", user.LastName);
                        command.Parameters.AddWithValue("@Department", user.Department ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Division", user.Division ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Gender", user.Gender ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@JD", user.JD ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Position", user.Position ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Status", user.Status ?? "Active");
                        command.Parameters.AddWithValue("@Permission", user.Permission ?? (object)DBNull.Value);
                        command.ExecuteNonQuery();
                    }
                }

                // ✅ เพิ่ม Log (Debugging)
                Console.WriteLine("✅ Registration successful for: " + user.Account);

                TempData["SuccessMessage"] = "🎉 Registration successful! Please login.";
                return RedirectToAction("Index", "Login"); // ✅ ต้องตรงกับ LoginController

            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error: " + ex.Message);
                ViewBag.ErrorMessage = "❌ An error occurred. Please try again.";
                return View(user);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
    }
}
