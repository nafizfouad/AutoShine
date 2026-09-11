using AutoShine.Data;
using AutoShine.Models.Entities;
using AutoShine.Repository.Implementations;
using AutoShine.Repository.Interfaces;
using Mapster;
using MapsterMapper;
using AutoShine.Service.Mappings;
using Microsoft.Extensions.DependencyInjection;

namespace AutoShine.UnitTests.Helpers;

/// <summary>
/// Assembles a real UnitOfWork backed by an in-memory database, plus a
/// properly configured Mapster IMapper — giving service tests full
/// end-to-end coverage without any mocking.
/// </summary>
public sealed class ServiceFixture : IDisposable
{
    public AppDbContext Ctx { get; }
    public IUnitOfWork Uow { get; }
    public IMapper Mapper { get; }

    public ServiceFixture(string? dbName = null)
    {
        Ctx = DbContextFactory.Create(dbName ?? Guid.NewGuid().ToString());

        // Wire up repositories exactly as DI does in production.
        var userRepo     = new UserRepository(Ctx);
        var bookingRepo  = new BookingRepository(Ctx);
        var inventoryRepo= new InventoryRepository(Ctx);
        var packageRepo  = new PackageRepository(Ctx);
        var reviewRepo   = new ReviewRepository(Ctx);
        var scheduleRepo = new ScheduleRepository(Ctx);

        Uow = new UnitOfWork(Ctx, userRepo, bookingRepo,
            inventoryRepo, packageRepo, reviewRepo, scheduleRepo);

        // Build a minimal service provider so ServiceMapper is satisfied.
        var config = new TypeAdapterConfig();
        new AutoShineProfile().Register(config);

        var services = new ServiceCollection();
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();
        var sp = services.BuildServiceProvider();
        Mapper = sp.GetRequiredService<IMapper>();
    }

    // Convenience: add & save in one line
    public async Task SeedAsync(params object[] entities)
    {
        foreach (var e in entities)
            Ctx.Add(e);
        await Ctx.SaveChangesAsync();
    }

    public void Dispose() => Ctx.Dispose();
}

