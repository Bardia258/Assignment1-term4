using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MunicipalElections.Web.Data;
using MunicipalElections.Web.Models;
using MunicipalElections.Web.Services;

namespace MunicipalElections.Web.Controllers.Api;

public sealed record SaveCandidateRequest([Range(1, int.MaxValue)] int CandidateId);
public sealed record SavedCandidateResponse(int Id, int CandidateId, DateTime DateSaved, CandidateResponse Candidate);

[ApiController, Route("api/savedcandidates")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SavedCandidatesController(ApplicationDbContext db, IImageStorage images) : ControllerBase
{
    private string UserId => User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value;
    private IQueryable<SavedCandidate> Owned => db.SavedCandidates.AsNoTracking().Where(s => s.UserId == UserId);
    private SavedCandidateResponse ResponseFor(SavedCandidate s) => new(s.SavedCandidateId, s.CandidateId,
        s.DateSaved, new DirectoryResponses(images).Candidate(s.Candidate));

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken) => Ok((await Owned
        .Include(s => s.Candidate).ThenInclude(c => c.Municipality)
        .Include(s => s.Candidate).ThenInclude(c => c.Position)
        .OrderBy(s => s.DateSaved).ThenBy(s => s.SavedCandidateId).ToListAsync(cancellationToken)).Select(ResponseFor));

    [HttpPost]
    public async Task<IActionResult> Save(SaveCandidateRequest request, CancellationToken cancellationToken)
    {
        var candidate = await db.Candidates.Include(c => c.Municipality).Include(c => c.Position)
            .SingleOrDefaultAsync(c => c.CandidateId == request.CandidateId, cancellationToken);
        if (candidate is null) return NotFound();
        if (await Owned.AnyAsync(s => s.CandidateId == request.CandidateId, cancellationToken))
            return Conflict(new { message = "Candidate already saved." });
        var saved = new SavedCandidate { Candidate = candidate, UserId = UserId };
        db.SavedCandidates.Add(saved);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException error) when (error.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            // The unique index also handles simultaneous requests for the same user/candidate.
            return Conflict(new { message = "Candidate already saved." });
        }
        return StatusCode(StatusCodes.Status201Created, ResponseFor(saved));
    }

    // id is the SAVED RECORD id returned by GET/POST, not the candidate id.
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await db.SavedCandidates.Where(s => s.SavedCandidateId == id && s.UserId == UserId)
            .ExecuteDeleteAsync(cancellationToken);
        return deleted == 0 ? NotFound() : NoContent();
    }
}
