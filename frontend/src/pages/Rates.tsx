import { useEffect, useState, type FormEvent } from 'react';
import { api, apiError } from '../api/client';
import type { RatePeriodDto, RoomType } from '../api/types';
import { PageHeader, Modal, money } from '../components/ui';

const TYPES: RoomType[] = ['Single', 'Double', 'Twin', 'Suite', 'Deluxe'];

export default function Rates() {
  const [items, setItems] = useState<RatePeriodDto[]>([]);
  const [editing, setEditing] = useState<RatePeriodDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState('');

  function load() { api.get<RatePeriodDto[]>('/rates').then((r) => setItems(r.data)).catch((e) => setError(apiError(e))); }
  useEffect(load, []);

  async function remove(id: string) {
    if (!confirm('Supprimer cette période tarifaire ?')) return;
    try { await api.delete(`/rates/${id}`); load(); } catch (e) { alert(apiError(e)); }
  }

  return (
    <>
      <PageHeader title="Tarifs saisonniers">
        <button className="btn" onClick={() => setCreating(true)}>+ Nouvelle période</button>
      </PageHeader>
      <div className="content">
        {error && <div className="error">{error}</div>}
        <div className="card">
          <table>
            <thead><tr><th>Nom</th><th>Type chambre</th><th>Début</th><th>Fin</th><th>Prix/nuit</th><th>Priorité</th><th></th></tr></thead>
            <tbody>
              {items.map((p) => (
                <tr key={p.id}>
                  <td>{p.name}</td><td>{p.roomType ?? 'Toutes'}</td><td>{p.startDate}</td><td>{p.endDate}</td>
                  <td>{money(p.pricePerNight)}</td><td>{p.priority}</td>
                  <td style={{ textAlign: 'right' }}>
                    <button className="btn secondary small" onClick={() => setEditing(p)}>Modifier</button>{' '}
                    <button className="btn danger small" onClick={() => remove(p.id)}>Suppr.</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      {(creating || editing) && (
        <RateForm period={editing} onClose={() => { setCreating(false); setEditing(null); }} onSaved={() => { setCreating(false); setEditing(null); load(); }} />
      )}
    </>
  );
}

function RateForm({ period, onClose, onSaved }: { period: RatePeriodDto | null; onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({
    name: period?.name ?? '', roomType: period?.roomType ?? '', startDate: period?.startDate ?? '',
    endDate: period?.endDate ?? '', pricePerNight: period?.pricePerNight ?? 0, priority: period?.priority ?? 0,
  });
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault(); setBusy(true); setError('');
    const body = { ...form, roomType: form.roomType || null };
    try {
      if (period) await api.put(`/rates/${period.id}`, body); else await api.post('/rates', body);
      onSaved();
    } catch (err) { setError(apiError(err)); } finally { setBusy(false); }
  }

  return (
    <Modal title={period ? period.name : 'Nouvelle période tarifaire'} onClose={onClose}>
      <form onSubmit={submit}>
        <label>Nom (ex. Haute saison)</label>
        <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
        <label>Type de chambre</label>
        <select value={form.roomType} onChange={(e) => setForm({ ...form, roomType: e.target.value as RoomType | '' })}>
          <option value="">Toutes</option>
          {TYPES.map((t) => <option key={t}>{t}</option>)}
        </select>
        <div className="row">
          <div><label>Début</label><input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} required /></div>
          <div><label>Fin</label><input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} required /></div>
        </div>
        <div className="row">
          <div><label>Prix / nuit</label><input type="number" min={0} value={form.pricePerNight} onChange={(e) => setForm({ ...form, pricePerNight: +e.target.value })} /></div>
          <div><label>Priorité</label><input type="number" value={form.priority} onChange={(e) => setForm({ ...form, priority: +e.target.value })} /></div>
        </div>
        {error && <div className="error">{error}</div>}
        <div style={{ marginTop: 18, display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
          <button type="button" className="btn secondary" onClick={onClose}>Annuler</button>
          <button className="btn" disabled={busy}>{busy ? '…' : 'Enregistrer'}</button>
        </div>
      </form>
    </Modal>
  );
}
