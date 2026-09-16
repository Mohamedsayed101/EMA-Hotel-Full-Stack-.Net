namespace Hotel_MVC.Models
{
    public class RoomImage
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public int DisplayOrder { get; set; }

        public int RoomTypeId { get; set; }
        public RoomType RoomType { get; set; }
    }
}
