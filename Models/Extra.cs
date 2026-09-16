using Hotel_MVC.Enums;

namespace Hotel_MVC.Models
{
    public class Extra
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public ExtraPricingType PricingType { get; set; }

        public ICollection<BookingExtra> BookingExtras { get; set; } = new List<BookingExtra>();
    }
}
