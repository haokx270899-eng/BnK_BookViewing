using Microsoft.Extensions.Logging;
using PropertyViewing.Application.DTOs;
using PropertyViewing.Application.Exceptions;
using PropertyViewing.Application.Interfaces;
using PropertyViewing.Domain.Entities;

namespace PropertyViewing.Application.Services;

public sealed class ViewingService(IViewingRepository repository, ILogger<ViewingService> logger) : IViewingService
{
    private const int SlotMinutes = 30;
    private const int MaxSearchDays = 31;

    public async Task<BookingResult> BookAsync(BookViewingCommand command, CancellationToken cancellationToken)
    {
        ValidateIdentifiers(command.PropertyId, command.UserId);
        ValidateSlot(command.StartTime);
        logger.LogInformation("Booking attempt for property {PropertyId} by user {UserId} at {StartTime}", command.PropertyId, command.UserId, command.StartTime);
        if (!await repository.PropertyExistsAsync(command.PropertyId, cancellationToken)) throw new NotFoundException("Property was not found.");
        if (!await repository.UserExistsAsync(command.UserId, cancellationToken)) throw new NotFoundException("User was not found.");
        var end = command.StartTime.AddMinutes(SlotMinutes);
        if (await repository.HasConflictAsync(command.PropertyId, command.StartTime, end, cancellationToken)) throw new BookingConflictException("The viewing slot is already booked.");
        var viewing = await repository.CreateAsync(new Viewing { PropertyId = command.PropertyId, UserId = command.UserId, StartTime = command.StartTime, EndTime = end, CreatedAt = DateTime.UtcNow }, cancellationToken);
        logger.LogInformation("Viewing {ViewingId} booked for property {PropertyId}", viewing.Id, viewing.PropertyId);
        return new BookingResult(viewing.Id, viewing.PropertyId, viewing.UserId, viewing.StartTime, viewing.EndTime);
    }

    public async Task<IReadOnlyList<ViewingSlotDto>> GetAvailableAsync(int propertyId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        if (propertyId <= 0) throw new ValidationException("PropertyId must be greater than zero.");
        if (to < from) throw new ValidationException("The from date must not be after the to date.");
        if (to.DayNumber - from.DayNumber + 1 > MaxSearchDays) throw new ValidationException($"Date range cannot exceed {MaxSearchDays} days.");
        if (!await repository.PropertyExistsAsync(propertyId, cancellationToken)) throw new NotFoundException("Property was not found.");
        var rangeStart = from.ToDateTime(TimeOnly.MinValue);
        var rangeEnd = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var booked = await repository.GetByPropertyAndDateRangeAsync(propertyId, rangeStart, rangeEnd, cancellationToken);
        var slots = new List<ViewingSlotDto>();
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var start = day.ToDateTime(new TimeOnly(9, 0));
            var close = day.ToDateTime(new TimeOnly(20, 0));
            while (start < close)
            {
                var end = start.AddMinutes(SlotMinutes);
                if (!booked.Any(v => v.StartTime < end && v.EndTime > start)) slots.Add(new ViewingSlotDto(start, end));
                start = end;
            }
        }
        return slots;
    }

    private static void ValidateIdentifiers(int propertyId, int userId)
    {
        if (propertyId <= 0) throw new ValidationException("PropertyId must be greater than zero.");
        if (userId <= 0) throw new ValidationException("UserId must be greater than zero.");
    }

    private static void ValidateSlot(DateTime start)
    {
        if (start.Second != 0 || start.Millisecond != 0 || start.Minute is not (0 or 30) || start.Hour < 9 || start.Hour >= 20)
            throw new ValidationException("StartTime must be a 30-minute boundary between 09:00 and 20:00.");
    }
}
