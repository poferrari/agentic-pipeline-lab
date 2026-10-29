using App.Domain.Common;
using App.Domain.MarketPulse.Entities;
using App.Domain.MarketPulse.ValueObjects;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace App.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<CurrencyWatchItem> CurrencyWatchItems => Set<CurrencyWatchItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}

public sealed class CurrencyWatchItemConfiguration : IEntityTypeConfiguration<CurrencyWatchItem>
{
    public void Configure(EntityTypeBuilder<CurrencyWatchItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("currency_watch_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(x => x.BaseCurrency)
            .HasColumnName("base_currency")
            .HasMaxLength(3)
            .HasConversion(
                v => v.Value,
                v => CurrencyCode.Create(v).Value)
            .IsRequired();

        builder.Property(x => x.TargetCurrency)
            .HasColumnName("target_currency")
            .HasMaxLength(3)
            .HasConversion(
                v => v.Value,
                v => CurrencyCode.Create(v).Value)
            .IsRequired();

        builder.Property(x => x.TargetRate)
            .HasColumnName("target_rate")
            .HasPrecision(18, 6)
            .HasConversion(
                v => v.Value,
                v => TargetRate.Create(v).Value)
            .IsRequired();

        builder.Property(x => x.NotifyWhenAbove)
            .HasColumnName("notify_when_above")
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasColumnName("notes")
            .HasMaxLength(CurrencyWatchItem.MaxNotesLength);

        builder.Property(x => x.LastKnownRate)
            .HasColumnName("last_known_rate")
            .HasPrecision(18, 6);

        builder.Property(x => x.IsTriggered)
            .HasColumnName("is_triggered")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(x => x.TriggeredAtUtc)
            .HasColumnName("triggered_at_utc");

        builder.HasIndex(x => new { x.OwnerId, x.BaseCurrency, x.TargetCurrency })
            .IsUnique()
            .HasDatabaseName("ux_currency_watch_items_owner_pair");

        builder.HasIndex(x => new { x.OwnerId, x.CreatedAtUtc })
            .HasDatabaseName("idx_currency_watch_items_owner_created");

        builder.Ignore(x => x.DomainEvents);
    }
}
