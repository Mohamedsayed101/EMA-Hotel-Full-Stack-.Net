namespace Hotel_MVC.ViewModels.RoomType
{
    public class RoomTypeListItemVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public string? ViewType { get; set; }
        public int RoomCount { get; set; }
        public string? CoverImageUrl { get; set; }
    }
}
