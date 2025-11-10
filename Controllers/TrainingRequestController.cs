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
        // GET: /TrainingRequest/Edit/{docNo}
        // ====================================================================
        [HttpGet]
        public async Task<IActionResult> Edit(string docNo)
        {
            if (string.IsNullOrEmpty(docNo))
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // Fetch Training Request data
                    string query = @"
                        SELECT
                            Id, DocNo, Company, TrainingType, Factory, CCEmail, Department, Position,
                            StartDate, EndDate, SeminarTitle, TrainingLocation, Instructor,
                            RegistrationCost, InstructorFee, EquipmentCost, FoodCost, OtherCost, OtherCostDescription,
                            TotalCost, CostPerPerson, PerPersonTrainingHours, TrainingObjective, OtherObjective,
                            URLSource, AdditionalNotes, ExpectedOutcome, TotalPeople,
                            SectionManagerId, Status_SectionManager, Comment_SectionManager, ApproveInfo_SectionManager,
                            DepartmentManagerId, Status_DepartmentManager, Comment_DepartmentManager, ApproveInfo_DepartmentManager,
                            ManagingDirectorId, Status_ManagingDirector, Comment_ManagingDirector, ApproveInfo_ManagingDirector,
                            Status, CreatedDate, CreatedBy
                        FROM [HRDSYSTEM].[dbo].[TrainingRequests]
                        WHERE DocNo = @DocNo AND IsActive = 1";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DocNo", docNo);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var model = new TrainingRequestEditViewModel
                                {
                                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                    DocNo = reader["DocNo"].ToString(),
                                    Company = reader["Company"].ToString(),
                                    TrainingType = reader["TrainingType"].ToString(),
                                    Factory = reader["Factory"].ToString(),
                                    CCEmail = reader["CCEmail"]?.ToString(),
                                    Department = reader["Department"].ToString(),
                                    Position = reader["Position"]?.ToString(),
                                    StartDate = reader["StartDate"] != DBNull.Value ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("StartDate")) : null,
                                    EndDate = reader["EndDate"] != DBNull.Value ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("EndDate")) : null,
                                    SeminarTitle = reader["SeminarTitle"].ToString(),
                                    TrainingLocation = reader["TrainingLocation"].ToString(),
                                    Instructor = reader["Instructor"].ToString(),
                                    RegistrationCost = reader["RegistrationCost"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("RegistrationCost")) : null,
                                    InstructorFee = reader["InstructorFee"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("InstructorFee")) : null,
                                    EquipmentCost = reader["EquipmentCost"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("EquipmentCost")) : null,
                                    FoodCost = reader["FoodCost"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("FoodCost")) : null,
                                    OtherCost = reader["OtherCost"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("OtherCost")) : null,
                                    OtherCostDescription = reader["OtherCostDescription"]?.ToString(),
                                    TotalCost = reader["TotalCost"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("TotalCost")) : null,
                                    CostPerPerson = reader["CostPerPerson"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("CostPerPerson")) : null,
                                    TrainingHours = reader["PerPersonTrainingHours"] != DBNull.Value ? (int?)reader.GetInt32(reader.GetOrdinal("PerPersonTrainingHours")) : null,
                                    TrainingObjective = reader["TrainingObjective"]?.ToString(),
                                    OtherObjective = reader["OtherObjective"]?.ToString(),
                                    URLSource = reader["URLSource"]?.ToString(),
                                    AdditionalNotes = reader["AdditionalNotes"]?.ToString(),
                                    ExpectedOutcome = reader["ExpectedOutcome"]?.ToString(),
                                    ParticipantCount = reader["TotalPeople"]?.ToString(),
                                    SectionManagerId = reader["SectionManagerId"]?.ToString(),
                                    Status_SectionManager = reader["Status_SectionManager"]?.ToString(),
                                    Comment_SectionManager = reader["Comment_SectionManager"]?.ToString(),
                                    ApproveInfo_SectionManager = reader["ApproveInfo_SectionManager"]?.ToString(),
                                    DepartmentManagerId = reader["DepartmentManagerId"]?.ToString(),
                                    Status_DepartmentManager = reader["Status_DepartmentManager"]?.ToString(),
                                    Comment_DepartmentManager = reader["Comment_DepartmentManager"]?.ToString(),
                                    ApproveInfo_DepartmentManager = reader["ApproveInfo_DepartmentManager"]?.ToString(),
                                    ManagingDirectorId = reader["ManagingDirectorId"]?.ToString(),
                                    Status_ManagingDirector = reader["Status_ManagingDirector"]?.ToString(),
                                    Comment_ManagingDirector = reader["Comment_ManagingDirector"]?.ToString(),
                                    ApproveInfo_ManagingDirector = reader["ApproveInfo_ManagingDirector"]?.ToString(),
                                    Status = reader["Status"].ToString(),
                                    CreatedDate = reader.GetDateTime(reader.GetOrdinal("CreatedDate")),
                                    CreatedBy = reader["CreatedBy"].ToString()
                                };

                                // Fetch employees for this training request
                                reader.Close();
                                model.Employees = await GetEmployeesForRequest(conn, model.Id);

                                return View(model);
                            }
                            else
                            {
                                TempData["Error"] = "ไม่พบข้อมูลคำร้องขออบรม";
                                return RedirectToAction("MonthlyRequests", "Home");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in Edit GET: {ex.Message}");
                TempData["Error"] = "เกิดข้อผิดพลาดในการโหลดข้อมูล";
                return RedirectToAction("MonthlyRequests", "Home");
            }
        }

        // ====================================================================
        // POST: /TrainingRequest/UpdateTrainingRequest
        // ====================================================================
        [HttpPost]
        public async Task<IActionResult> UpdateTrainingRequest([FromForm] TrainingRequestFormData formData, [FromForm] string docNo)
        {
            try
            {
                Console.WriteLine($"🔵 UpdateTrainingRequest called for DocNo: {docNo}");

                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    Console.WriteLine("✅ Database connected");

                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // 1. Update main training request data
                            await UpdateTrainingRequestData(conn, transaction, formData, docNo);
                            Console.WriteLine($"✅ Training Request Updated: {docNo}");

                            // 2. Get TrainingRequestId for employee updates
                            int trainingRequestId = await GetTrainingRequestId(conn, transaction, docNo);

                            // 3. Delete existing employees and insert new ones
                            if (!string.IsNullOrEmpty(formData.EmployeesJson))
                            {
                                await DeleteEmployees(conn, transaction, trainingRequestId);
                                await InsertEmployees(conn, transaction, trainingRequestId, formData.EmployeesJson);
                                Console.WriteLine("✅ Employees updated");
                            }

                            // 4. Handle file attachments if provided
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
                                message = "✅ อัพเดทข้อมูลสำเร็จ",
                                docNo = docNo
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
                    [TrainingRequestId], [EmployeeCode], [EmployeeName], [Position], [Level], [Department],
                    [PreviousTrainingHours], [PreviousTrainingCost],
                    [CurrentTrainingHours], [CurrentTrainingCost], [Notes]
                )
                VALUES (
                    @TrainingRequestId, @EmployeeCode, @EmployeeName, @Position, @Level, @Department,
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
                    cmd.Parameters.AddWithValue("@Level", emp.level ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Department", emp.department ?? (object)DBNull.Value);
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
        // Helper Methods for Edit functionality
        // ====================================================================

        private async Task<List<EmployeeViewModel>> GetEmployeesForRequest(SqlConnection conn, int trainingRequestId)
        {
            var employees = new List<EmployeeViewModel>();
            string query = @"
                SELECT
                    EmployeeCode, EmployeeName, Position, Level, Department,
                    PreviousTrainingHours, PreviousTrainingCost,
                    CurrentTrainingHours, CurrentTrainingCost, Notes
                FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees]
                WHERE TrainingRequestId = @TrainingRequestId";

            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@TrainingRequestId", trainingRequestId);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        employees.Add(new EmployeeViewModel
                        {
                            EmployeeCode = reader["EmployeeCode"]?.ToString(),
                            EmployeeName = reader["EmployeeName"]?.ToString(),
                            Position = reader["Position"]?.ToString(),
                            Level = reader["Level"]?.ToString(),
                            Department = reader["Department"]?.ToString(),
                            PreviousTrainingHours = reader["PreviousTrainingHours"] != DBNull.Value ? (int?)reader.GetInt32(reader.GetOrdinal("PreviousTrainingHours")) : null,
                            PreviousTrainingCost = reader["PreviousTrainingCost"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("PreviousTrainingCost")) : null,
                            CurrentTrainingHours = reader["CurrentTrainingHours"] != DBNull.Value ? (int?)reader.GetInt32(reader.GetOrdinal("CurrentTrainingHours")) : null,
                            CurrentTrainingCost = reader["CurrentTrainingCost"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("CurrentTrainingCost")) : null,
                            Notes = reader["Notes"]?.ToString()
                        });
                    }
                }
            }

            return employees;
        }

        private async Task UpdateTrainingRequestData(SqlConnection conn, SqlTransaction transaction,
            TrainingRequestFormData formData, string docNo)
        {
            string query = @"
                UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                SET
                    [Company] = @Company,
                    [TrainingType] = @TrainingType,
                    [Factory] = @Factory,
                    [CCEmail] = @CCEmail,
                    [Department] = @Department,
                    [Position] = @Position,
                    [StartDate] = @StartDate,
                    [EndDate] = @EndDate,
                    [SeminarTitle] = @SeminarTitle,
                    [TrainingLocation] = @TrainingLocation,
                    [Instructor] = @Instructor,
                    [RegistrationCost] = @RegistrationCost,
                    [InstructorFee] = @InstructorFee,
                    [EquipmentCost] = @EquipmentCost,
                    [FoodCost] = @FoodCost,
                    [OtherCost] = @OtherCost,
                    [OtherCostDescription] = @OtherCostDescription,
                    [TotalCost] = @TotalCost,
                    [CostPerPerson] = @CostPerPerson,
                    [PerPersonTrainingHours] = @PerPersonTrainingHours,
                    [TrainingObjective] = @TrainingObjective,
                    [OtherObjective] = @OtherObjective,
                    [URLSource] = @URLSource,
                    [AdditionalNotes] = @AdditionalNotes,
                    [ExpectedOutcome] = @ExpectedOutcome,
                    [TotalPeople] = @TotalPeople,
                    [SectionManagerId] = @SectionManagerId,
                    [Status_SectionManager] = @Status_SectionManager,
                    [Comment_SectionManager] = @Comment_SectionManager,
                    [ApproveInfo_SectionManager] = @ApproveInfo_SectionManager,
                    [DepartmentManagerId] = @DepartmentManagerId,
                    [Status_DepartmentManager] = @Status_DepartmentManager,
                    [Comment_DepartmentManager] = @Comment_DepartmentManager,
                    [ApproveInfo_DepartmentManager] = @ApproveInfo_DepartmentManager,
                    [ManagingDirectorId] = @ManagingDirectorId,
                    [Status_ManagingDirector] = @Status_ManagingDirector,
                    [Comment_ManagingDirector] = @Comment_ManagingDirector,
                    [ApproveInfo_ManagingDirector] = @ApproveInfo_ManagingDirector
                WHERE [DocNo] = @DocNo AND [IsActive] = 1";

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

                // Approvers
                cmd.Parameters.AddWithValue("@SectionManagerId", formData.SectionManagerId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Status_SectionManager", formData.Status_SectionManager ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Comment_SectionManager", formData.Comment_SectionManager ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ApproveInfo_SectionManager", formData.ApproveInfo_SectionManager ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@DepartmentManagerId", formData.DepartmentManagerId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Status_DepartmentManager", formData.Status_DepartmentManager ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Comment_DepartmentManager", formData.Comment_DepartmentManager ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ApproveInfo_DepartmentManager", formData.ApproveInfo_DepartmentManager ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@ManagingDirectorId", formData.ManagingDirectorId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Status_ManagingDirector", formData.Status_ManagingDirector ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Comment_ManagingDirector", formData.Comment_ManagingDirector ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ApproveInfo_ManagingDirector", formData.ApproveInfo_ManagingDirector ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task<int> GetTrainingRequestId(SqlConnection conn, SqlTransaction transaction, string docNo)
        {
            string query = "SELECT Id FROM [HRDSYSTEM].[dbo].[TrainingRequests] WHERE DocNo = @DocNo AND IsActive = 1";

            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@DocNo", docNo);
                var result = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(result);
            }
        }

        private async Task DeleteEmployees(SqlConnection conn, SqlTransaction transaction, int trainingRequestId)
        {
            string query = "DELETE FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] WHERE TrainingRequestId = @TrainingRequestId";

            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@TrainingRequestId", trainingRequestId);
                await cmd.ExecuteNonQueryAsync();
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


        // Controllers/TrainingRequestController.cs - เพิ่ม Action สำหรับดึงข้อมูล

        [HttpGet]
        public async Task<IActionResult> GetMonthlyRequests(
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? docNo = null,
            string? company = null,
            string? status = null)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                var requests = new List<dynamic>();

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();

                    // ตั้งค่าเริ่มต้นเป็นเดือนปัจจุบัน
                    DateTime filterStart = startDate ?? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                    DateTime filterEnd = endDate ?? filterStart.AddMonths(1).AddDays(-1);

                    string query = @"
                SELECT 
                    tr.Id,
                    tr.DocNo,
                    tr.Company,
                    tr.StartDate,
                    tr.SeminarTitle,
                    tr.CreatedBy,
                    tr.Status,
                    tr.CreatedDate,
                    (SELECT COUNT(*) FROM TrainingRequestEmployees WHERE TrainingRequestId = tr.Id) AS ParticipantCount
                FROM TrainingRequests tr
                WHERE CAST(tr.CreatedDate AS DATE) BETWEEN @StartDate AND @EndDate";

                    // เพิ่มเงื่อนไขการกรอง
                    if (!string.IsNullOrEmpty(docNo))
                        query += " AND tr.DocNo LIKE @DocNo";

                    if (!string.IsNullOrEmpty(company))
                        query += " AND tr.Company LIKE @Company";

                    if (!string.IsNullOrEmpty(status))
                        query += " AND tr.Status = @Status";

                    query += " ORDER BY tr.DocNo DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", filterStart);
                        cmd.Parameters.AddWithValue("@EndDate", filterEnd);

                        if (!string.IsNullOrEmpty(docNo))
                            cmd.Parameters.AddWithValue("@DocNo", "%" + docNo + "%");

                        if (!string.IsNullOrEmpty(company))
                            cmd.Parameters.AddWithValue("@Company", "%" + company + "%");

                        if (!string.IsNullOrEmpty(status))
                            cmd.Parameters.AddWithValue("@Status", status);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                requests.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    docNo = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                    company = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                    startDate = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                                    seminarTitle = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                    createdBy = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                    status = reader.IsDBNull(6) ? "" : reader.GetString(6),
                                    createdDate = reader.GetDateTime(7),
                                    participantCount = reader.GetInt32(8)
                                });
                            }
                        }
                    }
                }

                return Json(new { success = true, data = requests, count = requests.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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

        // ✅ Approval Status and Comments for each level
        // Section Manager
        public string? Status_SectionManager { get; set; }
        public string? Comment_SectionManager { get; set; }
        public string? ApproveInfo_SectionManager { get; set; }

        // Department Manager
        public string? Status_DepartmentManager { get; set; }
        public string? Comment_DepartmentManager { get; set; }
        public string? ApproveInfo_DepartmentManager { get; set; }

        // Managing Director
        public string? Status_ManagingDirector { get; set; }
        public string? Comment_ManagingDirector { get; set; }
        public string? ApproveInfo_ManagingDirector { get; set; }
    }

    public class EmployeeData
    {
        public string? empCode { get; set; }
        public string? fullName { get; set; }
        public string? position { get; set; }
        public string? level { get; set; }
        public string? department { get; set; }
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