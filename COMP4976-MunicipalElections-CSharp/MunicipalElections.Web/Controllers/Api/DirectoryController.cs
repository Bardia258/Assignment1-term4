using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Models;
using MunicipalElections.Web.Services;

namespace MunicipalElections.Web.Controllers.Api;

public sealed record MunicipalityResponse(int Id, string Name, string Province, DateTime ElectionDate,
    string Description, string? LogoUrl);
public sealed record PositionResponse(int Id, string Name, int SeatsAvailable, int MunicipalityId, string MunicipalityName);
public sealed record CandidateResponse(int Id, string Name, string Profile, string? PhotoUrl,
    string? Website, string? Email, DateTime DateCreated, int MunicipalityId, string MunicipalityName,
    int PositionId, string PositionTitle, MunicipalityResponse Municipality, PositionResponse Position);

public sealed class DirectoryResponses(IImageStorage images)
{
    public MunicipalityResponse Municipality(Municipality m) => new(m.MunicipalityId, m.Name, m.Province,
        m.ElectionDate, m.Description, images.Url(m.LogoFileName, "logos"));
    public PositionResponse Position(Position p) => new(p.PositionId, p.Name, p.SeatsAvailable, p.MunicipalityId, p.Municipality.Name);
    public CandidateResponse Candidate(Candidate c) => new(c.CandidateId, c.Name, c.Profile,
        images.Url(c.PhotoFileName, "candidates"), c.Website, c.Email, c.DateCreated, c.MunicipalityId,
        c.Municipality.Name, c.PositionId, c.Position.Name, Municipality(c.Municipality),
        new PositionResponse(c.PositionId, c.Position.Name, c.Position.SeatsAvailable, c.MunicipalityId, c.Municipality.Name));
}

[ApiController, AllowAnonymous, Route("api/municipalities")]
public sealed class MunicipalitiesApiController(ApplicationDbContext db, IImageStorage images) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok((await db.Municipalities
        .AsNoTracking().OrderBy(m => m.Name).ToListAsync(cancellationToken)).Select(new DirectoryResponses(images).Municipality));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var municipality = await db.Municipalities.AsNoTracking().SingleOrDefaultAsync(m => m.MunicipalityId == id, cancellationToken);
        return municipality is null ? NotFound() : Ok(new DirectoryResponses(images).Municipality(municipality));
    }
}

[ApiController, AllowAnonymous, Route("api/positions")]
public sealed class PositionsController(ApplicationDbContext db, IImageStorage images) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int? municipalityId, CancellationToken cancellationToken) => Ok((await db.Positions
        .AsNoTracking().Include(p => p.Municipality).Where(p => municipalityId == null || p.MunicipalityId == municipalityId)
        .OrderBy(p => p.Municipality.Name).ThenBy(p => p.Name).ToListAsync(cancellationToken))
        .Select(new DirectoryResponses(images).Position));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var position = await db.Positions.AsNoTracking().Include(p => p.Municipality)
            .SingleOrDefaultAsync(p => p.PositionId == id, cancellationToken);
        return position is null ? NotFound() : Ok(new DirectoryResponses(images).Position(position));
    }
}

[ApiController, AllowAnonymous, Route("api/candidates")]
public sealed class CandidatesApiController(ApplicationDbContext db, IImageStorage images) : ControllerBase
{
    [HttpGet, HttpGet("search")]
    public async Task<IActionResult> List(string? name, int? municipalityId, int? positionId, CancellationToken cancellationToken)
    {
        var query = db.Candidates.AsNoTracking().Include(c => c.Municipality).Include(c => c.Position).AsQueryable();
        if (!string.IsNullOrWhiteSpace(name))
        {
            var term = name.Trim().ToUpperInvariant();
            query = query.Where(c => c.Name.ToUpper().Contains(term));
        }
        if (municipalityId.HasValue) query = query.Where(c => c.MunicipalityId == municipalityId);
        if (positionId.HasValue) query = query.Where(c => c.PositionId == positionId);
        return Ok((await query.OrderBy(c => c.Name).ThenBy(c => c.CandidateId).ToListAsync(cancellationToken))
            .Select(new DirectoryResponses(images).Candidate));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var candidate = await db.Candidates.AsNoTracking().Include(c => c.Municipality).Include(c => c.Position)
            .SingleOrDefaultAsync(c => c.CandidateId == id, cancellationToken);
        return candidate is null ? NotFound() : Ok(new DirectoryResponses(images).Candidate(candidate));
    }
}
