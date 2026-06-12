using Microsoft.EntityFrameworkCore;
using ProtocolService.Data.Entities;

namespace ProtocolService.Data;

public class ProtocolDbContext : DbContext
{
    public ProtocolDbContext(DbContextOptions<ProtocolDbContext> options) : base(options) { }

    public DbSet<Protocol> Protocols => Set<Protocol>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<ProtocolSite> ProtocolSites => Set<ProtocolSite>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Ignore cross-service entities — owned by other services
        mb.Ignore<ComplianceReport>();
        mb.Ignore<KpiReport>();
        mb.Ignore<LabResult>();
        mb.Ignore<Notification>();
        mb.Ignore<Patient>();
        mb.Ignore<PatientEnrollment>();
        // Role is read-only — excluded from migrations
        mb.Entity<Role>(e => e.ToTable("Roles", t => t.ExcludeFromMigrations()));
        mb.Ignore<Sample>();
        mb.Ignore<UserSession>();
        mb.Ignore<Visit>();

        // Protocol
        mb.Entity<Protocol>(e =>
        {
            e.ToTable("Protocols");
            e.Property(x => x.ProtocolId).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Status).HasDefaultValue("UPCOMING");

            // Ignore navigations to ignored entities
            e.Ignore(x => x.ComplianceReports);
            e.Ignore(x => x.KpiReports);
        });

        // Site
        mb.Entity<Site>(e =>
        {
            e.ToTable("Sites");
            e.Property(x => x.SiteId)
             .HasDefaultValueSql("NEWSEQUENTIALID()")
             .ValueGeneratedOnAdd();
            e.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        });

        // ProtocolSite
        mb.Entity<ProtocolSite>(e =>
        {
            e.ToTable("ProtocolSites");
            e.Property(x => x.ProtocolSiteId).HasDefaultValueSql("NEWSEQUENTIALID()");
            e.Property(x => x.Status).HasDefaultValue("ACTIVE");

            // Ignore navigation to ignored entities
            e.Ignore(x => x.PatientEnrollments);
        });

        // User — read only, owned by UserService
        mb.Entity<User>(e =>
        {
            e.ToTable("Users", t => t.ExcludeFromMigrations());

            // Ignore navigations to ignored entities
            e.Ignore(x => x.ComplianceReports);
            e.Ignore(x => x.KpiReports);
            e.Ignore(x => x.LabResults);
            e.Ignore(x => x.Notifications);
            // Role navigation now available (read-only)
            e.Ignore(x => x.Samples);
            e.Ignore(x => x.UserSessions);
        });
    }
}