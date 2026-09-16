using Hotel_MVC.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hotel_MVC.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.FirstName)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(u => u.LastName)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(u => u.Nationality)
                   .HasMaxLength(50);

            builder.Property(u => u.PassportOrNationalId)
                   .HasMaxLength(50);

            builder.HasMany(u => u.Bookings)
                   .WithOne(b => b.Guest)
                   .HasForeignKey(b => b.GuestId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Reviews)
                   .WithOne(r => r.Guest)
                   .HasForeignKey(r => r.GuestId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
