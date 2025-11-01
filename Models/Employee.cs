using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingRequestApp.Models
{
    [Table("Employees")]
    public class Employee
    {
        [Key]
        [Column("ID_emp")]
        public long ID_emp { get; set; }

        [Column("Emp#")]
        [StringLength(50)]
        public string? EmpNumber { get; set; }

        [Column("Name")]
        [StringLength(50)]
        public string? Name { get; set; }

        [Column("lastname")]
        [StringLength(50)]
        public string? Lastname { get; set; }

        [Column("Department")]
        [StringLength(200)]
        public string? Department { get; set; }

        [Column("Position")]
        [StringLength(200)]
        public string? Position { get; set; }

        [Column("UserID")]
        [StringLength(50)]
        public string? UserID { get; set; }

        [Column("Status")]
        [StringLength(50)]
        public string? Status { get; set; }

        [Column("Company")]
        [StringLength(50)]
        public string? Company { get; set; }

        [Column("Mgr_JD")]
        [StringLength(200)]
        public string? Mgr_JD { get; set; }

        [Column("Groups")]
        [StringLength(200)]
        public string? Groups { get; set; }

        [Column("JD")]
        [StringLength(200)]
        public string? JD { get; set; }

        [Column("Email")]
        [StringLength(200)]
        public string? Email { get; set; }

        [Column("UserID_Old")]
        [StringLength(200)]
        public string? UserID_Old { get; set; }

        [Column("Prefix")]
        [StringLength(50)]
        public string? Prefix { get; set; }

        [Column("JD Name")]
        [StringLength(200)]
        public string? JDName { get; set; }

        [Column("Level")]
        [StringLength(200)]
        public string? Level { get; set; }

        [Column("Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Column("Remarks")]
        [StringLength(200)]
        public string? Remarks { get; set; }

        [Column("FingerTemplate")]
        public byte[]? FingerTemplate { get; set; }

        [Column("FingerTemplate2")]
        public byte[]? FingerTemplate2 { get; set; }

        [Column("account_permissions")]
        [StringLength(50)]
        public string? AccountPermissions { get; set; }

        [Column("UPassword")]
        public string? UPassword { get; set; }
    }
}