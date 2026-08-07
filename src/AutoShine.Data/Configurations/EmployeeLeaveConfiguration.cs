using AutoShine.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoShine.Data.Configurations;

public class EmployeeLeaveConfiguration : IEntityTypeConfiguration<EmployeeLeave>
{
    public void Configure(EntityTypeBuilder<EmployeeLeave> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Reason).HasMaxLength(500).IsRequired(false);

        builder.HasOne(l => l.Employee)
               .WithMany()
               .HasForeignKey(l => l.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.EmployeeId, l.Date });
    }
}
