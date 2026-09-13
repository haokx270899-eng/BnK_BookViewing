using PropertyViewing.Domain.Entities;

namespace PropertyViewing.Application.Interfaces;

public interface IViewingRepository
{
    Task<bool> PropertyExistsAsync(int propertyId, CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(int userId, CancellationToken cancellationToken);
    Task<string?> GetPropertyTimeZoneAsync(int propertyId, CancellationToken cancellationToken);
    Task<bool> HasConflictAsync(int propertyId, DateTime startTime, DateTime endTime, CancellationToken cancellationToken);
    Task<IReadOnlyList<Viewing>> GetByPropertyAndDateRangeAsync(int propertyId, DateTime from, DateTime toExclusive, CancellationToken cancellationToken);
    Task<Viewing> CreateAsync(Viewing viewing, CancellationToken cancellationToken);
}
