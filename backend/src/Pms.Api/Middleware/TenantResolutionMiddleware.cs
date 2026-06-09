using Pms.Infrastructure.MultiTenancy;
using Pms.Infrastructure.Security;

namespace Pms.Api.Middleware;

/// <summary>
/// Resolves the tenant for the request and pushes it into the scoped CurrentTenant
/// so the DbContext query filter is active for the rest of the pipeline. Order of
/// precedence: authenticated JWT tenant claim, then the X-Tenant header (used by the
/// login screen before a token exists).
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, CurrentTenant currentTenant)
    {
        var claim = context.User.FindFirst(JwtTokenService.TenantClaim)?.Value;
        if (Guid.TryParse(claim, out var tenantId))
        {
            currentTenant.Set(tenantId);
        }
        else if (context.Request.Headers.TryGetValue("X-Tenant", out var header)
                 && Guid.TryParse(header.ToString(), out var headerTenant))
        {
            currentTenant.Set(headerTenant);
        }

        await next(context);
    }
}
