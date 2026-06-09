namespace Pms.Application.Features.CheckIn;

public record CheckInRequest(string? AccompanyingGuests);

public record CheckInResult(
    Guid ReservationId,
    string GuestName,
    string RoomNumber,
    DateOnly CheckOut,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal InvoiceTotal,
    bool DisplayNotified,
    string? DisplayProvider,
    string? DisplayError);

public record CheckOutResult(
    Guid ReservationId,
    string GuestName,
    string RoomNumber,
    bool DisplayCleared);
