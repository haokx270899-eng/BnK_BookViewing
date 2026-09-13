using Microsoft.Extensions.Logging;
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

        // 1. Safe parsing TimeZone
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
        {
            throw new ValidationException($"Invalid timezone identifier '{timeZoneId}'.");
        }

        // 2. Chuẩn hóa StartTime về UTC
        var startTimeUtc = command.StartTime.Kind == DateTimeKind.Utc
            ? command.StartTime
            : DateTime.SpecifyKind(command.StartTime, DateTimeKind.Utc);

        // 3. Convert UTC -> Local để validate
        var startTimePropertyLocal = TimeZoneInfo.ConvertTimeFromUtc(startTimeUtc, timeZone);
        ValidateSlot(startTimePropertyLocal);

        var endTimeUtc = startTimeUtc.AddMinutes(SlotMinutes);

        if (await repository.HasConflictAsync(
                command.PropertyId, startTimeUtc, endTimeUtc, cancellationToken))
        {
            throw new BookingConflictException("The viewing slot is already booked.");
        }

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

        // Xác định range query DB (lấy dư biên để tránh hụt slot do lệch TimeZone)
        var localStartFirstDay = from.ToDateTime(new TimeOnly(9, 0));
        var localEndLastDay = to.ToDateTime(new TimeOnly(20, 0));

        var rangeStartUtc = ConvertToUtcSafe(localStartFirstDay, propertyTimeZone);
        var rangeEndUtc = ConvertToUtcSafe(localEndLastDay, propertyTimeZone);

        var booked = await repository.GetByPropertyAndDateRangeAsync(
            propertyId,
            rangeStartUtc,
            rangeEndUtc,
            cancellationToken);

        var slots = new List<ViewingSlotDto>();

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var localStart = day.ToDateTime(new TimeOnly(9, 0));
            var localClose = day.ToDateTime(new TimeOnly(20, 0));

            while (localStart < localClose)
            {
                var localEnd = localStart.AddMinutes(SlotMinutes);

                // Bỏ qua nếu thời gian local rơi vào khung giờ không tồn tại do đổi giờ DST
                if (!propertyTimeZone.IsInvalidTime(localStart))
                {
                    var slotStartUtc = ConvertToUtcSafe(localStart, propertyTimeZone);
                    var slotEndUtc = ConvertToUtcSafe(localEnd, propertyTimeZone);

                    var isBooked = booked.Any(
                        v => v.StartTime < slotEndUtc && v.EndTime > slotStartUtc);

                    if (!isBooked)
                    {
                        slots.Add(new ViewingSlotDto(slotStartUtc, slotEndUtc));
                    }
                }

                localStart = localEnd;
            }
        }

        return slots;
    }

    private static DateTime ConvertToUtcSafe(DateTime localDateTime, TimeZoneInfo timeZone)
    {
        // Xử lý trường hợp DST Invalid Time bằng cách dịch chuyển về thời gian hợp lệ
        if (timeZone.IsInvalidTime(localDateTime))
        {
            var adjustment = timeZone.GetAdjustmentRules()
                .FirstOrDefault(r => r.DateStart <= localDateTime && r.DateEnd >= localDateTime);

            var delta = adjustment?.DaylightDelta ?? TimeSpan.FromHours(1);
            localDateTime = localDateTime.Add(delta);
        }

        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
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