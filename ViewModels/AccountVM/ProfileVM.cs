using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Hotel_MVC.ViewModels.AccountVM
{
    public class ProfileVM
    {
        // =========================================================
        // PERSONAL DETAILS
        // =========================================================

        [Required]
        [Display(Name = "First Name")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;


        [Required]
        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;


        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;


        [Phone]
        public string? PhoneNumber { get; set; }


        public string? Nationality { get; set; }


        public string? PassportOrNationalId { get; set; }


        // =========================================================
        // PROFILE IMAGE
        // =========================================================

        public string? ProfileImagePath { get; set; }


        public IFormFile? ProfileImage { get; set; }


        // =========================================================
        // PASSWORD
        // =========================================================

        // Password fields are intentionally NOT populated
        // from the database or controller.

        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }


        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }


        [Compare(
            nameof(NewPassword),
            ErrorMessage = "The new passwords do not match."
        )]
        [Display(Name = "Confirm New Password")]
        [DataType(DataType.Password)]
        public string? ConfirmNewPassword { get; set; }


        // =========================================================
        // STAY PREFERENCES
        // =========================================================

        public bool BreakfastPreference { get; set; }


        public bool AirportPickupPreference { get; set; }


        public bool LateCheckoutPreference { get; set; }


        [StringLength(100)]
        public string? PreferredRoomView { get; set; }


        [StringLength(1000)]
        public string? SpecialRequests { get; set; }


        // =========================================================
        // PAYMOB - DISPLAY ONLY
        // =========================================================

        // IMPORTANT:
        // Never put PaymobCardToken here.
        // The token must remain server-side.

        public string? PaymobMaskedPan { get; set; }


        public string? PaymobCardSubtype { get; set; }


        public string? PaymobCardHolderName { get; set; }


        public DateTime? PaymobCardSavedAt { get; set; }
    }
}