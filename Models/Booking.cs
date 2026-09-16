using Hotel_MVC.Enums;

namespace Hotel_MVC.Models
{
    public class Booking
    {
        public int Id { get; set; }
        public string BookingReference { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

       
        public decimal TotalAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public PaymentMethod PaymentMethod { get; set; }

        public string? CancellationReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public DateTime? ActualCheckInAt { get; set; }
        public DateTime? ActualCheckOutAt { get; set; }

        public string GuestId { get; set; }
        public ApplicationUser Guest {  get; set; }

        public int RoomId { get; set; }
        public Room Room { get; set; }

        public ICollection<BookingExtra> BookingExtras {  get; set; } = new List<BookingExtra>();
        public Review? Review { get; set; }
    }
}
