using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingRequestApp.Models
{
    [Table("TrainingRequestEmployees")]
    public class TrainingRequestEmployee
    {
        [Key]
        public int Id { get; set; }

        public int? TrainingRequestId { get; set; }

        [StringLength(20)]
        public string? EmployeeCode { get; set; }

        [StringLength(100)]
        public string? EmployeeName { get; set; }

        [StringLength(100)]
        public string? Position { get; set; }

        [StringLength(100)]
        public string? Level { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        public int? PreviousTrainingHours { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? PreviousTrainingCost { get; set; }

        public int? CurrentTrainingHours { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? CurrentTrainingCost { get; set; }

        public int? RemainingHours { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal? RemainingCost { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation Property
        [ForeignKey("TrainingRequestId")]
        public virtual TrainingRequest? TrainingRequest { get; set; }
    }
}