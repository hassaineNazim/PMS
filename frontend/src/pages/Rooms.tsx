import { useEffect, useState, type FormEvent } from 'react';
import { api, apiError } from '../api/client';
import type { RoomDto, RoomType } from '../api/types';
import { PageHeader, Modal, Badge, money } from '../components/ui';

const TYPES: RoomType[] = ['Single', 'Double', 'Twin', 'Suite', 'Deluxe'];
const STATUSES = ['Available', 'Occupied', 'Dirty', 'OutOfService'];

export default function Rooms() {
  const [rooms, setRooms] = useState<RoomDto[]>([]);
  const [editing, setEditing] = useState<RoomDto | null>(null);
  const [creating, setCreating] = useState(false);
  const [error, setError] = useState('');

  function load() {
    api.get<RoomDto[]>('/rooms').then((r) => setRooms(r.data)).catch((e) => setError(apiError(e)));
  }
  useEffect(load, []);

  async function remove(id: string) {
    if (!confirm('Supprimer cette chambre ?')) return;
    try { await api.delete(`/rooms/${id}`); load(); } catch (e) { alert(apiError(e)); }
  }

  return (
    <>
      <PageHeader title="Chambres">
        <button className="btn" onClick={() => setCreating(true)}>+ Nouvelle chambre</button>
      </PageHeader>
      <div className="content">
        {error && <div className="error">{error}</div>}
        <div className="card">
          <table>
            <thead><tr><th>N°</th><th>Type</th><th>Étage</th><th>Capacité</th><th>Prix/nuit</th><th>Statut</th><th></th></tr></thead>
            <tbody>
              {rooms.map((r) => (
                <tr key={r.id}>
                  <td>{r.number}</td><td>{r.type}</td><td>{r.floor ?? '—'}</td><td>{r.capacity}</td>
                  <td>{money(r.pricePerNight)}</td><td><Badge value={r.status} /></td>
                  <td style={{ textAlign: 'right' }}>
                    <button className="btn secondary small" onClick={() => setEditing(r)}>Modifier</button>{' '}
                    <button className="btn danger small" onClick={() => remove(r.id)}>Suppr.</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      {(creating || editing) && (
        <RoomForm room={editing} onClose={() => { setCreating(false); setEditing(null); }} onSaved={() => { setCreating(false); setEditing(null); load(); }} />
      )}
    </>
  );
}

function RoomForm({ room, onClose, onSaved }: { room: RoomDto | null; onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({
    number: room?.number ?? '', type: room?.type ?? 'Single', status: room?.status ?? 'Available',
    floor: room?.floor ?? 1, capacity: room?.capacity ?? 1, pricePerNight: room?.pricePerNight ?? 0,
    description: room?.description ?? '',
  });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault(); setBusy(true); setError('');
    try {
      if (room) await api.put(`/rooms/${room.id}`, form);
      else await api.post('/rooms', form);
      onSaved();
    } catch (err) { setError(apiError(err)); } finally { setBusy(false); }
  }

  return (
    <Modal title={room ? `Chambre ${room.number}` : 'Nouvelle chambre'} onClose={onClose}>
      <form onSubmit={submit}>
        <div className="row">
          <div><label>Numéro</label><input value={form.number} onChange={(e) => setForm({ ...form, number: e.target.value })} required /></div>
          <div><label>Étage</label><input type="number" value={form.floor} onChange={(e) => setForm({ ...form, floor: +e.target.value })} /></div>
        </div>
        <div className="row">
          <div><label>Type</label><select value={form.type} onChange={(e) => setForm({ ...form, type: e.target.value as RoomType })}>{TYPES.map((t) => <option key={t}>{t}</option>)}</select></div>
          {room && <div><label>Statut</label><select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value as any })}>{STATUSES.map((s) => <option key={s}>{s}</option>)}</select></div>}
        </div>
        <div className="row">
          <div><label>Capacité</label><input type="number" min={1} value={form.capacity} onChange={(e) => setForm({ ...form, capacity: +e.target.value })} /></div>
          <div><label>Prix / nuit (DZD)</label><input type="number" min={0} step="0.01" value={form.pricePerNight} onChange={(e) => setForm({ ...form, pricePerNight: +e.target.value })} /></div>
        </div>
        <label>Description</label>
        <textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
        {error && <div className="error">{error}</div>}
        <div style={{ marginTop: 18, display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
          <button type="button" className="btn secondary" onClick={onClose}>Annuler</button>
          <button className="btn" disabled={busy}>{busy ? '…' : 'Enregistrer'}</button>
        </div>
      </form>
    </Modal>
  );
}
