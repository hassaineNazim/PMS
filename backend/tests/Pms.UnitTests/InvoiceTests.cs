using FluentAssertions;
using Pms.Domain.Entities;
using Xunit;

namespace Pms.UnitTests;

public class InvoiceTests
{
    [Theory]
    [InlineData(3, 100, 9, 300, 27, 327)]
    [InlineData(1, 8900, 9, 8900, 801, 9701)]
    [InlineData(5, 199.99, 0, 999.95, 0, 999.95)]
    public void Recalculate_produces_correct_totals(
        int nights, decimal price, decimal taxRate, decimal subtotal, decimal tax, decimal total)
    {
        var invoice = new Invoice { Nights = nights, PricePerNight = price, TaxRate = taxRate };
        invoice.Recalculate();

        invoice.Subtotal.Should().Be(subtotal);
        invoice.TaxAmount.Should().Be(tax);
        invoice.Total.Should().Be(total);
    }
}
