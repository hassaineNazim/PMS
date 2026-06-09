using Microsoft.EntityFrameworkCore;
using Pms.Application.Common;
using Pms.Domain.Entities;
using Pms.Domain.Exceptions;

namespace Pms.Application.Features.Guests;

public class GuestService(IApplicationDbContext db, ICurrentTenant tenant) : IGuestService
{
    public async Task<PagedResult<GuestDto>> SearchAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.Guests.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            // Provider-neutral case-insensitive search (works on PostgreSQL and the
            // in-memory test provider alike).
            var s = search.Trim().ToLower();
            query = query.Where(g =>
                g.FirstName.ToLower().Contains(s) ||
                g.LastName.ToLower().Contains(s) ||
                (g.Email != null && g.Email.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(g => g.LastName).ThenBy(g => g.FirstName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(g => Map(g)).ToListAsync(ct);

        return new PagedResult<GuestDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<IReadOnlyList<GuestDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Guests.OrderBy(g => g.LastName).Select(g => Map(g)).ToListAsync(ct);

    public async Task<GuestDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var guest = await db.Guests.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NotFoundException(nameof(Guest), id);
        return Map(guest);
    }

    public async Task<GuestDto> CreateAsync(CreateGuestRequest request, CancellationToken ct = default)
    {
        var guest = new Guest
        {
            TenantId = tenant.TenantId,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email?.Trim().ToLower(),
            Phone = request.Phone?.Trim(),
            Language = request.Language,
            Nationality = request.Nationality,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber
        };
        db.Guests.Add(guest);
        await db.SaveChangesAsync(ct);
        return Map(guest);
    }

    public async Task<GuestDto> UpdateAsync(Guid id, UpdateGuestRequest request, CancellationToken ct = default)
    {
        var guest = await db.Guests.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NotFoundException(nameof(Guest), id);

        guest.FirstName = request.FirstName.Trim();
        guest.LastName = request.LastName.Trim();
        guest.Email = request.Email?.Trim().ToLower();
        guest.Phone = request.Phone?.Trim();
        guest.Language = request.Language;
        guest.Nationality = request.Nationality;
        guest.DocumentType = request.DocumentType;
        guest.DocumentNumber = request.DocumentNumber;
        guest.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(guest);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var guest = await db.Guests.FirstOrDefaultAsync(g => g.Id == id, ct)
            ?? throw new NotFoundException(nameof(Guest), id);

        if (await db.Reservations.AnyAsync(r => r.GuestId == id, ct))
            throw new ConflictException("Cannot delete a guest who has reservations.");

        db.Guests.Remove(guest);
        await db.SaveChangesAsync(ct);
    }

    private static GuestDto Map(Guest g) =>
        new(g.Id, g.FirstName, g.LastName, g.FullName, g.Email, g.Phone, g.Language,
            g.Nationality, g.DocumentType, g.DocumentNumber);
}
