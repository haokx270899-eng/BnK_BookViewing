using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyViewing.Api.DTOs;
using PropertyViewing.Infrastructure.Persistence;

namespace PropertyViewing.Api.Controllers;

[ApiController]
[Route("api/admin/viewings")]
public sealed class AdminViewingsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminViewingItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AdminViewingItem>>> GetViewings(
        [FromQuery] int? propertyId,
        [FromQuery] DateOnly? date,
        [FromQuery] int? userId,
        CancellationToken cancellationToken)
    {
        if (propertyId is <= 0)
            return BadRequest("propertyId must be greater than zero.");

        if (userId is <= 0)
            return BadRequest("userId must be greater than zero.");

        var query = dbContext.Viewings.AsNoTracking().AsQueryable();

        if (propertyId.HasValue)
            query = query.Where(viewing => viewing.PropertyId == propertyId.Value);

        if (userId.HasValue)
            query = query.Where(viewing => viewing.UserId == userId.Value);

        // A property's local day can span UTC-14 through UTC+12. This keeps the
        // database filter index-friendly before checking each property's own zone.
        if (date.HasValue)
        {
            var utcRangeStart = DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddHours(-14);
            var utcRangeEnd = DateTime.SpecifyKind(date.Value.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc).AddHours(12);
            query = query.Where(viewing => viewing.StartTime >= utcRangeStart && viewing.StartTime < utcRangeEnd);
        }

        var viewings = await query
            .OrderByDescending(viewing => viewing.StartTime)
            .Select(viewing => new AdminViewingItem(
                viewing.Id,
                viewing.PropertyId,
                viewing.Property!.Address,
                viewing.Property.TimeZoneId,
                viewing.UserId,
                viewing.User!.Name,
                viewing.User.Email,
                viewing.StartTime,
                viewing.EndTime,
                viewing.CreatedAt))
            .ToListAsync(cancellationToken);

        if (date.HasValue)
            viewings = viewings.Where(viewing => IsOnPropertyLocalDate(viewing.StartTimeUtc, viewing.PropertyTimeZoneId, date.Value)).ToList();

        return Ok(viewings);
    }

    private static bool IsOnPropertyLocalDate(DateTime startTimeUtc, string timeZoneId, DateOnly date)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
            return DateOnly.FromDateTime(startTimeUtc) == date;

        var utc = DateTime.SpecifyKind(startTimeUtc, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone)) == date;
    }
}
