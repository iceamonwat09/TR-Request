using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TrainingRequestApp.Models;
using TrainingRequestApp.Services;

namespace TrainingRequestApp.Controllers
{
    public class TrainingRequestController : Controller
    {
        private readonly ITrainingRequestService _trainingRequestService;
        private readonly IEmployeeService _employeeService;
        private readonly IConfiguration _configuration;

        public TrainingRequestController(
            ITrainingRequestService trainingRequestService,
            IEmployeeService employeeService,
            IConfiguration configuration)
        {
            _trainingRequestService = trainingRequestService;
            _employeeService = employeeService;
            _configuration = configuration;
        }

        // ====================================================================
        // GET: /TrainingRequest/Create
        // ====================================================================
        public IActionResult Create()
        {
            return View();
        }

        // ====================================================================
        // POST: /TrainingRequest/SaveTrainingRequest
        // ====================================================================
        [HttpPost]
        public async Task<IActionResult> SaveTrainingRequest([FromForm] TrainingRequestFormData formData)
        {
            try
            {
                Console.WriteLine("🔵 SaveTrainingRequest called");
                Console.WriteLine($"Company: {formData.Company}");
                Console.WriteLine($"TrainingType: {formData.TrainingType}");

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    Console.WriteLine("✅ Database connected");

                    // ตรวจสอบ Database ที่เชื่อมต่อ
                    using (SqlCommand checkCmd = new SqlCommand("SELECT DB_NAME()", conn))
                    {
                        string dbName = (string)await checkCmd.ExecuteScalarAsync();
                        Console.WriteLine($"✅ Connected to Database: {dbName}");
                    }

                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Generate DocNo
                            string docNo = await GenerateDocNo(conn, transaction, formData.TrainingType);
                            Console.WriteLine($"✅ Generated DocNo: {docNo}");

                            // 2. Insert ข้อมูลหลัก
                            int trainingRequestId = await InsertTrainingRequest(conn, transaction, formData, docNo);
                            Console.WriteLine($"✅ TrainingRequestId: {trainingRequestId}");

                            // 3. Insert รายชื่อพนักงาน (ใช้ trainingRequestId แทน docNo)
                            if (!string.IsNullOrEmpty(formData.EmployeesJson))
                            {
                                await InsertEmployees(conn, transaction, trainingRequestId, formData.EmployeesJson);
                                Console.WriteLine("✅ Employees inserted");
                            }

                            // 4. Upload และบันทึกไฟล์แนบ
                            if (formData.AttachedFiles != null)
                            {
                                await SaveAttachment(conn, transaction, docNo, formData.AttachedFiles);
                                Console.WriteLine("✅ File uploaded");
                            }

                            transaction.Commit();
                            Console.WriteLine("✅ Transaction committed");

                            return Json(new
                            {
                                success = true,
                                message = "✅ บันทึกข้อมูลสำเร็จ",
                                docNo = docNo,
                                trainingRequestId = trainingRequestId
                            });
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"❌ Error in transaction: {ex.Message}");
                            Console.WriteLine($"StackTrace: {ex.StackTrace}");
                            return Json(new { success = false, message = "❌ เกิดข้อผิดพลาด: " + ex.Message });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "❌ เกิดข้อผิดพลาด: " + ex.Message });
            }
        }

        // ====================================================================
        // Approver APIs - เพิ่มใหม่
        // ====================================================================

        [HttpGet("api/employees/approvers/section-manager")]
        public async Task<IActionResult> GetSectionManagers(string q = "", string department = "", string position = "")
       {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT TOP 20 
                            UserID as id, 
                            CONCAT(Prefix, Name, ' ', Lastname) as name, 
                            Email as email, 
                            Level as level
                        FROM Employees
                        WHERE Level = 'Section Mgr.'
                        AND Department = @Department
                        AND Position = @Position
                        AND (Name LIKE @Search OR Email LIKE @Search)
                        ORDER BY Name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Department", department ?? "");
                        cmd.Parameters.AddWithValue("@Position", position ?? "");
                        cmd.Parameters.AddWithValue("@Search", $"%{q}%");

                        var results = new List<object>();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    id = reader["id"].ToString(),
                                    name = reader["name"].ToString(),
                                    email = reader["email"].ToString(),
                                    level = reader["level"].ToString()
                                });
                            }
                        }
                        return Json(results);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetSectionManagers Error: {ex.Message}");
                return Json(new List<object>());
            }
        }

        [HttpGet("api/employees/approvers/dept-manager")]
        public async Task<IActionResult> GetDeptManagers(string q = "", string department = "", string position = "")
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT TOP 20 
                            UserID as id, 
                            CONCAT(Prefix, Name, ' ', Lastname) as name, 
                            Email as email, 
                            Level as level
                        FROM Employees
                        WHERE Level = 'Department Mgr.'
                        AND Department = @Department
                        AND Position = @Position
                        AND (Name LIKE @Search OR Email LIKE @Search)
                        ORDER BY Name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Department", department ?? "");
                        cmd.Parameters.AddWithValue("@Position", position ?? "");
                        cmd.Parameters.AddWithValue("@Search", $"%{q}%");

                        var results = new List<object>();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    id = reader["id"].ToString(),
                                    name = reader["name"].ToString(),
                                    email = reader["email"].ToString(),
                                    level = reader["level"].ToString()
                                });
                            }
                        }
                        return Json(results);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetDeptManagers Error: {ex.Message}");
                return Json(new List<object>());
            }
        }

        [HttpGet("api/employees/approvers/director")]
        public async Task<IActionResult> GetDirectors(string q = "")
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT TOP 20 
                            UserID as id, 
                            CONCAT(Prefix, Name, ' ', Lastname) as name, 
                            Email as email, 
                            Level as level
                        FROM Employees
                        WHERE Level IN ('Director', 'AMD', 'DMD', 'MD', 'CEO')
                        AND (Name LIKE @Search OR Email LIKE @Search)
                        ORDER BY Name";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Search", $"%{q}%");

                        var results = new List<object>();
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                results.Add(new
                                {
                                    id = reader["id"].ToString(),
                                    name = reader["name"].ToString(),
                                    email = reader["email"].ToString(),
                                    level = reader["level"].ToString()
                                });
                            }
                        }
                        return Json(results);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetDirectors Error: {ex.Message}");
                return Json(new List<object>());
            }
        }

        // ====================================================================
        // Helper Methods
        // ====================================================================

        private async Task<string> GenerateDocNo(SqlConnection conn, SqlTransaction transaction, string trainingType)
        {
            string docNo = "";

            try
            {
                using (SqlCommand cmd = new SqlCommand("SP_GenerateDocNo", conn, transaction))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@TrainingType", trainingType ?? "Public");

                    SqlParameter outputParam = new SqlParameter("@DocNo", SqlDbType.NVarChar, 20);
                    outputParam.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(outputParam);

                    await cmd.ExecuteNonQueryAsync();
                    docNo = outputParam.Value?.ToString() ?? "";
                }

                if (string.IsNullOrEmpty(docNo))
                {
                    throw new Exception("SP_GenerateDocNo returned empty");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ SP_GenerateDocNo failed: {ex.Message}");
                // Fallback
                string prefix = trainingType == "Public" ? "PB" : "IN";
                string yearMonth = DateTime.Now.ToString("yyyy-MM");
                Random random = new Random();
                int runningNumber = random.Next(1, 999);
                docNo = $"{prefix}-{yearMonth}-{runningNumber:D3}";
                Console.WriteLine($"✅ Generated DocNo (fallback): {docNo}");
            }

            return docNo;
        }

        private async Task<int> InsertTrainingRequest(SqlConnection conn, SqlTransaction transaction,
            TrainingRequestFormData formData, string docNo)
        {
            string query = @"
                INSERT INTO [HRDSYSTEM].[dbo].[TrainingRequests] (
                    [DocNo], [Company], [TrainingType], [Factory], [CCEmail], [Department],[Position],
                    [StartDate], [EndDate], [SeminarTitle], [TrainingLocation], [Instructor],
                    [RegistrationCost], [InstructorFee], [EquipmentCost], [FoodCost], [OtherCost], [OtherCostDescription],
                    [TotalCost], [CostPerPerson],[PerPersonTrainingHours], [TrainingObjective], [OtherObjective],
                    [URLSource], [AdditionalNotes], [ExpectedOutcome], 
                    [Status], [CreatedDate], [CreatedBy], [IsActive],[TotalPeople],
                    [SectionManagerId], [DepartmentManagerId], [ManagingDirectorId]
                )
                VALUES (
                    @DocNo, @Company, @TrainingType, @Factory, @CCEmail, @Department,@Position,
                    @StartDate, @EndDate, @SeminarTitle, @TrainingLocation, @Instructor,
                    @RegistrationCost, @InstructorFee, @EquipmentCost, @FoodCost, @OtherCost, @OtherCostDescription,
                    @TotalCost, @CostPerPerson,@PerPersonTrainingHours, @TrainingObjective, @OtherObjective,
                    @URLSource, @AdditionalNotes, @ExpectedOutcome,
                    'Pending', GETDATE(), 'System', 1,@TotalPeople,
                    @SectionManagerId, @DepartmentManagerId, @ManagingDirectorId
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@DocNo", docNo);
                cmd.Parameters.AddWithValue("@Company", formData.Company ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TrainingType", formData.TrainingType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Factory", formData.Factory ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CCEmail", formData.CCEmail ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Department", formData.Department ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Position", formData.Position ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@StartDate", formData.StartDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EndDate", formData.EndDate ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@SeminarTitle", formData.SeminarTitle ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TrainingLocation", formData.TrainingLocation ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Instructor", formData.Instructor ?? (object)DBNull.Value);

                // งบประมาณแยกรายการ
                cmd.Parameters.AddWithValue("@RegistrationCost", formData.RegistrationCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@InstructorFee", formData.InstructorFee ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@EquipmentCost", formData.EquipmentCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@FoodCost", formData.FoodCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@OtherCost", formData.OtherCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@OtherCostDescription", formData.OtherCostDescription ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@TotalCost", formData.TotalCost ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@CostPerPerson", formData.CostPerPerson ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@PerPersonTrainingHours", formData.TrainingHours ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TrainingObjective", formData.TrainingObjective ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@OtherObjective", formData.OtherObjective ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@URLSource", formData.URLSource ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@AdditionalNotes", formData.AdditionalNotes ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ExpectedOutcome", formData.ExpectedOutcome ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TotalPeople", formData.ParticipantCount ?? (object)DBNull.Value);

                // ✅ เพิ่มใหม่ - Approvers
                cmd.Parameters.AddWithValue("@SectionManagerId", formData.SectionManagerId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@DepartmentManagerId", formData.DepartmentManagerId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ManagingDirectorId", formData.ManagingDirectorId ?? (object)DBNull.Value);

                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        private async Task InsertEmployees(SqlConnection conn, SqlTransaction transaction,
            int trainingRequestId, string employeesJson)
        {
            var employees = JsonSerializer.Deserialize<EmployeeData[]>(employeesJson);

            if (employees == null || employees.Length == 0) return;

            // ✅ ใช้ TrainingRequestId และไม่มี CreatedDate, IsActive
            string query = @"
                INSERT INTO [HRDSYSTEM].[dbo].[TrainingRequestEmployees] (
                    [TrainingRequestId], [EmployeeCode], [EmployeeName], [Position],
                    [PreviousTrainingHours], [PreviousTrainingCost],
                    [CurrentTrainingHours], [CurrentTrainingCost], [Notes]
                )
                VALUES (
                    @TrainingRequestId, @EmployeeCode, @EmployeeName, @Position,
                    @PreviousTrainingHours, @PreviousTrainingCost,
                    @CurrentTrainingHours, @CurrentTrainingCost, @Notes
                )";

            foreach (var emp in employees)
            {
                using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@TrainingRequestId", trainingRequestId);
                    cmd.Parameters.AddWithValue("@EmployeeCode", emp.empCode ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@EmployeeName", emp.fullName ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Position", emp.position ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PreviousTrainingHours", emp.currentYearHours);
                    cmd.Parameters.AddWithValue("@PreviousTrainingCost", emp.currentYearCost);
                    cmd.Parameters.AddWithValue("@CurrentTrainingHours", emp.thisTimeHours);
                    cmd.Parameters.AddWithValue("@CurrentTrainingCost", emp.thisTimeCost);
                    cmd.Parameters.AddWithValue("@Notes", emp.notes ?? (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task SaveAttachment(SqlConnection conn, SqlTransaction transaction,
            string docNo, IFormFile file)
        {
            string uploadFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                "uploads",
                DateTime.Now.Year.ToString()
            );
            Directory.CreateDirectory(uploadFolder);

            string fileName = $"{docNo}_{DateTime.Now:yyyyMMddHHmmss}_{file.FileName}";
            string filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string query = @"
                INSERT INTO [HRDSYSTEM].[dbo].[TrainingRequestAttachments] 
                ([DocNo], [File_Name], [Modify_Date])
                VALUES (@DocNo, @FileName, @ModifyDate)";

            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@DocNo", docNo);
                cmd.Parameters.AddWithValue("@FileName", fileName);
                cmd.Parameters.AddWithValue("@ModifyDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                await cmd.ExecuteNonQueryAsync();
            }

            string updateQuery = @"
                UPDATE [HRDSYSTEM].[dbo].[TrainingRequests] 
                SET [AttachedFilePath] = @FilePath 
                WHERE [DocNo] = @DocNo";

            using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction))
            {
                updateCmd.Parameters.AddWithValue("@FilePath", $"/uploads/{DateTime.Now.Year}/{fileName}");
                updateCmd.Parameters.AddWithValue("@DocNo", docNo);
                await updateCmd.ExecuteNonQueryAsync();
            }
        }

        // ====================================================================
        // โค้ดเดิม - ไม่แก้ไข
        // ====================================================================

        [HttpPost]
        public async Task<IActionResult> Create(string TrainingTitle, DateTime TrainingDate, string Location, string ParticipantsJson)
        {
            try
            {
                var trainingRequest = new TrainingRequest
                {
                    TrainingTitle = TrainingTitle,
                    TrainingDate = TrainingDate,
                    Location = Location
                };

                var createdRequest = await _trainingRequestService.CreateTrainingRequestAsync(trainingRequest);

                if (!string.IsNullOrWhiteSpace(ParticipantsJson))
                {
                    var participants = JsonSerializer.Deserialize<List<ParticipantViewModel>>(ParticipantsJson);

                    if (participants != null)
                    {
                        foreach (var participantModel in participants)
                        {
                            var participant = new TrainingParticipant
                            {
                                UserID = participantModel.UserID,
                                Prefix = participantModel.Prefix,
                                Name = participantModel.Name,
                                Lastname = participantModel.Lastname,
                                Level = participantModel.Level
                            };

                            await _trainingRequestService.AddParticipantAsync(createdRequest.Id, participant);
                        }
                    }
                }

                TempData["SuccessMessage"] = "สร้างคำขอฝึกอบรมเรียบร้อยแล้ว";
                return RedirectToAction(nameof(Details), new { id = createdRequest.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "เกิดข้อผิดพลาดในการบันทึกข้อมูล: " + ex.Message);
                ViewBag.TrainingTitle = TrainingTitle;
                ViewBag.TrainingDate = TrainingDate;
                ViewBag.Location = Location;
                return View();
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var trainingRequest = await _trainingRequestService.GetTrainingRequestByIdAsync(id);
            if (trainingRequest == null)
            {
                return NotFound();
            }

            return View(trainingRequest);
        }

        public async Task<IActionResult> Index()
        {
            var trainingRequests = await _trainingRequestService.GetAllTrainingRequestsAsync();
            return View(trainingRequests);
        }

        [HttpPost]
        public async Task<IActionResult> AddParticipant(int trainingRequestId, string userId)
        {
            var employee = await _employeeService.GetEmployeeByUserIdAsync(userId);
            if (employee == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลพนักงาน" });
            }

            var participant = new TrainingParticipant
            {
                UserID = employee.UserID ?? "",
                Prefix = employee.Prefix,
                Name = employee.Name,
                Lastname = employee.Lastname,
                Level = employee.Level
            };

            var result = await _trainingRequestService.AddParticipantAsync(trainingRequestId, participant);

            if (result)
            {
                return Json(new { success = true, message = "เพิ่มผู้เข้าร่วมเรียบร้อยแล้ว" });
            }
            else
            {
                return Json(new { success = false, message = "ไม่สามารถเพิ่มผู้เข้าร่วมได้ (อาจมีอยู่แล้ว)" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RemoveParticipant(int trainingRequestId, string userId)
        {
            var result = await _trainingRequestService.RemoveParticipantAsync(trainingRequestId, userId);

            if (result)
            {
                return Json(new { success = true, message = "ลบผู้เข้าร่วมเรียบร้อยแล้ว" });
            }
            else
            {
                return Json(new { success = false, message = "ไม่สามารถลบผู้เข้าร่วมได้" });
            }
        }
    }






    // ====================================================================
    // ViewModels
    // ====================================================================

    public class TrainingRequestFormData
    {
        public string? Company { get; set; }
        public string? TrainingType { get; set; }
        public string? Factory { get; set; }
        public string? CCEmail { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? SeminarTitle { get; set; }
        public string? TrainingLocation { get; set; }
        public string? Instructor { get; set; }

        // งบประมาณแยกรายการ
        public decimal? RegistrationCost { get; set; }
        public decimal? InstructorFee { get; set; }
        public decimal? EquipmentCost { get; set; }
        public decimal? FoodCost { get; set; }
        public decimal? OtherCost { get; set; }
        public string? OtherCostDescription { get; set; }

        public decimal? TotalCost { get; set; }
        public decimal? CostPerPerson { get; set; }
        public int? TrainingHours { get; set; }
        public string? TrainingObjective { get; set; }
        public string? OtherObjective { get; set; }
        public string? URLSource { get; set; }
        public string? AdditionalNotes { get; set; }
        public string? ExpectedOutcome { get; set; }
        public string? EmployeesJson { get; set; }
        public IFormFile? AttachedFiles { get; set; }
        public string? ParticipantCount { get; set; }

        // ✅ เพิ่มใหม่ - Approvers
        public string? SectionManagerId { get; set; }
        public string? DepartmentManagerId { get; set; }
        public string? ManagingDirectorId { get; set; }
    }

    public class EmployeeData
    {
        public string? empCode { get; set; }
        public string? fullName { get; set; }
        public string? position { get; set; }
        public int currentYearHours { get; set; }
        public decimal currentYearCost { get; set; }
        public int thisTimeHours { get; set; }
        public decimal thisTimeCost { get; set; }
        public string? notes { get; set; }
    }

    public class CreateTrainingRequestViewModel
    {
        public string TrainingTitle { get; set; } = string.Empty;
        public DateTime TrainingDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<ParticipantViewModel>? Participants { get; set; }
    }

    public class ParticipantViewModel
    {
        public string UserID { get; set; } = string.Empty;
        public string? Prefix { get; set; }
        public string? Name { get; set; }
        public string? Lastname { get; set; }
        public string? Level { get; set; }
    }
}