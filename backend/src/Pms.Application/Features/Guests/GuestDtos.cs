namespace Pms.Application.Features.Guests;

public record GuestDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string? Email,
    string? Phone,
    string Language,
    string? Nationality,
    string? DocumentType,
    string? DocumentNumber);

public record CreateGuestRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Language,
    string? Nationality,
    string? DocumentType,
    string? DocumentNumber);

public record UpdateGuestRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string Language,
    string? Nationality,
    string? DocumentType,
    string? DocumentNumber);
