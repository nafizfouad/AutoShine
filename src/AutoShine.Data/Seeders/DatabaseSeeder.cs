using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutoShine.Data.Seeders;

/// <summary>
/// Seed configuration. Credentials are never hard-coded: they come from
/// appsettings.json (local, git-ignored) or environment variables (production).
/// </summary>
/// <param name="AdminEmail">Seed:AdminEmail — used only when no admin account exists yet.</param>
/// <param name="AdminPassword">Seed:AdminPassword — min 8 characters.</param>
/// <param name="SeedDemoUsers">Create the demo employees/customer (Development only).</param>
/// <param name="DemoPassword">Seed:DemoPassword — shared password for the demo accounts.</param>
public record SeedSettings(
    string? AdminEmail,
    string? AdminPassword,
    bool SeedDemoUsers,
    string? DemoPassword);

public static class DatabaseSeeder
{
    private const int MinPasswordLength = 8;

    public static async Task SeedAsync(AppDbContext context, SeedSettings settings, ILogger logger)
    {
        await SeedAdminAsync(context, settings, logger);

        if (settings.SeedDemoUsers)
            await SeedDemoUsersAsync(context, settings, logger);

        await SeedCatalogAsync(context);
    }

    // ── Users ────────────────────────────────────────────────────────────────

    private static async Task SeedAdminAsync(AppDbContext context, SeedSettings settings, ILogger logger)
    {
        if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        if (string.IsNullOrWhiteSpace(settings.AdminEmail) || string.IsNullOrWhiteSpace(settings.AdminPassword))
        {
            logger.LogWarning(
                "No admin account exists. Set Seed:AdminEmail and Seed:AdminPassword " +
                "(appsettings.json locally, Seed__AdminEmail / Seed__AdminPassword env vars in production) " +
                "and restart to create one.");
            return;
        }

        if (settings.AdminPassword.Length < MinPasswordLength)
        {
            logger.LogWarning("Seed:AdminPassword must be at least {Min} characters. Admin account not created.", MinPasswordLength);
            return;
        }

        var email = settings.AdminEmail.Trim().ToLowerInvariant();
        var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existing != null)
        {
            logger.LogWarning("Seed:AdminEmail {Email} belongs to an existing non-admin account. Admin account not created.", email);
            return;
        }

        context.Users.Add(new User
        {
            FirstName = "Admin",
            LastName = "AutoShine",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(settings.AdminPassword),
            Role = UserRole.Admin,
            IsActive = true
        });
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded admin account {Email}.", email);
    }

    private static async Task SeedDemoUsersAsync(AppDbContext context, SeedSettings settings, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(settings.DemoPassword) || settings.DemoPassword.Length < MinPasswordLength)
        {
            logger.LogWarning(
                "Seed:DemoUsers is enabled but Seed:DemoPassword is missing or shorter than {Min} characters. Demo accounts not created.",
                MinPasswordLength);
            return;
        }

        var demoUsers = new[]
        {
            new User { FirstName = "John",  LastName = "Mechanic", Email = "john.mechanic@autoshine.com", Phone = "555-0101", Role = UserRole.Employee },
            new User { FirstName = "Sarah", LastName = "Washer",   Email = "sarah.washer@autoshine.com",  Phone = "555-0102", Role = UserRole.Employee },
            new User { FirstName = "Alice", LastName = "Customer", Email = "alice@example.com",           Phone = "555-0200", Role = UserRole.Customer },
        };

        var created = new List<User>();
        foreach (var user in demoUsers)
        {
            if (await context.Users.AnyAsync(u => u.Email == user.Email)) continue;
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(settings.DemoPassword);
            user.IsActive = true;
            context.Users.Add(user);
            created.Add(user);
        }
        if (created.Count == 0) return;
        await context.SaveChangesAsync();

        // Give new demo employees a Mon–Fri 09:00–17:00 schedule with a 13:00–14:00 break,
        // running from the start of this year for five years. Admins can edit it afterwards.
        const int MonToFri = 2 | 4 | 8 | 16 | 32;
        var year = DateTime.UtcNow.Year;
        context.EmployeeScheduleTemplates.AddRange(created
            .Where(u => u.Role == UserRole.Employee)
            .Select(emp => new EmployeeScheduleTemplate
            {
                EmployeeId = emp.Id,
                StartDate = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate = new DateTime(year + 4, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                WorkingDays = MonToFri,
                WorkStartTime = new TimeSpan(9, 0, 0),
                WorkEndTime = new TimeSpan(17, 0, 0),
                BreakStartTime = new TimeSpan(13, 0, 0),
                BreakEndTime = new TimeSpan(14, 0, 0),
                IsActive = true
            }));
        await context.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} demo account(s).", created.Count);
    }

    // ── Inventory & packages (no secrets) ────────────────────────────────────

    private static async Task SeedCatalogAsync(AppDbContext context)
    {
        if (!await context.InventoryItems.AnyAsync())
        {
            var items = new List<InventoryItem>
            {
                new() { ItemName = "Car Wash Soap", SKU = "SOAP-001", CurrentStock = 50, MinimumThreshold = 10, Unit = "liters" },
                new() { ItemName = "Microfiber Towels", SKU = "TOWEL-001", CurrentStock = 100, MinimumThreshold = 20, Unit = "pieces" },
                new() { ItemName = "Engine Oil (5W-30)", SKU = "OIL-5W30", CurrentStock = 30, MinimumThreshold = 5, Unit = "liters" },
                new() { ItemName = "Oil Filter", SKU = "FILTER-OIL-001", CurrentStock = 25, MinimumThreshold = 5, Unit = "pieces" },
                new() { ItemName = "Air Filter", SKU = "FILTER-AIR-001", CurrentStock = 20, MinimumThreshold = 5, Unit = "pieces" },
                new() { ItemName = "Brake Fluid", SKU = "FLUID-BRAKE-001", CurrentStock = 15, MinimumThreshold = 3, Unit = "liters" },
                new() { ItemName = "Brake Pad Set", SKU = "BRAKE-PAD-001", CurrentStock = 10, MinimumThreshold = 2, Unit = "sets" },
                new() { ItemName = "Wax Polish", SKU = "WAX-001", CurrentStock = 40, MinimumThreshold = 8, Unit = "liters" },
            };

            await context.InventoryItems.AddRangeAsync(items);
            await context.SaveChangesAsync();
        }

        if (!await context.Packages.AnyAsync())
        {
            var soap = await context.InventoryItems.FirstAsync(i => i.SKU == "SOAP-001");
            var towel = await context.InventoryItems.FirstAsync(i => i.SKU == "TOWEL-001");
            var oil = await context.InventoryItems.FirstAsync(i => i.SKU == "OIL-5W30");
            var oilFilter = await context.InventoryItems.FirstAsync(i => i.SKU == "FILTER-OIL-001");
            var airFilter = await context.InventoryItems.FirstAsync(i => i.SKU == "FILTER-AIR-001");
            var brakeFluid = await context.InventoryItems.FirstAsync(i => i.SKU == "FLUID-BRAKE-001");
            var brakePad = await context.InventoryItems.FirstAsync(i => i.SKU == "BRAKE-PAD-001");
            var wax = await context.InventoryItems.FirstAsync(i => i.SKU == "WAX-001");

            var packages = new List<Package>
            {
                new Package
                {
                    Name = "Basic Wash",
                    Description = "Quick exterior wash and rinse.",
                    Price = 15.00m,
                    EstimatedDurationMinutes = 30,
                    PackageItems = new List<PackageItem>
                    {
                        new() { InventoryItemId = soap.Id, QuantityRequired = 1 },
                        new() { InventoryItemId = towel.Id, QuantityRequired = 2 }
                    }
                },
                new Package
                {
                    Name = "Deluxe Wash & Wax",
                    Description = "Full exterior wash, wax polish, and towel dry.",
                    Price = 35.00m,
                    EstimatedDurationMinutes = 60,
                    PackageItems = new List<PackageItem>
                    {
                        new() { InventoryItemId = soap.Id, QuantityRequired = 2 },
                        new() { InventoryItemId = towel.Id, QuantityRequired = 4 },
                        new() { InventoryItemId = wax.Id, QuantityRequired = 1 }
                    }
                },
                new Package
                {
                    Name = "Full Synthetic Oil Change",
                    Description = "Full synthetic oil change with new oil filter.",
                    Price = 69.99m,
                    EstimatedDurationMinutes = 45,
                    PackageItems = new List<PackageItem>
                    {
                        new() { InventoryItemId = oil.Id, QuantityRequired = 5 },
                        new() { InventoryItemId = oilFilter.Id, QuantityRequired = 1 }
                    }
                },
                new Package
                {
                    Name = "Premium Service",
                    Description = "Oil change, air filter replacement, and full wash.",
                    Price = 99.99m,
                    EstimatedDurationMinutes = 90,
                    PackageItems = new List<PackageItem>
                    {
                        new() { InventoryItemId = oil.Id, QuantityRequired = 5 },
                        new() { InventoryItemId = oilFilter.Id, QuantityRequired = 1 },
                        new() { InventoryItemId = airFilter.Id, QuantityRequired = 1 },
                        new() { InventoryItemId = soap.Id, QuantityRequired = 1 },
                        new() { InventoryItemId = towel.Id, QuantityRequired = 2 }
                    }
                },
                new Package
                {
                    Name = "Brake Pad Replacement",
                    Description = "Front brake pad replacement with brake fluid top-up.",
                    Price = 149.99m,
                    EstimatedDurationMinutes = 120,
                    PackageItems = new List<PackageItem>
                    {
                        new() { InventoryItemId = brakePad.Id, QuantityRequired = 1 },
                        new() { InventoryItemId = brakeFluid.Id, QuantityRequired = 1 }
                    }
                }
            };

            await context.Packages.AddRangeAsync(packages);
            await context.SaveChangesAsync();
        }
    }
}
