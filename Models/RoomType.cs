namespace Hotel_MVC.Models
{
    public class RoomType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public decimal PricePerNight { get; set; }
        public string? ViewType { get; set; }
        public string? Amenities { get; set; }
        public string CancellationPolicy { get; set; }

        public ICollection<Room> Rooms { get; set; } = new List<Room>();
        public ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();
       
    }
}
