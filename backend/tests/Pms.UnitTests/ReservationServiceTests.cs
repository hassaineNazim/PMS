using FluentAssertions;
using Pms.Application.Features.Pricing;
using Pms.Application.Features.Reservations;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Pms.Domain.Exceptions;
using Xunit;

namespace Pms.UnitTests;

public class ReservationServiceTests
{
    private readonly Guid _tenant = Guid.NewGuid();

    private async Task<(ReservationService svc, Guid guestId, Guid roomId)> SeedAsync()
    {
        var db = InMemoryDb.Create(_tenant);
        db.Tenants.Add(new Tenant { Id = _tenant, Name = "T", Slug = "t", Currency = "DZD", DefaultTaxRate = 9m });
        var guest = new Guest { TenantId = _tenant, FirstName = "Test", LastName = "Guest" };
        var room = new Room { TenantId = _tenant, Number = "101", PricePerNight = 100m, Capacity = 2 };
        db.Guests.Add(guest);
        db.Rooms.Add(room);
        await db.SaveChangesAsync();
        return (new ReservationService(db, new TestTenant(_tenant), new PricingService(db)), guest.Id, room.Id);
    }

    private static CreateReservationRequest Req(Guid g, Guid r, string ci, string co,
        MealPlan plan = MealPlan.RoomOnly, int adults = 1) =>
        new(g, r, DateOnly.Parse(ci), DateOnly.Parse(co), adults, 0, plan, null, null);

    [Fact]
    public async Task Creating_a_reservation_computes_total_from_nights()
    {
        var (svc, g, r) = await SeedAsync();
        var dto = await svc.CreateAsync(Req(g, r, "2026-06-10", "2026-06-13", adults: 2));

        dto.Nights.Should().Be(3);
        dto.RoomTotal.Should().Be(300m);
        dto.Status.Should().Be(ReservationStatus.Confirmed);
    }

    [Fact]
    public async Task Meal_plan_adds_supplement_per_person_per_night()
    {
        var db = InMemoryDb.Create(_tenant);
        db.Tenants.Add(new Tenant { Id = _tenant, Name = "T", Slug = "t", Currency = "DZD", HalfBoardSupplement = 2000m });
        var guest = new Guest { TenantId = _tenant, FirstName = "A", LastName = "B" };
        var room = new Room { TenantId = _tenant, Number = "101", PricePerNight = 100m, Capacity = 4 };
        db.Guests.Add(guest); db.Rooms.Add(room);
        await db.SaveChangesAsync();
        var svc = new ReservationService(db, new TestTenant(_tenant), new PricingService(db));

        // 3 nights × 2 persons × 2000 = 12000 meal plan
        var dto = await svc.CreateAsync(Req(guest.Id, room.Id, "2026-06-10", "2026-06-13", MealPlan.HalfBoard, adults: 2));

        dto.MealPlanTotal.Should().Be(12000m);
        dto.TotalAmount.Should().Be(300m + 12000m);
    }

    [Fact]
    public async Task Overlapping_reservation_for_same_room_is_rejected()
    {
        var (svc, g, r) = await SeedAsync();
        await svc.CreateAsync(Req(g, r, "2026-06-10", "2026-06-15"));
        var act = async () => await svc.CreateAsync(Req(g, r, "2026-06-12", "2026-06-18"));
        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Adjacent_reservations_are_allowed()
    {
        var (svc, g, r) = await SeedAsync();
        await svc.CreateAsync(Req(g, r, "2026-06-10", "2026-06-12"));
        var second = await svc.CreateAsync(Req(g, r, "2026-06-12", "2026-06-14"));
        second.Should().NotBeNull();
    }

    [Fact]
    public async Task Availability_excludes_booked_room()
    {
        var (svc, g, r) = await SeedAsync();
        await svc.CreateAsync(Req(g, r, "2026-06-10", "2026-06-15"));
        var available = await svc.GetAvailableRoomsAsync(
            new AvailabilityRequest(new DateOnly(2026, 6, 11), new DateOnly(2026, 6, 13), null));
        available.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancelled_reservation_frees_the_dates()
    {
        var (svc, g, r) = await SeedAsync();
        var first = await svc.CreateAsync(Req(g, r, "2026-06-10", "2026-06-15"));
        await svc.CancelAsync(first.Id);
        var second = await svc.CreateAsync(Req(g, r, "2026-06-11", "2026-06-13"));
        second.Should().NotBeNull();
    }
}
