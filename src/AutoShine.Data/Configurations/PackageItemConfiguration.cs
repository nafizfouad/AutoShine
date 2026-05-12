using AutoShine.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoShine.Data.Configurations;

public class PackageItemConfiguration : IEntityTypeConfiguration<PackageItem>
{
    public void Configure(EntityTypeBuilder<PackageItem> builder)
    {
        builder.HasKey(pi => pi.Id);

        builder.HasOne(pi => pi.Package)
            .WithMany(p => p.PackageItems)
            .HasForeignKey(pi => pi.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pi => pi.InventoryItem)
            .WithMany(i => i.PackageItems)
            .HasForeignKey(pi => pi.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(pi => pi.QuantityRequired).IsRequired();
    }
}
