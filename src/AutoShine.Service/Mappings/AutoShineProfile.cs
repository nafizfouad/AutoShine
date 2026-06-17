using Mapster;
using AutoShine.Models.Entities;
using AutoShine.Service.DTOs.Bookings;
using AutoShine.Service.DTOs.Inventory;
using AutoShine.Service.DTOs.Packages;
using AutoShine.Service.DTOs.Reviews;
using AutoShine.Service.DTOs.Users;

namespace AutoShine.Service.Mappings;

/// <summary>
/// Mapster mapping configuration. Implements IRegister so it is
/// auto-discovered by config.Scan() in Program.cs.
/// </summary>
public class AutoShineProfile : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // ── User ────────────────────────────────────────────────────────
        config.NewConfig<User, UserDto>()
            .Map(dest => dest.Role, src => src.Role.ToString());

        // ── Package ─────────────────────────────────────────────────────
        config.NewConfig<Package, PackageDto>()
            .Map(dest => dest.Items, src => src.PackageItems);

        config.NewConfig<PackageItem, PackageItemDto>()
            .Map(dest => dest.ItemName,          src => src.InventoryItem.ItemName)
            .Map(dest => dest.Unit,              src => src.InventoryItem.Unit);

        // ── Inventory ───────────────────────────────────────────────────
        config.NewConfig<InventoryItem, InventoryItemDto>()
            .Map(dest => dest.IsLowStock, src => src.CurrentStock <= src.MinimumThreshold);

        // ── Booking ─────────────────────────────────────────────────────
        config.NewConfig<Booking, BookingDto>()
            .Map(dest => dest.CustomerName,  src => $"{src.Customer.FirstName} {src.Customer.LastName}")
            .Map(dest => dest.EmployeeName,  src => src.Employee != null
                ? $"{src.Employee.FirstName} {src.Employee.LastName}"
                : null)
            .Map(dest => dest.PackageName,   src => src.Package.Name)
            .Map(dest => dest.PackagePrice,  src => src.Package.Price)
            .Map(dest => dest.Status,        src => src.Status.ToString());

        // ── Review ──────────────────────────────────────────────────────
        config.NewConfig<Review, ReviewDto>()
            .Map(dest => dest.CustomerName, src => $"{src.Customer.FirstName} {src.Customer.LastName}")
            .Map(dest => dest.EmployeeName, src => $"{src.Employee.FirstName} {src.Employee.LastName}");
    }
}
