using Pms.Application.Integrations;
using Pms.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Pms.Infrastructure.Integrations.Pdf;

/// <summary>Deterministic, DGI-compliant invoice PDF using QuestPDF.</summary>
public class QuestPdfInvoiceGenerator : IInvoiceDocumentGenerator
{
    public byte[] Generate(Invoice invoice, Tenant tenant, Guest guest, Room room, IReadOnlyList<Charge> charges)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(c => Header(c, tenant, invoice));
                page.Content().Element(c => Body(c, invoice, guest, room, charges));
                page.Footer().Element(c => Footer(c, tenant));
            });
        }).GeneratePdf();
    }

    private static void Header(IContainer container, Tenant tenant, Invoice invoice)
    {
        container.Column(outer =>
        {
            outer.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(tenant.Name).FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                    if (!string.IsNullOrWhiteSpace(tenant.LegalName)) col.Item().Text(tenant.LegalName);
                    if (!string.IsNullOrWhiteSpace(tenant.Address)) col.Item().Text(tenant.Address!);
                    col.Item().Text($"{tenant.City} {tenant.Country}".Trim());
                    if (!string.IsNullOrWhiteSpace(tenant.Phone)) col.Item().Text($"Tél : {tenant.Phone}");
                });
                row.ConstantItem(190).Column(col =>
                {
                    col.Item().AlignRight().Text("FACTURE").FontSize(20).Bold();
                    col.Item().AlignRight().Text(invoice.Number).FontSize(12).SemiBold();
                    col.Item().AlignRight().Text($"Date : {invoice.CreatedAt:yyyy-MM-dd}");
                    col.Item().AlignRight().Text($"Statut : {invoice.Status}");
                });
            });

            // DGI legal identifiers
            var ids = new List<string>();
            if (!string.IsNullOrWhiteSpace(tenant.TaxId)) ids.Add($"NIF : {tenant.TaxId}");
            if (!string.IsNullOrWhiteSpace(tenant.StatId)) ids.Add($"NIS : {tenant.StatId}");
            if (!string.IsNullOrWhiteSpace(tenant.TradeRegister)) ids.Add($"RC : {tenant.TradeRegister}");
            if (!string.IsNullOrWhiteSpace(tenant.TaxArticle)) ids.Add($"Art. : {tenant.TaxArticle}");
            if (ids.Count > 0)
                outer.Item().PaddingTop(6).Text(string.Join("   ·   ", ids)).FontSize(8).FontColor(Colors.Grey.Darken1);
        });
    }

    private static void Body(IContainer container, Invoice invoice, Guest guest, Room room, IReadOnlyList<Charge> charges)
    {
        container.PaddingVertical(18).Column(col =>
        {
            col.Spacing(14);

            col.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(c =>
            {
                c.Item().Text("Client").SemiBold().FontColor(Colors.Grey.Darken1);
                c.Item().Text(guest.FullName).FontSize(12);
                if (!string.IsNullOrWhiteSpace(guest.Email)) c.Item().Text(guest.Email!);
                if (!string.IsNullOrWhiteSpace(guest.Phone)) c.Item().Text(guest.Phone!);
                if (!string.IsNullOrWhiteSpace(guest.DocumentNumber))
                    c.Item().Text($"Pièce : {guest.DocumentType} {guest.DocumentNumber}");
            });

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(c => { c.RelativeColumn(5); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); });
                table.Header(h =>
                {
                    h.Cell().Element(HeaderStyle).Text("Désignation");
                    h.Cell().Element(HeaderStyle).AlignRight().Text("Qté/Nuits");
                    h.Cell().Element(HeaderStyle).AlignRight().Text("P.U.");
                    h.Cell().Element(HeaderStyle).AlignRight().Text("Montant");
                });

                void Line(string label, string qty, string pu, string amount)
                {
                    table.Cell().Element(CellStyle).Text(label);
                    table.Cell().Element(CellStyle).AlignRight().Text(qty);
                    table.Cell().Element(CellStyle).AlignRight().Text(pu);
                    table.Cell().Element(CellStyle).AlignRight().Text(amount);
                }

                Line($"Chambre {room.Number} — {room.Type}\n{invoice.CheckIn:yyyy-MM-dd} → {invoice.CheckOut:yyyy-MM-dd}",
                    invoice.Nights.ToString(), Money(invoice.PricePerNight, invoice.Currency), Money(invoice.RoomSubtotal, invoice.Currency));

                if (invoice.MealPlanSubtotal > 0)
                    Line("Formule de pension", "", "", Money(invoice.MealPlanSubtotal, invoice.Currency));

                foreach (var ch in charges)
                    Line($"{ch.Category} — {ch.Label}", ch.Quantity.ToString(),
                        Money(ch.UnitPrice, invoice.Currency), Money(ch.Total, invoice.Currency));
            });

            col.Item().AlignRight().Column(t =>
            {
                void Total(string label, string value)
                {
                    t.Item().Row(r =>
                    {
                        r.ConstantItem(150).Text(label);
                        r.ConstantItem(130).AlignRight().Text(value);
                    });
                }

                Total("Sous-total", Money(invoice.Subtotal, invoice.Currency));
                Total($"TVA ({invoice.TaxRate}%)", Money(invoice.TaxAmount, invoice.Currency));
                if (invoice.StampDuty > 0) Total("Droit de timbre", Money(invoice.StampDuty, invoice.Currency));
                t.Item().PaddingTop(4).Row(r =>
                {
                    r.ConstantItem(150).Text("TOTAL").Bold().FontSize(13);
                    r.ConstantItem(130).AlignRight().Text(Money(invoice.Total, invoice.Currency)).Bold().FontSize(13);
                });
                Total("Payé", Money(invoice.AmountPaid, invoice.Currency));
                t.Item().Row(r =>
                {
                    var c = invoice.BalanceDue > 0 ? Colors.Red.Darken1 : Colors.Green.Darken1;
                    r.ConstantItem(150).Text("Reste à payer").SemiBold().FontColor(c);
                    r.ConstantItem(130).AlignRight().Text(Money(invoice.BalanceDue, invoice.Currency)).SemiBold().FontColor(c);
                });
            });
        });
    }

    private static IContainer HeaderStyle(IContainer c) =>
        c.PaddingVertical(6).BorderBottom(1).BorderColor(Colors.Grey.Medium).DefaultTextStyle(t => t.SemiBold());
    private static IContainer CellStyle(IContainer c) =>
        c.PaddingVertical(6).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

    private static void Footer(IContainer container, Tenant tenant)
    {
        container.BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(6).Column(c =>
        {
            c.Item().AlignCenter().Text("Facture arrêtée à la somme indiquée — TVA acquittée sur les encaissements.")
                .FontSize(8).FontColor(Colors.Grey.Darken1);
            c.Item().AlignCenter().Text($"{tenant.Name} — document généré par PMS").FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    private static string Money(decimal amount, string currency) => $"{amount:N2} {currency}";
}
