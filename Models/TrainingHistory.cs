using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingRequestApp.Models
{
    [Table("TrainingHistory")]
    public class TrainingHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TrainingRequestId { get; set; }

        [StringLength(20)]
        public string? EmployeeCode { get; set; }

        [StringLength(100)]
        public string? EmployeeName { get; set; }

        // Never(ไม่เคย), Ever(เคย), Similar(ใกล้เคียง)
        [StringLength(20)]
        public string? HistoryType { get; set; }

        [DataType(DataType.Date)]
        public DateTime? TrainingDate { get; set; }

        [StringLength(500)]
        public string? CourseName { get; set; }

        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        // Navigation Property
        [ForeignKey("TrainingRequestId")]
        public virtual TrainingRequest? TrainingRequest { get; set; }
    }
}
