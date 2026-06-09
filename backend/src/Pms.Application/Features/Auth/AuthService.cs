using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Auth;

public class AuthService(
    IApplicationDbContext db,
    IPasswordHasher hasher,
    IJwtTokenService jwt,
    ICurrentUser currentUser) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        // Users are tenant-scoped, but at login time there is no tenant context yet,
        // so we resolve across tenants by email (+ optional slug disambiguation) and
        // ignore the global query filter for this single lookup.
        var query = db.Users.IgnoreQueryFilters()
            .Where(u => u.Email == request.Email.Trim().ToLower() && u.IsActive);

        if (!string.IsNullOrWhiteSpace(request.TenantSlug))
        {
            var slug = request.TenantSlug.Trim().ToLower();
            query = query.Where(u => db.Tenants.Any(t => t.Id == u.TenantId && t.Slug == slug));
        }

        var user = await query.FirstOrDefaultAsync(ct);
        if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
            throw new BusinessRuleException("Invalid email or password.");

        var tenant = await db.Tenants.IgnoreQueryFilters()
            .Include(t => t.License)
            .FirstOrDefaultAsync(t => t.Id == user.TenantId, ct);
        if (tenant is null || !tenant.IsActive)
            throw new LicenseException("This establishment account is inactive.");
        if (tenant.License is null || !tenant.License.IsValid(DateTimeOffset.UtcNow))
            throw new LicenseException("The license for this establishment is missing or expired.");

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        var (token, expiresAt) = jwt.CreateToken(user);
        return new AuthResponse(token, expiresAt, Map(user));
    }

    public async Task<UserDto> GetMeAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            throw new BusinessRuleException("Not authenticated.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new NotFoundException(nameof(Pms.Domain.Entities.User), currentUser.UserId);
        return Map(user);
    }

    private static UserDto Map(Pms.Domain.Entities.User u) =>
        new(u.Id, u.TenantId, u.Email, u.FullName, u.Role);
}
