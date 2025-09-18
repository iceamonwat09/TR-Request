using System.ComponentModel.DataAnnotations;

namespace TrainingRequestApp.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "UserID is required")]
        [StringLength(7, MinimumLength = 7, ErrorMessage = "UserID must be exactly 7 characters")]
        public string UserID { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
        
        public string PEmail { get; set; }
    }
}
