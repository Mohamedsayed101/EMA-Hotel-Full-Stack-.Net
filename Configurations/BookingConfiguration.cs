using Hotel_MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hotel_MVC.Configurations
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.BookingReference)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(b => b.CancellationReason)
                   .HasMaxLength(50);

            builder.Property(b => b.TotalAmount)
                   .HasPrecision(18, 2);
        }
    }
}
