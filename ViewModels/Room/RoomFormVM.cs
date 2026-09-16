using Hotel_MVC.Enums;
using System.ComponentModel.DataAnnotations;

namespace Hotel_MVC.ViewModels.Room
{
    public class RoomFormVM
    {
        public int Id { get; set; }

        [Required, MaxLength(20)]
        [Display(Name = "Room Number")]
        public string RoomNumber { get; set; } = string.Empty;

        [Range(1, 100)]
        [Display(Name = "Floor")]
        public int FloorNumber { get; set; }

        [Required]
        public int RoomTypeId { get; set; }

        public RoomStatus Status { get; set; } = RoomStatus.Available;
    }
}
