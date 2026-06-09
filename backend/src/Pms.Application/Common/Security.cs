using Pms.Domain.Entities;

namespace Pms.Application.Common;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IJwtTokenService
{
    /// <summary>Issues a signed JWT carrying the user id, tenant id, email and role.</summary>
    (string Token, DateTimeOffset ExpiresAt) CreateToken(User user);
}
