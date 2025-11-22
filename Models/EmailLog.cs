using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingRequestApp.Models
{
    [Table("EmailLogs")]
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        public int? TrainingRequestId { get; set; }

        [StringLength(20)]
        public string? DocNo { get; set; }

        [Required]
        [StringLength(100)]
        public string RecipientEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EmailType { get; set; } = string.Empty;
        // Values: PENDING_NOTIFICATION, APPROVAL_REQUEST, APPROVAL_NOTIFICATION,
        //         REVISE_NOTIFICATION, REVISION_ADMIN_NOTIFICATION, REJECT_NOTIFICATION, FINAL_APPROVAL

        [StringLength(200)]
        public string? Subject { get; set; }

        [Required]
        public DateTime SentDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "SENT";
        // Values: SENT, FAILED

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        public int RetryCount { get; set; } = 0;

        // Navigation property
        [ForeignKey("TrainingRequestId")]
        public virtual TrainingRequest? TrainingRequest { get; set; }
    }
}
