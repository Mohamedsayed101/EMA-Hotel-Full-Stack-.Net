namespace Hotel_MVC.ViewModels.Receptionist
{
    public class ReceptionistBookingVM
    {
        public ReceptionistBookingFilterVM Filter { get; set; }
        public List<Models.Booking> Bookings { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Filter.PageSize);
    }
}
