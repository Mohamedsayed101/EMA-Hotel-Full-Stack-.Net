using Hotel_MVC.Enums;
using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.ViewModels.Receptionist
{
    public class ReceptionistBookingFilterVM
    {
        public string? Search { get; set; }   
        public BookingStatus? StatusFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
