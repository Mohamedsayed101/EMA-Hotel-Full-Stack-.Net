using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.ViewModels.AccountVM
{
    public class ForgotPasswordVM
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;
    }
}