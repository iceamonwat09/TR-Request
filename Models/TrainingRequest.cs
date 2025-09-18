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
