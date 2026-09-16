using Hotel_MVC.Enums;
using Hotel_MVC.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hotel_MVC.Data
{
    public class DbRoomsDataInitializer
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            await SeedRoomTypesAndRoomsAsync(context);
            await SeedExtrasAsync(context);
        }
        private static async Task SeedRoomTypesAndRoomsAsync(AppDbContext context)
        {
            if (await context.RoomTypes.AnyAsync())
                return;

            var roomTypes = new List<RoomType>
            {
                new RoomType
                {
                    Name = "Standard Room",
                    Description = "A comfortable room with a queen bed, perfect for solo travelers or couples on a budget.",
                    Capacity = 2,
                    ViewType = "City",
                    PricePerNight = 800,
                    CancellationPolicy = "Free cancellation up to 24 hours before check-in.",
                    Amenities = "Wi-Fi, TV, Air Conditioning"
                },
                new RoomType
                {
                    Name = "Deluxe Sea View",
                    Description = "Spacious room with a private balcony overlooking the sea, king-size bed and modern decor.",
                    Capacity = 2,
                    ViewType = "Sea",
                    PricePerNight = 1500,
                    CancellationPolicy = "Free cancellation up to 48 hours before check-in.",
                    Amenities = "Wi-Fi, TV, Air Conditioning, Sea View Balcony"
                },
                new RoomType
                {
                    Name = "Family Suite",
                    Description = "Two connected rooms ideal for families, with a living area and extra bedding for children.",
                    Capacity = 3,
                    ViewType = "City",
                    PricePerNight = 2200,
                    CancellationPolicy = "Free cancellation up to 72 hours before check-in.",
                    Amenities = "Wi-Fi, TV, Air Conditioning, Extra Beds, Breakfast Included"
                },
                new RoomType
                {
                    Name = "Executive City View",
                    Description = "A refined room overlooking the city skyline, designed for business travelers.",
                    Capacity = 2,
                    ViewType = "City",
                    PricePerNight = 1800,
                    CancellationPolicy = "Free cancellation up to 48 hours before check-in.",
                    Amenities = "Wi-Fi, TV, Air Conditioning, Work Desk"
                },
                new RoomType
                {
                    Name = "Presidential Suite",
                    Description = "Our most luxurious suite with a private lounge, dining area, and panoramic sea views.",
                    Capacity = 2,
                    ViewType = "City and Garden",
                    PricePerNight = 4500,
                    CancellationPolicy = "Free cancellation up to 7 days before check-in.",
                    Amenities = "Wi-Fi, TV, Air Conditioning, Jacuzzi, Breakfast Included, Butler Service"
                },
                new RoomType
                {
                    Name = "Garden View Twin",
                    Description = "A quiet room with two single beds facing the hotel gardens — great for friends traveling together.",
                    Capacity = 2,
                    ViewType = "Garden",
                    PricePerNight = 950,
                    CancellationPolicy = "Free cancellation up to 24 hours before check-in.",
                    Amenities = "Wi-Fi, TV, Air Conditioning, Garden View"
                }
            };

            context.RoomTypes.AddRange(roomTypes);
            await context.SaveChangesAsync();

          
            var random = new Random();
            var rooms = new List<Room>();
            int roomsPerType = 5; 

            foreach (var roomType in roomTypes)
            {
                for (int i = 1; i <= roomsPerType; i++)
                {
                    int floor = ((roomType.Id - 1) * roomsPerType + i) / 6 + 1;
                    rooms.Add(new Room
                    {
                        RoomNumber = $"{floor}0{i}",
                        FloorNumber = floor,
                        Status = RoomStatus.Available,
                        RoomTypeId = roomType.Id
                    });
                }
            }

            context.Rooms.AddRange(rooms);

            
            var roomImages = new List<RoomImage>();
            var slugMap = new Dictionary<string, string>
            {
                { "Standard Room", "standard" },
                { "Deluxe Sea View", "deluxe-sea-view" },
                { "Family Suite", "family-suite" },
                { "Executive City View", "executive-city-view" },
                { "Presidential Suite", "presidential-suite" },
                { "Garden View Twin", "garden-view-twin" }
            };

            foreach (var roomType in roomTypes)
            {
                var slug = slugMap[roomType.Name];
                roomImages.Add(new RoomImage
                {
                    ImageUrl = $"/Images/Rooms/Seed/{slug}-1.jpg",
                    DisplayOrder = 0,
                    RoomTypeId = roomType.Id
                });
                roomImages.Add(new RoomImage
                {
                    ImageUrl = $"/Images/Rooms/Seed/{slug}-2.jpg",
                    DisplayOrder = 1,
                    RoomTypeId = roomType.Id
                });
            }

            context.RoomImages.AddRange(roomImages);
            await context.SaveChangesAsync();
        }

        
        private static async Task SeedExtrasAsync(AppDbContext context)
        {
            if (await context.Extras.AnyAsync())
                return;

            var extras = new List<Extra>
            {
                new Extra { Name = "Breakfast", Price = 150, PricingType = ExtraPricingType.PerNight },
                new Extra { Name = "Airport Pickup", Price = 300, PricingType = ExtraPricingType.PerStay },
                new Extra { Name = "Late Checkout", Price = 200, PricingType = ExtraPricingType.PerStay },
                new Extra { Name = "Spa Access", Price = 250, PricingType = ExtraPricingType.PerNight },
                new Extra { Name = "Extra Bed", Price = 180, PricingType = ExtraPricingType.PerNight }
            };

            context.Extras.AddRange(extras);
            await context.SaveChangesAsync();
        }
    }
}

