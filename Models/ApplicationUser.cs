using Microsoft.AspNetCore.Identity;

namespace Hotel_MVC.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? Nationality { get; set; }

        public string? PassportOrNationalId { get; set; }

        public string? ProfileImagePath { get; set; }

        // Stay Preferences
        public bool BreakfastPreference { get; set; }

        public bool AirportPickupPreference { get; set; }

        public bool LateCheckoutPreference { get; set; }

        public string? PreferredRoomView { get; set; }

        public string? SpecialRequests { get; set; }

        // Paymob saved card token.
        // Never store card number or CVV.
        public string? PaymobCardToken { get; set; }

        public string? PaymobMaskedPan { get; set; }

        public string? PaymobCardSubtype { get; set; }

        public string? PaymobCardHolderName { get; set; }

        public DateTime? PaymobCardSavedAt { get; set; }

        public ICollection<Booking> Bookings { get; set; }
            = new List<Booking>();

        public ICollection<Review> Reviews { get; set; }
            = new List<Review>();
    }
}