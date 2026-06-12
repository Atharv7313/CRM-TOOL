using System;
using System.Collections.Generic;
using AuditService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditService.Data;

public partial class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.AuditId).HasName("PK__AuditLog__A17F2398383E1624");

            entity.Property(e => e.AuditId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Timestamp).HasDefaultValueSql("(getutcdate())");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
