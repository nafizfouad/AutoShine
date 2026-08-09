using AutoShine.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoShine.Data.Configurations;

public class EmployeeScheduleTemplateConfiguration : IEntityTypeConfiguration<EmployeeScheduleTemplate>
{
    public void Configure(EntityTypeBuilder<EmployeeScheduleTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.WorkingDays).IsRequired();
        builder.Property(t => t.WorkStartTime).IsRequired();
        builder.Property(t => t.WorkEndTime).IsRequired();
        builder.Property(t => t.BreakStartTime).IsRequired(false);
        builder.Property(t => t.BreakEndTime).IsRequired(false);
        builder.Property(t => t.IsActive).HasDefaultValue(true);

        builder.HasOne(t => t.Employee)
               .WithMany()
               .HasForeignKey(t => t.EmployeeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.EmployeeId);
    }
}
