using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Pms.IntegrationTests;

public class CheckInFlowTests(PmsApiFactory factory) : IClassFixture<PmsApiFactory>
{
    private record AuthResponse(string Token);
    private record IdResponse(Guid Id);

    private async Task<HttpClient> LoggedInClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@demo.com", password = "admin123", tenantSlug = "demo" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task Full_flow_books_checks_in_and_produces_invoice()
    {
        var client = await LoggedInClientAsync();

        var rooms = await client.GetFromJsonAsync<List<IdResponse>>("/api/rooms");
        var guests = await (await client.GetAsync("/api/guests")).Content.ReadFromJsonAsync<PagedGuests>();
        var roomId = rooms!.First().Id;
        var guestId = guests!.Items.First().Id;

        var createRes = await client.PostAsJsonAsync("/api/reservations", new
        {
            guestId, roomId,
            checkIn = "2026-07-01", checkOut = "2026-07-04",
            adults = 2, children = 0
        });
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var reservation = await createRes.Content.ReadFromJsonAsync<IdResponse>();

        var checkin = await client.PostAsJsonAsync($"/api/checkin/{reservation!.Id}", new { });
        checkin.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await checkin.Content.ReadFromJsonAsync<CheckInResult>();
        result!.InvoiceTotal.Should().BeGreaterThan(0);
        result.InvoiceNumber.Should().StartWith("INV-");
    }

    [Fact]
    public async Task Double_booking_is_rejected_with_conflict()
    {
        var client = await LoggedInClientAsync();
        var rooms = await client.GetFromJsonAsync<List<IdResponse>>("/api/rooms");
        var guests = await (await client.GetAsync("/api/guests")).Content.ReadFromJsonAsync<PagedGuests>();
        var roomId = rooms!.Last().Id;
        var guestId = guests!.Items.First().Id;

        var first = await client.PostAsJsonAsync("/api/reservations", new
        {
            guestId, roomId, checkIn = "2026-08-01", checkOut = "2026-08-10", adults = 1, children = 0
        });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var overlap = await client.PostAsJsonAsync("/api/reservations", new
        {
            guestId, roomId, checkIn = "2026-08-05", checkOut = "2026-08-12", adults = 1, children = 0
        });
        overlap.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private record PagedGuests(List<IdResponse> Items);
    private record CheckInResult(string InvoiceNumber, decimal InvoiceTotal);
}
