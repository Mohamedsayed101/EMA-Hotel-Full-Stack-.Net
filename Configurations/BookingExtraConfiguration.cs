using Hotel_MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hotel_MVC.Configurations
{
    public class BookingExtraConfiguration : IEntityTypeConfiguration<BookingExtra>
    {
        public void Configure(EntityTypeBuilder<BookingExtra> builder)
        {
            builder.HasKey(be => be.Id);

            builder.Property(be => be.Quantity)
                   .IsRequired();

            builder.Property(be => be.UnitPrice)
                   .IsRequired()
                   .HasColumnType("decimal(18,2)");

            builder.HasOne(be => be.Booking)
                   .WithMany(b => b.BookingExtras)
                   .HasForeignKey(be => be.BookingId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(be => be.Extra)
                   .WithMany(e => e.BookingExtras)
                   .HasForeignKey(be => be.ExtraId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
