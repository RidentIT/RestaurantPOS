using MediatR;

using Microsoft.EntityFrameworkCore;

using RestaurantPOS.Application.Common.Interfaces;
using RestaurantPOS.Domain.Common;
using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly IPublisher? _publisher;

    public AppDbContext(DbContextOptions<AppDbContext> options, IPublisher? publisher = null) : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<UserModulePermission> UserModulePermissions => Set<UserModulePermission>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<RawMaterial> RawMaterials => Set<RawMaterial>();

    public DbSet<Recipe> Recipes => Set<Recipe>();

    public DbSet<Supplier> Suppliers => Set<Supplier>();

    public DbSet<StockLevel> StockLevels => Set<StockLevel>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<GoodsReceivedNote> GoodsReceivedNotes => Set<GoodsReceivedNote>();

    public DbSet<StockRelease> StockReleases => Set<StockRelease>();

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();

    public DbSet<SupplierPrice> SupplierPrices => Set<SupplierPrice>();

    public DbSet<SupplierPriceHistoryEntry> SupplierPriceHistoryEntries => Set<SupplierPriceHistoryEntry>();

    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        if (_publisher != null)
        {
            await PublishDomainEventsAsync(cancellationToken);
        }

        return result;
    }

    private async Task PublishDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        foreach (var entity in domainEntities)
        {
            entity.Entity.ClearDomainEvents();
        }

        foreach (var domainEvent in domainEvents)
        {
            await _publisher!.Publish(domainEvent, cancellationToken);
        }
    }
}