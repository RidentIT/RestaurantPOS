using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantPOS.Domain.Common;

namespace RestaurantPOS.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly IPublisher? _publisher;

    public AppDbContext(DbContextOptions<AppDbContext> options, IPublisher? publisher = null) : base(options)
    {
        _publisher = publisher;
    }

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