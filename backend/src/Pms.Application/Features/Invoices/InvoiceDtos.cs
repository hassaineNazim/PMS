using Pms.Domain.Enums;

namespace Pms.Application.Features.Invoices;

public record InvoiceDto(
    Guid Id,
    string Number,
    Guid ReservationId,
    Guid GuestId,
    string GuestName,
    Guid RoomId,
    string RoomNumber,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    decimal PricePerNight,
    decimal RoomSubtotal,
    decimal MealPlanSubtotal,
    decimal ExtrasSubtotal,
    decimal Subtotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal StampDuty,
    decimal Total,
    decimal AmountPaid,
    decimal BalanceDue,
    string Currency,
    InvoiceStatus Status,
    DateTimeOffset CreatedAt);

public record InvoicePdf(string FileName, byte[] Content);
