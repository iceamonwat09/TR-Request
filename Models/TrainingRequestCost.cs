using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingRequestApp.Models
{
    [Table("TrainingRequest_Cost")]
    public class TrainingRequestCost
    {
        [Key]
        public int ID { get; set; }

        [Required(ErrorMessage = "กรุณาเลือกฝ่าย")]
        [StringLength(100)]
        [Display(Name = "ฝ่าย")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาเลือกปี")]
        [StringLength(50)]
        [Display(Name = "ปี")]
        public string Year { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกชั่วโมง")]
        [Display(Name = "โควต้าชั่วโมง (ชม.)")]
        public int Qhours { get; set; }

        [Required(ErrorMessage = "กรุณากรอกงบประมาณ")]
        [Column(TypeName = "decimal(12,2)")]
        [Display(Name = "โควต้าเงิน (บาท)")]
        public decimal Cost { get; set; }

        [StringLength(100)]
        [Display(Name = "สร้างโดย")]
        public string? CreatedBy { get; set; }
    }
}
