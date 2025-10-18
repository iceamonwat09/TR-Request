using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TrainingRequestApp.Models;

namespace TrainingRequestApp.Services
{
    public interface IEmployeeService
    {
        Task<Employee?> GetEmployeeByUserIdAsync(string userId);
        Task<List<Employee>> GetAllEmployeesAsync();
        Task<EmployeeQuotaDto?> GetEmployeeWithQuotaAsync(string userId);
    }

    public class EmployeeQuotaDto
    {
        public string UserID { get; set; } = string.Empty;
        public string? Prefix { get; set; }
        public string? Name { get; set; }
        public string? Lastname { get; set; }
        public string? Level { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public string? Email { get; set; }
        public string? Company { get; set; }
        public int CurrentYearHours { get; set; }
        public decimal CurrentYearCost { get; set; }
        public int RemainingHours { get; set; }
        public decimal RemainingCost { get; set; }
        public int PendingRequestCount { get; set; }
        public int PendingHours { get; set; }
        public decimal PendingCost { get; set; }
    }

    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public EmployeeService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
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

        public async Task<EmployeeQuotaDto?> GetEmployeeWithQuotaAsync(string userId)
        {
            Debug.WriteLine("========================================");
            Debug.WriteLine($"SERVICE: GetEmployeeWithQuotaAsync called for: {userId}");

            if (string.IsNullOrWhiteSpace(userId))
            {
                Debug.WriteLine("SERVICE: UserId is empty");
                return null;
            }

            var employee = await _context.Employees
                .Where(e => e.UserID == userId)
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                Debug.WriteLine($"SERVICE: Employee not found: {userId}");
                return null;
            }

            Debug.WriteLine($"SERVICE: Found employee: {employee.Name} {employee.Lastname}");

            int currentYearHours = 0;
            decimal currentYearCost = 0;
            int pendingRequestCount = 0;
            int pendingHours = 0;
            decimal pendingCost = 0;

            string connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    Debug.WriteLine("SERVICE: SQL Connection opened");

                    string approvedQuery = @"
                        SELECT 
                            ISNULL(SUM(tre.CurrentTrainingHours), 0) AS TotalHours,
                            ISNULL(SUM(tre.CurrentTrainingCost), 0) AS TotalCost
                        FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
                        INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr 
                            ON tre.TrainingRequestId = tr.Id
                        WHERE tre.EmployeeCode = @EmployeeCode
                            AND UPPER(tr.Status) = 'APPROVED'
                            AND UPPER(tr.TrainingType) = 'PUBLIC'
                            AND YEAR(tr.StartDate) = YEAR(GETDATE())
                    ";

                    Debug.WriteLine($"SERVICE: Running APPROVED query for: {userId}");

                    using (SqlCommand command = new SqlCommand(approvedQuery, connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeCode", userId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                currentYearHours = reader.GetInt32(0);
                                currentYearCost = reader.GetDecimal(1);
                                Debug.WriteLine($"SERVICE: APPROVED Result - Hours: {currentYearHours}, Cost: {currentYearCost}");
                            }
                            else
                            {
                                Debug.WriteLine("SERVICE: APPROVED Query returned no rows");
                            }
                        }
                    }

                    string pendingQuery = @"
                        SELECT 
                            COUNT(DISTINCT tr.Id) AS RequestCount,
                            ISNULL(SUM(tre.CurrentTrainingHours), 0) AS TotalHours,
                            ISNULL(SUM(tre.CurrentTrainingCost), 0) AS TotalCost
                        FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
                        INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr 
                            ON tre.TrainingRequestId = tr.Id
                        WHERE tre.EmployeeCode = @EmployeeCode
                            AND UPPER(tr.Status) = 'PENDING'
                            AND UPPER(tr.TrainingType) = 'PUBLIC'
                            AND YEAR(tr.StartDate) = YEAR(GETDATE())
                    ";

                    using (SqlCommand command = new SqlCommand(pendingQuery, connection))
                    {
                        command.Parameters.AddWithValue("@EmployeeCode", userId);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                pendingRequestCount = reader.GetInt32(0);
                                pendingHours = reader.GetInt32(1);
                                pendingCost = reader.GetDecimal(2);
                                Debug.WriteLine($"SERVICE: PENDING Result - Count: {pendingRequestCount}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"SERVICE: SQL Error: {ex.Message}");
                throw;
            }

            int remainingHours = 12 - currentYearHours;
            decimal remainingCost = 10000 - currentYearCost;

            Debug.WriteLine($"SERVICE: Calculated - RemainingHours: {remainingHours}, RemainingCost: {remainingCost}");

            var result = new EmployeeQuotaDto
            {
                UserID = employee.UserID ?? "",
                Prefix = employee.Prefix,
                Name = employee.Name,
                Lastname = employee.Lastname,
                Level = employee.Level,
                Department = employee.Department,
                Position = employee.Position,
                Email = employee.Email,
                Company = employee.Company,
                CurrentYearHours = currentYearHours,
                CurrentYearCost = currentYearCost,
                RemainingHours = remainingHours,
                RemainingCost = remainingCost,
                PendingRequestCount = pendingRequestCount,
                PendingHours = pendingHours,
                PendingCost = pendingCost
            };

            Debug.WriteLine($"SERVICE: Returning - CurrentYearHours: {result.CurrentYearHours}, CurrentYearCost: {result.CurrentYearCost}");
            Debug.WriteLine("========================================");

            return result;
        }
    }
}