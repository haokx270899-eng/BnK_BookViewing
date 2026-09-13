using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyViewing.Application.Exceptions;
using PropertyViewing.Application.Interfaces;
using PropertyViewing.Domain.Entities;
using PropertyViewing.Infrastructure.Persistence;

namespace PropertyViewing.Infrastructure.Repositories;

public sealed class ViewingRepository(AppDbContext dbContext) : IViewingRepository
{
    public Task<bool> PropertyExistsAsync(
        int propertyId,
        CancellationToken cancellationToken)
    {
        return dbContext.Properties
            .AnyAsync(
                x => x.Id == propertyId,
                cancellationToken);
    }

    public Task<bool> UserExistsAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return dbContext.Users
            .AnyAsync(
                x => x.Id == userId,
                cancellationToken);
    }

    public Task<bool> HasConflictAsync(
        int propertyId,
        DateTime startTime,
        DateTime endTime,
        CancellationToken cancellationToken)
    {
        return dbContext.Viewings
            .AnyAsync(
                x =>
                    x.PropertyId == propertyId &&
                    x.StartTime < endTime &&
                    x.EndTime > startTime,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Viewing>> GetByPropertyAndDateRangeAsync(
        int propertyId,
        DateTime from,
        DateTime toExclusive,
        CancellationToken cancellationToken)
    {
        return await dbContext.Viewings
            .AsNoTracking()
            .Where(
                x =>
                    x.PropertyId == propertyId &&
                    x.StartTime < toExclusive &&
                    x.EndTime > from)
            .ToListAsync(cancellationToken);
    }

    public async Task<Viewing> CreateAsync(
        Viewing viewing,
        CancellationToken cancellationToken)
    {
        dbContext.Viewings.Add(viewing);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            return viewing;
        }
        catch (
            DbUpdateException exception
        )
            when (
                exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation
                })
        {
            throw new BookingConflictException(
                "The viewing slot is already booked.");
        }
    }

    //Get TimeZone of Property
    public async Task<string?> GetPropertyTimeZoneAsync(
    int propertyId,
    CancellationToken cancellationToken)
    {
        return await dbContext.Properties
            .AsNoTracking()
            .Where(p => p.Id == propertyId)
            .Select(p => p.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}