namespace PropertyViewing.Application.DTOs;

public sealed record BookViewingCommand(int PropertyId, int UserId, DateTime StartTime);
public sealed record ViewingSlotDto(
    DateTime LocalStartTime,
    DateTime LocalEndTime,
    DateTime UtcStartTime,
    DateTime UtcEndTime); 
public sealed record BookingResult(int Id, int PropertyId, int UserId, DateTime StartTime, DateTime EndTime);
