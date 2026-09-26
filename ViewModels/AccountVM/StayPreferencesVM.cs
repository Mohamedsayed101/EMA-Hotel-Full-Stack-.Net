using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.ViewModels.AccountVM
{
    public class StayPreferencesVM
    {
        public bool BreakfastPreference { get; set; }

        public bool AirportPickupPreference { get; set; }

        public bool LateCheckoutPreference { get; set; }

        [Display(Name = "Preferred Room View")]
        public string? PreferredRoomView { get; set; }

        [Display(Name = "Special Requests")]
        [StringLength(1000)]
        public string? SpecialRequests { get; set; }
    }
}