using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TrainingRequestApp.Models;
using TrainingRequestApp.Services;

namespace TrainingRequestApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;

        public EmployeesController(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        /// <summary>
        /// ค้นหาพนักงานพร้อมข้อมูลโควต้า
        /// </summary>
        // ✅ แก้ไข: ใช้ GET + Path Parameter (ตรงกับ JavaScript เดิม)
        [HttpGet("search/{employeeCode}")]
        public async Task<ActionResult> SearchEmployee(string employeeCode)
        {
            try
            {
                // ✅ Validation: ตรวจสอบ employeeCode
                if (string.IsNullOrWhiteSpace(employeeCode))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ กรุณากรอกรหัสพนักงาน"
                    });
                }

                // ✅ Validation: ตรวจสอบความยาว
                if (employeeCode.Length != 7)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "❌ รหัสพนักงานต้องเป็น 7 หลัก"
                    });
                }

                // เรียก Service เพื่อคำนวณโควต้า
                var employeeWithQuota = await _employeeService.GetEmployeeWithQuotaAsync(employeeCode);

                if (employeeWithQuota == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"❌ ไม่พบข้อมูลพนักงานรหัส: {employeeCode}"
                    });
                }

                // ✅ สร้างข้อความเตือน (ถ้ามีคำขอรออนุมัติ)
                string? warningMessage = null;
                if (employeeWithQuota.PendingRequestCount > 0)
                {
                    warningMessage = $"⚠️ พนักงานคนนี้มีคำขอรออนุมัติอยู่ {employeeWithQuota.PendingRequestCount} รายการ " +
                                   $"({employeeWithQuota.PendingHours} ชม., {employeeWithQuota.PendingCost:N0} บาท)";
                }

                // ส่งข้อมูลกลับ
                return Ok(new
                {
                    success = true,
                    employee = new
                    {
                        empCode = employeeWithQuota.UserID,
                        prefix = employeeWithQuota.Prefix,
                        name = employeeWithQuota.Name,
                        lastname = employeeWithQuota.Lastname,
                        position = employeeWithQuota.Position,
                        level = employeeWithQuota.Level,
                        department = employeeWithQuota.Department,
                        company = employeeWithQuota.Company,
                        email = employeeWithQuota.Email,

                        // ✅ ข้อมูลโควต้า
                        currentYearHours = employeeWithQuota.CurrentYearHours,
                        currentYearCost = employeeWithQuota.CurrentYearCost,
                        thisTimeHours = 0,  // จะกรอกในฟอร์มหลัก
                        thisTimeCost = 0,   // จะกรอกในฟอร์มหลัก
                        remainingHours = employeeWithQuota.RemainingHours,
                        remainingCost = employeeWithQuota.RemainingCost,

                        // ✅ ข้อมูลคำขอที่รออนุมัติ
                        pendingRequestCount = employeeWithQuota.PendingRequestCount,
                        pendingHours = employeeWithQuota.PendingHours,
                        pendingCost = employeeWithQuota.PendingCost
                    },
                    warningMessage = warningMessage
                });
            }
            catch (Exception ex)
            {
                // Log error (ถ้ามี logging system)
                Console.WriteLine($"❌ Error in SearchEmployee: {ex.Message}");

                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ เกิดข้อผิดพลาดในการค้นหาข้อมูล กรุณาลองใหม่อีกครั้ง"
                });
            }
        }

        /// <summary>
        /// ดึงรายชื่อพนักงานทั้งหมด (สำหรับ autocomplete หรือ dropdown)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<EmployeeDto>>> GetAllEmployees()
        {
            try
            {
                var employees = await _employeeService.GetAllEmployeesAsync();

                var employeeDtos = employees.Select(emp => new EmployeeDto
                {
                    UserID = emp.UserID,
                    Prefix = emp.Prefix,
                    Name = emp.Name,
                    Lastname = emp.Lastname,
                    Level = emp.Level,
                    Department = emp.Department,
                    Position = emp.Position,
                    Email = emp.Email
                }).ToList();

                return Ok(employeeDtos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetAllEmployees: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "❌ เกิดข้อผิดพลาดในการดึงข้อมูล"
                });
            }
        }
    }

    public class EmployeeSearchRequest
    {
        public string UserId { get; set; } = string.Empty;
    }

    public class EmployeeDto
    {
        public string? UserID { get; set; }
        public string? Prefix { get; set; }
        public string? Name { get; set; }
        public string? Lastname { get; set; }
        public string? Level { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Email { get; set; }
    }
}