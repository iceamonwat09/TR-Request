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
                          AND Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED')
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
                            SUM(CASE WHEN Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED') THEN 1 ELSE 0 END) AS ApprovedCount
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
        /// API: ดึงข้อมูลงบประมาณแต่ละฝ่าย (Bar Chart) พร้อมยอดรออนุมัติ
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
                            ISNULL(SUM(CASE WHEN tr.Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED') THEN tr.TotalCost ELSE 0 END), 0) AS TotalUsed,
                            ISNULL(SUM(CASE WHEN tr.Status NOT IN ('APPROVED', 'COMPLETE', 'RESCHEDULED') THEN tr.TotalCost ELSE 0 END), 0) AS PendingAmount,
                            qc.Cost AS Quota,
                            (qc.Cost - ISNULL(SUM(CASE WHEN tr.Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED') THEN tr.TotalCost ELSE 0 END), 0)) AS Remaining,
                            CASE
                                WHEN qc.Cost > 0 THEN (ISNULL(SUM(CASE WHEN tr.Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED') THEN tr.TotalCost ELSE 0 END), 0) / qc.Cost * 100)
                                ELSE 0
                            END AS UsagePercent
                        FROM [TrainingRequest_Cost] qc
                        LEFT JOIN [TrainingRequests] tr
                            ON tr.Department = qc.Department
                            AND tr.StartDate >= @StartDate
                            AND tr.StartDate <= @EndDate
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
                                    pendingAmount = reader.GetDecimal(reader.GetOrdinal("PendingAmount")),
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

        /// <summary>
        /// API: ดึงข้อมูลงบประมาณแยกตามฝ่ายและประเภท (Public vs In House)
        /// GET: /Home/GetCostByDepartmentAndType?year=2025
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCostByDepartmentAndType(int? year, DateTime? startDate, DateTime? endDate)
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
                            ISNULL(SUM(CASE WHEN tr.TrainingType = 'Public' AND tr.Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED') THEN tr.TotalCost ELSE 0 END), 0) AS PublicCost,
                            ISNULL(SUM(CASE WHEN tr.TrainingType = 'In House' AND tr.Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED') THEN tr.TotalCost ELSE 0 END), 0) AS InHouseCost,
                            ISNULL(SUM(CASE WHEN tr.Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED') THEN tr.TotalCost ELSE 0 END), 0) AS TotalCost
                        FROM [TrainingRequest_Cost] qc
                        LEFT JOIN [TrainingRequests] tr
                            ON tr.Department = qc.Department
                            AND tr.StartDate >= @StartDate
                            AND tr.StartDate <= @EndDate
                            AND tr.IsActive = 1
                        WHERE qc.Year = @Year
                        GROUP BY qc.Department
                        ORDER BY qc.Department";

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
                                    publicCost = reader.GetDecimal(reader.GetOrdinal("PublicCost")),
                                    inHouseCost = reader.GetDecimal(reader.GetOrdinal("InHouseCost")),
                                    totalCost = reader.GetDecimal(reader.GetOrdinal("TotalCost"))
                                });
                            }
                        }
                    }

                    return Json(new { success = true, data = result });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetCostByDepartmentAndType: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: ดึงข้อมูลเปรียบเทียบ Training Type (Public vs In House)
        /// GET: /Home/GetCostByTrainingType?year=2025
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCostByTrainingType(int? year, DateTime? startDate, DateTime? endDate, string? department)
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

                    // Query สำหรับ Public Training
                    string publicQuery = @"
                        SELECT
                            COUNT(*) AS TotalRequests,
                            ISNULL(SUM(TotalCost), 0) AS TotalCost,
                            ISNULL(SUM(RegistrationCost), 0) AS RegistrationCost
                        FROM [TrainingRequests]
                        WHERE TrainingType = 'Public'
                          AND StartDate >= @StartDate AND StartDate <= @EndDate
                          AND Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED')
                          AND IsActive = 1"
                        + (string.IsNullOrEmpty(department) ? "" : " AND Department = @Department");

                    int publicCount = 0;
                    decimal publicTotalCost = 0;
                    decimal publicRegistrationCost = 0;

                    using (SqlCommand cmd = new SqlCommand(publicQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", dateStart);
                        cmd.Parameters.AddWithValue("@EndDate", dateEnd);
                        if (!string.IsNullOrEmpty(department))
                            cmd.Parameters.AddWithValue("@Department", department);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                publicCount = reader.GetInt32(0);
                                publicTotalCost = reader.GetDecimal(1);
                                publicRegistrationCost = reader.GetDecimal(2);
                            }
                        }
                    }

                    // Query สำหรับ In House Training
                    string inHouseQuery = @"
                        SELECT
                            COUNT(*) AS TotalRequests,
                            ISNULL(SUM(TotalCost), 0) AS TotalCost,
                            ISNULL(SUM(RegistrationCost), 0) AS RegistrationCost,
                            ISNULL(SUM(InstructorFee), 0) AS InstructorFee,
                            ISNULL(SUM(EquipmentCost), 0) AS EquipmentCost,
                            ISNULL(SUM(FoodCost), 0) AS FoodCost,
                            ISNULL(SUM(OtherCost), 0) AS OtherCost
                        FROM [TrainingRequests]
                        WHERE TrainingType = 'In House'
                          AND StartDate >= @StartDate AND StartDate <= @EndDate
                          AND Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED')
                          AND IsActive = 1"
                        + (string.IsNullOrEmpty(department) ? "" : " AND Department = @Department");

                    int inHouseCount = 0;
                    decimal inHouseTotalCost = 0;
                    decimal inHouseRegistrationCost = 0;
                    decimal inHouseInstructorFee = 0;
                    decimal inHouseEquipmentCost = 0;
                    decimal inHouseFoodCost = 0;
                    decimal inHouseOtherCost = 0;

                    using (SqlCommand cmd = new SqlCommand(inHouseQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", dateStart);
                        cmd.Parameters.AddWithValue("@EndDate", dateEnd);
                        if (!string.IsNullOrEmpty(department))
                            cmd.Parameters.AddWithValue("@Department", department);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                inHouseCount = reader.GetInt32(0);
                                inHouseTotalCost = reader.GetDecimal(1);
                                inHouseRegistrationCost = reader.GetDecimal(2);
                                inHouseInstructorFee = reader.GetDecimal(3);
                                inHouseEquipmentCost = reader.GetDecimal(4);
                                inHouseFoodCost = reader.GetDecimal(5);
                                inHouseOtherCost = reader.GetDecimal(6);
                            }
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        data = new
                        {
                            publicTraining = new
                            {
                                count = publicCount,
                                totalCost = publicTotalCost,
                                registrationCost = publicRegistrationCost
                            },
                            inHouseTraining = new
                            {
                                count = inHouseCount,
                                totalCost = inHouseTotalCost,
                                costBreakdown = new
                                {
                                    registrationCost = inHouseRegistrationCost,
                                    instructorFee = inHouseInstructorFee,
                                    equipmentCost = inHouseEquipmentCost,
                                    foodCost = inHouseFoodCost,
                                    otherCost = inHouseOtherCost
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetCostByTrainingType: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: Export Dashboard Data to CSV
        /// GET: /Home/ExportDashboardData?year=2025&startDate=...&endDate=...&department=...
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportDashboardData(int? year, DateTime? startDate, DateTime? endDate, string? department)
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

                    // SQL Query: Join TrainingRequests with TrainingRequestEmployees (44 columns total)
                    string query = @"
                        SELECT
                            tr.DocNo, tr.Company, tr.TrainingType, tr.Factory, tr.CCEmail,
                            tr.Position, tr.Department, tr.StartDate, tr.EndDate, tr.SeminarTitle,
                            tr.TrainingLocation, tr.Instructor,
                            tr.RegistrationCost, tr.InstructorFee, tr.EquipmentCost,
                            tr.FoodCost, tr.OtherCost, tr.OtherCostDescription, tr.TotalCost,
                            tr.CostPerPerson, tr.TrainingHours, tr.TrainingObjective,
                            tr.OtherObjective, tr.URLSource, tr.AdditionalNotes, tr.ExpectedOutcome,
                            tr.ParticipantCount,
                            tr.SectionManagerId, tr.Status_SectionManager, tr.Comment_SectionManager, tr.ApproveInfo_SectionManager,
                            tr.DepartmentManagerId, tr.Status_DepartmentManager, tr.Comment_DepartmentManager, tr.ApproveInfo_DepartmentManager,
                            tr.HRDAdminId, tr.Status_HRDAdmin, tr.Comment_HRDAdmin, tr.ApproveInfo_HRDAdmin,
                            tr.HRDConfirmationId, tr.Status_HRDConfirmation, tr.Comment_HRDConfirmation, tr.ApproveInfo_HRDConfirmation,
                            tr.ManagingDirectorId, tr.Status_ManagingDirector, tr.Comment_ManagingDirector, tr.ApproveInfo_ManagingDirector,
                            tr.Status, tr.CreatedDate, tr.CreatedBy,
                            emp.EmployeeCode, emp.EmployeeName, emp.Position AS EmpPosition, emp.Level,
                            emp.Department AS EmpDepartment, emp.PreviousTrainingHours, emp.PreviousTrainingCost,
                            emp.CurrentTrainingHours, emp.CurrentTrainingCost, emp.RemainingHours,
                            emp.RemainingCost, emp.Notes
                        FROM [TrainingRequests] tr
                        INNER JOIN [TrainingRequestEmployees] emp
                            ON emp.TrainingRequestId = tr.Id
                        WHERE tr.StartDate >= @StartDate
                          AND tr.StartDate <= @EndDate
                          AND tr.IsActive = 1"
                        + (string.IsNullOrEmpty(department) ? "" : " AND tr.Department = @Department")
                        + @"
                        ORDER BY tr.CreatedDate DESC, emp.EmployeeCode";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", dateStart);
                        cmd.Parameters.AddWithValue("@EndDate", dateEnd);
                        if (!string.IsNullOrEmpty(department))
                            cmd.Parameters.AddWithValue("@Department", department);

                        var csv = new System.Text.StringBuilder();

                        // CSV Header (44 columns + Employee fields = 63 columns total)
                        csv.AppendLine(string.Join(",", new[]
                        {
                            "DocNo", "Company", "TrainingType", "Factory", "CCEmail",
                            "Position", "Department", "StartDate", "EndDate", "SeminarTitle",
                            "TrainingLocation", "Instructor",
                            "RegistrationCost", "InstructorFee", "EquipmentCost",
                            "FoodCost", "OtherCost", "OtherCostDescription", "TotalCost",
                            "CostPerPerson", "TrainingHours", "TrainingObjective",
                            "OtherObjective", "URLSource", "AdditionalNotes", "ExpectedOutcome",
                            "ParticipantCount",
                            "SectionManagerId", "Status_SectionManager", "Comment_SectionManager", "ApproveInfo_SectionManager",
                            "DepartmentManagerId", "Status_DepartmentManager", "Comment_DepartmentManager", "ApproveInfo_DepartmentManager",
                            "HRDAdminId", "Status_HRDAdmin", "Comment_HRDAdmin", "ApproveInfo_HRDAdmin",
                            "HRDConfirmationId", "Status_HRDConfirmation", "Comment_HRDConfirmation", "ApproveInfo_HRDConfirmation",
                            "ManagingDirectorId", "Status_ManagingDirector", "Comment_ManagingDirector", "ApproveInfo_ManagingDirector",
                            "Status", "CreatedDate", "CreatedBy",
                            "EmployeeCode", "EmployeeName", "EmpPosition", "Level",
                            "EmpDepartment", "PreviousTrainingHours", "PreviousTrainingCost",
                            "CurrentTrainingHours", "CurrentTrainingCost", "RemainingHours",
                            "RemainingCost", "Notes"
                        }));

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var row = new[]
                                {
                                    EscapeCsvValue(reader["DocNo"]?.ToString()),
                                    EscapeCsvValue(reader["Company"]?.ToString()),
                                    EscapeCsvValue(reader["TrainingType"]?.ToString()),
                                    EscapeCsvValue(reader["Factory"]?.ToString()),
                                    EscapeCsvValue(reader["CCEmail"]?.ToString()),
                                    EscapeCsvValue(reader["Position"]?.ToString()),
                                    EscapeCsvValue(reader["Department"]?.ToString()),
                                    EscapeCsvValue(reader["StartDate"]?.ToString()),
                                    EscapeCsvValue(reader["EndDate"]?.ToString()),
                                    EscapeCsvValue(reader["SeminarTitle"]?.ToString()),
                                    EscapeCsvValue(reader["TrainingLocation"]?.ToString()),
                                    EscapeCsvValue(reader["Instructor"]?.ToString()),
                                    EscapeCsvValue(reader["RegistrationCost"]?.ToString()),
                                    EscapeCsvValue(reader["InstructorFee"]?.ToString()),
                                    EscapeCsvValue(reader["EquipmentCost"]?.ToString()),
                                    EscapeCsvValue(reader["FoodCost"]?.ToString()),
                                    EscapeCsvValue(reader["OtherCost"]?.ToString()),
                                    EscapeCsvValue(reader["OtherCostDescription"]?.ToString()),
                                    EscapeCsvValue(reader["TotalCost"]?.ToString()),
                                    EscapeCsvValue(reader["CostPerPerson"]?.ToString()),
                                    EscapeCsvValue(reader["TrainingHours"]?.ToString()),
                                    EscapeCsvValue(reader["TrainingObjective"]?.ToString()),
                                    EscapeCsvValue(reader["OtherObjective"]?.ToString()),
                                    EscapeCsvValue(reader["URLSource"]?.ToString()),
                                    EscapeCsvValue(reader["AdditionalNotes"]?.ToString()),
                                    EscapeCsvValue(reader["ExpectedOutcome"]?.ToString()),
                                    EscapeCsvValue(reader["ParticipantCount"]?.ToString()),
                                    EscapeCsvValue(reader["SectionManagerId"]?.ToString()),
                                    EscapeCsvValue(reader["Status_SectionManager"]?.ToString()),
                                    EscapeCsvValue(reader["Comment_SectionManager"]?.ToString()),
                                    EscapeCsvValue(reader["ApproveInfo_SectionManager"]?.ToString()),
                                    EscapeCsvValue(reader["DepartmentManagerId"]?.ToString()),
                                    EscapeCsvValue(reader["Status_DepartmentManager"]?.ToString()),
                                    EscapeCsvValue(reader["Comment_DepartmentManager"]?.ToString()),
                                    EscapeCsvValue(reader["ApproveInfo_DepartmentManager"]?.ToString()),
                                    EscapeCsvValue(reader["HRDAdminId"]?.ToString()),
                                    EscapeCsvValue(reader["Status_HRDAdmin"]?.ToString()),
                                    EscapeCsvValue(reader["Comment_HRDAdmin"]?.ToString()),
                                    EscapeCsvValue(reader["ApproveInfo_HRDAdmin"]?.ToString()),
                                    EscapeCsvValue(reader["HRDConfirmationId"]?.ToString()),
                                    EscapeCsvValue(reader["Status_HRDConfirmation"]?.ToString()),
                                    EscapeCsvValue(reader["Comment_HRDConfirmation"]?.ToString()),
                                    EscapeCsvValue(reader["ApproveInfo_HRDConfirmation"]?.ToString()),
                                    EscapeCsvValue(reader["ManagingDirectorId"]?.ToString()),
                                    EscapeCsvValue(reader["Status_ManagingDirector"]?.ToString()),
                                    EscapeCsvValue(reader["Comment_ManagingDirector"]?.ToString()),
                                    EscapeCsvValue(reader["ApproveInfo_ManagingDirector"]?.ToString()),
                                    EscapeCsvValue(reader["Status"]?.ToString()),
                                    EscapeCsvValue(reader["CreatedDate"]?.ToString()),
                                    EscapeCsvValue(reader["CreatedBy"]?.ToString()),
                                    EscapeCsvValue(reader["EmployeeCode"]?.ToString()),
                                    EscapeCsvValue(reader["EmployeeName"]?.ToString()),
                                    EscapeCsvValue(reader["EmpPosition"]?.ToString()),
                                    EscapeCsvValue(reader["Level"]?.ToString()),
                                    EscapeCsvValue(reader["EmpDepartment"]?.ToString()),
                                    EscapeCsvValue(reader["PreviousTrainingHours"]?.ToString()),
                                    EscapeCsvValue(reader["PreviousTrainingCost"]?.ToString()),
                                    EscapeCsvValue(reader["CurrentTrainingHours"]?.ToString()),
                                    EscapeCsvValue(reader["CurrentTrainingCost"]?.ToString()),
                                    EscapeCsvValue(reader["RemainingHours"]?.ToString()),
                                    EscapeCsvValue(reader["RemainingCost"]?.ToString()),
                                    EscapeCsvValue(reader["Notes"]?.ToString())
                                };

                                csv.AppendLine(string.Join(",", row));
                            }
                        }
                    }

                    // Create filename with timestamp
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string deptFilter = string.IsNullOrEmpty(department) ? "All" : department;
                    string fileName = $"Dashboard_Export_{selectedYear}_{deptFilter}_{timestamp}.csv";

                    // Add UTF-8 BOM for Thai language support
                    byte[] csvBytes = System.Text.Encoding.UTF8.GetPreamble()
                        .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
                        .ToArray();

                    Console.WriteLine($"✅ Exported {csv.ToString().Split('\n').Length - 2} rows to {fileName}");

                    return File(csvBytes, "text/csv", fileName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ExportDashboardData: {ex.Message}");
                return BadRequest($"Export failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper: Escape CSV values to prevent CSV injection
        /// </summary>
        private string EscapeCsvValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // Escape quotes and wrap in quotes if contains comma, quote, or newline
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }
    }
}
