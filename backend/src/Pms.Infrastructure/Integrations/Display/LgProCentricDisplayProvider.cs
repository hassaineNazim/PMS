using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pms.Application.Integrations;

namespace Pms.Infrastructure.Integrations.Display;

/// <summary>
/// LG Pro:Centric / SuperSign provider. Pushes the guest welcome to the in-room
/// TV via the middleware REST API (JSON), falling back to the HTNG XML profile if
/// the JSON endpoint is unavailable — exactly the dual-path strategy hotels expect.
/// Swapping in another screen brand means writing a new IDisplayProvider; the
/// booking/check-in core never changes.
/// </summary>
public class LgProCentricDisplayProvider(
    HttpClient http,
    IOptions<DisplayOptions> options,
    ILogger<LgProCentricDisplayProvider> logger) : IDisplayProvider
{
    private readonly DisplayOptions _options = options.Value;

    public string Name => "lg-procentric";

    public async Task<DisplayResult> ShowWelcomeAsync(GuestDisplayInfo guest, CancellationToken ct = default)
    {
        var payload = new
        {
            guest_name = guest.GuestName,
            room_number = guest.RoomNumber,
            check_out_date = guest.CheckOutDate.ToString("yyyy-MM-dd"),
            language = guest.Language,
            hotel_name = guest.HotelName
        };

        try
        {
            await PostJsonAsync("checkin", payload, ct);
            return DisplayResult.Ok(Name, "json");
        }
        catch (Exception jsonEx)
        {
            logger.LogWarning(jsonEx, "LG JSON push failed, trying HTNG XML fallback");
            try
            {
                await PostXmlAsync("checkin", BuildHtngXml("HTNG_CheckInNotification", guest), ct);
                return DisplayResult.Ok(Name, "xml");
            }
            catch (Exception xmlEx)
            {
                return DisplayResult.Fail(Name, $"JSON: {jsonEx.Message} | XML: {xmlEx.Message}");
            }
        }
    }

    public async Task<DisplayResult> ClearAsync(string roomNumber, CancellationToken ct = default)
    {
        try
        {
            await PostJsonAsync("checkout", new { room_number = roomNumber }, ct);
            return DisplayResult.Ok(Name, "json");
        }
        catch (Exception ex)
        {
            return DisplayResult.Fail(Name, ex.Message);
        }
    }

    private async Task PostJsonAsync(string path, object payload, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, BuildUrl(path))
        {
            Content = JsonContent.Create(payload)
        };
        ApplyAuth(req);
        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    private async Task PostXmlAsync(string path, string xml, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, BuildUrl(path))
        {
            Content = new StringContent(xml, Encoding.UTF8, "application/xml")
        };
        ApplyAuth(req);
        var resp = await http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    private void ApplyAuth(HttpRequestMessage req)
    {
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_options.ApiKey}");
    }

    private string BuildUrl(string path) => $"{_options.BaseUrl.TrimEnd('/')}/{path}";

    private static string BuildHtngXml(string root, GuestDisplayInfo g) =>
        $"""
         <?xml version="1.0" encoding="UTF-8"?>
         <{root} xmlns="http://htng.org/2014B">
           <GuestName>{System.Security.SecurityElement.Escape(g.GuestName)}</GuestName>
           <RoomNumber>{System.Security.SecurityElement.Escape(g.RoomNumber)}</RoomNumber>
           <CheckOutDate>{g.CheckOutDate:yyyy-MM-dd}</CheckOutDate>
           <Language>{System.Security.SecurityElement.Escape(g.Language)}</Language>
         </{root}>
         """;
}
