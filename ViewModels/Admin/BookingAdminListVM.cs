using Hotel_MVC.Enums;

namespace Hotel_MVC.ViewModels.Admin
{
    public class BookingAdminListVM
    {

        public string? Search { get; set; }
        public BookingStatus? Status { get; set; }
        public List<Hotel_MVC.Models.Booking> Bookings { get; set; } = new();
        public DateTime? Date { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
