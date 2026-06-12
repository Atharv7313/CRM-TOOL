using AuditLogService.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AuditLogService.API.Data;

public class AuditLogDbContext : DbContext
{
    public AuditLogDbContext(DbContextOptions<AuditLogDbContext> options) : base(options) { }

    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.ActorName)   .HasMaxLength(100).IsRequired();
            e.Property(x => x.ActorEmail)  .HasMaxLength(150);
            e.Property(x => x.Action)      .HasMaxLength(100).IsRequired();
            e.Property(x => x.ServiceName) .HasMaxLength(50) .IsRequired();
            e.Property(x => x.Description) .HasMaxLength(500);
            e.Property(x => x.EntityId)    .HasMaxLength(100);
            e.Property(x => x.EntityName)  .HasMaxLength(200);
            e.Property(x => x.IpAddress)   .HasMaxLength(45);
            e.Property(x => x.IsSuccess)   .IsRequired();
            e.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .HasColumnType("datetime2");

            e.HasIndex(x => x.ServiceName);
            e.HasIndex(x => x.ActorUserId);
            e.HasIndex(x => x.CreatedAt);
        });
    }
}
