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
        public IActionResult SearchEmployee(string employeeCode)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("========================================");
                System.Diagnostics.Debug.WriteLine($"[SEARCH] SearchEmployee called for: {employeeCode}");

                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    return BadRequest(new { success = false, message = "Employee Code is required" });
                }

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine("[SEARCH] SQL Connection opened");

                    // 1. ดึงข้อมูลพนักงาน
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

                    string empCode = "";
                    string prefix = "";
                    string name = "";
                    string lastname = "";
                    string level = "";
                    string position = "";
                    string department = "";
                    string company = "";
                    string email = "";

                    using (SqlCommand command = new SqlCommand(employeeQuery, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", employeeCode);

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
                                department = reader["Department"].ToString();
                                company = reader["Company"].ToString();
                                email = reader["Email"].ToString();

                                System.Diagnostics.Debug.WriteLine($"[SEARCH] Found employee: {name} {lastname}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("[SEARCH] Employee not found");
                                return NotFound(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                            }
                        }
                    }

                    // 2. คำนวณโควต้า (Approved + Public + ปีปัจจุบัน)
                    int currentYearHours = 0;
                    decimal currentYearCost = 0;

                    string quotaQuery = @"
                SELECT 
                    ISNULL(SUM(tre.CurrentTrainingHours), 0) AS TotalHours,
                    ISNULL(SUM(tre.CurrentTrainingCost), 0) AS TotalCost
                FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
                INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr 
                    ON tre.TrainingRequestId = tr.Id
                WHERE tre.EmployeeCode = @EmployeeCode
                    AND UPPER(tr.Status) = 'APPROVED'
                    AND UPPER(tr.TrainingType) = 'PUBLIC'
                    AND YEAR(tr.StartDate) = YEAR(GETDATE())";

                    System.Diagnostics.Debug.WriteLine("[SEARCH] Running quota query...");

                    using (SqlCommand command = new SqlCommand(quotaQuery, connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeCode", employeeCode);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentYearHours = Convert.ToInt32(reader["TotalHours"]);
                                currentYearCost = Convert.ToDecimal(reader["TotalCost"]);

                                System.Diagnostics.Debug.WriteLine($"[SEARCH] Quota Result - Hours: {currentYearHours}, Cost: {currentYearCost}");
                            }
                        }
                    }

                    // 3. คำนวณค่าคงเหลือ
                    int remainingHours = 12 - currentYearHours;
                    decimal remainingCost = 10000 - currentYearCost;

                    System.Diagnostics.Debug.WriteLine($"[SEARCH] Remaining - Hours: {remainingHours}, Cost: {remainingCost}");

                    // 4. สร้าง response
                    var employee = new
                    {
                        empCode = empCode,
                        prefix = prefix,
                        name = name,
                        lastname = lastname,
                        position = position,
                        level = level,
                        department = department,
                        company = company,
                        email = email,
                        currentYearHours = currentYearHours,
                        currentYearCost = currentYearCost,
                        thisTimeHours = 0,
                        thisTimeCost = 0,
                        remainingHours = remainingHours,
                        remainingCost = remainingCost
                    };

                    System.Diagnostics.Debug.WriteLine("[SEARCH] Returning employee data");
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