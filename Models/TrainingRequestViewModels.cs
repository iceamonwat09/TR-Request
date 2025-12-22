using System;
using System.Collections.Generic;

namespace TrainingRequestApp.Models
{
    public class TrainingRequestEditViewModel
    {
        public int Id { get; set; }
        public string? DocNo { get; set; }
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
        public string? ParticipantCount { get; set; }

        // Approvers - 6 levels
        public string? SectionManagerId { get; set; }
        public string? Status_SectionManager { get; set; }
        public string? Comment_SectionManager { get; set; }
        public string? ApproveInfo_SectionManager { get; set; }

        public string? DepartmentManagerId { get; set; }
        public string? Status_DepartmentManager { get; set; }
        public string? Comment_DepartmentManager { get; set; }
        public string? ApproveInfo_DepartmentManager { get; set; }

        public string? HRDAdminId { get; set; }
        public string? Status_HRDAdmin { get; set; }
        public string? Comment_HRDAdmin { get; set; }
        public string? ApproveInfo_HRDAdmin { get; set; }

        public string? HRDConfirmationId { get; set; }
        public string? Status_HRDConfirmation { get; set; }
        public string? Comment_HRDConfirmation { get; set; }
        public string? ApproveInfo_HRDConfirmation { get; set; }

        public string? ManagingDirectorId { get; set; }
        public string? Status_ManagingDirector { get; set; }
        public string? Comment_ManagingDirector { get; set; }
        public string? ApproveInfo_ManagingDirector { get; set; }

        // 🆕 Deputy Managing Director (ท้ายสุด)
        public string? DeputyManagingDirectorId { get; set; }
        public string? Status_DeputyManagingDirector { get; set; }
        public string? Comment_DeputyManagingDirector { get; set; }
        public string? ApproveInfo_DeputyManagingDirector { get; set; }

        public string? Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }

        public List<EmployeeViewModel> Employees { get; set; } = new List<EmployeeViewModel>();
    }

    public class EmployeeViewModel
    {
        public string? EmployeeCode { get; set; }
        public string? EmployeeName { get; set; }
        public string? Position { get; set; }
        public string? Level { get; set; }
        public string? Department { get; set; }
        public int? PreviousTrainingHours { get; set; }
        public decimal? PreviousTrainingCost { get; set; }
        public int? CurrentTrainingHours { get; set; }
        public decimal? CurrentTrainingCost { get; set; }
        public int? RemainingHours { get; set; }
        public decimal? RemainingCost { get; set; }
        public string? Notes { get; set; }
    }
}