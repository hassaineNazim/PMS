namespace Pms.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "pms-api";
    public string Audience { get; set; } = "pms-clients";

    /// <summary>HMAC signing key. MUST be overridden in production via configuration.</summary>
    public string Secret { get; set; } = "CHANGE_ME_super_secret_key_at_least_32_chars_long!!";

    public int ExpiryMinutes { get; set; } = 480; // 8h shift
}
