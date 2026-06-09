using FluentAssertions;
using Pms.Application.Features.Billing;
using Pms.Domain.Entities;
using Pms.Domain.Enums;
using Xunit;

namespace Pms.UnitTests;

public class FiscalAndFolioTests
{
    [Theory]
    [InlineData(1000, 10)]    // 10 slices × 1 DA = 10
    [InlineData(1050, 11)]    // 11 started slices × 1 DA = 11
    [InlineData(100, 5)]      // 1 slice = 1 DA -> below minimum 5
    [InlineData(0, 0)]        // nothing due on 0
    public void Fiscal_stamp_follows_algerian_rounding(decimal cash, decimal expected)
    {
        var tenant = new Tenant { FiscalStampEnabled = true, FiscalStampRate = 1m, FiscalStampMinimum = 5m };
        tenant.ComputeFiscalStamp(cash).Should().Be(expected);
    }

    [Fact]
    public void Disabled_stamp_returns_zero()
    {
        var tenant = new Tenant { FiscalStampEnabled = false };
        tenant.ComputeFiscalStamp(100000).Should().Be(0);
    }

    [Fact]
    public void Folio_sums_room_meal_extras_tax_stamp_minus_payments()
    {
        var tenant = new Tenant { DefaultTaxRate = 10m };
        var res = new Reservation
        {
            CheckIn = new DateOnly(2026, 6, 1),
            CheckOut = new DateOnly(2026, 6, 3), // 2 nights
            Adults = 2,
            MealPlanSupplement = 1000m, // 2 nights × 2 pers × 1000 = 4000
            TotalAmount = 20000m        // room
        };
        var charges = new List<Charge> { new() { Total = 1000m } };
        var payments = new List<Payment>
        {
            new() { Amount = 5000m, Type = PaymentType.Deposit, StampDuty = 50m }
        };

        var f = FolioService.Compute(res, tenant, charges, payments);

        f.Room.Should().Be(20000m);
        f.Meal.Should().Be(4000m);
        f.Extras.Should().Be(1000m);
        f.Subtotal.Should().Be(25000m);
        f.Tax.Should().Be(2500m);       // 10%
        f.Stamp.Should().Be(50m);
        f.Total.Should().Be(27550m);    // 25000 + 2500 + 50
        f.Paid.Should().Be(5000m);
        f.Balance.Should().Be(22550m);
    }
}
