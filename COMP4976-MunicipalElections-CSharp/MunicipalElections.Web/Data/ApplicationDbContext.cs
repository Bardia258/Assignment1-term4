using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Models;

namespace MunicipalElections.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<MunicipalityAdminGroup> MunicipalityAdminGroups => Set<MunicipalityAdminGroup>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        RefreshRevisions();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        RefreshRevisions();
        return base.SaveChanges();
    }

    private void RefreshRevisions()
    {
        foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Modified
            && (e.Entity is Municipality or Position or Candidate)))
            entry.Property("Revision").CurrentValue = Guid.NewGuid().ToString("N");
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Municipality>().Property(m => m.Name).UseCollation("NOCASE");
        builder.Entity<Municipality>().Property(m => m.Province).UseCollation("NOCASE");
        builder.Entity<Position>().Property(p => p.Name).UseCollation("NOCASE");
        builder.Entity<Municipality>().HasIndex(m => new { m.Name, m.Province }).IsUnique();
        builder.Entity<Position>().HasIndex(p => new { p.MunicipalityId, p.Name }).IsUnique();
        builder.Entity<MunicipalityAdminGroup>().HasKey(g => g.MunicipalityId);
        builder.Entity<MunicipalityAdminGroup>().HasIndex(g => g.RoleId).IsUnique();
        builder.Entity<MunicipalityAdminGroup>().HasOne(g => g.Municipality).WithMany()
            .HasForeignKey(g => g.MunicipalityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MunicipalityAdminGroup>().HasOne(g => g.Role).WithMany()
            .HasForeignKey(g => g.RoleId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Municipality>().HasIndex(m => m.SeedKey).IsUnique();
        builder.Entity<Position>().HasIndex(p => p.SeedKey).IsUnique();
        builder.Entity<Candidate>().HasIndex(c => c.SeedKey).IsUnique();

        builder.Entity<Position>().ToTable(t =>
            t.HasCheckConstraint("CK_Positions_SeatsAvailable", "\"SeatsAvailable\" > 0"));

        builder.Entity<Position>()
            .HasOne(p => p.Municipality).WithMany(m => m.Positions)
            .HasForeignKey(p => p.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Candidate>()
            .HasOne(c => c.Municipality).WithMany(m => m.Candidates)
            .HasForeignKey(c => c.MunicipalityId).OnDelete(DeleteBehavior.Restrict);

        // The composite FK rejects positions belonging to a different municipality,
        // including writes through MVC forms or directly through SQLite.
        builder.Entity<Candidate>()
            .HasOne(c => c.Position).WithMany(p => p.Candidates)
            .HasForeignKey(c => new { c.PositionId, c.MunicipalityId })
            .HasPrincipalKey(p => new { p.PositionId, p.MunicipalityId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Municipality).WithMany()
            .HasForeignKey(u => u.MunicipalityId).OnDelete(DeleteBehavior.SetNull);
    }
}
