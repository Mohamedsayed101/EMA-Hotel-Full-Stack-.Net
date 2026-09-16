using Hotel_MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hotel_MVC.Configurations
{
    public class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RoomNumber)
                   .IsRequired();

            builder.Property(x => x.FloorNumber)
                   .IsRequired();

            builder.HasMany(x => x.Bookings)
                   .WithOne(r => r.Room)
                   .HasForeignKey(r => r.RoomId);
        }
    }
}
