using Hotel_MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hotel_MVC.Configurations
{
    public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
    {
        public void Configure(EntityTypeBuilder<RoomType> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .HasMaxLength(150)
                   .IsRequired();

            builder.Property(x => x.PricePerNight)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.HasMany(x => x.Rooms)
                   .WithOne(r => r.RoomType)
                   .HasForeignKey(r => r.RoomTypeId);

            builder.HasMany(x => x.RoomImages)
                   .WithOne(ri => ri.RoomType)
                   .HasForeignKey(ri => ri.RoomTypeId);
        }
    }
}
