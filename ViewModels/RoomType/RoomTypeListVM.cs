namespace Hotel_MVC.ViewModels.RoomType
{
    public class RoomTypeListVM
    {
        public RoomTypeListFilterVM Filter { get; set; }
        public List<RoomTypeListItemVM> RoomTypes { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)Filter.PageSize);
    }
}
