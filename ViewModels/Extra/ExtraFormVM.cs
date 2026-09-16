using Hotel_MVC.Enums;
using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.ViewModels.Extra
{
    public class ExtraFormVM
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 100000)]
        public decimal Price { get; set; }

        [Display(Name = "Pricing Type")]
        public ExtraPricingType PricingType { get; set; }
    }
}
