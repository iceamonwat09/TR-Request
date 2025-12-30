using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using TrainingRequestApp.Models;

namespace TrainingRequestApp.Controllers
{
    public class QuotaManagementController : Controller
    {
        private readonly IConfiguration _configuration;

        public QuotaManagementController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET: QuotaManagement/Index
        public IActionResult Index(string yearFilter = null)
        {
            if (string.IsNullOrEmpty(yearFilter))
            {
                yearFilter = DateTime.Now.Year.ToString();
            }

            ViewBag.SelectedYear = yearFilter;
            ViewBag.Years = GetYearList();

            var quotas = GetQuotasByYear(yearFilter);
            return View(quotas);
        }

        // GET: QuotaManagement/Create
        public IActionResult Create()
        {
            ViewBag.Departments = GetDepartments();
            ViewBag.Years = GetYearList();
            return View();
        }

        // POST: QuotaManagement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TrainingRequestCost model)
        {
            // 🔍 DEBUG: Log incoming data
            Console.WriteLine("=== QuotaManagement Create POST ===");
            Console.WriteLine($"Department: [{model.Department}]");
            Console.WriteLine($"Year: [{model.Year}]");
            Console.WriteLine($"Qhours: [{model.Qhours}]");
            Console.WriteLine($"Cost: [{model.Cost}]");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");

            // 🔍 DEBUG: Log validation errors if any
            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ ModelState Validation Errors:");
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key]?.Errors;
                    if (errors != null && errors.Count > 0)
                    {
                        foreach (var error in errors)
                        {
                            Console.WriteLine($"  - {key}: {error.ErrorMessage}");
                        }
                    }
                }
            }

            if (ModelState.IsValid)
            {
                // ตรวจสอบว่ามีข้อมูลซ้ำหรือไม่
                if (IsDuplicateQuota(model.Department, model.Year, 0))
                {
                    ModelState.AddModelError("", $"มีข้อมูลโควต้าสำหรับฝ่าย {model.Department} ปี {model.Year} อยู่แล้ว");
                    ViewBag.Departments = GetDepartments();
                    ViewBag.Years = GetYearList();
                    return View(model);
                }

                // บันทึกข้อมูลพร้อม Error Handling & Transaction
                string userName = HttpContext.Session.GetString("UserEmail") ?? "System";
                model.CreatedBy = userName;

                // เตรียมข้อมูล ModifyBy (สร้างครั้งแรก)
                string currentDate = DateTime.Now.ToString("dd/MM/yyyy");
                string currentTime = DateTime.Now.ToString("HH:mm");
                string modifyBy = $"{userName} / {currentDate} / {currentTime}";
                model.ModifyBy = modifyBy;

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (SqlTransaction transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                string query = @"
                                    INSERT INTO [HRDSYSTEM].[dbo].[TrainingRequest_Cost]
                                    (Department, Year, Qhours, Cost, CreatedBy, ModifyBy)
                                    VALUES (@Department, @Year, @Qhours, @Cost, @CreatedBy, @ModifyBy)";

                                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@Department", model.Department);
                                    command.Parameters.AddWithValue("@Year", model.Year);
                                    command.Parameters.AddWithValue("@Qhours", model.Qhours);
                                    command.Parameters.AddWithValue("@Cost", model.Cost);
                                    command.Parameters.AddWithValue("@CreatedBy", (object?)model.CreatedBy ?? DBNull.Value);
                                    command.Parameters.AddWithValue("@ModifyBy", (object?)model.ModifyBy ?? DBNull.Value);

                                    int rowsAffected = command.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        transaction.Commit();
                                        Console.WriteLine($"[QuotaManagement] Created quota - Dept: {model.Department}, Year: {model.Year}, By: {userName}");
                                        TempData["SuccessMessage"] = "เพิ่มข้อมูลโควต้าเรียบร้อยแล้ว";
                                        return RedirectToAction(nameof(Index), new { yearFilter = model.Year });
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        ModelState.AddModelError("", "ไม่สามารถบันทึกข้อมูลได้ กรุณาลองใหม่อีกครั้ง");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                Console.WriteLine($"[QuotaManagement] Transaction Error: {ex.Message}");
                                throw;
                            }
                        }
                    }
                }
                catch (SqlException sqlEx)
                {
                    Console.WriteLine($"[QuotaManagement] SQL Error: {sqlEx.Message} | Number: {sqlEx.Number}");
                    ModelState.AddModelError("", $"เกิดข้อผิดพลาดในการเชื่อมต่อฐานข้อมูล: {sqlEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[QuotaManagement] Unexpected Error: {ex.Message}");
                    ModelState.AddModelError("", $"เกิดข้อผิดพลาด: {ex.Message}");
                }
            }

            ViewBag.Departments = GetDepartments();
            ViewBag.Years = GetYearList();
            return View(model);
        }

        // GET: QuotaManagement/Edit/5
        public IActionResult Edit(int id)
        {
            var quota = GetQuotaById(id);
            if (quota == null)
            {
                return NotFound();
            }

            ViewBag.Departments = GetDepartments();
            ViewBag.Years = GetYearList();
            return View(quota);
        }

        // POST: QuotaManagement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, TrainingRequestCost model)
        {
            if (id != model.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // ตรวจสอบว่ามีข้อมูลซ้ำหรือไม่ (ยกเว้น record ปัจจุบัน)
                if (IsDuplicateQuota(model.Department, model.Year, id))
                {
                    ModelState.AddModelError("", $"มีข้อมูลโควต้าสำหรับฝ่าย {model.Department} ปี {model.Year} อยู่แล้ว");
                    ViewBag.Departments = GetDepartments();
                    ViewBag.Years = GetYearList();
                    return View(model);
                }

                // เตรียมข้อมูล ModifyBy
                string userEmail = HttpContext.Session.GetString("UserEmail") ?? "System";
                string currentDate = DateTime.Now.ToString("dd/MM/yyyy");
                string currentTime = DateTime.Now.ToString("HH:mm");
                string modifyBy = $"{userEmail} / {currentDate} / {currentTime}";

                // อัพเดตข้อมูลพร้อม Error Handling & Transaction
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                try
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        using (SqlTransaction transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                string query = @"
                                    UPDATE [HRDSYSTEM].[dbo].[TrainingRequest_Cost]
                                    SET Department = @Department,
                                        Year = @Year,
                                        Qhours = @Qhours,
                                        Cost = @Cost,
                                        ModifyBy = @ModifyBy
                                    WHERE ID = @ID";

                                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@ID", id);
                                    command.Parameters.AddWithValue("@Department", model.Department);
                                    command.Parameters.AddWithValue("@Year", model.Year);
                                    command.Parameters.AddWithValue("@Qhours", model.Qhours);
                                    command.Parameters.AddWithValue("@Cost", model.Cost);
                                    command.Parameters.AddWithValue("@ModifyBy", modifyBy);

                                    int rowsAffected = command.ExecuteNonQuery();

                                    if (rowsAffected > 0)
                                    {
                                        transaction.Commit();
                                        Console.WriteLine($"[QuotaManagement] Updated quota ID: {id} - Dept: {model.Department}, Year: {model.Year}");
                                        TempData["SuccessMessage"] = "อัพเดตข้อมูลโควต้าเรียบร้อยแล้ว";
                                        return RedirectToAction(nameof(Index), new { yearFilter = model.Year });
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        ModelState.AddModelError("", "ไม่พบข้อมูลที่ต้องการแก้ไข");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                Console.WriteLine($"[QuotaManagement] Transaction Error: {ex.Message}");
                                throw;
                            }
                        }
                    }
                }
                catch (SqlException sqlEx)
                {
                    Console.WriteLine($"[QuotaManagement] SQL Error: {sqlEx.Message} | Number: {sqlEx.Number}");
                    ModelState.AddModelError("", $"เกิดข้อผิดพลาดในการเชื่อมต่อฐานข้อมูล: {sqlEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[QuotaManagement] Unexpected Error: {ex.Message}");
                    ModelState.AddModelError("", $"เกิดข้อผิดพลาด: {ex.Message}");
                }
            }

            ViewBag.Departments = GetDepartments();
            ViewBag.Years = GetYearList();
            return View(model);
        }

        // POST: QuotaManagement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id, string yearFilter)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM [HRDSYSTEM].[dbo].[TrainingRequest_Cost] WHERE ID = @ID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ID", id);
                    command.ExecuteNonQuery();
                }
            }

            TempData["SuccessMessage"] = "ลบข้อมูลโควต้าเรียบร้อยแล้ว";
            return RedirectToAction(nameof(Index), new { yearFilter });
        }

        // Helper Methods
        private List<TrainingRequestCost> GetQuotasByYear(string year)
        {
            var quotas = new List<TrainingRequestCost>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT ID, Department, Year, Qhours, Cost
                    FROM [HRDSYSTEM].[dbo].[TrainingRequest_Cost]
                    WHERE Year = @Year
                    ORDER BY Department";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Year", year);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            quotas.Add(new TrainingRequestCost
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                Department = reader["Department"].ToString() ?? "",
                                Year = reader["Year"].ToString() ?? "",
                                Qhours = Convert.ToInt32(reader["Qhours"]),
                                Cost = Convert.ToDecimal(reader["Cost"])
                            });
                        }
                    }
                }
            }

            return quotas;
        }

        private TrainingRequestCost? GetQuotaById(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT ID, Department, Year, Qhours, Cost, CreatedBy, ModifyBy
                    FROM [HRDSYSTEM].[dbo].[TrainingRequest_Cost]
                    WHERE ID = @ID";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ID", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new TrainingRequestCost
                            {
                                ID = Convert.ToInt32(reader["ID"]),
                                Department = reader["Department"].ToString() ?? "",
                                Year = reader["Year"].ToString() ?? "",
                                Qhours = Convert.ToInt32(reader["Qhours"]),
                                Cost = Convert.ToDecimal(reader["Cost"]),
                                CreatedBy = reader["CreatedBy"]?.ToString(),
                                ModifyBy = reader["ModifyBy"]?.ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }

        private List<string> GetDepartments()
        {
            var departments = new List<string>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT DISTINCT Department
                    FROM Employees
                    WHERE Department IS NOT NULL
                    AND LTRIM(RTRIM(Department)) != ''
                    ORDER BY Department";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dept = reader["Department"].ToString()?.Trim() ?? "";
                            if (!string.IsNullOrWhiteSpace(dept))
                            {
                                departments.Add(dept);
                            }
                        }
                    }
                }
            }

            return departments;
        }

        private List<string> GetYearList()
        {
            var years = new List<string>();
            for (int year = 2025; year <= 2050; year++)
            {
                years.Add(year.ToString());
            }
            return years;
        }

        private bool IsDuplicateQuota(string department, string year, int excludeId)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
                    SELECT COUNT(*)
                    FROM [HRDSYSTEM].[dbo].[TrainingRequest_Cost]
                    WHERE Department = @Department
                    AND Year = @Year
                    AND ID != @ExcludeId";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Department", department);
                    command.Parameters.AddWithValue("@Year", year);
                    command.Parameters.AddWithValue("@ExcludeId", excludeId);

                    int count = Convert.ToInt32(command.ExecuteScalar());
                    return count > 0;
                }
            }
        }
    }
}
