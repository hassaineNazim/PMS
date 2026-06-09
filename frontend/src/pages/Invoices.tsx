import { useEffect, useState } from 'react';
import { api, apiError } from '../api/client';
import type { InvoiceDto } from '../api/types';
import { PageHeader, Badge, money } from '../components/ui';

export default function Invoices() {
  const [items, setItems] = useState<InvoiceDto[]>([]);
  const [error, setError] = useState('');

  useEffect(() => {
    api.get<InvoiceDto[]>('/invoices').then((r) => setItems(r.data)).catch((e) => setError(apiError(e)));
  }, []);

  async function downloadPdf(inv: InvoiceDto) {
    try {
      const res = await api.get(`/invoices/${inv.id}/pdf`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement('a');
      a.href = url; a.download = `${inv.number}.pdf`; a.click();
      URL.revokeObjectURL(url);
    } catch (e) { alert(apiError(e)); }
  }

  return (
    <>
      <PageHeader title="Factures" />
      <div className="content">
        {error && <div className="error">{error}</div>}
        <div className="card">
          <table>
            <thead><tr><th>N°</th><th>Client</th><th>Chambre</th><th>Total</th><th>Payé</th><th>Solde</th><th>Statut</th><th></th></tr></thead>
            <tbody>
              {items.map((i) => (
                <tr key={i.id}>
                  <td>{i.number}</td><td>{i.guestName}</td><td>{i.roomNumber}</td>
                  <td>{money(i.total, i.currency)}</td>
                  <td>{money(i.amountPaid, i.currency)}</td>
                  <td style={{ color: i.balanceDue > 0 ? 'var(--red)' : 'var(--green)' }}>{money(i.balanceDue, i.currency)}</td>
                  <td><Badge value={i.status} /></td>
                  <td style={{ textAlign: 'right' }}>
                    <button className="btn secondary small" onClick={() => downloadPdf(i)}>PDF</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
