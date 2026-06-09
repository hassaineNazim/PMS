using Pms.Application.Common;

namespace Pms.Infrastructure.Security;

/// <summary>BCrypt password hashing (work factor 12).</summary>
public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }
}
