using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.ViewModels.RoomType
{
    public class RoomTypeFormVM
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Range(1, 10)]
        public int Capacity { get; set; }

        [Range(1, 100000)]
        [Display(Name = "Price Per Night")]
        public decimal PricePerNight { get; set; }

        [Display(Name = "View Type")]
        public string? ViewType { get; set; }

        [Display(Name = "Cancellation Policy")]
        public string? CancellationPolicy { get; set; }

        public string? Amenities { get; set; }

       
        [Display(Name = "Add Images")]
        public List<IFormFile>? NewImages { get; set; }

        public List<ExistingImageVM> ExistingImages { get; set; } = new();

      
        public List<int>? ImagesToDelete { get; set; }
    }
}
