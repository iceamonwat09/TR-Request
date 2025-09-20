using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;

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
                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    return BadRequest(new { success = false, message = "Employee Code is required" });
                }

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = @"
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
                        WHERE UserID = @UserID ";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", employeeCode);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var employee = new
                                {
                                    empCode = reader["UserID"].ToString(),
                                    prefix = reader["Prefix"].ToString(),
                                    name = reader["Name"].ToString(),
                                    lastname = reader["Lastname"].ToString(),
                                    position = reader["Position"].ToString(),
                                    level = reader["Level"].ToString(),
                                    department = reader["Department"].ToString(),
                                    company = reader["Company"].ToString(),
                                    email = reader["Email"].ToString(),
                                    // ข้อมูลการอบรม (ปัจจุบันใส่เป็น 0 ก่อน - สามารถปรับเชื่อมกับตารางการอบรมได้)
                                    currentYearHours = 0,
                                    currentYearCost = 0,
                                    thisTimeHours = 0,
                                    thisTimeCost = 0,
                                    remainingHours = 12,
                                    remainingCost = 10000
                                };

                                return Ok(new { success = true, employee = employee });
                            }
                            else
                            {
                                return NotFound(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching employee: {ex.Message}");
                return StatusCode(500, new { success = false, message = "เกิดข้อผิดพลาดในการค้นหาข้อมูล" });
            }
        }
    }
}