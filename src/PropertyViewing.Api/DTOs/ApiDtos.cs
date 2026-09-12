namespace PropertyViewing.Api.DTOs;
public sealed record BookViewingRequest(int PropertyId, int UserId, DateTime StartTime);
public sealed record ErrorResponse(int Status, string Message);
