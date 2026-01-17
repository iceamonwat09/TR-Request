using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using TrainingRequestApp.Services;

namespace TrainingRequestApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IPdfReportService _pdfReportService;

        public HomeController(IConfiguration configuration, IPdfReportService pdfReportService)
        {
            _configuration = configuration;
            _pdfReportService = pdfReportService;
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
        /// API: Export Dashboard Data เป็น CSV
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

                    // SQL Query - ใช้ LEFT JOIN เพื่อดึงข้อมูล TrainingRequests ทั้งหมด (รวมที่ไม่มี Employee)
                    string query = @"
                        SELECT
                            tr.DocNo, tr.Company, tr.TrainingType, tr.Factory, tr.CCEmail,
                            tr.Position, tr.Department, tr.StartDate, tr.EndDate, tr.SeminarTitle,
                            tr.TrainingLocation, tr.Instructor, tr.TotalCost, tr.CostPerPerson,
                            tr.PerPersonTrainingHours, tr.TrainingObjective, tr.OtherObjective,
                            tr.URLSource, tr.AdditionalNotes, tr.ExpectedOutcome, tr.AttachedFilePath,
                            tr.Status, tr.CreatedDate, tr.CreatedBy, tr.UpdatedDate, tr.UpdatedBy,
                            tr.RegistrationCost, tr.InstructorFee, tr.EquipmentCost, tr.FoodCost,
                            tr.OtherCost, tr.OtherCostDescription, tr.TotalPeople,
                            ISNULL(emp.EmployeeCode, '') AS EmployeeCode,
                            ISNULL(emp.EmployeeName, '') AS EmployeeName,
                            ISNULL(emp.Position, '') AS EmployeePosition,
                            ISNULL(emp.PreviousTrainingHours, 0) AS PreviousTrainingHours,
                            ISNULL(emp.PreviousTrainingCost, 0) AS PreviousTrainingCost,
                            ISNULL(emp.CurrentTrainingHours, 0) AS CurrentTrainingHours,
                            ISNULL(emp.CurrentTrainingCost, 0) AS CurrentTrainingCost,
                            ISNULL(emp.Notes, '') AS Notes,
                            ISNULL(emp.[level], '') AS Level,
                            ISNULL(emp.Department, '') AS EmployeeDepartment,
                            ISNULL(emp.RemainingHours, 0) AS RemainingHours,
                            ISNULL(emp.RemainingCost, 0) AS RemainingCost
                        FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
                        LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
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

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            // สร้าง CSV ด้วย StringBuilder (รวดเร็วและประหยัด memory)
                            var csv = new System.Text.StringBuilder();

                            // 🔹 Header Row (ชื่อคอลัมน์ภาษาไทย + อังกฤษ)
                            csv.AppendLine(string.Join(",", new[]
                            {
                                "เลขที่เอกสาร", "บริษัท", "ประเภทการอบรม", "โรงงาน", "CC Email",
                                "แผนก", "ฝ่าย", "วันที่เริ่ม", "วันที่สิ้นสุด", "หัวข้ออบรม",
                                "สถานที่อบรม", "วิทยากร", "ค่าใช้จ่ายรวม", "ค่าใช้จ่ายต่อคน",
                                "ชั่วโมงอบรมต่อคน", "วัตถุประสงค์", "วัตถุประสงค์อื่นๆ",
                                "แหล่งข้อมูล", "หมายเหตุเพิ่มเติม", "ผลที่คาดหวัง", "ไฟล์แนบ",
                                "สถานะ", "วันที่สร้าง", "ผู้สร้าง", "วันที่แก้ไข", "ผู้แก้ไข",
                                "ค่าลงทะเบียน", "ค่าวิทยากร", "ค่าอุปกรณ์", "ค่าอาหาร",
                                "ค่าใช้จ่ายอื่น", "รายละเอียดค่าใช้จ่ายอื่น", "จำนวนคนทั้งหมด",
                                "รหัสพนักงาน", "ชื่อพนักงาน", "แผนกพนักงาน",
                                "ชั่วโมงอบรมก่อนหน้า", "ค่าใช้จ่ายอบรมก่อนหน้า",
                                "ชั่วโมงอบรมปัจจุบัน", "ค่าใช้จ่ายอบรมปัจจุบัน", "หมายเหตุพนักงาน",
                                "ระดับ", "ฝ่ายพนักงาน", "ชั่วโมงคงเหลือ", "ค่าใช้จ่ายคงเหลือ"
                            }));

                            // 🔹 Data Rows
                            int rowCount = 0;
                            while (await reader.ReadAsync())
                            {
                                rowCount++;
                                var row = new[]
                                {
                                    EscapeCsvValue(reader["DocNo"]?.ToString()),
                                    EscapeCsvValue(reader["Company"]?.ToString()),
                                    EscapeCsvValue(reader["TrainingType"]?.ToString()),
                                    EscapeCsvValue(reader["Factory"]?.ToString()),
                                    EscapeCsvValue(reader["CCEmail"]?.ToString()),
                                    EscapeCsvValue(reader["Position"]?.ToString()),
                                    EscapeCsvValue(reader["Department"]?.ToString()),
                                    reader["StartDate"] != DBNull.Value ? Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd") : "",
                                    reader["EndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EndDate"]).ToString("yyyy-MM-dd") : "",
                                    EscapeCsvValue(reader["SeminarTitle"]?.ToString()),
                                    EscapeCsvValue(reader["TrainingLocation"]?.ToString()),
                                    EscapeCsvValue(reader["Instructor"]?.ToString()),
                                    reader["TotalCost"]?.ToString() ?? "0",
                                    reader["CostPerPerson"]?.ToString() ?? "0",
                                    reader["PerPersonTrainingHours"]?.ToString() ?? "0",
                                    EscapeCsvValue(reader["TrainingObjective"]?.ToString()),
                                    EscapeCsvValue(reader["OtherObjective"]?.ToString()),
                                    EscapeCsvValue(reader["URLSource"]?.ToString()),
                                    EscapeCsvValue(reader["AdditionalNotes"]?.ToString()),
                                    EscapeCsvValue(reader["ExpectedOutcome"]?.ToString()),
                                    EscapeCsvValue(reader["AttachedFilePath"]?.ToString()),
                                    EscapeCsvValue(reader["Status"]?.ToString()),
                                    reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]).ToString("yyyy-MM-dd HH:mm:ss") : "",
                                    EscapeCsvValue(reader["CreatedBy"]?.ToString()),
                                    reader["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedDate"]).ToString("yyyy-MM-dd HH:mm:ss") : "",
                                    EscapeCsvValue(reader["UpdatedBy"]?.ToString()),
                                    reader["RegistrationCost"]?.ToString() ?? "0",
                                    reader["InstructorFee"]?.ToString() ?? "0",
                                    reader["EquipmentCost"]?.ToString() ?? "0",
                                    reader["FoodCost"]?.ToString() ?? "0",
                                    reader["OtherCost"]?.ToString() ?? "0",
                                    EscapeCsvValue(reader["OtherCostDescription"]?.ToString()),
                                    reader["TotalPeople"]?.ToString() ?? "0",
                                    EscapeCsvValue(reader["EmployeeCode"]?.ToString()),
                                    EscapeCsvValue(reader["EmployeeName"]?.ToString()),
                                    EscapeCsvValue(reader["EmployeePosition"]?.ToString()),
                                    reader["PreviousTrainingHours"]?.ToString() ?? "0",
                                    reader["PreviousTrainingCost"]?.ToString() ?? "0",
                                    reader["CurrentTrainingHours"]?.ToString() ?? "0",
                                    reader["CurrentTrainingCost"]?.ToString() ?? "0",
                                    EscapeCsvValue(reader["Notes"]?.ToString()),
                                    EscapeCsvValue(reader["Level"]?.ToString()),
                                    EscapeCsvValue(reader["EmployeeDepartment"]?.ToString()),
                                    reader["RemainingHours"]?.ToString() ?? "0",
                                    reader["RemainingCost"]?.ToString() ?? "0"
                                };

                                csv.AppendLine(string.Join(",", row));
                            }

                            Console.WriteLine($"✅ Export: {rowCount} rows exported");

                            // 🔹 สร้างชื่อไฟล์
                            string fileName = $"Dashboard_Export_{selectedYear}";
                            if (!string.IsNullOrEmpty(department))
                                fileName += $"_{department}";
                            fileName += $"_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                            // 🔹 Return CSV file with UTF-8 BOM (สำหรับภาษาไทยใน Excel)
                            byte[] csvBytes = System.Text.Encoding.UTF8.GetPreamble()
                                .Concat(System.Text.Encoding.UTF8.GetBytes(csv.ToString()))
                                .ToArray();

                            return File(csvBytes, "text/csv", fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ExportDashboardData: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Helper: Escape CSV values (จัดการ comma, quotes, newlines)
        /// </summary>
        private string EscapeCsvValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // ถ้ามี comma, quote, หรือ newline ต้อง wrap ด้วย quotes
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                // Escape double quotes โดยใช้ double quotes สองตัว
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }

        // ====================================================================
        // 📡 INTERFACE API ENDPOINTS (สำหรับส่งข้อมูลไป Course, OpenCourse, TimeStramp)
        // ====================================================================

        /// <summary>
        /// Interface Page - แสดงหน้า Interface สำหรับส่งข้อมูลไประบบอบรม
        /// GET: /Home/Interface
        /// </summary>
        [HttpGet]
        public IActionResult Interface()
        {
            // ตรวจสอบว่าผู้ใช้ล็อกอินหรือไม่
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
            {
                return RedirectToAction("Index", "Login");
            }

            // ตรวจสอบสิทธิ์ Admin
            string? userRole = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(userRole) ||
                !(userRole.Contains("Admin") || userRole.Contains("HRD") || userRole.Contains("System")))
            {
                TempData["Error"] = "คุณไม่มีสิทธิ์เข้าถึงหน้านี้";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.UserEmail = HttpContext.Session.GetString("UserEmail");
            ViewBag.UserRole = userRole;

            Console.WriteLine($"🔹 Interface page accessed by: {ViewBag.UserEmail}");

            return View();
        }

        /// <summary>
        /// API: ดึงข้อมูล Training Requests สำหรับ Interface (Status = APPROVED, COMPLETE, RESCHEDULED)
        /// GET: /Home/GetInterfaceRequests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetInterfaceRequests()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT
                            tr.Id,
                            tr.DocNo,
                            tr.SeminarTitle,
                            tr.StartDate,
                            tr.EndDate,
                            tr.TrainingLocation,
                            tr.Instructor,
                            tr.Company,
                            tr.Department,
                            tr.Status,
                            tr.TrainingType,
                            tr.TotalCost,
                            (SELECT COUNT(*) FROM TrainingRequestEmployees WHERE TrainingRequestId = tr.Id) AS EmployeeCount
                        FROM [TrainingRequests] tr
                        WHERE tr.Status IN ('APPROVED', 'COMPLETE', 'RESCHEDULED')
                          AND tr.IsActive = 1
                        ORDER BY tr.StartDate DESC, tr.DocNo DESC";

                    var result = new List<object>();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new
                                {
                                    id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    docNo = reader["DocNo"]?.ToString() ?? "",
                                    seminarTitle = reader["SeminarTitle"]?.ToString() ?? "",
                                    startDate = reader["StartDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd") : "",
                                    endDate = reader["EndDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["EndDate"]).ToString("yyyy-MM-dd") : "",
                                    trainingLocation = reader["TrainingLocation"]?.ToString() ?? "",
                                    instructor = reader["Instructor"]?.ToString() ?? "",
                                    company = reader["Company"]?.ToString() ?? "",
                                    department = reader["Department"]?.ToString() ?? "",
                                    status = reader["Status"]?.ToString() ?? "",
                                    trainingType = reader["TrainingType"]?.ToString() ?? "",
                                    totalCost = reader["TotalCost"] != DBNull.Value
                                        ? Convert.ToDecimal(reader["TotalCost"]) : 0,
                                    employeeCount = reader.GetInt32(reader.GetOrdinal("EmployeeCount"))
                                });
                            }
                        }
                    }

                    Console.WriteLine($"✅ GetInterfaceRequests: Found {result.Count} records");
                    return Json(new { success = true, data = result });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetInterfaceRequests: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// API: ส่งข้อมูล Interface ไปบันทึกที่ Course, OpenCourse, TimeStramp
        /// POST: /Home/SendInterfaceData
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendInterfaceData([FromBody] InterfaceDataRequest request)
        {
            if (request == null)
            {
                return Json(new { success = false, message = "ข้อมูลไม่ถูกต้อง" });
            }

            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            string userEmail = HttpContext.Session.GetString("UserEmail") ?? "Unknown";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        Console.WriteLine($"🔹 SendInterfaceData started for TrainingRequestId: {request.TrainingRequestId}");

                        // 1. ดึงข้อมูล TrainingRequest
                        string getRequestQuery = @"
                            SELECT DocNo, SeminarTitle, StartDate, Instructor, Company, TrainingLocation, Status
                            FROM TrainingRequests
                            WHERE Id = @Id AND IsActive = 1";

                        string docNo = "", seminarTitle = "", instructor = "", company = "", trainingLocation = "";
                        DateTime startDate = DateTime.Now;
                        string currentStatus = "";

                        using (SqlCommand cmd = new SqlCommand(getRequestQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", request.TrainingRequestId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    docNo = reader["DocNo"]?.ToString() ?? "";
                                    seminarTitle = reader["SeminarTitle"]?.ToString() ?? "";
                                    startDate = reader["StartDate"] != DBNull.Value
                                        ? Convert.ToDateTime(reader["StartDate"]) : DateTime.Now;
                                    instructor = reader["Instructor"]?.ToString() ?? "";
                                    company = reader["Company"]?.ToString() ?? "";
                                    trainingLocation = reader["TrainingLocation"]?.ToString() ?? "";
                                    currentStatus = reader["Status"]?.ToString() ?? "";
                                }
                                else
                                {
                                    return Json(new { success = false, message = "ไม่พบข้อมูล Training Request" });
                                }
                            }
                        }

                        // ตรวจสอบว่าถูกส่งไปแล้วหรือไม่
                        if (currentStatus == "COMPLETE")
                        {
                            return Json(new { success = false, message = "ข้อมูลนี้ถูกส่งไปแล้ว ไม่สามารถส่งซ้ำได้" });
                        }

                        // 2. Insert ลง Course และได้ ID กลับมา
                        string insertCourseQuery = @"
                            INSERT INTO Course (CID, CName)
                            OUTPUT INSERTED.ID
                            VALUES (@CID, @CName)";

                        int courseId = 0;
                        using (SqlCommand cmd = new SqlCommand(insertCourseQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@CID", docNo);
                            cmd.Parameters.AddWithValue("@CName", seminarTitle);
                            var result = await cmd.ExecuteScalarAsync();
                            courseId = Convert.ToInt32(result);
                        }

                        Console.WriteLine($"✅ Course inserted with ID: {courseId}");

                        // 3. คำนวณชั่วโมงจากเวลาเริ่ม-สิ้นสุด
                        TimeSpan timeIn = TimeSpan.Parse(request.TimeIn ?? "08:00");
                        TimeSpan timeOut = TimeSpan.Parse(request.TimeOut ?? "17:00");
                        decimal trainingHours = (decimal)(timeOut - timeIn).TotalHours;
                        if (trainingHours < 0) trainingHours = 0;

                        // 4. Insert ลง OpenCourse และได้ OID กลับมา
                        string insertOpenCourseQuery = @"
                            INSERT INTO OpenCourse (OCID, OOpenDate, Language, categoryC, OLO, time, Course_Provider)
                            OUTPUT INSERTED.OID
                            VALUES (@OCID, @OOpenDate, NULL, NULL, @OLO, @time, @Course_Provider)";

                        int openCourseId = 0;
                        using (SqlCommand cmd = new SqlCommand(insertOpenCourseQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OCID", courseId);
                            cmd.Parameters.AddWithValue("@OOpenDate", startDate);
                            cmd.Parameters.AddWithValue("@OLO", trainingLocation ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@time", trainingHours);
                            cmd.Parameters.AddWithValue("@Course_Provider", "Interface");
                            var result = await cmd.ExecuteScalarAsync();
                            openCourseId = Convert.ToInt32(result);
                        }

                        Console.WriteLine($"✅ OpenCourse inserted with OID: {openCourseId}");

                        // 5. ดึงรายชื่อ Employees ของ Training Request นี้
                        string getEmployeesQuery = @"
                            SELECT EmployeeCode
                            FROM TrainingRequestEmployees
                            WHERE TrainingRequestId = @TrainingRequestId";

                        var employeeCodes = new List<string>();
                        using (SqlCommand cmd = new SqlCommand(getEmployeesQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TrainingRequestId", request.TrainingRequestId);
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string empCode = reader["EmployeeCode"]?.ToString() ?? "";
                                    if (!string.IsNullOrEmpty(empCode))
                                    {
                                        employeeCodes.Add(empCode);
                                    }
                                }
                            }
                        }

                        Console.WriteLine($"📋 Found {employeeCodes.Count} employees for this training");

                        // 6. Insert ลง TimeStramp สำหรับแต่ละ Employee
                        int sYear = startDate.Year;
                        int gen = request.Gen ?? 1;
                        string checkIn = $"Interface {userEmail} {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                        string insertTimeStrampQuery = @"
                            INSERT INTO TimeStramp (OID, Emp, check_pass, Expert, Examiner, TranslatorName,
                                                   Company, datetime_in, datetime_out, Gen, SYear, Check_in)
                            VALUES (@OID, @Emp, @check_pass, @Expert, @Examiner, @TranslatorName,
                                   @Company, @datetime_in, @datetime_out, @Gen, @SYear, @Check_in)";

                        int insertedCount = 0;
                        foreach (var empCode in employeeCodes)
                        {
                            using (SqlCommand cmd = new SqlCommand(insertTimeStrampQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@OID", openCourseId);
                                cmd.Parameters.AddWithValue("@Emp", empCode);
                                cmd.Parameters.AddWithValue("@check_pass", "Pass");
                                cmd.Parameters.AddWithValue("@Expert", instructor ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@Examiner", DBNull.Value);
                                cmd.Parameters.AddWithValue("@TranslatorName", DBNull.Value);
                                cmd.Parameters.AddWithValue("@Company", company ?? (object)DBNull.Value);
                                cmd.Parameters.AddWithValue("@datetime_in", TimeSpan.Parse(request.TimeIn ?? "08:00"));
                                cmd.Parameters.AddWithValue("@datetime_out", TimeSpan.Parse(request.TimeOut ?? "17:00"));
                                cmd.Parameters.AddWithValue("@Gen", gen);
                                cmd.Parameters.AddWithValue("@SYear", sYear);
                                cmd.Parameters.AddWithValue("@Check_in", checkIn);
                                await cmd.ExecuteNonQueryAsync();
                                insertedCount++;
                            }
                        }

                        Console.WriteLine($"✅ TimeStramp inserted for {insertedCount} employees");

                        // 7. Update Status เป็น COMPLETE
                        string updateStatusQuery = @"
                            UPDATE TrainingRequests
                            SET Status = 'COMPLETE',
                                UpdatedDate = GETDATE(),
                                UpdatedBy = @UpdatedBy
                            WHERE Id = @Id";

                        using (SqlCommand cmd = new SqlCommand(updateStatusQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Id", request.TrainingRequestId);
                            cmd.Parameters.AddWithValue("@UpdatedBy", userEmail);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        Console.WriteLine($"✅ TrainingRequest status updated to COMPLETE");

                        // Commit Transaction
                        transaction.Commit();

                        Console.WriteLine($"✅ SendInterfaceData completed successfully!");

                        return Json(new {
                            success = true,
                            message = $"ส่งข้อมูลสำเร็จ! บันทึกพนักงาน {insertedCount} คน",
                            courseId = courseId,
                            openCourseId = openCourseId,
                            employeeCount = insertedCount
                        });
                    }
                    catch (Exception ex)
                    {
                        // Rollback on error
                        transaction.Rollback();
                        Console.WriteLine($"❌ Error in SendInterfaceData: {ex.Message}");
                        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                        return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
                    }
                }
            }
        }

        /// <summary>
        /// Request Model สำหรับ SendInterfaceData
        /// </summary>
        public class InterfaceDataRequest
        {
            public int TrainingRequestId { get; set; }
            public string? TimeIn { get; set; } = "08:00";
            public string? TimeOut { get; set; } = "17:00";
            public int? Gen { get; set; } = 1;
        }

        /// <summary>
        /// Export Training Request as PDF
        /// GET: /Home/ExportTrainingRequestPdf/5
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportTrainingRequestPdf(int id)
        {
            try
            {
                // ตรวจสอบ Session
                if (string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")))
                {
                    return RedirectToAction("Index", "Login");
                }

                Console.WriteLine($"🔹 Generating PDF for Training Request ID: {id}");

                // เรียกใช้ Service สร้าง PDF
                byte[] pdfBytes = await _pdfReportService.GenerateTrainingRequestPdfAsync(id);

                // สร้างชื่อไฟล์
                string fileName = $"TrainingRequest_{id}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                Console.WriteLine($"✅ PDF generated successfully: {fileName} ({pdfBytes.Length} bytes)");

                // Return PDF file
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in ExportTrainingRequestPdf: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"เกิดข้อผิดพลาด: {ex.Message}" });
            }
        }
    }
}
