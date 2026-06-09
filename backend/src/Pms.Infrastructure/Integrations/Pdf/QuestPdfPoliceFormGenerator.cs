using Pms.Application.Integrations;
using Pms.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Pms.Infrastructure.Integrations.Pdf;

/// <summary>
/// "Fiche de police" / déclaration de voyageur — the per-guest form Algerian hotels
/// must keep/declare. Pre-filled from the guest + reservation to save daily work.
/// </summary>
public class QuestPdfPoliceFormGenerator : IPoliceFormGenerator
{
    public byte[] Generate(Reservation reservation, Guest guest, Room room, Tenant tenant)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(t => t.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("FICHE DE POLICE").FontSize(16).Bold();
                    col.Item().AlignCenter().Text("Déclaration de voyageur / registre des étrangers").FontSize(10).FontColor(Colors.Grey.Darken1);
                    col.Item().PaddingTop(4).AlignCenter().Text(tenant.Name).SemiBold();
                });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(2);
                    Field(col, "Nom", guest.LastName);
                    Field(col, "Prénom", guest.FirstName);
                    Field(col, "Nationalité", guest.Nationality);
                    Field(col, "Type de pièce", guest.DocumentType);
                    Field(col, "N° de pièce / passeport", guest.DocumentNumber);
                    Field(col, "Téléphone", guest.Phone);
                    Field(col, "Email", guest.Email);
                    col.Item().PaddingTop(10);
                    Field(col, "Chambre", $"{room.Number} ({room.Type})");
                    Field(col, "Date d'arrivée", reservation.CheckIn.ToString("yyyy-MM-dd"));
                    Field(col, "Date de départ prévue", reservation.CheckOut.ToString("yyyy-MM-dd"));
                    Field(col, "Nombre de personnes", reservation.Occupants.ToString());

                    col.Item().PaddingTop(30).Row(r =>
                    {
                        r.RelativeItem().Text("Signature du voyageur :");
                        r.RelativeItem().Text("Cachet de l'établissement :");
                    });
                });

                page.Footer().AlignRight().Text($"Émise le {DateTime.UtcNow:yyyy-MM-dd HH:mm} — PMS")
                    .FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }).GeneratePdf();
    }

    private static void Field(ColumnDescriptor col, string label, string? value)
    {
        col.Item().PaddingVertical(3).Row(r =>
        {
            r.ConstantItem(190).Text(label + " :").SemiBold();
            r.RelativeItem().BorderBottom(1).BorderColor(Colors.Grey.Lighten1).Text(value ?? "");
        });
    }
}
