using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }
        public string? Comment { get; set; }
       

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public string GuestId { get; set; }
        public ApplicationUser Guest { get; set; }
    }
}
