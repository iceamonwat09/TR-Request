using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingRequestApp.Models
{
    [Table("ApprovalHistory")]
    public class ApprovalHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TrainingRequestId { get; set; }

        [StringLength(20)]
        public string? DocNo { get; set; }

        [Required]
        [StringLength(50)]
        public string ApproverRole { get; set; } = string.Empty;
        // Values: SectionManager, DepartmentManager, HRDAdmin, HRDConfirmation, ManagingDirector

        [Required]
        [StringLength(100)]
        public string ApproverEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Action { get; set; } = string.Empty;
        // Values: APPROVED, Revise, REJECTED

        [StringLength(500)]
        public string? Comment { get; set; }

        [Required]
        public DateTime ActionDate { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string? PreviousStatus { get; set; }

        [StringLength(50)]
        public string? NewStatus { get; set; }

        [StringLength(50)]
        public string? IpAddress { get; set; }

        // Navigation property
        [ForeignKey("TrainingRequestId")]
        public virtual TrainingRequest? TrainingRequest { get; set; }
    }
}
