using Pms.Domain.Entities;

namespace Pms.Application.Integrations;

/// <summary>
/// Generates the "fiche de police" / foreigner declaration form required of
/// Algerian hotels, from the guest + reservation data.
/// </summary>
public interface IPoliceFormGenerator
{
    byte[] Generate(Reservation reservation, Guest guest, Room room, Tenant tenant);
}
