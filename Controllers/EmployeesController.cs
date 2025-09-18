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

        [HttpPost("search")]
        public async Task<ActionResult<EmployeeDto>> SearchEmployee([FromBody] EmployeeSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId))
            {
                return BadRequest("UserId is required");
            }

            var employee = await _employeeService.GetEmployeeByUserIdAsync(request.UserId);

            if (employee == null)
            {
                return NotFound();
            }

            var employeeDto = new EmployeeDto
            {
                UserID = employee.UserID,
                Prefix = employee.Prefix,
                Name = employee.Name,
                Lastname = employee.Lastname,
                Level = employee.Level,
                Department = employee.Department,
                Position = employee.Position,
                Email = employee.Email
            };

            return Ok(employeeDto);
        }

        [HttpGet]
        public async Task<ActionResult<List<EmployeeDto>>> GetAllEmployees()
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