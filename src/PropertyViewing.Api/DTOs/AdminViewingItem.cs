namespace PropertyViewing.Api.DTOs;

public sealed record AdminViewingItem(
    int Id,
    int PropertyId,
    string PropertyAddress,
    string PropertyTimeZoneId,
    int UserId,
    string UserName,
    string? UserEmail,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    DateTime CreatedAt);
