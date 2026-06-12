using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using ReportingService.Data.Entities;

namespace ReportingService.Data;

public partial class ServicesDbContext : DbContext
{
    public ServicesDbContext(DbContextOptions<ServicesDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ComplianceReport> ComplianceReports { get; set; }

    public virtual DbSet<KpiReport> KpiReports { get; set; }

    public virtual DbSet<LabResult> LabResults { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<PatientEnrollment> PatientEnrollments { get; set; }

    public virtual DbSet<Protocol> Protocols { get; set; }

    public virtual DbSet<ProtocolSite> ProtocolSites { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Sample> Samples { get; set; }

    public virtual DbSet<Site> Sites { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserSession> UserSessions { get; set; }

    public virtual DbSet<Visit> Visits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComplianceReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__Complian__D5BD4805A3292A42");

            entity.Property(e => e.ReportId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.GeneratedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasDefaultValue("DRAFT");

            entity.HasOne(d => d.GeneratedByUser).WithMany(p => p.ComplianceReports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComplianceReports_GeneratedByUserId");

            entity.HasOne(d => d.Protocol).WithMany(p => p.ComplianceReports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ComplianceReports_ProtocolId");
        });

        modelBuilder.Entity<KpiReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__KpiRepor__D5BD4805CC199C16");

            entity.Property(e => e.ReportId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.GeneratedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.GeneratedByUser).WithMany(p => p.KpiReports)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_KpiReports_GeneratedByUserId");

            entity.HasOne(d => d.Protocol).WithMany(p => p.KpiReports).HasConstraintName("FK_KpiReports_ProtocolId");
        });

        modelBuilder.Entity<LabResult>(entity =>
        {
            entity.HasKey(e => e.ResultId).HasName("PK__LabResul__9769020843354922");

            entity.Property(e => e.ResultId).HasDefaultValueSql("(newsequentialid())");

            entity.HasOne(d => d.RecordedByUser).WithMany(p => p.LabResults)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_LabResults_RecordedByUserId");

            entity.HasOne(d => d.Sample).WithMany(p => p.LabResults).HasConstraintName("FK_LabResults_SampleId");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Notifica__20CF2E12051C010C");

            entity.Property(e => e.NotificationId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Channel).HasDefaultValue("IN_APP");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Status).HasDefaultValue("UNREAD");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasConstraintName("FK_Notifications_UserId");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(e => e.PatientId).HasName("PK__Patients__970EC366D83C3BF7");

            entity.Property(e => e.PatientId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.PatientStatus).HasDefaultValue("ACTIVE");
        });

        modelBuilder.Entity<PatientEnrollment>(entity =>
        {
            entity.HasKey(e => e.EnrollmentId).HasName("PK__PatientE__7F68771B1F36FD7B");

            entity.Property(e => e.EnrollmentId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.EnrolledAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.EnrollmentStatus).HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.Patient).WithMany(p => p.PatientEnrollments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatientEnrollments_PatientId");

            entity.HasOne(d => d.ProtocolSite).WithMany(p => p.PatientEnrollments)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PatientEnrollments_ProtocolSite");
        });

        modelBuilder.Entity<Protocol>(entity =>
        {
            entity.HasKey(e => e.ProtocolId).HasName("PK__Protocol__C1131817174640D1");

            entity.Property(e => e.ProtocolId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Status).HasDefaultValue("DRAFT");

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Protocols)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Protocols_CreatedByUserId");
        });

        modelBuilder.Entity<ProtocolSite>(entity =>
        {
            entity.HasKey(e => e.ProtocolSiteId).HasName("PK__Protocol__E753597F555154B1");

            entity.Property(e => e.ProtocolSiteId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Status).HasDefaultValue("ACTIVE");

            entity.HasOne(d => d.InvestigatorUser).WithMany(p => p.ProtocolSites)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProtocolSites_Investigator");

            entity.HasOne(d => d.Protocol).WithMany(p => p.ProtocolSites).HasConstraintName("FK_ProtocolSites_ProtocolId");

            entity.HasOne(d => d.Site).WithMany(p => p.ProtocolSites)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProtocolSites_SiteId");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__8AFACE1A605CD519");

            entity.Property(e => e.RoleId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Sample>(entity =>
        {
            entity.HasKey(e => e.SampleId).HasName("PK__Samples__8B99EC6A1A85838E");

            entity.Property(e => e.SampleId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.Status).HasDefaultValue("COLLECTED");

            entity.HasOne(d => d.CollectedByUser).WithMany(p => p.Samples)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Samples_CollectedByUserId");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Samples)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Samples_EnrollmentId");
        });

        modelBuilder.Entity<Site>(entity =>
        {
            entity.HasKey(e => e.SiteId).HasName("PK__Sites__B9DCB96346F61CFD");

            entity.Property(e => e.SiteId).HasDefaultValueSql("(newsequentialid())");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C7B24EEA7");

            entity.Property(e => e.UserId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_RoleId");
        });

        modelBuilder.Entity<UserSession>(entity =>
        {
            entity.HasKey(e => e.SessionId).HasName("PK__UserSess__C9F4929033B2C9BA");

            entity.Property(e => e.SessionId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getutcdate())");

            entity.HasOne(d => d.User).WithMany(p => p.UserSessions).HasConstraintName("FK_UserSessions_UserId");
        });

        modelBuilder.Entity<Visit>(entity =>
        {
            entity.HasKey(e => e.VisitId).HasName("PK__Visits__4D3AA1DE7B9692C3");

            entity.Property(e => e.VisitId).HasDefaultValueSql("(newsequentialid())");
            entity.Property(e => e.VisitStatus).HasDefaultValue("SCHEDULED");

            entity.HasOne(d => d.Enrollment).WithMany(p => p.Visits).HasConstraintName("FK_Visits_EnrollmentId");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
