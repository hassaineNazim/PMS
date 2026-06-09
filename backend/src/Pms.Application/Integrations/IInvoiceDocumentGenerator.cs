using Pms.Domain.Entities;

namespace Pms.Application.Integrations;

/// <summary>Renders an invoice to a deterministic PDF document (QuestPDF impl).</summary>
public interface IInvoiceDocumentGenerator
{
    byte[] Generate(Invoice invoice, Tenant tenant, Guest guest, Room room, IReadOnlyList<Charge> charges);
}
