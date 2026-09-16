using Hotel_MVC.Enums;
using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.ViewModels.Booking
{
    public class CreateBookingVM
    {
        public int RoomTypeId { get; set; }
        public string RoomTypeName { get; set; }
        public string? CoverImageUrl { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckIn { get; set; }

        [DataType(DataType.Date)]
        public DateTime CheckOut { get; set; }

        [Range(1, 10)]
        public int Guests { get; set; }

        public decimal PricePerNight { get; set; }
        public int Nights => (CheckOut - CheckIn).Days;
        public decimal TotalAmount { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public PaymentMethod PaymentMethod { get; set; }
    }
}
