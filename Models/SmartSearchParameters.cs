using System;

namespace Hotel_MVC.Models
{
    public class SmartSearchParameters
    {
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public decimal? MaxPricePerNight { get; set; }
        public string Keywords { get; set; } = string.Empty;
    }
}