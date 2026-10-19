using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Models;

namespace MunicipalElections.Web.Data;

public static class DbSeeder
{
    // Called only in Development by Program.cs. Stable keys survive administrator
    // edits to display names, biographies, and relationships. Existing rows are never updated.
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var municipalities = new[]
        {
            new Municipality
            {
                SeedKey = "demo:pitt-meadows", Name = "Pitt Meadows", Province = "BC",
                ElectionDate = new DateTime(2026, 10, 17),
                Description = "Fictional classroom election dataset for Pitt Meadows, featuring community planning, parks, and local services."
            },
            new Municipality
            {
                SeedKey = "demo:maple-ridge", Name = "Maple Ridge", Province = "BC",
                ElectionDate = new DateTime(2026, 10, 17),
                Description = "Fictional classroom election dataset for Maple Ridge, featuring neighbourhood connections, recreation, and education."
            },
            new Municipality
            {
                SeedKey = "demo:burnaby", Name = "Burnaby", Province = "BC",
                ElectionDate = new DateTime(2026, 10, 17),
                Description = "Fictional classroom election dataset for Burnaby, featuring housing, sustainable transportation, and inclusive public spaces."
            }
        };

        foreach (var sample in municipalities)
        {
            var municipality = await db.Municipalities.SingleOrDefaultAsync(
                m => m.SeedKey == sample.SeedKey, cancellationToken);
            if (municipality is null)
            {
                municipality = sample;
                db.Municipalities.Add(municipality);
                await db.SaveChangesAsync(cancellationToken);
            }

            var seats = sample.SeedKey == "demo:burnaby" ? new[] { 1, 8, 7 } : new[] { 1, 6, sample.SeedKey == "demo:pitt-meadows" ? 2 : 5 };
            var names = new[] { "Mayor", "Councillor", "School Trustee" };
            var positionKeys = new[] { "mayor", "councillor", "school-trustee" };

            for (var index = 0; index < names.Length; index++)
            {
                var key = $"{sample.SeedKey}:{positionKeys[index]}";
                if (!await db.Positions.AnyAsync(p => p.SeedKey == key, cancellationToken))
                {
                    db.Positions.Add(new Position
                    {
                        SeedKey = key, Name = names[index], SeatsAvailable = seats[index],
                        MunicipalityId = municipality.MunicipalityId
                    });
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        var candidates = new[]
        {
            ("pitt-meadows", "mayor", "avery-brookfield", "Avery Brookfield", "A fictional community volunteer proposing accessible town halls and improvements to neighbourhood walking routes."),
            ("pitt-meadows", "councillor", "morgan-cedar", "Morgan Cedar", "A fictional recreation coordinator advocating for affordable youth activities and well-maintained community parks."),
            ("pitt-meadows", "councillor", "rowan-fairhaven", "Rowan Fairhaven", "A fictional small-business mentor interested in local shopping areas and transparent municipal budgeting."),
            ("pitt-meadows", "school-trustee", "jamie-willowby", "Jamie Willowby", "A fictional parent volunteer focusing on student wellbeing, accessible learning resources, and safe school journeys."),
            ("maple-ridge", "mayor", "casey-alderwood", "Casey Alderwood", "A fictional neighbourhood organizer proposing better public engagement and long-term recreation planning."),
            ("maple-ridge", "councillor", "taylor-meadowcroft", "Taylor Meadowcroft", "A fictional environmental educator supporting urban tree planting and connected walking trails."),
            ("maple-ridge", "councillor", "quinn-mapleford", "Quinn Mapleford", "A fictional accessibility advocate interested in inclusive civic facilities and reliable local transportation."),
            ("maple-ridge", "school-trustee", "alex-riverstone", "Alex Riverstone", "A fictional library volunteer proposing stronger reading programs and family participation in school planning."),
            ("burnaby", "mayor", "reese-hazelbrook", "Reese Hazelbrook", "A fictional community planner advocating for diverse housing choices and welcoming public gathering spaces."),
            ("burnaby", "councillor", "drew-fernvale", "Drew Fernvale", "A fictional cycling instructor interested in safer intersections and convenient connections to transit."),
            ("burnaby", "councillor", "sage-brambleton", "Sage Brambleton", "A fictional arts volunteer supporting neighbourhood cultural events and accessible recreation programs."),
            ("burnaby", "school-trustee", "parker-elmgrove", "Parker Elmgrove", "A fictional science-club mentor focusing on inclusive classrooms and practical learning opportunities.")
        };

        foreach (var (municipalityKey, positionKey, candidateKey, name, profile) in candidates)
        {
            var key = $"demo:{candidateKey}";
            if (await db.Candidates.AnyAsync(c => c.SeedKey == key, cancellationToken))
            {
                continue;
            }

            var fullPositionKey = $"demo:{municipalityKey}:{positionKey}";
            var position = await db.Positions.SingleAsync(p => p.SeedKey == fullPositionKey, cancellationToken);
            db.Candidates.Add(new Candidate
            {
                SeedKey = key, Name = name, Profile = profile,
                PositionId = position.PositionId, MunicipalityId = position.MunicipalityId,
                DateCreated = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                Website = $"https://example.com/candidates/{candidateKey}",
                Email = $"{candidateKey}@example.com"
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
