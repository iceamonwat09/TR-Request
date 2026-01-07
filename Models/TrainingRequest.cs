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

        [Required]
        [Display(Name = "Training Title")]
        public string TrainingTitle { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Training Date")]
        [DataType(DataType.Date)]
        public DateTime TrainingDate { get; set; }

        [Required]
        [Display(Name = "Location")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Status")]
        public string Status { get; set; } = "Pending";

        // ?? ������Ŵ짺����ҳ�¡��¡��
        [Display(Name = "���ŧ����¹/�Է�ҡ�")]
        public decimal RegistrationCost { get; set; } = 0;

        [Display(Name = "��Ҥ������Է�ҡ�")]
        public decimal InstructorFee { get; set; } = 0;

        [Display(Name = "����ػ�ó�")]
        public decimal EquipmentCost { get; set; } = 0;

        [Display(Name = "��������")]
        public decimal FoodCost { get; set; } = 0;

        [Display(Name = "����")]
        public decimal OtherCost { get; set; } = 0;

        [Display(Name = "�к���¡������")]
        [StringLength(500)]
        public string? OtherCostDescription { get; set; }

        [Display(Name = "����ط��")]
        public decimal TotalCost { get; set; } = 0;

        // ? ������÷Ѵ���
        [Display(Name = "CC Email")]
        [StringLength(1000)]
        public string? CCEmail { get; set; }

        // ===== HRD Record Section (Admin/System Admin/HRD Admin/HRD Confirmation Only) =====
        [Display(Name = "วันที่ติดต่อสถาบัน")]
        [DataType(DataType.Date)]
        public DateTime? HRD_ContactDate { get; set; }

        [Display(Name = "ชื่อผู้ที่ติดต่อด้วย")]
        [StringLength(100)]
        public string? HRD_ContactPerson { get; set; }

        [Display(Name = "วันที่ชำระเงิน")]
        [DataType(DataType.Date)]
        public DateTime? HRD_PaymentDate { get; set; }

        [Display(Name = "วิธีชำระเงิน")]
        [StringLength(20)]
        public string? HRD_PaymentMethod { get; set; } // "Check" or "Cash"

        [Display(Name = "ผู้บันทึก")]
        [StringLength(100)]
        public string? HRD_RecorderSignature { get; set; }

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
        [StringLength(50)]
        public string UserID { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Prefix { get; set; }

        [StringLength(50)]
        public string? Name { get; set; }

        [StringLength(50)]
        public string? Lastname { get; set; }

        [StringLength(200)]
        public string? Level { get; set; }

        [Display(Name = "Added Date")]
        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation property
        [ForeignKey("TrainingRequestId")]
        public virtual TrainingRequest TrainingRequest { get; set; } = null!;
    }
}