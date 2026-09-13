using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropertyViewing.Infrastructure.Persistence;

namespace PropertyViewing.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class LookupsController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("properties")]
    public Task<List<PropertyListItem>> GetProperties(CancellationToken cancellationToken) =>
        dbContext.Properties
            .AsNoTracking()
            .OrderBy(property => property.Id)
            .Select(property => new PropertyListItem(property.Id, property.Address, property.TimeZoneId))
            .ToListAsync(cancellationToken);

    [HttpGet("users")]
    public Task<List<UserListItem>> GetUsers(CancellationToken cancellationToken) =>
        dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.Id)
            .Select(user => new UserListItem(user.Id, user.Name, user.Email))
            .ToListAsync(cancellationToken);
}

// Bổ sung TimeZoneId vào DTO để match với Type Property bên Frontend
public sealed record PropertyListItem(int Id, string Address, string TimeZoneId);

public sealed record UserListItem(int Id, string Name, string? Email);