import { useEffect, useState } from 'react';
import dayjs from 'dayjs';
import { api, apiError } from '../api/client';
import type { MainCouranteEntryDto } from '../api/types';
import { PageHeader, Badge } from '../components/ui';

export default function Reports() {
  const [date, setDate] = useState(dayjs().format('YYYY-MM-DD'));
  const [entries, setEntries] = useState<MainCouranteEntryDto[]>([]);
  const [error, setError] = useState('');

  function load() {
    api.get<MainCouranteEntryDto[]>('/reports/main-courante', { params: { date } })
      .then((r) => setEntries(r.data)).catch((e) => setError(apiError(e)));
  }
  useEffect(load, [date]);

  async function downloadCsv(path: string, name: string) {
    try {
      const res = await api.get(path, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement('a'); a.href = url; a.download = name; a.click();
      URL.revokeObjectURL(url);
    } catch (e) { alert(apiError(e)); }
  }

  return (
    <>
      <PageHeader title="Rapports" />
      <div className="content">
        {error && <div className="error">{error}</div>}
        <div className="toolbar">
          <div className="row" style={{ alignItems: 'flex-end', maxWidth: 280 }}>
            <div><label>Date (main courante)</label><input type="date" value={date} onChange={(e) => setDate(e.target.value)} /></div>
          </div>
          <div style={{ display: 'flex', gap: 8 }}>
            <button className="btn secondary" onClick={() => downloadCsv('/reports/reservations.csv', 'reservations.csv')}>Export réservations (CSV)</button>
            <button className="btn secondary" onClick={() => downloadCsv('/reports/revenue.csv', 'revenue.csv')}>Export CA (CSV)</button>
          </div>
        </div>

        <div className="card">
          <h3>Main courante — {date}</h3>
          <table>
            <thead><tr><th>Mouvement</th><th>Client</th><th>Chambre</th><th>Arrivée</th><th>Départ</th><th>Statut</th></tr></thead>
            <tbody>
              {entries.length === 0 && <tr><td colSpan={6} className="muted">Aucun mouvement ce jour.</td></tr>}
              {entries.map((e, i) => (
                <tr key={i}>
                  <td><Badge value={e.movement === 'Arrivée' ? 'Confirmed' : 'CheckedOut'} />{' '}{e.movement}</td>
                  <td>{e.guestName}</td><td>{e.roomNumber}</td><td>{e.checkIn}</td><td>{e.checkOut}</td>
                  <td><Badge value={e.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}
