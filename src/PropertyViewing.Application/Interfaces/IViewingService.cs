using PropertyViewing.Application.DTOs;

namespace PropertyViewing.Application.Interfaces;

public interface IViewingService
{
    Task<BookingResult> BookAsync(BookViewingCommand command, CancellationToken cancellationToken);
    Task<IReadOnlyList<ViewingSlotDto>> GetAvailableAsync(int propertyId, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
