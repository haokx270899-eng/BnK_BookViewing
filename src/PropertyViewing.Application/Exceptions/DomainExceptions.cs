namespace PropertyViewing.Application.Exceptions;

public sealed class ValidationException(string message) : Exception(message);
public sealed class NotFoundException(string message) : Exception(message);
public sealed class BookingConflictException(string message) : Exception(message);
