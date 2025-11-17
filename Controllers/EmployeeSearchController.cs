using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace TrainingRequestApp.Controllers
{
    [Route("api/employee")]
    [ApiController]
    public class EmployeeSearchController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public EmployeeSearchController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("search/{employeeCode}")]
        public IActionResult SearchEmployee(string employeeCode, [FromQuery] string department = null, [FromQuery] string trainingType = null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine($"[SEARCH] SearchEmployee called for: {employeeCode}");
                System.Diagnostics.Debug.WriteLine($"[SEARCH] Department Filter: {department}");
                System.Diagnostics.Debug.WriteLine($"[SEARCH] Training Type: {trainingType}");

                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    return BadRequest(new { success = false, message = "Employee Code is required" });
                }

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine("[SEARCH] SQL Connection opened");

                    // 1. ดึงข้อมูลพนักงาน + กรอง Department (ถ้าเป็น Public)
                    string employeeQuery = @"
                SELECT
                    UserID,
                    ISNULL(Prefix, '') as Prefix,
                    ISNULL(Name, '') as Name,
                    ISNULL(lastname, '') as Lastname,
                    ISNULL([Level], '') as Level,
                    ISNULL(Position, '') as Position,
                    ISNULL(Department, '') as Department,
                    ISNULL(Company, '') as Company,
                    ISNULL(Email, '') as Email
                FROM Employees
                WHERE UserID = @UserID";

                    // ✅ เพิ่มเงื่อนไข Department (เมื่อ TrainingType = Public)
                    if (!string.IsNullOrWhiteSpace(department) && trainingType?.ToUpper() == "PUBLIC")
                    {
                        employeeQuery += " AND Department = @Department";
                    }

                    string empCode = "";
                    string prefix = "";
                    string name = "";
                    string lastname = "";
                    string level = "";
                    string position = "";
                    string empDepartment = "";
                    string company = "";
                    string email = "";

                    using (SqlCommand command = new SqlCommand(employeeQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", employeeCode);

                        // ✅ เพิ่ม parameter Department (ถ้ามี)
                        if (!string.IsNullOrWhiteSpace(department) && trainingType?.ToUpper() == "PUBLIC")
                        {
                            command.Parameters.AddWithValue("@Department", department);
                        }

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                empCode = reader["UserID"].ToString();
                                prefix = reader["Prefix"].ToString();
                                name = reader["Name"].ToString();
                                lastname = reader["Lastname"].ToString();
                                position = reader["Position"].ToString();
                                level = reader["Level"].ToString();
                                empDepartment = reader["Department"].ToString();
                                company = reader["Company"].ToString();
                                email = reader["Email"].ToString();

                                System.Diagnostics.Debug.WriteLine($"[SEARCH] Found employee: {name} {lastname} ({empDepartment})");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[SEARCH] Employee not found or not in selected department");

                                // ✅ ข้อความ error ชัดเจนขึ้น
                                string errorMsg = (!string.IsNullOrWhiteSpace(department) && trainingType?.ToUpper() == "PUBLIC")
                                    ? $"ไม่พบพนักงานรหัส {employeeCode} ในฝ่าย {department}"
                                    : "ไม่พบข้อมูลพนักงาน";

                                return NotFound(new { success = false, message = errorMsg });
                            }
                        }
                    }

                    // 2. ดึงโควต้าฝ่ายจาก TrainingRequest_Cost (เฉพาะ Public)
                    decimal departmentQuota = 0;
                    int departmentQhours = 0;

                    if (trainingType?.ToUpper() == "PUBLIC")
                    {
                        string quotaDeptQuery = @"
                    SELECT
                        ISNULL([Cost], 0) AS DepartmentQuota,
                        ISNULL([Qhours], 0) AS DepartmentQhours
                    FROM [HRDSYSTEM].[dbo].[TrainingRequest_Cost]
                    WHERE [Department] = @DeptParam
                        AND [Year] = @Year";

                        System.Diagnostics.Debug.WriteLine("[SEARCH] Fetching department quota...");

                        using (SqlCommand command = new SqlCommand(quotaDeptQuery, connection))
                        {
                            command.Parameters.AddWithValue("@DeptParam", empDepartment);
                            command.Parameters.AddWithValue("@Year", DateTime.Now.Year.ToString());

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    departmentQuota = Convert.ToDecimal(reader["DepartmentQuota"]);
                                    departmentQhours = Convert.ToInt32(reader["DepartmentQhours"]);

                                    System.Diagnostics.Debug.WriteLine($"[SEARCH] Department Quota - Cost: {departmentQuota}, Hours: {departmentQhours}");
                                }
                            }
                        }

                        // ✅ เช็คว่ามีโควต้าหรือไม่
                        if (departmentQuota == 0 || departmentQhours == 0)
                        {
                            return BadRequest(new
                            {
                                success = false,
                                message = $"❌ ไม่มี Cost หรือ Qhours สำหรับฝ่าย {empDepartment} กรุณาติดต่อผู้ดูแลระบบ"
                            });
                        }
                    }

                    // 3. คำนวณยอดใช้ไปของฝ่าย (Department-based สำหรับ Public)
                    int currentYearHours = 0;
                    decimal currentYearCost = 0;

                    if (trainingType?.ToUpper() == "PUBLIC")
                    {
                        string usageQuery = @"
                    SELECT
                        ISNULL(SUM(tre.CurrentTrainingHours), 0) AS TotalHours,
                        ISNULL(SUM(tre.CurrentTrainingCost), 0) AS TotalCost
                    FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
                    INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr
                        ON tre.TrainingRequestId = tr.Id
                    WHERE tre.Department = @DeptParam
                        AND UPPER(tr.Status) IN ('APPROVED', 'RESCHEDULED', 'COMPLETE')
                        AND UPPER(tr.TrainingType) = 'PUBLIC'
                        AND YEAR(tr.StartDate) = YEAR(GETDATE())";

                        System.Diagnostics.Debug.WriteLine("[SEARCH] Running department usage query...");

                        using (SqlCommand command = new SqlCommand(usageQuery, connection))
                        {
                            command.Parameters.AddWithValue("@DeptParam", empDepartment);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    currentYearHours = Convert.ToInt32(reader["TotalHours"]);
                                    currentYearCost = Convert.ToDecimal(reader["TotalCost"]);

                                    System.Diagnostics.Debug.WriteLine($"[SEARCH] Department Usage - Hours: {currentYearHours}, Cost: {currentYearCost}");
                                }
                            }
                        }
                    }
                    else
                    {
                        // ✅ In House ยังคงคำนวณแบบเดิม (Individual) - ถ้าต้องการ
                        string usageQuery = @"
                    SELECT
                        ISNULL(SUM(tre.CurrentTrainingHours), 0) AS TotalHours,
                        ISNULL(SUM(tre.CurrentTrainingCost), 0) AS TotalCost
                    FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
                    INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr
                        ON tre.TrainingRequestId = tr.Id
                    WHERE tre.EmployeeCode = @EmployeeCode
                        AND UPPER(tr.Status) IN ('APPROVED', 'RESCHEDULED', 'COMPLETE')
                        AND UPPER(tr.TrainingType) = 'IN HOUSE'
                        AND YEAR(tr.StartDate) = YEAR(GETDATE())";

                        System.Diagnostics.Debug.WriteLine("[SEARCH] Running individual usage query (In House)...");

                        using (SqlCommand command = new SqlCommand(usageQuery, connection))
                        {
                            command.Parameters.AddWithValue("@EmployeeCode", employeeCode);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    currentYearHours = Convert.ToInt32(reader["TotalHours"]);
                                    currentYearCost = Convert.ToDecimal(reader["TotalCost"]);
                                }
                            }
                        }
                    }

                    // 4. คำนวณค่าคงเหลือ (ไม่ส่งกลับ frontend - ให้ frontend คำนวณเอง)
                    int remainingHours = (trainingType?.ToUpper() == "PUBLIC" ? departmentQhours : 12) - currentYearHours;
                    decimal remainingCost = (trainingType?.ToUpper() == "PUBLIC" ? departmentQuota : 10000) - currentYearCost;

                    System.Diagnostics.Debug.WriteLine($"[SEARCH] Remaining - Hours: {remainingHours}, Cost: {remainingCost}");

                    // 5. สร้าง response
                    var employee = new
                    {
                        empCode = empCode,
                        prefix = prefix,
                        name = name,
                        lastname = lastname,
                        position = position,
                        level = level,
                        department = empDepartment,
                        company = company,
                        email = email,
                        currentYearHours = currentYearHours,
                        currentYearCost = currentYearCost,
                        thisTimeHours = 0,
                        thisTimeCost = 0,
                        remainingHours = remainingHours,
                        remainingCost = remainingCost,
                        // ✅ เพิ่มโควต้าฝ่าย (สำหรับ Public)
                        departmentQuota = trainingType?.ToUpper() == "PUBLIC" ? departmentQuota : 0,
                        departmentQhours = trainingType?.ToUpper() == "PUBLIC" ? departmentQhours : 0
                    };

                    System.Diagnostics.Debug.WriteLine("[SEARCH] Returning employee data");
                    System.Diagnostics.Debug.WriteLine($"[SEARCH] Department Quota: {departmentQuota}, Qhours: {departmentQhours}");
                    System.Diagnostics.Debug.WriteLine("========================================");

                    return Ok(new { success = true, employee = employee });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SEARCH] Error: {ex.Message}");
                Console.WriteLine($"Error searching employee: {ex.Message}");
                return StatusCode(500, new { success = false, message = "เกิดข้อผิดพลาดในการค้นหาข้อมูล" });
            }
        }

        [HttpGet("departments-positions")]
        public IActionResult GetDepartmentsAndPositions()
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                var departments = new List<string>();
                var positions = new List<string>();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    // ดึงข้อมูล Department ที่ไม่ซ้ำกัน
                    string departmentQuery = @"
                        SELECT DISTINCT Department 
                        FROM Employees 
                        WHERE Department IS NOT NULL 
                        AND LTRIM(RTRIM(Department)) != ''
                        ORDER BY Department";

                    using (SqlCommand command = new SqlCommand(departmentQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string dept = reader["Department"].ToString().Trim();
                                if (!string.IsNullOrWhiteSpace(dept))
                                {
                                    departments.Add(dept);
                                }
                            }
                        }
                    }

                    // ดึงข้อมูล Position ที่ไม่ซ้ำกัน
                    string positionQuery = @"
                        SELECT DISTINCT Position 
                        FROM Employees 
                        WHERE  Position IS NOT NULL 
                        AND LTRIM(RTRIM(Position)) != ''
                        ORDER BY Position";

                    using (SqlCommand command = new SqlCommand(positionQuery, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string pos = reader["Position"].ToString().Trim();
                                if (!string.IsNullOrWhiteSpace(pos))
                                {
                                    positions.Add(pos);
                                }
                            }
                        }
                    }
                }

                return Ok(new
                {
                    success = true,
                    departments = departments,
                    positions = positions
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting departments and positions: {ex.Message}");
                return StatusCode(500, new { success = false, message = "เกิดข้อผิดพลาดในการดึงข้อมูล" });
            }
        }
    }
}