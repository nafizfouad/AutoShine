using AutoShine.Data;
using AutoShine.Repository.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace AutoShine.Repository.Implementations;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository Users { get; }
    public IBookingRepository Bookings { get; }
    public IInventoryRepository Inventory { get; }
    public IPackageRepository Packages { get; }
    public IReviewRepository Reviews { get; }
    public IScheduleRepository Schedules { get; }

    public UnitOfWork(AppDbContext context,
        IUserRepository users,
        IBookingRepository bookings,
        IInventoryRepository inventory,
        IPackageRepository packages,
        IReviewRepository reviews,
        IScheduleRepository schedules)
    {
        _context = context;
        Users = users;
        Bookings = bookings;
        Inventory = inventory;
        Packages = packages;
        Reviews = reviews;
        Schedules = schedules;
    }

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync()
        => _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
