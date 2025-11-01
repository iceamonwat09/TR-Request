using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingRequestApp.Models;
using TrainingRequestApp.Services;

namespace TrainingRequestApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController : ControllerBase
    {
        private readonly IEmployeeService _employeeService;
        private readonly ApplicationDbContext _context;

        public EmployeesController(IEmployeeService employeeService, ApplicationDbContext context)
        {
            _employeeService = employeeService;
            _context = context;
        }


        // Controllers/EmployeesController.cs

        // ✅ เพิ่ม 2 methods นี้
        [HttpGet("emails")]
        public async Task<ActionResult<List<EmailDto>>> GetAllEmails()
        {
            var emails = await _employeeService.GetAllEmailsAsync();
            return Ok(emails);
        }

        [HttpGet("emails/search")]
        public async Task<ActionResult<List<EmailDto>>> SearchEmails([FromQuery] string q)
        {
            var emails = await _employeeService.SearchEmailsAsync(q);
            return Ok(emails);
        }
        // Controllers/EmployeesController.cs - เพิ่ม endpoints เหล่านี้

        [HttpGet("departments")]
        public async Task<ActionResult<List<string>>> GetAllDepartments()
        {
            var departments = await _employeeService.GetAllDepartmentsAsync();
            return Ok(departments);
        }

        [HttpGet("positions")]
        public async Task<ActionResult<List<string>>> GetPositionsByDepartment([FromQuery] string department)
        {
            if (string.IsNullOrWhiteSpace(department))
            {
                return BadRequest("Department is required");
            }

            var positions = await _employeeService.GetPositionsByDepartmentAsync(department);
            return Ok(positions);
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

        /// <summary>
        /// ค้นหา Section Manager โดยกรองจาก Department + Position + Level = "Section Mgr."
        /// </summary>
        [HttpGet("approvers/section-manager")]
        public async Task<ActionResult<List<ApproverResultDto>>> SearchSectionManagers(
            [FromQuery] string? department,
            [FromQuery] string? position,
            [FromQuery] string? q)
        {
            try
            {
                // ถ้าไม่มี search term ให้ return empty
                if (string.IsNullOrWhiteSpace(q))
                    q = "";

                var searchLower = q.ToLower();

                var approvers = await _context.Employees
                    .Where(e => !string.IsNullOrEmpty(e.Level) &&
                                e.Level.ToUpper() == "SECTION MGR." &&
                                // Filter by department
                                (string.IsNullOrEmpty(department) || (e.Department != null && e.Department.ToUpper() == department.ToUpper())) &&
                                // Filter by position
                                (string.IsNullOrEmpty(position) || (e.Position != null && e.Position.ToUpper() == position.ToUpper())) &&
                                // Search filter
                                (string.IsNullOrEmpty(q) ||
                                 (e.Name != null && e.Name.ToLower().Contains(searchLower)) ||
                                 (e.Lastname != null && e.Lastname.ToLower().Contains(searchLower)) ||
                                 (e.UserID != null && e.UserID.ToLower().Contains(searchLower))))
                    .Select(e => new ApproverResultDto
                    {
                        Id = e.UserID ?? "",
                        Name = (e.Prefix + " " + e.Name + " " + e.Lastname).Trim(),
                        Department = e.Department ?? "",
                        Position = e.Position ?? "",
                        Level = e.Level ?? ""
                    })
                    .Distinct()
                    .OrderBy(e => e.Name)
                    .Take(20)
                    .ToListAsync();

                return Ok(approvers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SearchSectionManagers: {ex.Message}");
                return StatusCode(500, new { success = false, message = "❌ เกิดข้อผิดพลาด: " + ex.Message });
            }
        }

        /// <summary>
        /// ค้นหา Department Manager โดยกรองจาก Department + Position + Level = "Department Mgr."
        /// </summary>
        [HttpGet("approvers/department-manager")]
        public async Task<ActionResult<List<ApproverResultDto>>> SearchDepartmentManagers(
            [FromQuery] string? department,
            [FromQuery] string? position,
            [FromQuery] string? q)
        {
            try
            {
                // ถ้าไม่มี search term ให้ return empty
                if (string.IsNullOrWhiteSpace(q))
                    q = "";

                var searchLower = q.ToLower();

                var approvers = await _context.Employees
                    .Where(e => !string.IsNullOrEmpty(e.Level) &&
                                e.Level.ToUpper() == "DEPARTMENT MGR." &&
                                // Filter by department
                                (string.IsNullOrEmpty(department) || (e.Department != null && e.Department.ToUpper() == department.ToUpper())) &&
                                // Filter by position
                                (string.IsNullOrEmpty(position) || (e.Position != null && e.Position.ToUpper() == position.ToUpper())) &&
                                // Search filter
                                (string.IsNullOrEmpty(q) ||
                                 (e.Name != null && e.Name.ToLower().Contains(searchLower)) ||
                                 (e.Lastname != null && e.Lastname.ToLower().Contains(searchLower)) ||
                                 (e.UserID != null && e.UserID.ToLower().Contains(searchLower))))
                    .Select(e => new ApproverResultDto
                    {
                        Id = e.UserID ?? "",
                        Name = (e.Prefix + " " + e.Name + " " + e.Lastname).Trim(),
                        Department = e.Department ?? "",
                        Position = e.Position ?? "",
                        Level = e.Level ?? ""
                    })
                    .Distinct()
                    .OrderBy(e => e.Name)
                    .Take(20)
                    .ToListAsync();

                return Ok(approvers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SearchDepartmentManagers: {ex.Message}");
                return StatusCode(500, new { success = false, message = "❌ เกิดข้อผิดพลาด: " + ex.Message });
            }
        }

        /// <summary>
        /// ค้นหา Managing Director โดยกรองจาก Level = "Director" OR "AMD" OR "DMD" OR "MD" OR "CEO"
        /// </summary>
        [HttpGet("approvers/managing-director")]
        public async Task<ActionResult<List<ApproverResultDto>>> SearchManagingDirectors([FromQuery] string? q)
        {
            try
            {
                // ถ้าไม่มี search term ให้ return empty
                if (string.IsNullOrWhiteSpace(q))
                    q = "";

                var searchLower = q.ToLower();
                var validLevels = new[] { "DIRECTOR", "AMD", "DMD", "MD", "CEO" };

                var approvers = await _context.Employees
                    .Where(e => !string.IsNullOrEmpty(e.Level) &&
                                validLevels.Contains(e.Level.ToUpper()) &&
                                // Search filter
                                (string.IsNullOrEmpty(q) ||
                                 (e.Name != null && e.Name.ToLower().Contains(searchLower)) ||
                                 (e.Lastname != null && e.Lastname.ToLower().Contains(searchLower)) ||
                                 (e.UserID != null && e.UserID.ToLower().Contains(searchLower))))
                    .Select(e => new ApproverResultDto
                    {
                        Id = e.UserID ?? "",
                        Name = (e.Prefix + " " + e.Name + " " + e.Lastname).Trim(),
                        Department = e.Department ?? "",
                        Position = e.Position ?? "",
                        Level = e.Level ?? ""
                    })
                    .Distinct()
                    .OrderBy(e => e.Name)
                    .Take(20)
                    .ToListAsync();

                return Ok(approvers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in SearchManagingDirectors: {ex.Message}");
                return StatusCode(500, new { success = false, message = "❌ เกิดข้อผิดพลาด: " + ex.Message });
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

    public class ApproverDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Level { get; set; }
    }

    public class ApproverResultDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
    }

}
