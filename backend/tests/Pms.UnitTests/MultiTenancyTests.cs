using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Pms.Domain.Entities;
using Xunit;

namespace Pms.UnitTests;

public class MultiTenancyTests
{
    [Fact]
    public async Task Query_filter_isolates_rooms_by_tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var dbName = $"shared-{Guid.NewGuid()}";

        // Two contexts over the SAME in-memory store but different current tenants.
        var optionsA = new DbContextOptionsBuilder<Pms.Infrastructure.Persistence.AppDbContext>()
            .UseInMemoryDatabase(dbName).Options;

        using (var ctx = new Pms.Infrastructure.Persistence.AppDbContext(optionsA, new TestTenant(tenantA)))
        {
            ctx.Rooms.Add(new Room { TenantId = tenantA, Number = "A1", PricePerNight = 50 });
            ctx.Rooms.Add(new Room { TenantId = tenantB, Number = "B1", PricePerNight = 60 });
            await ctx.SaveChangesAsync();
        }

        using (var ctxA = new Pms.Infrastructure.Persistence.AppDbContext(optionsA, new TestTenant(tenantA)))
        {
            var rooms = await ctxA.Rooms.ToListAsync();
            rooms.Should().HaveCount(1);
            rooms[0].Number.Should().Be("A1");
        }

        using (var ctxB = new Pms.Infrastructure.Persistence.AppDbContext(optionsA, new TestTenant(tenantB)))
        {
            var rooms = await ctxB.Rooms.ToListAsync();
            rooms.Should().HaveCount(1);
            rooms[0].Number.Should().Be("B1");
        }
    }
}
