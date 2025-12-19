using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using TrainingRequestApp.Models;

namespace TrainingRequestApp.Services
{
    public class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly string _connectionString;
        private readonly string _baseUrl;

        // 🆕 Constant สำหรับ "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"
        private const string SKIP_APPROVER = "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ";

        public ApprovalWorkflowService(IConfiguration configuration, IEmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:1253";
        }

        // 🆕 Helper Method: ตรวจสอบว่าเป็น SKIP_APPROVER หรือไม่
        private bool IsSkipApprover(string approverId)
        {
            return string.Equals(approverId?.Trim(), SKIP_APPROVER, StringComparison.OrdinalIgnoreCase);
        }

        #region Helper Methods

        public string GetNextApprovalStatus(string currentStatus)
        {
            return currentStatus switch
            {
                "Pending" => "WAITING_FOR_SECTION_MANAGER",
                "WAITING_FOR_SECTION_MANAGER" => "WAITING_FOR_DEPARTMENT_MANAGER",
                "WAITING_FOR_DEPARTMENT_MANAGER" => "WAITING_FOR_HRD_ADMIN",
                "WAITING_FOR_HRD_ADMIN" => "WAITING_FOR_HRD_CONFIRMATION",
                "WAITING_FOR_HRD_CONFIRMATION" => "WAITING_FOR_MANAGING_DIRECTOR",
                "WAITING_FOR_MANAGING_DIRECTOR" => "APPROVED",
                "Revision Admin" => "WAITING_FOR_HRD_CONFIRMATION",
                _ => currentStatus
            };
        }

        public string GetNextApproverEmail(TrainingRequestEditViewModel request, string nextStatus)
        {
            var email = nextStatus switch
            {
                "WAITING_FOR_SECTION_MANAGER" => request.SectionManagerId,
                "WAITING_FOR_DEPARTMENT_MANAGER" => request.DepartmentManagerId,
                "WAITING_FOR_HRD_ADMIN" => request.HRDAdminId,
                "WAITING_FOR_HRD_CONFIRMATION" => request.HRDConfirmationId,
                "WAITING_FOR_MANAGING_DIRECTOR" => request.ManagingDirectorId,
                "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR" => request.DeputyManagingDirectorId, // 🆕
                _ => null
            };

            // ⚠️ ถ้าเป็น SKIP_APPROVER → return null (ไม่ส่ง email)
            if (IsSkipApprover(email))
                return null;

            return email?.Trim();
        }

        private string GetApproverRoleName(string role)
        {
            return role switch
            {
                "SectionManager" => "ผู้จัดการส่วน (Section Manager)",
                "DepartmentManager" => "ผู้จัดการฝ่าย (Department Manager)",
                "HRDAdmin" => "เจ้าหน้าที่พัฒนาบุคลากร (HRD Admin)",
                "HRDConfirmation" => "ผู้รับรองการฝึกอบรม (HRD Confirmation)",
                "ManagingDirector" => "กรรมการผู้จัดการ (Managing Director)",
                "DeputyManagingDirector" => "รองกรรมการผู้จัดการ (Deputy Managing Director)", // 🆕
                _ => "ผู้อนุมัติ"
            };
        }

        // 🆕 GetNextApprovalStatus ที่รองรับ Skip Logic
        public string GetNextApprovalStatusWithSkip(TrainingRequestEditViewModel request, string currentStatus)
        {
            switch (currentStatus)
            {
                case "Pending":
                case "WAITING_FOR_SECTION_MANAGER":
                    // ข้าม Section Manager แล้ว → ตรวจสอบ Department Manager
                    if (!IsSkipApprover(request.DepartmentManagerId))
                        return "WAITING_FOR_DEPARTMENT_MANAGER";
                    // ข้าม Department Manager → HRD Admin (บังคับมีจริง)
                    return "WAITING_FOR_HRD_ADMIN";

                case "WAITING_FOR_DEPARTMENT_MANAGER":
                    // Department Manager อนุมัติแล้ว → HRD Admin (บังคับมีจริง)
                    return "WAITING_FOR_HRD_ADMIN";

                case "WAITING_FOR_HRD_ADMIN":
                    // HRD Admin อนุมัติแล้ว → HRD Confirmation (บังคับมีจริง)
                    return "WAITING_FOR_HRD_CONFIRMATION";

                case "WAITING_FOR_HRD_CONFIRMATION":
                    // HRD Confirmation อนุมัติแล้ว → ตรวจสอบ Managing Director
                    if (!IsSkipApprover(request.ManagingDirectorId))
                        return "WAITING_FOR_MANAGING_DIRECTOR";
                    // ข้าม MD → ตรวจสอบ Deputy MD
                    if (!IsSkipApprover(request.DeputyManagingDirectorId))
                        return "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR";
                    // ข้ามทั้งคู่ → APPROVED
                    return "APPROVED";

                case "WAITING_FOR_MANAGING_DIRECTOR":
                    // Managing Director อนุมัติแล้ว → ตรวจสอบ Deputy MD
                    if (!IsSkipApprover(request.DeputyManagingDirectorId))
                        return "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR";
                    // ข้าม Deputy MD → APPROVED
                    return "APPROVED";

                case "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR":
                    // 🆕 Deputy MD อนุมัติแล้ว → APPROVED (ท้ายสุด!)
                    return "APPROVED";

                case "Revision Admin":
                    // Revision Admin → กลับไปที่ HRD Confirmation
                    return "WAITING_FOR_HRD_CONFIRMATION";

                default:
                    return currentStatus;
            }
        }

        #endregion

        #region Check Permission

        public async Task<ApprovalPermissionResult> CheckApprovalPermission(string docNo, string userEmail)
        {
            var result = new ApprovalPermissionResult
            {
                CanApprove = false,
                Message = "คุณไม่มีสิทธิ์อนุมัติเอกสารนี้"
            };

            try
            {
                var request = await GetTrainingRequest(docNo);
                if (request == null)
                {
                    result.Message = "ไม่พบเอกสาร";
                    return result;
                }

                result.Request = request;

                // 🔧 Debug logging
                Console.WriteLine($"🔍 CheckApprovalPermission:");
                Console.WriteLine($"   User Email: {userEmail}");
                Console.WriteLine($"   Status: {request.Status}");
                Console.WriteLine($"   Section Manager: {request.SectionManagerId}");
                Console.WriteLine($"   Department Manager: {request.DepartmentManagerId}");
                Console.WriteLine($"   HRD Admin: {request.HRDAdminId}");
                Console.WriteLine($"   HRD Confirmation: {request.HRDConfirmationId}");
                Console.WriteLine($"   Managing Director: {request.ManagingDirectorId}");
                Console.WriteLine($"   Deputy Managing Director: {request.DeputyManagingDirectorId}"); // 🆕

                // ตรวจสอบสิทธิ์ตาม Status และ Email (Case-Insensitive)
                if (request.Status == "WAITING_FOR_SECTION_MANAGER" &&
                    string.Equals(userEmail, request.SectionManagerId, StringComparison.OrdinalIgnoreCase))
                {
                    result.CanApprove = true;
                    result.ApproverRole = "SectionManager";
                    result.Message = "คุณมีสิทธิ์อนุมัติในฐานะ Section Manager";
                    Console.WriteLine($"✅ Permission granted: Section Manager");
                }
                else if (request.Status == "WAITING_FOR_DEPARTMENT_MANAGER" &&
                         string.Equals(userEmail, request.DepartmentManagerId, StringComparison.OrdinalIgnoreCase))
                {
                    result.CanApprove = true;
                    result.ApproverRole = "DepartmentManager";
                    result.Message = "คุณมีสิทธิ์อนุมัติในฐานะ Department Manager";
                    Console.WriteLine($"✅ Permission granted: Department Manager");
                }
                else if (request.Status == "WAITING_FOR_HRD_ADMIN" &&
                         string.Equals(userEmail, request.HRDAdminId, StringComparison.OrdinalIgnoreCase))
                {
                    result.CanApprove = true;
                    result.ApproverRole = "HRDAdmin";
                    result.Message = "คุณมีสิทธิ์อนุมัติในฐานะ HRD Admin";
                    Console.WriteLine($"✅ Permission granted: HRD Admin");
                }
                else if (request.Status == "WAITING_FOR_HRD_CONFIRMATION" &&
                         string.Equals(userEmail, request.HRDConfirmationId, StringComparison.OrdinalIgnoreCase))
                {
                    result.CanApprove = true;
                    result.ApproverRole = "HRDConfirmation";
                    result.Message = "คุณมีสิทธิ์อนุมัติในฐานะ HRD Confirmation";
                    Console.WriteLine($"✅ Permission granted: HRD Confirmation");
                }
                else if (request.Status == "WAITING_FOR_MANAGING_DIRECTOR" &&
                         string.Equals(userEmail, request.ManagingDirectorId, StringComparison.OrdinalIgnoreCase))
                {
                    result.CanApprove = true;
                    result.ApproverRole = "ManagingDirector";
                    result.Message = "คุณมีสิทธิ์อนุมัติในฐานะ Managing Director";
                    Console.WriteLine($"✅ Permission granted: Managing Director");
                }
                // 🆕 Deputy Managing Director
                else if (request.Status == "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR" &&
                         string.Equals(userEmail, request.DeputyManagingDirectorId, StringComparison.OrdinalIgnoreCase))
                {
                    result.CanApprove = true;
                    result.ApproverRole = "DeputyManagingDirector";
                    result.Message = "คุณมีสิทธิ์อนุมัติในฐานะ Deputy Managing Director";
                    Console.WriteLine($"✅ Permission granted: Deputy Managing Director");
                }
                else if (request.Status == "Revision Admin" &&
                         string.Equals(userEmail, request.HRDAdminId, StringComparison.OrdinalIgnoreCase))
                {
                    result.CanApprove = true;
                    result.ApproverRole = "HRDAdmin";
                    result.Message = "คุณมีสิทธิ์ดำเนินการในฐานะ HRD Admin (Revision Admin Mode)";
                    Console.WriteLine($"✅ Permission granted: HRD Admin (Revision Mode)");
                }
                else
                {
                    Console.WriteLine($"❌ Permission denied: User email does not match any approver for current status");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CheckApprovalPermission Error: {ex.Message}");
                result.Message = "เกิดข้อผิดพลาดในการตรวจสอบสิทธิ์";
                return result;
            }
        }

        #endregion

        #region Process Approval

        public async Task<WorkflowResult> ProcessApproval(string docNo, string userEmail, string comment, string ipAddress)
        {
            var result = new WorkflowResult { Success = false };

            try
            {
                // ตรวจสอบสิทธิ์
                var permission = await CheckApprovalPermission(docNo, userEmail);
                if (!permission.CanApprove)
                {
                    result.Message = permission.Message;
                    return result;
                }

                var request = permission.Request;
                string approverRole = permission.ApproverRole;
                string previousStatus = request.Status;

                // อัพเดท Status และ ApproveInfo ตาม Role
                await UpdateApprovalStatus(docNo, approverRole, "APPROVED", comment, userEmail, ipAddress);

                // หา Status ถัดไป (ใช้ GetNextApprovalStatusWithSkip เพื่อรองรับ SKIP_APPROVER)
                string nextStatus = GetNextApprovalStatusWithSkip(request, previousStatus);

                // ⭐ ถ้า previousStatus = "Revision Admin" และ HRD Admin อนุมัติ
                // ต้อง Reset Status_HRDConfirmation และ Status_ManagingDirector เป็น Pending
                if (previousStatus == "Revision Admin" && approverRole == "HRDAdmin")
                {
                    Console.WriteLine($"🔄 Revision Admin → WAITING_FOR_HRD_CONFIRMATION: Resetting HRD Confirmation & Managing Director status");
                    await ResetApprovalStatus(docNo, "HRDAdmin");
                }

                // อัพเดท Status หลัก
                await UpdateMainStatus(docNo, nextStatus, userEmail);

                // บันทึก History
                await SaveApprovalHistory(request.Id, docNo, approverRole, userEmail, "APPROVED", comment, previousStatus, nextStatus, ipAddress);

                // ⭐ Refresh request object เพื่อดึงข้อมูลล่าสุดหลัง UPDATE
                request = await GetTrainingRequest(docNo);

                // ส่ง Email แจ้ง CreatedBy + CCEmail
                await SendApprovalNotificationEmail(request, approverRole, comment);

                // ถ้ายังไม่ APPROVED สุดท้าย ให้ส่ง Email ให้ Approver คนถัดไป
                if (nextStatus != "APPROVED")
                {
                    string nextApproverEmail = GetNextApproverEmail(request, nextStatus);
                    if (!string.IsNullOrEmpty(nextApproverEmail))
                    {
                        await SendApprovalRequestEmail(request, nextApproverEmail, nextStatus);
                    }
                }
                else
                {
                    // APPROVED สมบูรณ์ - ส่ง Email ให้ทุกคน
                    await SendFinalApprovalEmail(request);
                }

                result.Success = true;
                result.Message = "อนุมัติสำเร็จ";
                result.NewStatus = nextStatus;

                Console.WriteLine($"✅ Approval Success: {docNo} → {nextStatus}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ProcessApproval Error: {ex.Message}");
                result.Message = $"เกิดข้อผิดพลาด: {ex.Message}";
                return result;
            }
        }

        #endregion

        #region Process Revise

        public async Task<WorkflowResult> ProcessRevise(string docNo, string userEmail, string comment, string ipAddress)
        {
            var result = new WorkflowResult { Success = false };

            try
            {
                // Validate Comment (บังคับกรอก)
                if (string.IsNullOrWhiteSpace(comment))
                {
                    result.Message = "กรุณากรอกหมายเหตุสำหรับการ Revise";
                    return result;
                }

                var permission = await CheckApprovalPermission(docNo, userEmail);
                if (!permission.CanApprove)
                {
                    result.Message = permission.Message;
                    return result;
                }

                var request = permission.Request;
                string approverRole = permission.ApproverRole;
                string previousStatus = request.Status;

                // ตรวจสอบว่าเป็น Revise กรณีที่ 1 หรือ 2
                bool isRevisionAdminCase = (approverRole == "HRDConfirmation" ||
                                           approverRole == "ManagingDirector" ||
                                           approverRole == "DeputyManagingDirector"); // 🆕

                string newStatus;
                if (isRevisionAdminCase)
                {
                    // กรณีที่ 2: HRD Confirmation/Managing Director/Deputy Managing Director → Revision Admin
                    newStatus = "Revision Admin";
                }
                else
                {
                    // กรณีที่ 1: Section/Dept/HRD Admin → Revise
                    newStatus = "Revise";
                }

                // อัพเดท Status และ ApproveInfo
                await UpdateApprovalStatus(docNo, approverRole, "Revise", comment, userEmail, ipAddress);

                // อัพเดท Status หลัก
                await UpdateMainStatus(docNo, newStatus, userEmail);

                // บันทึก History
                await SaveApprovalHistory(request.Id, docNo, approverRole, userEmail, "Revise", comment, previousStatus, newStatus, ipAddress);

                // ⭐ Refresh request object เพื่อดึงข้อมูลล่าสุดหลัง UPDATE
                request = await GetTrainingRequest(docNo);

                // ส่ง Email
                if (isRevisionAdminCase)
                {
                    // ส่งให้ HRD Admin + CreatedBy + CCEmail
                    await SendRevisionAdminEmail(request, approverRole, comment);
                }
                else
                {
                    // ส่งกลับ CreatedBy + CCEmail
                    await SendReviseEmail(request, approverRole, comment);
                }

                result.Success = true;
                result.Message = "ส่งกลับเพื่อแก้ไขสำเร็จ";
                result.NewStatus = newStatus;

                Console.WriteLine($"✅ Revise Success: {docNo} → {newStatus}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ProcessRevise Error: {ex.Message}");
                result.Message = $"เกิดข้อผิดพลาด: {ex.Message}";
                return result;
            }
        }

        #endregion

        #region Process Reject

        public async Task<WorkflowResult> ProcessReject(string docNo, string userEmail, string comment, string ipAddress)
        {
            var result = new WorkflowResult { Success = false };

            try
            {
                // Validate Comment (บังคับกรอก)
                if (string.IsNullOrWhiteSpace(comment))
                {
                    result.Message = "กรุณากรอกหมายเหตุสำหรับการ Reject";
                    return result;
                }

                var permission = await CheckApprovalPermission(docNo, userEmail);
                if (!permission.CanApprove)
                {
                    result.Message = permission.Message;
                    return result;
                }

                var request = permission.Request;
                string approverRole = permission.ApproverRole;
                string previousStatus = request.Status;
                string newStatus = "REJECTED";

                // อัพเดท Status และ ApproveInfo
                await UpdateApprovalStatus(docNo, approverRole, "REJECTED", comment, userEmail, ipAddress);

                // อัพเดท Status หลัก
                await UpdateMainStatus(docNo, newStatus, userEmail);

                // บันทึก History
                await SaveApprovalHistory(request.Id, docNo, approverRole, userEmail, "REJECTED", comment, previousStatus, newStatus, ipAddress);

                // ⭐ Refresh request object เพื่อดึงข้อมูลล่าสุดหลัง UPDATE
                request = await GetTrainingRequest(docNo);

                // ส่ง Email แจ้ง CreatedBy + CCEmail
                await SendRejectionEmail(request, approverRole, comment);

                result.Success = true;
                result.Message = "ปฏิเสธคำขอสำเร็จ";
                result.NewStatus = newStatus;

                Console.WriteLine($"✅ Reject Success: {docNo} → {newStatus}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ProcessReject Error: {ex.Message}");
                result.Message = $"เกิดข้อผิดพลาด: {ex.Message}";
                return result;
            }
        }

        #endregion

        #region Start Workflow

        public async Task<bool> StartWorkflow(string docNo)
        {
            try
            {
                Console.WriteLine($"\n========================================");
                Console.WriteLine($"🚀 StartWorkflow STARTED: {docNo}");
                Console.WriteLine($"========================================\n");

                var request = await GetTrainingRequest(docNo);
                if (request == null)
                {
                    Console.WriteLine($"❌ [ERROR] Request not found - {docNo}");
                    return false;
                }

                Console.WriteLine($"✅ [STEP 1/5] GetTrainingRequest SUCCESS");
                Console.WriteLine($"   DocNo: {request.DocNo}");
                Console.WriteLine($"   Status: {request.Status}");
                Console.WriteLine($"   CreatedBy: {request.CreatedBy}");

                // ⭐ Validation: เช็คว่ามีผู้อนุมัติหรือไม่
                Console.WriteLine($"\n📋 [STEP 2/5] Validating Approver Assignments:");
                Console.WriteLine($"   Section Manager: {request.SectionManagerId ?? "❌ NOT ASSIGNED"}");
                Console.WriteLine($"   Department Manager: {request.DepartmentManagerId ?? "⚠️ NOT ASSIGNED"}");
                Console.WriteLine($"   HRD Admin: {request.HRDAdminId ?? "⚠️ NOT ASSIGNED"}");
                Console.WriteLine($"   HRD Confirmation: {request.HRDConfirmationId ?? "⚠️ NOT ASSIGNED"}");
                Console.WriteLine($"   Managing Director: {request.ManagingDirectorId ?? "⚠️ NOT ASSIGNED"}");
                Console.WriteLine($"   Deputy Managing Director: {request.DeputyManagingDirectorId ?? "⚠️ NOT ASSIGNED"}");

                // ตรวจสอบว่ามี approver ที่จำเป็นหรือไม่
                if (string.IsNullOrWhiteSpace(request.SectionManagerId))
                {
                    Console.WriteLine($"\n❌ [ERROR] Section Manager not assigned!");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(request.HRDAdminId) || IsSkipApprover(request.HRDAdminId))
                {
                    Console.WriteLine($"\n❌ [ERROR] HRD Admin is required and cannot be skipped!");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(request.HRDConfirmationId) || IsSkipApprover(request.HRDConfirmationId))
                {
                    Console.WriteLine($"\n❌ [ERROR] HRD Confirmation is required and cannot be skipped!");
                    return false;
                }

                Console.WriteLine($"✅ [STEP 2/5] Validation SUCCESS");

                // ⭐ Dynamic first approver - หาผู้อนุมัติคนแรกที่ไม่ใช่ SKIP
                string firstApprover;
                string firstStatus;

                if (!IsSkipApprover(request.SectionManagerId))
                {
                    firstApprover = request.SectionManagerId;
                    firstStatus = "WAITING_FOR_SECTION_MANAGER";
                    Console.WriteLine($"📍 First Approver: Section Manager ({firstApprover})");
                }
                else if (!IsSkipApprover(request.DepartmentManagerId))
                {
                    firstApprover = request.DepartmentManagerId;
                    firstStatus = "WAITING_FOR_DEPARTMENT_MANAGER";
                    Console.WriteLine($"📍 First Approver: Department Manager ({firstApprover}) - Section Manager skipped");
                }
                else
                {
                    firstApprover = request.HRDAdminId;
                    firstStatus = "WAITING_FOR_HRD_ADMIN";
                    Console.WriteLine($"📍 First Approver: HRD Admin ({firstApprover}) - Section & Department skipped");
                }

                // ส่ง Email #1: แจ้ง CreatedBy + CCEmail
                Console.WriteLine($"\n📧 [STEP 3/5] Sending Pending Notification Email...");
                Console.WriteLine($"   To: {request.CreatedBy}");
                if (!string.IsNullOrEmpty(request.CCEmail))
                {
                    Console.WriteLine($"   CC: {request.CCEmail}");
                }

                await SendPendingNotificationEmail(request);
                Console.WriteLine($"✅ [STEP 3/5] Pending Notification Email sent");

                // เพิ่ม delay เล็กน้อยเพื่อไม่ให้ส่ง email ติดกันเกินไป
                await Task.Delay(500);

                // อัพเดท Status
                Console.WriteLine($"\n📝 [STEP 4/5] Updating Status to {firstStatus}...");
                Console.WriteLine($"   DocNo: {docNo}");
                Console.WriteLine($"   Current Status: {request.Status}");
                Console.WriteLine($"   New Status: {firstStatus}");

                await UpdateMainStatus(docNo, firstStatus);
                Console.WriteLine($"✅ [STEP 4/5] Status Update SUCCESS");

                // เพิ่ม delay เล็กน้อย
                await Task.Delay(500);

                // ส่ง Email #2: ขออนุมัติจากผู้อนุมัติคนแรก
                Console.WriteLine($"\n📧 [STEP 5/5] Sending Approval Request Email...");
                Console.WriteLine($"   To: {firstApprover}");
                Console.WriteLine($"   Status: {firstStatus}");

                await SendApprovalRequestEmail(request, firstApprover, firstStatus);
                Console.WriteLine($"✅ [STEP 5/5] Approval Request Email sent");

                Console.WriteLine($"\n========================================");
                Console.WriteLine($"✅ ✅ ✅ StartWorkflow SUCCESS: {docNo}");
                Console.WriteLine($"========================================\n");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n========================================");
                Console.WriteLine($"❌ ❌ ❌ StartWorkflow FAILED: {docNo}");
                Console.WriteLine($"========================================");
                Console.WriteLine($"Error Type: {ex.GetType().Name}");
                Console.WriteLine($"Error Message: {ex.Message}");
                Console.WriteLine($"StackTrace:\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"========================================\n");
                return false;
            }
        }

        #endregion

        #region Reset Approval Status

        public async Task ResetApprovalStatus(string docNo, string resetType)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = "";

                    // ⭐ แยก 2 กรณีอย่างชัดเจน
                    if (resetType == "HRDAdmin" || resetType == "RevisionAdmin")
                    {
                        // กรณี 2: Revision Admin → Reset เฉพาะ ระดับ 4-6 (ไม่แตะ ระดับ 1-3!)
                        query = @"
                            UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                            SET
                                Status_HRDConfirmation = 'Pending',
                                ApproveInfo_HRDConfirmation = NULL,
                                Status_ManagingDirector = 'Pending',
                                ApproveInfo_ManagingDirector = NULL,
                                Status_DeputyManagingDirector = 'Pending',
                                ApproveInfo_DeputyManagingDirector = NULL
                            WHERE DocNo = @DocNo";

                        Console.WriteLine($"🔄 Resetting Level 4-6 (HRD Confirmation + Managing Director + Deputy Managing Director) for {docNo}");
                    }
                    else
                    {
                        // กรณี 1: Revise → Reset เฉพาะ ระดับ 1-3 (ไม่แตะ ระดับ 4-6!)
                        query = @"
                            UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                            SET
                                Status_SectionManager = 'Pending',
                                ApproveInfo_SectionManager = NULL,
                                Status_DepartmentManager = 'Pending',
                                ApproveInfo_DepartmentManager = NULL,
                                Status_HRDAdmin = 'Pending',
                                ApproveInfo_HRDAdmin = NULL
                            WHERE DocNo = @DocNo";

                        Console.WriteLine($"🔄 Resetting Level 1-3 (Section + Dept + HRD Admin) for {docNo}");
                    }

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@DocNo", docNo);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                Console.WriteLine($"✅ Reset Approval Status: {docNo} (Type: {resetType})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ResetApprovalStatus Error: {ex.Message}");
            }
        }

        #endregion

        #region Retry Email

        /// <summary>
        /// Retry Email - ส่ง Email ขออนุมัติซ้ำ (1 ฉบับเดียว)
        /// สำหรับ Admin/System Admin เท่านั้น
        /// ส่งหา: Approver (To) + CreatedBy + CC + Admin ที่กด (CC)
        /// </summary>
        public async Task<WorkflowResult> RetryEmail(string docNo, string adminEmail, string ipAddress)
        {
            var result = new WorkflowResult { Success = false };

            try
            {
                Console.WriteLine($"\n========================================");
                Console.WriteLine($"🔄 RetryEmail STARTED: {docNo}");
                Console.WriteLine($"  Admin: {adminEmail}");
                Console.WriteLine($"  IP: {ipAddress}");
                Console.WriteLine($"========================================\n");

                var request = await GetTrainingRequest(docNo);
                if (request == null)
                {
                    result.Message = "ไม่พบเอกสาร";
                    Console.WriteLine($"❌ Request not found");
                    return result;
                }

                string currentStatus = request.Status;
                Console.WriteLine($"📋 Current Status: {currentStatus}");

                // ตรวจสอบว่า Status สามารถ Retry Email ได้หรือไม่
                // ⚠️ Block เฉพาะ REJECTED (เพราะเอกสารถูกปฏิเสธแล้ว ไม่มีผู้อนุมัติ)
                // Pending, APPROVED, WAITING_XXX, Revise, Revision Admin → ส่งได้
                if (string.Equals(currentStatus, "REJECTED", StringComparison.OrdinalIgnoreCase))
                {
                    result.Message = $"ไม่สามารถ Retry Email สำหรับเอกสารที่ถูกปฏิเสธ (REJECTED)";
                    Console.WriteLine($"⚠️ Cannot retry email for REJECTED status");
                    return result;
                }

                // หาผู้อนุมัติคนปัจจุบัน
                string nextApproverEmail = GetNextApproverEmail(request, currentStatus);
                Console.WriteLine($"📧 Next Approver: {nextApproverEmail ?? "N/A"}");

                // ส่ง Email ขออนุมัติ (1 ฉบับเดียว) พร้อม CC ทุกคน
                if (!string.IsNullOrEmpty(nextApproverEmail))
                {
                    Console.WriteLine($"\n📧 Sending approval request with CC...");
                    Console.WriteLine($"   To: {nextApproverEmail}");
                    Console.WriteLine($"   CC: CreatedBy + CC + Admin ({adminEmail})");

                    await SendApprovalRequestEmailWithCC(request, nextApproverEmail, currentStatus, adminEmail);
                }
                else
                {
                    Console.WriteLine($"\n⚠️ No approver email found for status: {currentStatus}");
                    result.Message = $"ไม่พบ Email ของผู้อนุมัติสำหรับสถานะ: {currentStatus}";
                    return result;
                }

                // บันทึก Retry History
                Console.WriteLine($"\n💾 Saving Retry History...");
                await SaveRetryHistory(request.Id, docNo, adminEmail, currentStatus, nextApproverEmail, ipAddress);

                result.Success = true;
                result.Message = $"✅ ส่ง Email ซ้ำสำเร็จ (ผู้อนุมัติ: {nextApproverEmail})";

                Console.WriteLine($"\n========================================");
                Console.WriteLine($"✅ RetryEmail SUCCESS: {docNo}");
                Console.WriteLine($"========================================\n");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n========================================");
                Console.WriteLine($"❌ RetryEmail FAILED: {docNo}");
                Console.WriteLine($"========================================");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"StackTrace:\n{ex.StackTrace}");

                result.Message = $"เกิดข้อผิดพลาด: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// แปลง Status เป็นชื่อที่อ่านง่าย
        /// </summary>
        private string GetStatusDisplayName(string status)
        {
            return status switch
            {
                "WAITING_FOR_SECTION_MANAGER" => "รอ Section Manager อนุมัติ",
                "WAITING_FOR_DEPARTMENT_MANAGER" => "รอ Department Manager อนุมัติ",
                "WAITING_FOR_HRD_ADMIN" => "รอ HRD Admin อนุมัติ",
                "WAITING_FOR_HRD_CONFIRMATION" => "รอ HRD Confirmation อนุมัติ",
                "WAITING_FOR_MANAGING_DIRECTOR" => "รอ Managing Director อนุมัติ",
                "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR" => "รอ Deputy Managing Director อนุมัติ", // 🆕
                "Revise" => "ส่งกลับแก้ไข",
                "Revision Admin" => "ส่งกลับ HRD Admin แก้ไข",
                "APPROVED" => "อนุมัติสมบูรณ์",
                "REJECTED" => "ไม่อนุมัติ",
                _ => status
            };
        }

        #endregion

        #region Database Methods

        private async Task<TrainingRequestEditViewModel> GetTrainingRequest(string docNo)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string query = @"
                    SELECT
                        Id, DocNo, Company, TrainingType, SeminarTitle, TrainingLocation,
                        TotalCost, StartDate, Status, CreatedBy, CCEmail,
                        SectionManagerId, Status_SectionManager, Comment_SectionManager, ApproveInfo_SectionManager,
                        DepartmentManagerId, Status_DepartmentManager, Comment_DepartmentManager, ApproveInfo_DepartmentManager,
                        HRDAdminid AS HRDAdminId, Status_HRDAdmin, Comment_HRDAdmin, ApproveInfo_HRDAdmin,
                        HRDConfirmationid AS HRDConfirmationId, Status_HRDConfirmation, Comment_HRDConfirmation, ApproveInfo_HRDConfirmation,
                        ManagingDirectorId, Status_ManagingDirector, Comment_ManagingDirector, ApproveInfo_ManagingDirector,
                        DeputyManagingDirectorId, Status_DeputyManagingDirector, Comment_DeputyManagingDirector, ApproveInfo_DeputyManagingDirector,
                        TrainingObjective, ExpectedOutcome
                    FROM [HRDSYSTEM].[dbo].[TrainingRequests]
                    WHERE DocNo = @DocNo AND IsActive = 1";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DocNo", docNo);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new TrainingRequestEditViewModel
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                DocNo = reader["DocNo"].ToString(),
                                Company = reader["Company"].ToString(),
                                TrainingType = reader["TrainingType"].ToString(),
                                SeminarTitle = reader["SeminarTitle"].ToString(),
                                TrainingLocation = reader["TrainingLocation"]?.ToString(),
                                TotalCost = reader["TotalCost"] != DBNull.Value ? (decimal?)reader.GetDecimal(reader.GetOrdinal("TotalCost")) : null,
                                StartDate = reader["StartDate"] != DBNull.Value ? (DateTime?)reader.GetDateTime(reader.GetOrdinal("StartDate")) : null,
                                Status = reader["Status"].ToString(),
                                CreatedBy = reader["CreatedBy"].ToString(),
                                CCEmail = reader["CCEmail"]?.ToString(),
                                SectionManagerId = reader["SectionManagerId"]?.ToString(),
                                Status_SectionManager = reader["Status_SectionManager"]?.ToString(),
                                Comment_SectionManager = reader["Comment_SectionManager"]?.ToString(),
                                ApproveInfo_SectionManager = reader["ApproveInfo_SectionManager"]?.ToString(),
                                DepartmentManagerId = reader["DepartmentManagerId"]?.ToString(),
                                Status_DepartmentManager = reader["Status_DepartmentManager"]?.ToString(),
                                Comment_DepartmentManager = reader["Comment_DepartmentManager"]?.ToString(),
                                ApproveInfo_DepartmentManager = reader["ApproveInfo_DepartmentManager"]?.ToString(),
                                HRDAdminId = reader["HRDAdminId"]?.ToString(),
                                Status_HRDAdmin = reader["Status_HRDAdmin"]?.ToString(),
                                Comment_HRDAdmin = reader["Comment_HRDAdmin"]?.ToString(),
                                ApproveInfo_HRDAdmin = reader["ApproveInfo_HRDAdmin"]?.ToString(),
                                HRDConfirmationId = reader["HRDConfirmationId"]?.ToString(),
                                Status_HRDConfirmation = reader["Status_HRDConfirmation"]?.ToString(),
                                Comment_HRDConfirmation = reader["Comment_HRDConfirmation"]?.ToString(),
                                ApproveInfo_HRDConfirmation = reader["ApproveInfo_HRDConfirmation"]?.ToString(),
                                ManagingDirectorId = reader["ManagingDirectorId"]?.ToString(),
                                Status_ManagingDirector = reader["Status_ManagingDirector"]?.ToString(),
                                Comment_ManagingDirector = reader["Comment_ManagingDirector"]?.ToString(),
                                ApproveInfo_ManagingDirector = reader["ApproveInfo_ManagingDirector"]?.ToString(),
                                DeputyManagingDirectorId = reader["DeputyManagingDirectorId"]?.ToString(),
                                Status_DeputyManagingDirector = reader["Status_DeputyManagingDirector"]?.ToString(),
                                Comment_DeputyManagingDirector = reader["Comment_DeputyManagingDirector"]?.ToString(),
                                ApproveInfo_DeputyManagingDirector = reader["ApproveInfo_DeputyManagingDirector"]?.ToString(),
                                TrainingObjective = reader["TrainingObjective"]?.ToString(),
                                ExpectedOutcome = reader["ExpectedOutcome"]?.ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }

        private async Task UpdateApprovalStatus(string docNo, string approverRole, string status, string comment, string approverEmail, string ipAddress)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                string approveInfo = $"{approverEmail} / {DateTime.Now:dd/MM/yyyy} / {DateTime.Now:HH:mm}";

                string query = approverRole switch
                {
                    "SectionManager" => @"
                        UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                        SET Status_SectionManager = @Status,
                            Comment_SectionManager = @Comment,
                            ApproveInfo_SectionManager = @ApproveInfo
                        WHERE DocNo = @DocNo",
                    "DepartmentManager" => @"
                        UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                        SET Status_DepartmentManager = @Status,
                            Comment_DepartmentManager = @Comment,
                            ApproveInfo_DepartmentManager = @ApproveInfo
                        WHERE DocNo = @DocNo",
                    "HRDAdmin" => @"
                        UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                        SET Status_HRDAdmin = @Status,
                            Comment_HRDAdmin = @Comment,
                            ApproveInfo_HRDAdmin = @ApproveInfo
                        WHERE DocNo = @DocNo",
                    "HRDConfirmation" => @"
                        UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                        SET Status_HRDConfirmation = @Status,
                            Comment_HRDConfirmation = @Comment,
                            ApproveInfo_HRDConfirmation = @ApproveInfo
                        WHERE DocNo = @DocNo",
                    "ManagingDirector" => @"
                        UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                        SET Status_ManagingDirector = @Status,
                            Comment_ManagingDirector = @Comment,
                            ApproveInfo_ManagingDirector = @ApproveInfo
                        WHERE DocNo = @DocNo",
                    // 🆕 Deputy Managing Director
                    "DeputyManagingDirector" => @"
                        UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                        SET Status_DeputyManagingDirector = @Status,
                            Comment_DeputyManagingDirector = @Comment,
                            ApproveInfo_DeputyManagingDirector = @ApproveInfo
                        WHERE DocNo = @DocNo",
                    _ => null
                };

                if (query != null)
                {
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", status);
                        cmd.Parameters.AddWithValue("@Comment", comment ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@ApproveInfo", approveInfo);
                        cmd.Parameters.AddWithValue("@DocNo", docNo);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        private async Task UpdateMainStatus(string docNo, string newStatus, string updatedBy = "SYSTEM")
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
                        SET Status = @Status, UpdatedDate = GETDATE(), UpdatedBy = @UpdatedBy
                        WHERE DocNo = @DocNo";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Status", newStatus);
                        cmd.Parameters.AddWithValue("@UpdatedBy", updatedBy);
                        cmd.Parameters.AddWithValue("@DocNo", docNo);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ UpdateMainStatus: {docNo} → {newStatus} by {updatedBy} (Rows affected: {rowsAffected})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ UpdateMainStatus Error: {ex.Message}");
                Console.WriteLine($"   DocNo: {docNo}");
                Console.WriteLine($"   NewStatus: {newStatus}");
                Console.WriteLine($"   StackTrace: {ex.StackTrace}");
                throw; // Re-throw เพื่อให้ StartWorkflow catch ได้
            }
        }

        private async Task SaveApprovalHistory(int trainingRequestId, string docNo, string approverRole, string approverEmail, string action, string comment, string previousStatus, string newStatus, string ipAddress)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        INSERT INTO [HRDSYSTEM].[dbo].[ApprovalHistory]
                        (TrainingRequestId, DocNo, ApproverRole, ApproverEmail, Action, Comment, ActionDate, PreviousStatus, NewStatus, IpAddress)
                        VALUES
                        (@TrainingRequestId, @DocNo, @ApproverRole, @ApproverEmail, @Action, @Comment, GETDATE(), @PreviousStatus, @NewStatus, @IpAddress)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TrainingRequestId", trainingRequestId);
                        cmd.Parameters.AddWithValue("@DocNo", docNo);
                        cmd.Parameters.AddWithValue("@ApproverRole", approverRole);
                        cmd.Parameters.AddWithValue("@ApproverEmail", approverEmail);
                        cmd.Parameters.AddWithValue("@Action", action);
                        cmd.Parameters.AddWithValue("@Comment", comment ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PreviousStatus", previousStatus);
                        cmd.Parameters.AddWithValue("@NewStatus", newStatus);
                        cmd.Parameters.AddWithValue("@IpAddress", ipAddress ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ SaveApprovalHistory Error: {ex.Message}");
            }
        }

        private async Task SaveRetryHistory(int trainingRequestId, string docNo, string retryBy, string statusAtRetry, string approverEmail, string ipAddress)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"
                        INSERT INTO [HRDSYSTEM].[dbo].[RetryEmailHistory]
                        (TrainingRequestId, DocNo, RetryBy, RetryDate, StatusAtRetry, ApproverEmail, IPAddress)
                        VALUES
                        (@TrainingRequestId, @DocNo, @RetryBy, GETDATE(), @StatusAtRetry, @ApproverEmail, @IPAddress)";

                    using (var cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TrainingRequestId", trainingRequestId);
                        cmd.Parameters.AddWithValue("@DocNo", docNo);
                        cmd.Parameters.AddWithValue("@RetryBy", retryBy);
                        cmd.Parameters.AddWithValue("@StatusAtRetry", statusAtRetry);
                        cmd.Parameters.AddWithValue("@ApproverEmail", approverEmail ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@IPAddress", ipAddress ?? (object)DBNull.Value);

                        await cmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"✅ SaveRetryHistory: {docNo} by {retryBy} (Status: {statusAtRetry})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ SaveRetryHistory Error: {ex.Message}");
            }
        }

        #endregion

        #region Email Methods

        private async Task SendApprovalRequestEmail(TrainingRequestEditViewModel request, string approverEmail, string statusWaitingFor)
        {
            // ⭐ Validation: เช็คว่ามี approver email หรือไม่
            if (string.IsNullOrWhiteSpace(approverEmail))
            {
                Console.WriteLine($"⚠️ SendApprovalRequestEmail: Approver email is NULL or EMPTY!");
                Console.WriteLine($"   DocNo: {request.DocNo}");
                Console.WriteLine($"   Status: {statusWaitingFor}");
                Console.WriteLine($"   ❌ Cannot send approval email - Please assign approver first!");
                return; // ไม่ส่ง email ถ้าไม่มี email
            }

            string approverRoleName = statusWaitingFor switch
            {
                "WAITING_FOR_SECTION_MANAGER" => "ผู้จัดการส่วน (Section Manager)",
                "WAITING_FOR_DEPARTMENT_MANAGER" => "ผู้จัดการฝ่าย (Department Manager)",
                "WAITING_FOR_HRD_ADMIN" => "เจ้าหน้าที่พัฒนาบุคลากร (HRD Admin)",
                "WAITING_FOR_HRD_CONFIRMATION" => "ผู้รับรองการฝึกอบรม (HRD Confirmation)",
                "WAITING_FOR_MANAGING_DIRECTOR" => "กรรมการผู้จัดการ (Managing Director)",
                "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR" => "รองกรรมการผู้จัดการ (Deputy Managing Director)", // 🆕
                _ => "ผู้อนุมัติ"
            };

            string subject = $"ขออนุมัติ {request.TrainingType} {request.DocNo}";
            string approvalLink = $"{_baseUrl}/TrainingRequest/Edit?docNo={request.DocNo}";

            string body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #ffffff; padding: 30px; border-left: 1px solid #e0e0e0; border-right: 1px solid #e0e0e0; }}
        .btn {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .info-box {{ background: #f8f9fa; padding: 15px; border-left: 4px solid #667eea; margin: 15px 0; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; color: #666; }}
        .status-badge {{ display: inline-block; padding: 5px 10px; border-radius: 3px; font-size: 12px; }}
        .status-pending {{ background: #ffc107; color: #000; }}
        .status-approved {{ background: #28a745; color: #fff; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>📧 แจ้งขออนุมัติคำขอฝึกอบรม</h2>
        </div>
        <div class='content'>
            <p>เรียน <strong>{approverRoleName}</strong></p>

            <p>มีคำขอฝึกอบรมรอการอนุมัติจากท่าน</p>

            <div class='info-box'>
                <strong>📄 เลขที่เอกสาร:</strong> {request.DocNo}<br>
                <strong>🏢 บริษัท:</strong> {request.Company}<br>
                <strong>📚 ประเภท:</strong> {request.TrainingType}<br>
                <strong>📖 หัวข้อ:</strong> {request.SeminarTitle}<br>
                <strong>📍 สถานที่:</strong> {request.TrainingLocation}<br>
                <strong>💰 ค่าใช้จ่าย:</strong> {request.TotalCost:N2} บาท<br>
                <strong>🎯 วัตถุประสงค์:</strong> {request.TrainingObjective}<br>
                <strong>✨ ผลที่คาดว่าจะได้รับ:</strong> {request.ExpectedOutcome}
            </div>

            <div style='text-align: center;'>
                <a href='{approvalLink}' class='btn'>คลิกที่นี่เพื่อดูรายละเอียดและอนุมัติ</a>
            </div>

            <hr style='margin: 30px 0;'>

            <h3>📊 สถานะการอนุมัติ</h3>
            {GenerateApprovalStatusHtml(request)}
        </div>
        <div class='footer'>
            <p>ระบบ Training Request Management</p>
            <p><small>Email นี้ถูกส่งอัตโนมัติ กรุณาอย่าตอบกลับ</small></p>
        </div>
    </div>
</body>
</html>";

            await _emailService.SendEmailAsync(approverEmail, subject, body, request.Id, "APPROVAL_REQUEST", request.DocNo);
        }

        /// <summary>
        /// ส่ง Email ขออนุมัติพร้อม CC (สำหรับ Retry Email)
        /// ส่งหา: Approver (To) + CreatedBy + CC + Admin ที่กด (CC)
        /// </summary>
        private async Task SendApprovalRequestEmailWithCC(TrainingRequestEditViewModel request, string approverEmail, string statusWaitingFor, string adminRetryEmail = null)
        {
            // ⭐ Validation: เช็คว่ามี approver email หรือไม่
            if (string.IsNullOrWhiteSpace(approverEmail))
            {
                Console.WriteLine($"⚠️ SendApprovalRequestEmailWithCC: Approver email is NULL or EMPTY!");
                Console.WriteLine($"   DocNo: {request.DocNo}");
                Console.WriteLine($"   Status: {statusWaitingFor}");
                Console.WriteLine($"   ❌ Cannot send approval email - Please assign approver first!");
                return;
            }

            string approverRoleName = statusWaitingFor switch
            {
                "WAITING_FOR_SECTION_MANAGER" => "ผู้จัดการส่วน (Section Manager)",
                "WAITING_FOR_DEPARTMENT_MANAGER" => "ผู้จัดการฝ่าย (Department Manager)",
                "WAITING_FOR_HRD_ADMIN" => "เจ้าหน้าที่พัฒนาบุคลากร (HRD Admin)",
                "WAITING_FOR_HRD_CONFIRMATION" => "ผู้รับรองการฝึกอบรม (HRD Confirmation)",
                "WAITING_FOR_MANAGING_DIRECTOR" => "กรรมการผู้จัดการ (Managing Director)",
                "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR" => "รองกรรมการผู้จัดการ (Deputy Managing Director)", // 🆕
                _ => "ผู้อนุมัติ"
            };

            string subject = $"🔄 Retry Email - ขออนุมัติ {request.TrainingType} {request.DocNo}";
            string approvalLink = $"{_baseUrl}/TrainingRequest/Edit?docNo={request.DocNo}";

            // เพิ่มข้อความแจ้ง Admin ที่กด Retry
            string retryInfoHtml = !string.IsNullOrEmpty(adminRetryEmail)
                ? $"<div style='background: #d1ecf1; padding: 15px; border-left: 4px solid #17a2b8; margin: 15px 0;'><strong>🔄 Retry Email:</strong> ถูกส่งโดย Admin: {adminRetryEmail}</div>"
                : "";

            string body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .content {{ background: #ffffff; padding: 30px; border-left: 1px solid #e0e0e0; border-right: 1px solid #e0e0e0; }}
        .btn {{ display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .info-box {{ background: #f8f9fa; padding: 15px; border-left: 4px solid #667eea; margin: 15px 0; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; border-radius: 0 0 10px 10px; color: #666; }}
        .status-badge {{ display: inline-block; padding: 5px 10px; border-radius: 3px; font-size: 12px; }}
        .status-pending {{ background: #ffc107; color: #000; }}
        .status-approved {{ background: #28a745; color: #fff; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>📧 แจ้งขออนุมัติคำขอฝึกอบรม</h2>
        </div>
        <div class='content'>
            {retryInfoHtml}

            <p>เรียน <strong>{approverRoleName}</strong></p>

            <p>มีคำขอฝึกอบรมรอการอนุมัติจากท่าน</p>

            <div class='info-box'>
                <strong>📄 เลขที่เอกสาร:</strong> {request.DocNo}<br>
                <strong>🏢 บริษัท:</strong> {request.Company}<br>
                <strong>📚 ประเภท:</strong> {request.TrainingType}<br>
                <strong>📖 หัวข้อ:</strong> {request.SeminarTitle}<br>
                <strong>📍 สถานที่:</strong> {request.TrainingLocation}<br>
                <strong>💰 ค่าใช้จ่าย:</strong> {request.TotalCost:N2} บาท<br>
                <strong>🎯 วัตถุประสงค์:</strong> {request.TrainingObjective}<br>
                <strong>✨ ผลที่คาดว่าจะได้รับ:</strong> {request.ExpectedOutcome}
            </div>

            <div style='text-align: center;'>
                <a href='{approvalLink}' class='btn'>คลิกที่นี่เพื่อดูรายละเอียดและอนุมัติ</a>
            </div>

            <hr style='margin: 30px 0;'>

            <h3>📊 สถานะการอนุมัติ</h3>
            {GenerateApprovalStatusHtml(request)}
        </div>
        <div class='footer'>
            <p>ระบบ Training Request Management</p>
            <p><small>Email นี้ถูกส่งอัตโนมัติ กรุณาอย่าตอบกลับ</small></p>
        </div>
    </div>
</body>
</html>";

            // สร้าง CC List: CreatedBy + CC + Admin ที่กด
            var ccList = new System.Collections.Generic.List<string> { request.CreatedBy };

            if (!string.IsNullOrEmpty(request.CCEmail))
            {
                ccList.AddRange(request.CCEmail.Split(',').Select(e => e.Trim()));
            }

            if (!string.IsNullOrEmpty(adminRetryEmail))
            {
                ccList.Add(adminRetryEmail);
            }

            await _emailService.SendEmailWithCCAsync(approverEmail, ccList.ToArray(), subject, body, request.Id, "RETRY_APPROVAL_REQUEST", request.DocNo);
        }

        private async Task SendApprovalNotificationEmail(TrainingRequestEditViewModel request, string approverRole, string comment)
        {
            string approverRoleName = GetApproverRoleName(approverRole);
            string subject = $"อนุมัติ {request.TrainingType} {request.DocNo}";
            string docLink = $"{_baseUrl}/TrainingRequest/Edit?docNo={request.DocNo}";

            string body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: #28a745; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
            <h2>✅ การอนุมัติสำเร็จ</h2>
        </div>
        <div style='background: #ffffff; padding: 20px; border: 1px solid #e0e0e0;'>
            <p>เรียน ผู้เกี่ยวข้อง</p>

            <p>คำขอฝึกอบรม <strong>{request.DocNo}</strong> ได้รับการอนุมัติจาก <strong>{approverRoleName}</strong> แล้ว</p>

            <div style='background: #d4edda; padding: 15px; border-left: 4px solid #28a745; margin: 15px 0;'>
                <strong>📄 เอกสาร:</strong> {request.DocNo}<br>
                <strong>📖 หัวข้อ:</strong> {request.SeminarTitle}<br>
                <strong>✅ ผู้อนุมัติ:</strong> {approverRoleName}<br>
                <strong>💬 หมายเหตุ:</strong> {(string.IsNullOrEmpty(comment) ? "-" : comment)}
            </div>

            <div style='text-align: center; margin: 20px 0;'>
                <a href='{docLink}' style='display: inline-block; padding: 12px 30px; background: #28a745; color: white; text-decoration: none; border-radius: 5px;'>ดูรายละเอียดเอกสาร</a>
            </div>

            <h3>📊 สถานะการอนุมัติ</h3>
            {GenerateApprovalStatusHtml(request)}
        </div>
    </div>
</body>
</html>";

            // ส่งให้ CreatedBy + CC ในฉบับเดียว
            var ccEmails = !string.IsNullOrEmpty(request.CCEmail)
                ? request.CCEmail.Split(',').Select(e => e.Trim()).ToArray()
                : null;

            await _emailService.SendEmailWithCCAsync(request.CreatedBy, ccEmails, subject, body, request.Id, "APPROVAL_NOTIFICATION", request.DocNo);
        }

        private async Task SendPendingNotificationEmail(TrainingRequestEditViewModel request)
        {
            string subject = $"แจ้งการเปิดคำขอฝึกอบรม {request.TrainingType} {request.DocNo}";
            string docLink = $"{_baseUrl}/TrainingRequest/Edit?docNo={request.DocNo}";

            string body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: #007bff; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
            <h2>📌 แจ้งการเปิดคำขอฝึกอบรม</h2>
        </div>
        <div style='background: #ffffff; padding: 20px; border: 1px solid #e0e0e0;'>
            <p>เรียน {request.CreatedBy}</p>

            <p>คำขอฝึกอบรมของท่านได้ถูกเปิดเรียบร้อยแล้ว</p>

            <div style='background: #e7f3ff; padding: 15px; border-left: 4px solid #007bff; margin: 15px 0;'>
                <strong>📄 เลขที่เอกสาร:</strong> {request.DocNo}<br>
                <strong>📖 หัวข้อ:</strong> {request.SeminarTitle}<br>
                <strong>📍 สถานที่:</strong> {request.TrainingLocation}<br>
                <strong>💰 ค่าใช้จ่าย:</strong> {request.TotalCost:N2} บาท
            </div>

            <p>ระบบกำลังส่งคำขอไปยังผู้อนุมัติ</p>

            <div style='text-align: center; margin: 20px 0;'>
                <a href='{docLink}' style='display: inline-block; padding: 12px 30px; background: #007bff; color: white; text-decoration: none; border-radius: 5px;'>ดูรายละเอียดเอกสาร</a>
            </div>
        </div>
    </div>
</body>
</html>";

            // ส่งให้ CreatedBy + CC ในฉบับเดียว
            var ccEmails = !string.IsNullOrEmpty(request.CCEmail)
                ? request.CCEmail.Split(',').Select(e => e.Trim()).ToArray()
                : null;

            await _emailService.SendEmailWithCCAsync(request.CreatedBy, ccEmails, subject, body, request.Id, "PENDING_NOTIFICATION", request.DocNo);
        }

        private async Task SendReviseEmail(TrainingRequestEditViewModel request, string approverRole, string comment)
        {
            string approverRoleName = GetApproverRoleName(approverRole);
            string subject = $"🔄 ต้องแก้ไข - คำขอ {request.DocNo}";
            string editLink = $"{_baseUrl}/TrainingRequest/Edit?docNo={request.DocNo}";

            string body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: #ffc107; color: #000; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
            <h2>🔄 คำขอต้องแก้ไข</h2>
        </div>
        <div style='background: #ffffff; padding: 20px; border: 1px solid #e0e0e0;'>
            <p>เรียน {request.CreatedBy}</p>

            <p><strong>{approverRoleName}</strong> ขอให้แก้ไขคำขอฝึกอบรม</p>

            <div style='background: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin: 15px 0;'>
                <strong>📄 เอกสาร:</strong> {request.DocNo}<br>
                <strong>📖 หัวข้อ:</strong> {request.SeminarTitle}<br>
                <strong>💬 เหตุผล:</strong><br>
                <div style='background: white; padding: 10px; margin-top: 10px; border-radius: 5px;'>
                    {comment}
                </div>
            </div>

            <p>กรุณาแก้ไขและส่งใหม่</p>

            <div style='text-align: center; margin: 20px 0;'>
                <a href='{editLink}' style='display: inline-block; padding: 12px 30px; background: #ffc107; color: #000; text-decoration: none; border-radius: 5px;'>คลิกที่นี่เพื่อแก้ไข</a>
            </div>
        </div>
    </div>
</body>
</html>";

            // ส่งให้ CreatedBy + CC ในฉบับเดียว
            var ccEmails = !string.IsNullOrEmpty(request.CCEmail)
                ? request.CCEmail.Split(',').Select(e => e.Trim()).ToArray()
                : null;

            await _emailService.SendEmailWithCCAsync(request.CreatedBy, ccEmails, subject, body, request.Id, "REVISE_NOTIFICATION", request.DocNo);
        }

        private async Task SendRevisionAdminEmail(TrainingRequestEditViewModel request, string approverRole, string comment)
        {
            string approverRoleName = GetApproverRoleName(approverRole);
            string subject = $"🔄 HRD Admin ต้องดำเนินการ - {request.DocNo}";
            string editLink = $"{_baseUrl}/TrainingRequest/Edit?docNo={request.DocNo}";

            string body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: #ff9800; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
            <h2>🔄 Revision Admin Required</h2>
        </div>
        <div style='background: #ffffff; padding: 20px; border: 1px solid #e0e0e0;'>
            <p>เรียน HRD Admin และผู้เกี่ยวข้อง</p>

            <p><strong>{approverRoleName}</strong> ขอให้แก้ไขเอกสาร</p>

            <div style='background: #fff3cd; padding: 15px; border-left: 4px solid #ff9800; margin: 15px 0;'>
                <strong>📄 เอกสาร:</strong> {request.DocNo}<br>
                <strong>📖 หัวข้อ:</strong> {request.SeminarTitle}<br>
                <strong>💬 เหตุผล:</strong><br>
                <div style='background: white; padding: 10px; margin-top: 10px; border-radius: 5px;'>
                    {comment}
                </div>
            </div>

            <p><strong>สำหรับ HRD Admin:</strong> กรุณาดำเนินการแก้ไขหรือส่งกลับผู้ขอ</p>

            <div style='text-align: center; margin: 20px 0;'>
                <a href='{editLink}' style='display: inline-block; padding: 12px 30px; background: #ff9800; color: white; text-decoration: none; border-radius: 5px;'>คลิกที่นี่เพื่อดำเนินการ</a>
            </div>
        </div>
    </div>
</body>
</html>";

            // ส่งให้ HRD Admin (To) + CreatedBy + CC (CC field) ในฉบับเดียว
            if (!string.IsNullOrEmpty(request.HRDAdminId))
            {
                // สร้าง CC list: CreatedBy + CC
                var ccList = new System.Collections.Generic.List<string> { request.CreatedBy };

                if (!string.IsNullOrEmpty(request.CCEmail))
                {
                    ccList.AddRange(request.CCEmail.Split(',').Select(e => e.Trim()));
                }

                await _emailService.SendEmailWithCCAsync(request.HRDAdminId, ccList.ToArray(), subject, body, request.Id, "REVISION_ADMIN_NOTIFICATION", request.DocNo);
            }
        }

        private async Task SendRejectionEmail(TrainingRequestEditViewModel request, string approverRole, string comment)
        {
            string approverRoleName = GetApproverRoleName(approverRole);
            string subject = $"❌ ปฏิเสธคำขอ - {request.DocNo}";
            string docLink = $"{_baseUrl}/TrainingRequest/Edit?docNo={request.DocNo}";

            string body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0;'>
            <h2>❌ คำขอถูกปฏิเสธ</h2>
        </div>
        <div style='background: #ffffff; padding: 20px; border: 1px solid #e0e0e0;'>
            <p>เรียน {request.CreatedBy}</p>

            <p>คำขอฝึกอบรมถูกปฏิเสธโดย <strong>{approverRoleName}</strong></p>

            <div style='background: #f8d7da; padding: 15px; border-left: 4px solid #dc3545; margin: 15px 0;'>
                <strong>📄 เอกสาร:</strong> {request.DocNo}<br>
                <strong>📖 หัวข้อ:</strong> {request.SeminarTitle}<br>
                <strong>❌ ผู้ปฏิเสธ:</strong> {approverRoleName}<br>
                <strong>💬 เหตุผล:</strong><br>
                <div style='background: white; padding: 10px; margin-top: 10px; border-radius: 5px;'>
                    {comment}
                </div>
            </div>

            <p>กรุณาติดต่อผู้อนุมัติเพื่อสอบถามรายละเอียดเพิ่มเติม</p>

            <div style='text-align: center; margin: 20px 0;'>
                <a href='{docLink}' style='display: inline-block; padding: 12px 30px; background: #dc3545; color: white; text-decoration: none; border-radius: 5px;'>ดูรายละเอียดเอกสาร</a>
            </div>
        </div>
    </div>
</body>
</html>";

            // ส่งให้ CreatedBy + CC ในฉบับเดียว
            var ccEmails = !string.IsNullOrEmpty(request.CCEmail)
                ? request.CCEmail.Split(',').Select(e => e.Trim()).ToArray()
                : null;

            await _emailService.SendEmailWithCCAsync(request.CreatedBy, ccEmails, subject, body, request.Id, "REJECT_NOTIFICATION", request.DocNo);
        }

        private async Task SendFinalApprovalEmail(TrainingRequestEditViewModel request)
        {
            string subject = $"✅ คำขอฝึกอบรม {request.DocNo} ได้รับการอนุมัติ";
            string docLink = $"{_baseUrl}/TrainingRequest/Edit?docNo={request.DocNo}";

            string body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;'>
            <h1>🎉 อนุมัติสมบูรณ์!</h1>
        </div>
        <div style='background: #ffffff; padding: 30px; border: 1px solid #e0e0e0;'>
            <p>เรียน ผู้เกี่ยวข้องทุกท่าน</p>

            <p style='font-size: 18px;'><strong>คำขอฝึกอบรมได้รับการอนุมัติสมบูรณ์แล้ว! 🎊</strong></p>

            <div style='background: #d4edda; padding: 20px; border-left: 4px solid #28a745; margin: 20px 0;'>
                <strong>📄 เลขที่เอกสาร:</strong> {request.DocNo}<br>
                <strong>📖 หัวข้อ:</strong> {request.SeminarTitle}<br>
                <strong>📍 สถานที่:</strong> {request.TrainingLocation}<br>
                <strong>💰 งบประมาณ:</strong> {request.TotalCost:N2} บาท<br>
                <strong>📅 วันที่:</strong> {request.StartDate?.ToString("dd/MM/yyyy")}
            </div>

            <p>สามารถดำเนินการฝึกอบรมตามแผนได้</p>

            <div style='text-align: center; margin: 30px 0;'>
                <a href='{docLink}' style='display: inline-block; padding: 15px 40px; background: #28a745; color: white; text-decoration: none; border-radius: 5px; font-size: 16px;'>ดูรายละเอียดเอกสาร</a>
            </div>

            <h3>📊 สถานะการอนุมัติทั้งหมด</h3>
            {GenerateApprovalStatusHtml(request)}
        </div>
    </div>
</body>
</html>";

            // ส่งให้ทุกคน
            var allEmails = new System.Collections.Generic.List<string> { request.CreatedBy };

            if (!string.IsNullOrEmpty(request.CCEmail))
            {
                allEmails.AddRange(request.CCEmail.Split(',').Select(e => e.Trim()));
            }

            // ⭐ Add approvers (skip if they are SKIP_APPROVER)
            if (!string.IsNullOrEmpty(request.SectionManagerId) && !IsSkipApprover(request.SectionManagerId))
                allEmails.Add(request.SectionManagerId);
            if (!string.IsNullOrEmpty(request.DepartmentManagerId) && !IsSkipApprover(request.DepartmentManagerId))
                allEmails.Add(request.DepartmentManagerId);
            if (!string.IsNullOrEmpty(request.HRDAdminId) && !IsSkipApprover(request.HRDAdminId))
                allEmails.Add(request.HRDAdminId);
            if (!string.IsNullOrEmpty(request.HRDConfirmationId) && !IsSkipApprover(request.HRDConfirmationId))
                allEmails.Add(request.HRDConfirmationId);
            if (!string.IsNullOrEmpty(request.ManagingDirectorId) && !IsSkipApprover(request.ManagingDirectorId))
                allEmails.Add(request.ManagingDirectorId);
            if (!string.IsNullOrEmpty(request.DeputyManagingDirectorId) && !IsSkipApprover(request.DeputyManagingDirectorId))
                allEmails.Add(request.DeputyManagingDirectorId);

            var uniqueEmails = allEmails.Distinct().ToArray();

            await _emailService.SendEmailToMultipleRecipientsAsync(uniqueEmails, subject, body, request.Id, "FINAL_APPROVAL", request.DocNo);
        }

        private string GenerateApprovalStatusHtml(TrainingRequestEditViewModel request)
        {
            return $@"
<table style='width: 100%; border-collapse: collapse;'>
    <tr style='border-bottom: 1px solid #e0e0e0;'>
        <td style='padding: 10px; font-weight: bold;'>ผู้จัดการส่วน (Section Manager)</td>
        <td style='padding: 10px;'>{request.SectionManagerId ?? "-"}</td>
        <td style='padding: 10px;'><span class='status-badge {GetStatusClass(request.Status_SectionManager)}'>{request.Status_SectionManager ?? "รออนุมัติ"}</span></td>
    </tr>
    <tr style='border-bottom: 1px solid #e0e0e0;'>
        <td style='padding: 10px; font-weight: bold;'>ผู้จัดการฝ่าย (Department Manager)</td>
        <td style='padding: 10px;'>{request.DepartmentManagerId ?? "-"}</td>
        <td style='padding: 10px;'><span class='status-badge {GetStatusClass(request.Status_DepartmentManager)}'>{request.Status_DepartmentManager ?? "รออนุมัติ"}</span></td>
    </tr>
    <tr style='border-bottom: 1px solid #e0e0e0;'>
        <td style='padding: 10px; font-weight: bold;'>เจ้าหน้าที่พัฒนาบุคลากร (HRD Admin)</td>
        <td style='padding: 10px;'>{request.HRDAdminId ?? "-"}</td>
        <td style='padding: 10px;'><span class='status-badge {GetStatusClass(request.Status_HRDAdmin)}'>{request.Status_HRDAdmin ?? "รออนุมัติ"}</span></td>
    </tr>
    <tr style='border-bottom: 1px solid #e0e0e0;'>
        <td style='padding: 10px; font-weight: bold;'>ผู้รับรองการฝึกอบรม (HRD Confirmation)</td>
        <td style='padding: 10px;'>{request.HRDConfirmationId ?? "-"}</td>
        <td style='padding: 10px;'><span class='status-badge {GetStatusClass(request.Status_HRDConfirmation)}'>{request.Status_HRDConfirmation ?? "รออนุมัติ"}</span></td>
    </tr>
    <tr style='border-bottom: 1px solid #e0e0e0;'>
        <td style='padding: 10px; font-weight: bold;'>กรรมการผู้จัดการ (Managing Director)</td>
        <td style='padding: 10px;'>{request.ManagingDirectorId ?? "-"}</td>
        <td style='padding: 10px;'><span class='status-badge {GetStatusClass(request.Status_ManagingDirector)}'>{request.Status_ManagingDirector ?? "รออนุมัติ"}</span></td>
    </tr>
    <tr>
        <td style='padding: 10px; font-weight: bold;'>รองกรรมการผู้จัดการ (Deputy Managing Director)</td>
        <td style='padding: 10px;'>{request.DeputyManagingDirectorId ?? "-"}</td>
        <td style='padding: 10px;'><span class='status-badge {GetStatusClass(request.Status_DeputyManagingDirector)}'>{request.Status_DeputyManagingDirector ?? "รออนุมัติ"}</span></td>
    </tr>
</table>";
        }

        private string GetStatusClass(string status)
        {
            return status switch
            {
                "APPROVED" => "status-approved",
                "Pending" => "status-pending",
                _ => "status-pending"
            };
        }

        #endregion
    }
}
