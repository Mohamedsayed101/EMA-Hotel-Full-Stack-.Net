namespace Hotel_MVC.Models
{
    public class BookingExtra
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; }

        public int ExtraId { get; set; }
        public Extra Extra { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
