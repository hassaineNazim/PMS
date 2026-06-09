using Pms.Domain.Enums;

namespace Pms.Application.Features.Invoices;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceDto>> GetAllAsync(CancellationToken ct = default);
    Task<InvoiceDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<InvoiceDto> SetStatusAsync(Guid id, InvoiceStatus status, CancellationToken ct = default);
    Task<InvoicePdf> GeneratePdfAsync(Guid id, CancellationToken ct = default);
}
