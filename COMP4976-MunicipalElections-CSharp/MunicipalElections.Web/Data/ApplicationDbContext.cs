using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Models;

namespace MunicipalElections.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Municipality> Municipalities => Set<Municipality>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<SavedCandidate> SavedCandidates => Set<SavedCandidate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
        // including writes from the future API or directly through SQLite.
        builder.Entity<Candidate>()
            .HasOne(c => c.Position).WithMany(p => p.Candidates)
            .HasForeignKey(c => new { c.PositionId, c.MunicipalityId })
            .HasPrincipalKey(p => new { p.PositionId, p.MunicipalityId })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Municipality).WithMany()
            .HasForeignKey(u => u.MunicipalityId).OnDelete(DeleteBehavior.SetNull);

        builder.Entity<SavedCandidate>()
            .HasIndex(s => new { s.UserId, s.CandidateId }).IsUnique();

        builder.Entity<SavedCandidate>()
            .HasOne(s => s.User).WithMany(u => u.SavedCandidates)
            .HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);

        builder.Entity<SavedCandidate>()
            .HasOne(s => s.Candidate).WithMany(c => c.SavedCandidates)
            .HasForeignKey(s => s.CandidateId).OnDelete(DeleteBehavior.Cascade);
    }
}
