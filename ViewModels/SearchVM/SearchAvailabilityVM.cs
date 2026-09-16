using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.ViewModels.SearchVM
{
    public class SearchAvailabilityVM
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-in")]
        public DateTime CheckIn { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Check-out")]
        public DateTime CheckOut { get; set; } = DateTime.Today.AddDays(1);

        [Range(1, 10)]
        public int Guests { get; set; } = 2;

        
        public string? ViewType { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SortBy { get; set; } 

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 6;
    }
}
