using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoShine.Data.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(b => b.Notes).HasMaxLength(500);

        // Review (one-to-one with booking)
        builder.HasOne(b => b.Review)
            .WithOne(r => r.Booking)
            .HasForeignKey<Review>(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
