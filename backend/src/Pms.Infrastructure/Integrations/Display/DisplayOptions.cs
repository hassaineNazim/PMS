namespace Pms.Infrastructure.Integrations.Display;

public class DisplayOptions
{
    public const string SectionName = "Display";

    /// <summary>"none" (default), "lg" (Pro:Centric / SuperSign).</summary>
    public string Provider { get; set; } = "none";

    /// <summary>Base URL of the LG Pro:Centric / SuperSign middleware REST API.</summary>
    public string BaseUrl { get; set; } = "http://localhost:8080/procentric/api";

    /// <summary>Optional API key/token for the middleware.</summary>
    public string? ApiKey { get; set; }
}
