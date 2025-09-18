using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TrainingRequestApp.Models;

namespace TrainingRequestApp.Services
{
    public interface IEmployeeService
    {
        Task<Employee?> GetEmployeeByUserIdAsync(string userId);
        Task<List<Employee>> GetAllEmployeesAsync();
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;

        public EmployeeService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Employee?> GetEmployeeByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            return await _context.Employees
                .Where(e => e.UserID == userId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            return await _context.Employees
                .OrderBy(e => e.Name)
                .ToListAsync();
        }
    }
}