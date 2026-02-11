using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingRequestApp.Models
{
    [Table("TrainingRequests")]
    public class TrainingRequest
    {
        [Key]
        public int Id { get; set; }

        // ===== Base Columns =====
        [StringLength(40)]
        public string? DocNo { get; set; }

        [StringLength(100)]
        public string? Company { get; set; }

        [StringLength(40)]
        public string? TrainingType { get; set; }

        [StringLength(200)]
        public string? Factory { get; set; }

        [StringLength(500)]
        public string? CCEmail { get; set; }

        [StringLength(200)]
        public string? Position { get; set; }

        [StringLength(200)]
        public string? Department { get; set; }

        [StringLength(40)]
        public string? EmployeeCode { get; set; }

        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [StringLength(400)]
        public string? SeminarTitle { get; set; }

        [StringLength(400)]
        public string? TrainingLocation { get; set; }

        [StringLength(300)]
        public string? Instructor { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? TotalCost { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? CostPerPerson { get; set; }

        public int? PerPersonTrainingHours { get; set; }

        // ===== Training Details =====
        [StringLength(200)]
        public string? TrainingObjective { get; set; }

        [StringLength(1000)]
        public string? OtherObjective { get; set; }

        [StringLength(1000)]
        public string? URLSource { get; set; }

        [StringLength(2000)]
        public string? AdditionalNotes { get; set; }

        [StringLength(2000)]
        public string? ExpectedOutcome { get; set; }

        [StringLength(1000)]
        public string? AttachedFilePath { get; set; }

        [StringLength(100)]
        public string? Status { get; set; } = "DRAFT";

        // ===== Audit Columns =====
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        [StringLength(200)]
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(200)]
        public string? UpdatedBy { get; set; }

        public bool? IsActive { get; set; } = true;

        // ===== Cost Breakdown =====
        [Column(TypeName = "decimal(12,2)")]
        public decimal? RegistrationCost { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? InstructorFee { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? EquipmentCost { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? FoodCost { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? OtherCost { get; set; }

        [StringLength(1000)]
        public string? OtherCostDescription { get; set; }

        public int? TotalPeople { get; set; }

        // ===== Approval Workflow =====
        [StringLength(200)]
        public string? SectionManagerId { get; set; }
        [StringLength(40)]
        public string? Status_SectionManager { get; set; }
        [StringLength(1000)]
        public string? Comment_SectionManager { get; set; }
        [StringLength(400)]
        public string? ApproveInfo_SectionManager { get; set; }

        [StringLength(200)]
        public string? DepartmentManagerId { get; set; }
        [StringLength(40)]
        public string? Status_DepartmentManager { get; set; }
        [StringLength(1000)]
        public string? Comment_DepartmentManager { get; set; }
        [StringLength(400)]
        public string? ApproveInfo_DepartmentManager { get; set; }

        [StringLength(200)]
        public string? ManagingDirectorId { get; set; }
        [StringLength(40)]
        public string? Status_ManagingDirector { get; set; }
        [StringLength(1000)]
        public string? Comment_ManagingDirector { get; set; }
        [StringLength(400)]
        public string? ApproveInfo_ManagingDirector { get; set; }

        [StringLength(200)]
        public string? HRDAdminId { get; set; }
        [StringLength(40)]
        public string? Status_HRDAdmin { get; set; }
        [StringLength(1000)]
        public string? Comment_HRDAdmin { get; set; }
        [StringLength(400)]
        public string? ApproveInfo_HRDAdmin { get; set; }

        [StringLength(200)]
        public string? HRDConfirmationId { get; set; }
        [StringLength(40)]
        public string? Status_HRDConfirmation { get; set; }
        [StringLength(1000)]
        public string? Comment_HRDConfirmation { get; set; }
        [StringLength(400)]
        public string? ApproveInfo_HRDConfirmation { get; set; }

        [StringLength(200)]
        public string? DeputyManagingDirectorId { get; set; }
        [StringLength(40)]
        public string? Status_DeputyManagingDirector { get; set; }
        [StringLength(1000)]
        public string? Comment_DeputyManagingDirector { get; set; }
        [StringLength(400)]
        public string? ApproveInfo_DeputyManagingDirector { get; set; }

        // ===== HRD Record Section =====
        [DataType(DataType.Date)]
        public DateTime? HRD_ContactDate { get; set; }

        [StringLength(200)]
        public string? HRD_ContactPerson { get; set; }

        [DataType(DataType.Date)]
        public DateTime? HRD_PaymentDate { get; set; }

        [StringLength(40)]
        public string? HRD_PaymentMethod { get; set; }

        [StringLength(200)]
        public string? HRD_RecorderSignature { get; set; }

        // ===== Travel & Target Group =====
        [StringLength(40)]
        public string? TravelMethod { get; set; }

        [StringLength(400)]
        public string? TargetGroup { get; set; }

        // ===== Knowledge Management (KM) Section =====
        public bool? KM_SubmitDocument { get; set; }
        public bool? KM_CreateReport { get; set; }

        [DataType(DataType.Date)]
        public DateTime? KM_CreateReportDate { get; set; }

        public bool? KM_KnowledgeSharing { get; set; }

        [DataType(DataType.Date)]
        public DateTime? KM_KnowledgeSharingDate { get; set; }

        // ===== HRD Budget & Membership Section =====
        [StringLength(20)]
        public string? HRD_BudgetPlan { get; set; }

        [StringLength(40)]
        public string? HRD_BudgetUsage { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? HRD_DepartmentBudgetRemaining { get; set; }

        [StringLength(40)]
        public string? HRD_MembershipType { get; set; }

        [Column(TypeName = "decimal(12,2)")]
        public decimal? HRD_MembershipCost { get; set; }

        // ===== HRD Section 4 =====
        public bool? HRD_TrainingRecord { get; set; }
        public bool? HRD_KnowledgeManagementDone { get; set; }
        public bool? HRD_CourseCertification { get; set; }

        [StringLength(40)]
        public string? BudgetSource { get; set; }

        // Navigation property for participants
        public virtual ICollection<TrainingParticipant> Participants { get; set; } = new List<TrainingParticipant>();
    }

    [Table("TrainingParticipants")]
    public class TrainingParticipant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TrainingRequestId { get; set; }

        [Required]
        [StringLength(100)]
        public string UserID { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Prefix { get; set; }

        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(100)]
        public string? Lastname { get; set; }

        [StringLength(400)]
        public string? Level { get; set; }

        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("TrainingRequestId")]
        public virtual TrainingRequest TrainingRequest { get; set; } = null!;
    }
}
