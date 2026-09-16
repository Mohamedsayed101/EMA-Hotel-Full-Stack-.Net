namespace Hotel_MVC.ViewModels.Room
{
    public class RoomBrowseItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ViewType { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public string? Amenities { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}
