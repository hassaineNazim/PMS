using Pms.Domain.Enums;

namespace Pms.Application.Features.Reservations;

public record ReservationDto(
    Guid Id,
    Guid GuestId,
    string GuestName,
    Guid RoomId,
    string RoomNumber,
    RoomType RoomType,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Nights,
    ReservationStatus Status,
    int Adults,
    int Children,
    MealPlan MealPlan,
    decimal MealPlanTotal,
    decimal RoomTotal,
    decimal TotalAmount,
    string? Notes,
    string? AccompanyingGuests);

public record CreateReservationRequest(
    Guid GuestId,
    Guid RoomId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Adults,
    int Children,
    MealPlan MealPlan,
    string? Notes,
    string? Source);

public record UpdateReservationRequest(
    Guid GuestId,
    Guid RoomId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    int Adults,
    int Children,
    MealPlan MealPlan,
    string? Notes);

/// <summary>Query for free rooms over a date range (used by the booking screen).</summary>
public record AvailabilityRequest(DateOnly CheckIn, DateOnly CheckOut, RoomType? Type);

public record AvailableRoomDto(
    Guid RoomId,
    string Number,
    RoomType Type,
    int Capacity,
    decimal PricePerNight,
    int Nights,
    decimal EstimatedTotal);
