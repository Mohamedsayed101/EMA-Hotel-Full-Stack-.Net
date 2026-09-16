using Hotel_MVC.Enums;
using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.ViewModels.Receptionist
{
    public class WalkInBookingVM
    {
        [Required]
        [Display(Name = "Guest First Name")]
        public string GuestFirstName { get; set; }

        [Required]
        [Display(Name = "Guest Last Name")]
        public string GuestLastName { get; set; }


        [Required]
        [EmailAddress]
        [Display(Name = "Guest Email")]
        public string GuestEmail { get; set; }

        public string? GuestPhone { get; set; }

        [Required]
        public int RoomTypeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckIn { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);

        [Range(1, 10)]
        public int Guests { get; set; } = 1;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    }
}
