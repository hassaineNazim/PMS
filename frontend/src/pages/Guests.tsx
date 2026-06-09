import { useEffect, useState, type FormEvent } from 'react';
import { api, apiError } from '../api/client';
import type { GuestDto, PagedResult } from '../api/types';
import { PageHeader, Modal } from '../components/ui';

export default function Guests() {
  const [data, setData] = useState<PagedResult<GuestDto> | null>(null);
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<GuestDto | null>(null);
  const [creating, setCreating] = useState(false);

  function load() {
    api.get<PagedResult<GuestDto>>('/guests', { params: { search, pageSize: 100 } }).then((r) => setData(r.data));
  }
  useEffect(() => { const t = setTimeout(load, 250); return () => clearTimeout(t); }, [search]);

  async function remove(id: string) {
    if (!confirm('Supprimer ce client ?')) return;
    try { await api.delete(`/guests/${id}`); load(); } catch (e) { alert(apiError(e)); }
  }

  return (
    <>
      <PageHeader title="Clients">
        <button className="btn" onClick={() => setCreating(true)}>+ Nouveau client</button>
      </PageHeader>
      <div className="content">
        <div className="toolbar">
          <input placeholder="Rechercher un client…" value={search} onChange={(e) => setSearch(e.target.value)} style={{ maxWidth: 320 }} />
        </div>
        <div className="card">
          <table>
            <thead><tr><th>Nom</th><th>Email</th><th>Téléphone</th><th>Langue</th><th>Nationalité</th><th></th></tr></thead>
            <tbody>
              {data?.items.map((g) => (
                <tr key={g.id}>
                  <td>{g.fullName}</td><td>{g.email ?? '—'}</td><td>{g.phone ?? '—'}</td>
                  <td>{g.language}</td><td>{g.nationality ?? '—'}</td>
                  <td style={{ textAlign: 'right' }}>
                    <button className="btn secondary small" onClick={() => setEditing(g)}>Modifier</button>{' '}
                    <button className="btn danger small" onClick={() => remove(g.id)}>Suppr.</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      {(creating || editing) && (
        <GuestForm guest={editing} onClose={() => { setCreating(false); setEditing(null); }} onSaved={() => { setCreating(false); setEditing(null); load(); }} />
      )}
    </>
  );
}

function GuestForm({ guest, onClose, onSaved }: { guest: GuestDto | null; onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({
    firstName: guest?.firstName ?? '', lastName: guest?.lastName ?? '', email: guest?.email ?? '',
    phone: guest?.phone ?? '', language: guest?.language ?? 'fr', nationality: guest?.nationality ?? '',
    documentType: guest?.documentType ?? '', documentNumber: guest?.documentNumber ?? '',
  });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault(); setBusy(true); setError('');
    try {
      if (guest) await api.put(`/guests/${guest.id}`, form);
      else await api.post('/guests', form);
      onSaved();
    } catch (err) { setError(apiError(err)); } finally { setBusy(false); }
  }

  return (
    <Modal title={guest ? guest.fullName : 'Nouveau client'} onClose={onClose}>
      <form onSubmit={submit}>
        <div className="row">
          <div><label>Prénom</label><input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} required /></div>
          <div><label>Nom</label><input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} required /></div>
        </div>
        <label>Email</label><input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
        <div className="row">
          <div><label>Téléphone</label><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></div>
          <div><label>Langue</label><input value={form.language} onChange={(e) => setForm({ ...form, language: e.target.value })} maxLength={5} /></div>
        </div>
        <div className="row">
          <div><label>Nationalité</label><input value={form.nationality} onChange={(e) => setForm({ ...form, nationality: e.target.value })} /></div>
          <div><label>Pièce (n°)</label><input value={form.documentNumber} onChange={(e) => setForm({ ...form, documentNumber: e.target.value })} /></div>
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
