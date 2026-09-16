namespace Hotel_MVC.ViewModels.SearchVM
{
    public class RoomSearchResultItemVM
    {
        public int RoomTypeId { get; set; }
        public string Name { get; set; }
        public string? ViewType { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal TotalPriceForStay { get; set; }
        public int AvailableRoomsCount { get; set; }
        public string? CoverImageUrl { get; set; }
    }

    public class RoomSearchResultsViewModel
    {
        public SearchAvailabilityVM Search { get; set; }
        public List<RoomSearchResultItemVM> Results { get; set; } = new();
        public int TotalCount { get; set; }

        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Search.PageSize);
    }
}
