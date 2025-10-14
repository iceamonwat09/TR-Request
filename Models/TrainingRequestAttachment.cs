using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TrainingRequestApp.Models
{
    [Table("TrainingRequestAttachments")]
    public class TrainingRequestAttachment
    {
        [Key]
        [Column("ID")]
        public int ID { get; set; }

        [StringLength(20)]
        public string? DocNo { get; set; }

        [StringLength(255)]
        public string? File_Name { get; set; }

        [StringLength(50)]
        public string? Modify_Date { get; set; }
    }
}