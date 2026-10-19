using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gateway.Infrastructure.Persistence;

public sealed class GatewayDbContext(DbContextOptions<GatewayDbContext> options) : DbContext(options)
{
    public DbSet<DocumentRow> Documents => Set<DocumentRow>();
    public DbSet<DocumentVersionRow> DocumentVersions => Set<DocumentVersionRow>();
    public DbSet<DocumentAclRow> DocumentAcls => Set<DocumentAclRow>();
    public DbSet<ExportJobRow> ExportJobs => Set<ExportJobRow>();
    public DbSet<ExportAttemptRow> ExportAttempts => Set<ExportAttemptRow>();
    public DbSet<AuditEventRow> AuditEvents => Set<AuditEventRow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        var d = b.Entity<DocumentRow>(); d.ToTable("Documents"); d.HasKey(x => x.Id);
        d.HasAlternateKey(x => new { x.Id, x.TenantId });
        d.Property(x => x.Title).HasMaxLength(200); d.Property(x => x.Purpose).HasMaxLength(1000);
        d.Property(x => x.RowVersion).IsRowVersion();
        d.HasIndex(x => new { x.TenantId, x.OwnerOid });
        var v = b.Entity<DocumentVersionRow>(); v.ToTable("DocumentVersions"); v.HasKey(x => x.Id);
        v.HasAlternateKey(x => new { x.Id, x.DocumentId, x.TenantId });
        v.Property(x => x.DisplayFilename).HasMaxLength(255); v.Property(x => x.RowVersion).IsRowVersion();
        v.HasOne<DocumentRow>().WithMany().HasForeignKey(x => new { x.DocumentId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.NoAction);
        d.HasOne<DocumentVersionRow>().WithMany().HasForeignKey(x => new { x.CurrentPublishedVersionId, x.Id, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.DocumentId, x.TenantId }).OnDelete(DeleteBehavior.NoAction);
        var a = b.Entity<DocumentAclRow>(); a.ToTable("DocumentAcls"); a.HasKey(x => new { x.DocumentId, x.TenantId, x.RequesterOid });
        a.HasOne<DocumentRow>().WithMany().HasForeignKey(x => new { x.DocumentId, x.TenantId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.NoAction);
        var j = b.Entity<ExportJobRow>(); j.ToTable("ExportJobs", t =>
        {
            t.HasCheckConstraint("CK_ExportJobs_SafeStates", "[State] IN (0,1,2,3,4,6,7,9)"); // Ready/Expired have no writer in this checkpoint.
            t.HasCheckConstraint("CK_ExportJobs_Digest", "LEN([RequestDigest]) = 64");
            t.HasCheckConstraint("CK_ExportJobs_Mode", "[Mode] IN (0,1) AND ([State] <> 9 OR [Mode] = 0)");
            t.HasCheckConstraint("CK_ExportJobs_SourceKind", "[SourceKind] IN (0,1)");
            t.HasCheckConstraint("CK_ExportJobs_Attempts", "[AttemptCount] BETWEEN 0 AND 3 AND [Fence] >= 0");
            t.HasCheckConstraint("CK_ExportJobs_Snapshot", "ISJSON([SnapshotJson]) = 1");
            t.HasCheckConstraint("CK_ExportJobs_Lease", "([LeaseToken] IS NULL AND [LeaseOwner] IS NULL AND [LeaseExpiresAt] IS NULL) OR ([LeaseToken] IS NOT NULL AND [LeaseOwner] IS NOT NULL AND [LeaseExpiresAt] IS NOT NULL)");
        });
        j.HasKey(x => x.Id); j.Property(x => x.RequestDigest).HasMaxLength(64).IsUnicode(false).UseCollation("Latin1_General_100_BIN2");
        j.Property(x => x.SnapshotJson).HasColumnType("nvarchar(max)");
        j.Property(x => x.LeaseOwner).HasMaxLength(100).UseCollation("Latin1_General_100_BIN2"); j.Property(x => x.SafeErrorCode).HasMaxLength(80);
        j.Property(x => x.RowVersion).IsRowVersion();
        j.HasIndex(x => new { x.TenantId, x.RequesterOid, x.IdempotencyKey }).IsUnique();
        j.HasIndex(x => new { x.State, x.AvailableAt, x.LeaseExpiresAt });
        j.HasIndex(x => new { x.TenantId, x.RequesterOid, x.CreatedAt });
        var attempt = b.Entity<ExportAttemptRow>(); attempt.ToTable("ExportAttempts"); attempt.HasKey(x => x.Id);
        attempt.HasIndex(x => new { x.JobId, x.Number }).IsUnique();
        attempt.HasIndex(x => new { x.JobId, x.Fence }).IsUnique();
        attempt.Property(x => x.Worker).HasMaxLength(100); attempt.Property(x => x.Outcome).HasMaxLength(80);
        attempt.HasOne<ExportJobRow>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
        var audit = b.Entity<AuditEventRow>(); audit.ToTable("AuditEvents"); audit.HasKey(x => x.Id);
        audit.Property(x => x.EventType).HasMaxLength(80); audit.HasIndex(x => new { x.TenantId, x.At });
        audit.HasOne<ExportJobRow>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
    }
}

// Design-time only; migrations/scripts do not connect to or mutate any database.
public sealed class GatewayDesignTimeFactory : IDesignTimeDbContextFactory<GatewayDbContext>
{
    public GatewayDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<GatewayDbContext>()
        .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=GatewayDesignOnly;Integrated Security=true;Encrypt=false",
            sql => sql.UseCompatibilityLevel(160)).Options);
}
