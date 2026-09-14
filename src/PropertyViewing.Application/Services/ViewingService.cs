using Microsoft.EntityFrameworkCore;
using Npgsql;
using PropertyViewing.Application.DTOs;
using PropertyViewing.Application.Exceptions;
using PropertyViewing.Application.Interfaces;
using PropertyViewing.Domain.Entities;

namespace PropertyViewing.Application.Services;

public sealed class ViewingService(
    IViewingRepository repository) : IViewingService
{
    private const int SlotMinutes = 30;
    private const int MaxSearchDays = 31;

    public async Task<BookingResult> BookAsync(
        BookViewingCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            ValidateIdentifiers(command.PropertyId, command.UserId);

        var timeZoneId = await repository.GetPropertyTimeZoneAsync(
            command.PropertyId, cancellationToken);

        if (timeZoneId is null)
        {
            throw new NotFoundException("Property was not found.");
        }

        if (!await repository.UserExistsAsync(command.UserId, cancellationToken))
        {
            throw new NotFoundException("User was not found.");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var propertyTimeZone))
        {
            throw new ValidationException($"Invalid timezone identifier '{timeZoneId}'.");
        }

        // 1. Treat command.StartTime as wall-clock time in the property's local timezone (Unspecified Kind)
        var localStartTime = DateTime.SpecifyKind(command.StartTime, DateTimeKind.Unspecified);

        // 2. Validate slot constraints in local time (business hours 09:00 - 20:00, 30-minute boundaries)
        ValidateSlot(localStartTime);

        // 3. Convert safely from Local Time to UTC handling Daylight Saving Time (DST)
        var startTimeUtc = ConvertToUtcSafe(localStartTime, propertyTimeZone);
        var endTimeUtc = startTimeUtc.AddMinutes(SlotMinutes);

        // 4. Check for existing slot conflicts using UTC timestamps
        if (await repository.HasConflictAsync(
                command.PropertyId, startTimeUtc, endTimeUtc, cancellationToken))
        {
            throw new BookingConflictException("The viewing slot is already booked.");
        }

        // 5. Persist the booking entity
        var viewing = await repository.CreateAsync(
            new Viewing
            {
                PropertyId = command.PropertyId,
                UserId = command.UserId,
                StartTime = startTimeUtc,
                EndTime = endTimeUtc,
                CreatedAt = DateTime.UtcNow
            },
            cancellationToken);

        return new BookingResult(
            viewing.Id,
            viewing.PropertyId,
            viewing.UserId,
            viewing.StartTime,
            viewing.EndTime);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" })
        {
            // Catch PostgreSQL Unique Constraint Violation (SQLState 23505) triggered by the unique index 
            // on (PropertyId, StartTime) to handle race conditions gracefully and translate to a 409 Conflict.
            throw new BookingConflictException("The viewing slot has just been booked by another user.");
        }
    }

    public async Task<IReadOnlyList<ViewingSlotDto>> GetAvailableAsync(
        int propertyId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        ValidateGetAvailableInputs(propertyId, from, to);

        var timeZoneId = await repository.GetPropertyTimeZoneAsync(
            propertyId, cancellationToken);

        if (timeZoneId is null)
        {
            throw new NotFoundException("Property was not found.");
        }

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var propertyTimeZone))
        {
            throw new ValidationException($"Invalid timezone identifier '{timeZoneId}'.");
        }

        // Expand query range to full local day boundaries (Start of 'from' day to Start of 'to + 1' day)
        // to safely prevent missing booked slots due to timezone conversions
        var rangeStartUtc = ConvertToUtcSafe(from.ToDateTime(TimeOnly.MinValue), propertyTimeZone);
        var rangeEndUtc = ConvertToUtcSafe(to.AddDays(1).ToDateTime(TimeOnly.MinValue), propertyTimeZone);

        var bookedViewings = await repository.GetByPropertyAndDateRangeAsync(
            propertyId,
            rangeStartUtc,
            rangeEndUtc,
            cancellationToken);

        // Store booked slots in a HashSet for O(1) fast lookup
        var bookedLookup = bookedViewings
            .Select(v => (v.StartTime, v.EndTime))
            .ToHashSet();

        var slots = new List<ViewingSlotDto>();

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var localStart = day.ToDateTime(new TimeOnly(9, 0));
            var localClose = day.ToDateTime(new TimeOnly(20, 0));

            while (localStart < localClose)
            {
                var localEnd = localStart.AddMinutes(SlotMinutes);

                // Skip invalid local times caused by Spring Forward DST transitions
                if (!propertyTimeZone.IsInvalidTime(localStart))
                {
                    var slotStartUtc = ConvertToUtcSafe(localStart, propertyTimeZone);
                    var slotEndUtc = ConvertToUtcSafe(localEnd, propertyTimeZone);

                    // O(1) overlap check against existing bookings
                    var isBooked = bookedLookup.Any(
                        b => b.StartTime < slotEndUtc && b.EndTime > slotStartUtc);

                    if (!isBooked)
                    {
                        slots.Add(new ViewingSlotDto(localStart, localEnd, slotStartUtc, slotEndUtc));
                    }
                }

                localStart = localEnd;
            }
        }

        return slots;
    }

    private static DateTime ConvertToUtcSafe(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        var unspecifiedTime = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        // Handle DST transitions where local wall-clock times do not exist (Spring Forward)
        if (timeZone.IsInvalidTime(unspecifiedTime))
        {
            // Find the active adjustment rule covering the target date
            var adjustment = timeZone.GetAdjustmentRules()
                .FirstOrDefault(r => r.DateStart <= unspecifiedTime && r.DateEnd >= unspecifiedTime);

            // Shift time by the active DaylightDelta, falling back to 1 hour if unspecified
            var delta = adjustment?.DaylightDelta ?? TimeSpan.FromHours(1);
            unspecifiedTime = unspecifiedTime.Add(delta);
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecifiedTime, timeZone);
    }

    private static void ValidateGetAvailableInputs(int propertyId, DateOnly from, DateOnly to)
    {
        if (propertyId <= 0)
            throw new ValidationException("PropertyId must be greater than zero.");

        if (to < from)
            throw new ValidationException("The from date must not be after the to date.");

        if (to.DayNumber - from.DayNumber + 1 > MaxSearchDays)
            throw new ValidationException($"Date range cannot exceed {MaxSearchDays} days.");
    }

    private static void ValidateIdentifiers(int propertyId, int userId)
    {
        if (propertyId <= 0)
            throw new ValidationException("PropertyId must be greater than zero.");

        if (userId <= 0)
            throw new ValidationException("UserId must be greater than zero.");
    }

    private static void ValidateSlot(DateTime localStartTime)
    {
        if (localStartTime.Second != 0 ||
            localStartTime.Millisecond != 0 ||
            localStartTime.Minute is not (0 or 30) ||
            localStartTime.Hour < 9 ||
            localStartTime.Hour >= 20)
        {
            throw new ValidationException(
                "StartTime must be a 30-minute boundary between 09:00 and 20:00 in property local time.");
        }
    }
}