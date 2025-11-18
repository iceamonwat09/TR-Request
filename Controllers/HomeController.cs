using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace TrainingRequestApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
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

        // ====================================================================
        // 📊 DASHBOARD API ENDPOINTS
        // ====================================================================

        /// <summary>
        /// API: ดึงข้อมูล KPI Cards (4 หลัก)
        /// GET: /Home/GetDashboardSummary?year=2025&startDate=2025-01-01&endDate=2025-12-31&department=IT
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetDashboardSummary(int? year, DateTime? startDate, DateTime? endDate, string? department)
        {
            try
            {
                // Default ปีปัจจุบัน
                int selectedYear = year ?? DateTime.Now.Year;
                DateTime dateStart = startDate ?? new DateTime(selectedYear, 1, 1);
                DateTime dateEnd = endDate ?? new DateTime(selectedYear, 12, 31);

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // ✅ 1. Total Cost
                    string costQuery = @"
                        SELECT
                            ISNULL(SUM(TotalCost), 0) AS TotalCost,
                            COUNT(*) AS TotalRequests
                        FROM [TrainingRequests]
                        WHERE StartDate >= @StartDate AND StartDate <= @EndDate
                          AND Status IN ('APPROVED', 'COMPLETE')
                          AND IsActive = 1"
                        + (string.IsNullOrEmpty(department) ? "" : " AND Department = @Department");

                    decimal totalCost = 0;
                    int totalApproved = 0;

                    using (SqlCommand cmd = new SqlCommand(costQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", dateStart);
                        cmd.Parameters.AddWithValue("@EndDate", dateEnd);
                        if (!string.IsNullOrEmpty(department))
                            cmd.Parameters.AddWithValue("@Department", department);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                totalCost = reader.GetDecimal(0);
                                totalApproved = reader.GetInt32(1);
                            }
                        }
                    }

                    // ✅ 2. Total Quota
                    string quotaQuery = @"
                        SELECT ISNULL(SUM(Cost), 0) AS TotalQuota
                        FROM [TrainingRequest_Cost]
                        WHERE Year = @Year"
                        + (string.IsNullOrEmpty(department) ? "" : " AND Department = @Department");

                    decimal totalQuota = 0;

                    using (SqlCommand cmd = new SqlCommand(quotaQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Year", selectedYear.ToString());
                        if (!string.IsNullOrEmpty(department))
                            cmd.Parameters.AddWithValue("@Department", department);

                        var result = await cmd.ExecuteScalarAsync();
                        totalQuota = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                    }

                    // ✅ 3. Total Requests & Approval Rate
                    string requestsQuery = @"
                        SELECT
                            COUNT(*) AS TotalRequests,
                            SUM(CASE WHEN Status IN ('APPROVED', 'COMPLETE') THEN 1 ELSE 0 END) AS ApprovedCount
                        FROM [TrainingRequests]
                        WHERE StartDate >= @StartDate AND StartDate <= @EndDate
                          AND IsActive = 1"
                        + (string.IsNullOrEmpty(department) ? "" : " AND Department = @Department");

                    int totalRequests = 0;
                    int approvedCount = 0;

                    using (SqlCommand cmd = new SqlCommand(requestsQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", dateStart);
                        cmd.Parameters.AddWithValue("@EndDate", dateEnd);
                        if (!string.IsNullOrEmpty(department))
                            cmd.Parameters.AddWithValue("@Department", department);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                totalRequests = reader.GetInt32(0);
                                approvedCount = reader.GetInt32(1);
                            }
                        }
                    }

                    // คำนวณ %
                    decimal budgetUsagePercent = totalQuota > 0 ? (totalCost / totalQuota * 100) : 0;
                    decimal approvalRate = totalRequests > 0 ? ((decimal)approvedCount / totalRequests * 100) : 0;

                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            totalCost = totalCost,
                            totalCostFormatted = totalCost.ToString("N0"),
                            budgetUsagePercent = Math.Round(budgetUsagePercent, 1),
                            totalQuota = totalQuota,
                            totalQuotaFormatted = totalQuota.ToString("N0"),
                            remaining = totalQuota - totalCost,
                            remainingFormatted = (totalQuota - totalCost).ToString("N0"),
                            totalRequests = totalRequests,
                            approvedCount = approvedCount,
                            approvalRate = Math.Round(approvalRate, 1)
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetDashboardSummary: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: ดึงข้อมูลงบประมาณแต่ละฝ่าย (Bar Chart)
        /// GET: /Home/GetCostByDepartment?year=2025
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCostByDepartment(int? year, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                int selectedYear = year ?? DateTime.Now.Year;
                DateTime dateStart = startDate ?? new DateTime(selectedYear, 1, 1);
                DateTime dateEnd = endDate ?? new DateTime(selectedYear, 12, 31);

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT
                            qc.Department,
                            ISNULL(SUM(tr.TotalCost), 0) AS TotalUsed,
                            qc.Cost AS Quota,
                            (qc.Cost - ISNULL(SUM(tr.TotalCost), 0)) AS Remaining,
                            CASE
                                WHEN qc.Cost > 0 THEN (ISNULL(SUM(tr.TotalCost), 0) / qc.Cost * 100)
                                ELSE 0
                            END AS UsagePercent
                        FROM [TrainingRequest_Cost] qc
                        LEFT JOIN [TrainingRequests] tr
                            ON tr.Department = qc.Department
                            AND tr.StartDate >= @StartDate
                            AND tr.StartDate <= @EndDate
                            AND tr.Status IN ('APPROVED', 'COMPLETE')
                            AND tr.IsActive = 1
                        WHERE qc.Year = @Year
                        GROUP BY qc.Department, qc.Cost
                        ORDER BY UsagePercent DESC";

                    var result = new List<object>();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Year", selectedYear.ToString());
                        cmd.Parameters.AddWithValue("@StartDate", dateStart);
                        cmd.Parameters.AddWithValue("@EndDate", dateEnd);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new
                                {
                                    department = reader["Department"].ToString(),
                                    totalUsed = reader.GetDecimal(reader.GetOrdinal("TotalUsed")),
                                    quota = reader.GetDecimal(reader.GetOrdinal("Quota")),
                                    remaining = reader.GetDecimal(reader.GetOrdinal("Remaining")),
                                    usagePercent = Math.Round(reader.GetDecimal(reader.GetOrdinal("UsagePercent")), 1)
                                });
                            }
                        }
                    }

                    return Json(new { success = true, data = result });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetCostByDepartment: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: ดึงข้อมูลสถานะเอกสาร (Donut Chart)
        /// GET: /Home/GetStatusDistribution?year=2025
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStatusDistribution(int? year, DateTime? startDate, DateTime? endDate, string? department)
        {
            try
            {
                int selectedYear = year ?? DateTime.Now.Year;
                DateTime dateStart = startDate ?? new DateTime(selectedYear, 1, 1);
                DateTime dateEnd = endDate ?? new DateTime(selectedYear, 12, 31);

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT
                            Status,
                            COUNT(*) AS Total
                        FROM [TrainingRequests]
                        WHERE StartDate >= @StartDate AND StartDate <= @EndDate
                          AND IsActive = 1"
                        + (string.IsNullOrEmpty(department) ? "" : " AND Department = @Department")
                        + @"
                        GROUP BY Status
                        ORDER BY Total DESC";

                    var result = new List<object>();
                    int totalCount = 0;

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", dateStart);
                        cmd.Parameters.AddWithValue("@EndDate", dateEnd);
                        if (!string.IsNullOrEmpty(department))
                            cmd.Parameters.AddWithValue("@Department", department);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int count = reader.GetInt32(1);
                                totalCount += count;
                                result.Add(new
                                {
                                    status = reader["Status"].ToString(),
                                    count = count
                                });
                            }
                        }
                    }

                    // คำนวณ %
                    var resultWithPercent = result.Select(r =>
                    {
                        dynamic item = r;
                        return new
                        {
                            status = item.status,
                            count = item.count,
                            percentage = totalCount > 0 ? Math.Round((decimal)item.count / totalCount * 100, 1) : 0
                        };
                    }).ToList();

                    return Json(new { success = true, data = resultWithPercent });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetStatusDistribution: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: ดึงข้อมูล Trend รายเดือน (Line Chart)
        /// GET: /Home/GetMonthlyTrend?year=2025
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMonthlyTrend(int? year, string? department)
        {
            try
            {
                int selectedYear = year ?? DateTime.Now.Year;

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT
                            MONTH(StartDate) AS Month,
                            COUNT(*) AS TotalRequests,
                            ISNULL(SUM(TotalCost), 0) AS TotalCost
                        FROM [TrainingRequests]
                        WHERE YEAR(StartDate) = @Year
                          AND IsActive = 1"
                        + (string.IsNullOrEmpty(department) ? "" : " AND Department = @Department")
                        + @"
                        GROUP BY MONTH(StartDate)
                        ORDER BY MONTH(StartDate)";

                    var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                    var requests = new int[12];
                    var costs = new decimal[12];

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Year", selectedYear);
                        if (!string.IsNullOrEmpty(department))
                            cmd.Parameters.AddWithValue("@Department", department);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int month = reader.GetInt32(0) - 1; // 0-indexed
                                requests[month] = reader.GetInt32(1);
                                costs[month] = reader.GetDecimal(2);
                            }
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            months = months,
                            requests = requests,
                            costs = costs
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetMonthlyTrend: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}
