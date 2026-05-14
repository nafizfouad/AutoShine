using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoShine.Data.Seeders;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        if (!await context.Users.AnyAsync())
        {
            var admin = new User
            {
                FirstName = "Admin",
                LastName = "AutoShine",
                Email = "admin@autoshine.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Phone = "555-0100",
                Role = UserRole.Admin,
                IsActive = true
            };

            var emp1 = new User
            {
                FirstName = "John",
                LastName = "Mechanic",
                Email = "john.mechanic@autoshine.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                Phone = "555-0101",
                Role = UserRole.Employee,
                IsActive = true
            };

            var emp2 = new User
            {
                FirstName = "Sarah",
                LastName = "Washer",
                Email = "sarah.washer@autoshine.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Employee@123"),
                Phone = "555-0102",
                Role = UserRole.Employee,
                IsActive = true
            };

            var customer = new User
            {
                FirstName = "Alice",
                LastName = "Customer",
                Email = "alice@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123"),
                Phone = "555-0200",
                Role = UserRole.Customer,
                IsActive = true
            };

            await context.Users.AddRangeAsync(admin, emp1, emp2, customer);
            await context.SaveChangesAsync();
        }

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
