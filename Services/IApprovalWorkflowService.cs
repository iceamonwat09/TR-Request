using System.Threading.Tasks;
using TrainingRequestApp.Models;

namespace TrainingRequestApp.Services
{
    public interface IApprovalWorkflowService
    {
        /// <summary>
        /// หาสถานะการอนุมัติถัดไป
        /// </summary>
        string GetNextApprovalStatus(string currentStatus);

        /// <summary>
        /// หา Email ของ Approver ถัดไป
        /// </summary>
        string GetNextApproverEmail(TrainingRequestEditViewModel request, string nextStatus);

        /// <summary>
        /// ตรวจสอบว่า User มีสิทธิ์อนุมัติหรือไม่
        /// </summary>
        Task<ApprovalPermissionResult> CheckApprovalPermission(string docNo, string userEmail);

        /// <summary>
        /// ประมวลผลการอนุมัติ (Approve)
        /// </summary>
        Task<WorkflowResult> ProcessApproval(string docNo, string userEmail, string comment, string ipAddress);

        /// <summary>
        /// ประมวลผลการ Revise
        /// </summary>
        Task<WorkflowResult> ProcessRevise(string docNo, string userEmail, string comment, string ipAddress);

        /// <summary>
        /// ประมวลผลการ Reject
        /// </summary>
        Task<WorkflowResult> ProcessReject(string docNo, string userEmail, string comment, string ipAddress);

        /// <summary>
        /// ส่ง Email เริ่มต้น Workflow (Pending → WAITING_FOR_SECTION_MANAGER)
        /// </summary>
        Task<bool> StartWorkflow(string docNo);

        /// <summary>
        /// Reset Approval Status แยก 2 กรณี:
        /// - resetType = null/"Revise": Reset ระดับ 1-3 (Section/Dept/HRD Admin)
        /// - resetType = "HRDAdmin"/"RevisionAdmin": Reset ระดับ 4-5 (HRD Confirmation/MD)
        /// Comment_XXX ทั้งหมดจะถูกเก็บไว้เสมอ
        /// </summary>
        Task ResetApprovalStatus(string docNo, string resetType);

        /// <summary>
        /// ส่ง Email ขออนุมัติซ้ำ (1 ฉบับเดียว) สำหรับ Admin/System Admin
        /// ส่งหา: Approver (To) + CreatedBy + CC + Admin ที่กด (CC)
        /// </summary>
        Task<WorkflowResult> RetryEmail(string docNo, string adminEmail, string ipAddress);
    }

    public class ApprovalPermissionResult
    {
        public bool CanApprove { get; set; }
        public string ApproverRole { get; set; }
        public string Message { get; set; }
        public TrainingRequestEditViewModel Request { get; set; }
    }

    public class WorkflowResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string NewStatus { get; set; }
    }
}
