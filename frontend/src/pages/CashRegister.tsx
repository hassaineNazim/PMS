import { useEffect, useState } from 'react';
import { api, apiError } from '../api/client';
import type { CashSessionDto } from '../api/types';
import { PageHeader, Badge, money } from '../components/ui';

export default function CashRegister() {
  const [current, setCurrent] = useState<CashSessionDto | null>(null);
  const [history, setHistory] = useState<CashSessionDto[]>([]);
  const [openingFloat, setOpeningFloat] = useState(0);
  const [counted, setCounted] = useState(0);
  const [error, setError] = useState('');

  function load() {
    api.get<CashSessionDto | ''>('/cash/current').then((r) => setCurrent(r.data || null)).catch((e) => setError(apiError(e)));
    api.get<CashSessionDto[]>('/cash/history').then((r) => setHistory(r.data)).catch(() => {});
  }
  useEffect(load, []);

  async function open() { try { await api.post('/cash/open', { openingFloat }); load(); } catch (e) { alert(apiError(e)); } }
  async function close() {
    try {
      const res = await api.post<CashSessionDto>('/cash/close', { countedCash: counted });
      alert(`Caisse clôturée. Écart : ${money(res.data.discrepancy ?? 0)}`);
      load();
    } catch (e) { alert(apiError(e)); }
  }

  return (
    <>
      <PageHeader title="Caisse" />
      <div className="content">
        {error && <div className="error">{error}</div>}
        <div className="card" style={{ marginBottom: 16 }}>
          {current ? (
            <>
              <h3>Session ouverte</h3>
              <div className="grid cols-4">
                <Kpi label="Fond d'ouverture" value={money(current.openingFloat)} />
                <Kpi label="Mouvements espèces" value={money(current.cashMovements)} />
                <Kpi label="Espèces attendues" value={money(current.expectedCash)} />
                <Kpi label="Ouverte le" value={new Date(current.openedAt).toLocaleString('fr-FR')} />
              </div>
              <div className="row" style={{ marginTop: 14, alignItems: 'flex-end' }}>
                <div><label>Montant compté en caisse</label><input type="number" value={counted} onChange={(e) => setCounted(+e.target.value)} /></div>
                <button className="btn danger" onClick={close}>Clôturer la caisse</button>
              </div>
            </>
          ) : (
            <>
              <h3>Aucune caisse ouverte</h3>
              <div className="row" style={{ alignItems: 'flex-end' }}>
                <div><label>Fond de caisse initial</label><input type="number" value={openingFloat} onChange={(e) => setOpeningFloat(+e.target.value)} /></div>
                <button className="btn" onClick={open}>Ouvrir la caisse</button>
              </div>
            </>
          )}
        </div>

        <div className="card">
          <h3>Historique des clôtures</h3>
          <table>
            <thead><tr><th>Utilisateur</th><th>Ouverte</th><th>Fermée</th><th>Fond</th><th>Attendu</th><th>Compté</th><th>Écart</th><th>Statut</th></tr></thead>
            <tbody>
              {history.map((s) => (
                <tr key={s.id}>
                  <td>{s.userName}</td>
                  <td>{new Date(s.openedAt).toLocaleString('fr-FR')}</td>
                  <td>{s.closedAt ? new Date(s.closedAt).toLocaleString('fr-FR') : '—'}</td>
                  <td>{money(s.openingFloat)}</td><td>{money(s.expectedCash)}</td>
                  <td>{s.countedCash != null ? money(s.countedCash) : '—'}</td>
                  <td style={{ color: (s.discrepancy ?? 0) !== 0 ? 'var(--red)' : 'var(--green)' }}>{s.discrepancy != null ? money(s.discrepancy) : '—'}</td>
                  <td><Badge value={s.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </>
  );
}

function Kpi({ label, value }: { label: string; value: string }) {
  return <div className="card stat"><div className="label">{label}</div><div className="value" style={{ fontSize: 18 }}>{value}</div></div>;
}
