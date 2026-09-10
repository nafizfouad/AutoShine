using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AutoShine.UnitTests.Helpers;

/// <summary>Creates a fresh in-memory AppDbContext for each test.</summary>
public static class DbContextFactory
{
    public static AppDbContext Create(string? dbName = null)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            // InMemory silently ignores transactions — suppress the warning-as-error
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(opts);
    }
}


/// <summary>Pre-built entity helpers to keep test code terse.</summary>
public static class Seed
{
    public static User Employee(int id = 1, string first = "Alice", string last = "Smith")
        => new()
        {
            Id = id, FirstName = first, LastName = last,
            Email = $"emp{id}@test.com", PasswordHash = "hash",
            Phone = "555-0001", Role = UserRole.Employee, IsActive = true
        };

    public static User Customer(int id = 100, string first = "Bob", string last = "Jones")
        => new()
        {
            Id = id, FirstName = first, LastName = last,
            Email = $"cust{id}@test.com", PasswordHash = "hash",
            Phone = "555-0002", Role = UserRole.Customer, IsActive = true
        };

    public static User Admin(int id = 200)
        => new()
        {
            Id = id, FirstName = "Admin", LastName = "User",
            Email = $"admin{id}@test.com", PasswordHash = "hash",
            Phone = "555-0003", Role = UserRole.Admin, IsActive = true
        };

    public static Package Package(int id = 1, bool active = true, int durationMin = 60)
        => new()
        {
            Id = id, Name = $"Package {id}", Description = "Desc",
            Price = 49.99m, EstimatedDurationMinutes = durationMin, IsActive = active
        };

    public static InventoryItem InventoryItem(int id = 1, int stock = 100)
        => new()
        {
            Id = id, ItemName = $"Item {id}", SKU = $"SKU-{id:D3}",
            CurrentStock = stock, MinimumThreshold = 5, Unit = "pieces"
        };

    public static Booking Booking(int id, int customerId, int employeeId, int packageId,
        BookingStatus status = BookingStatus.Pending,
        DateTime? start = null)
    {
        var s = start ?? DateTime.UtcNow.AddDays(1);
        return new Booking
        {
            Id = id, CustomerId = customerId, EmployeeId = employeeId,
            PackageId = packageId, StartTime = s, EndTime = s.AddHours(1),
            Status = status
        };
    }

    public static Review Review(int id, int bookingId, int customerId, int employeeId, int rating = 5)
        => new()
        {
            Id = id, BookingId = bookingId, CustomerId = customerId,
            EmployeeId = employeeId, Rating = rating, Comment = "Great!"
        };

    public static EmployeeScheduleTemplate Template(int id, int employeeId,
        int workingDays = 62 /* Mon-Fri */)
        => new()
        {
            Id = id, EmployeeId = employeeId,
            StartDate = DateTime.UtcNow.Date.AddDays(-30),
            EndDate   = DateTime.UtcNow.Date.AddDays(30),
            WorkingDays = workingDays,
            WorkStartTime = new TimeSpan(9, 0, 0),
            WorkEndTime   = new TimeSpan(17, 0, 0),
            IsActive = true
        };

    public static EmployeeLeave Leave(int id, int employeeId, DateTime? date = null)
        => new()
        {
            Id = id, EmployeeId = employeeId,
            Date = (date ?? DateTime.UtcNow.Date).Date,
            Reason = "Sick"
        };
}
