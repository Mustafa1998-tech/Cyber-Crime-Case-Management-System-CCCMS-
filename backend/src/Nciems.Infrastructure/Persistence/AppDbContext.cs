using Microsoft.EntityFrameworkCore;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<CaseAssignment> CaseAssignments => Set<CaseAssignment>();
    public DbSet<Suspect> Suspects => Set<Suspect>();
    public DbSet<Evidence> Evidence => Set<Evidence>();
    public DbSet<EvidenceVersion> EvidenceVersions => Set<EvidenceVersion>();
    public DbSet<EvidenceAccessLog> EvidenceAccessLogs => Set<EvidenceAccessLog>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(256).IsRequired();
            entity.Property(x => x.PasswordHash).IsRequired();
            entity.Property(x => x.PasswordSalt).IsRequired();
            entity.HasIndex(x => x.UserName).IsUnique();
            entity.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(256);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Complaint>(entity =>
        {
            entity.ToTable("Complaints");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ComplainantName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CrimeType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(4000).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Case>(entity =>
        {
            entity.ToTable("Cases");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Priority).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>().IsRequired();
            entity.HasIndex(x => x.ComplaintId).IsUnique();
            entity.HasOne(x => x.Complaint).WithOne(x => x.Case).HasForeignKey<Case>(x => x.ComplaintId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedInvestigator).WithMany().HasForeignKey(x => x.AssignedInvestigatorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CaseAssignment>(entity =>
        {
            entity.ToTable("CaseAssignments");
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.Case).WithMany(x => x.Assignments).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Investigator).WithMany().HasForeignKey(x => x.InvestigatorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedByUser).WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Suspect>(entity =>
        {
            entity.ToTable("Suspects");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NationalId).HasMaxLength(50);
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.IpAddress).HasMaxLength(100);
            entity.Property(x => x.AccountInfo).HasMaxLength(250);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasOne(x => x.Case).WithMany(x => x.Suspects).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Evidence>(entity =>
        {
            entity.ToTable("Evidence");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasOne(x => x.Case).WithMany(x => x.EvidenceItems).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceVersion>(entity =>
        {
            entity.ToTable("EvidenceVersions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StoredFilePath).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Sha256Hash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Md5Hash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.MimeType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.EncryptionIv).HasMaxLength(128).IsRequired();
            entity.Property(x => x.DeviceInfo).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => new { x.EvidenceId, x.VersionNumber }).IsUnique();
            entity.HasOne(x => x.Evidence).WithMany(x => x.Versions).HasForeignKey(x => x.EvidenceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EvidenceAccessLog>(entity =>
        {
            entity.ToTable("EvidenceAccessLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AccessType).HasConversion<int>().IsRequired();
            entity.HasOne(x => x.EvidenceVersion).WithMany(x => x.AccessLogs).HasForeignKey(x => x.EvidenceVersionId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.AccessedByUser).WithMany().HasForeignKey(x => x.AccessedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.ToTable("Reports");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReportType).HasConversion<int>().IsRequired();
            entity.Property(x => x.Content).HasMaxLength(10000).IsRequired();
            entity.Property(x => x.DigitalSignature).HasMaxLength(256).IsRequired();
            entity.Property(x => x.QrPayload).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.PdfPath).HasMaxLength(500);
            entity.HasOne(x => x.Case).WithMany(x => x.Reports).HasForeignKey(x => x.CaseId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.GeneratedByUser).WithMany().HasForeignKey(x => x.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(4000).IsRequired();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
