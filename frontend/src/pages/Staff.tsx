import { useEffect, useState, type FormEvent } from 'react';
import dayjs from 'dayjs';
import { api, apiError } from '../api/client';
import type { StaffDto, StaffRole, StaffStatus } from '../api/types';
import { PageHeader, Modal, Badge } from '../components/ui';

const ROLES: StaffRole[] = ['Manager', 'Receptionist', 'Housekeeper', 'Maintenance', 'Security', 'Other'];
const STATUSES: StaffStatus[] = ['Active', 'Inactive', 'OnLeave'];

export default function Staff() {
  const [items, setItems] = useState<StaffDto[]>([]);
  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<StaffDto | null>(null);
  const [error, setError] = useState('');

  function load() {
    api.get<StaffDto[]>('/staff').then((r) => setItems(r.data)).catch((e) => setError(apiError(e)));
  }
  useEffect(load, []);

  async function remove(id: string) {
    if (!confirm('Supprimer cet employé ?')) return;
    try { await api.delete(`/staff/${id}`); load(); } catch (e) { alert(apiError(e)); }
  }

  return (
    <>
      <PageHeader title="Personnel">
        <button className="btn" onClick={() => setCreating(true)}>+ Nouvel employé</button>
      </PageHeader>
      <div className="content">
        {error && <div className="error">{error}</div>}
        <div className="card">
          <table>
            <thead><tr><th>Nom</th><th>Rôle</th><th>Département</th><th>Embauche</th><th>Statut</th><th></th></tr></thead>
            <tbody>
              {items.map((s) => (
                <tr key={s.id}>
                  <td>{s.fullName}</td><td>{s.role}</td><td>{s.department ?? '—'}</td>
                  <td>{s.hireDate}</td><td><Badge value={s.status} /></td>
                  <td style={{ textAlign: 'right' }}>
                    <button className="btn secondary small" onClick={() => setEditing(s)}>Modifier</button>{' '}
                    <button className="btn danger small" onClick={() => remove(s.id)}>Suppr.</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      {(creating || editing) && (
        <StaffForm staff={editing} onClose={() => { setCreating(false); setEditing(null); }} onSaved={() => { setCreating(false); setEditing(null); load(); }} />
      )}
    </>
  );
}

function StaffForm({ staff, onClose, onSaved }: { staff: StaffDto | null; onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState({
    firstName: staff?.firstName ?? '', lastName: staff?.lastName ?? '', email: staff?.email ?? '',
    phone: staff?.phone ?? '', role: staff?.role ?? 'Other', department: staff?.department ?? '',
    hireDate: staff?.hireDate ?? dayjs().format('YYYY-MM-DD'), status: staff?.status ?? 'Active',
  });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault(); setBusy(true); setError('');
    try {
      if (staff) await api.put(`/staff/${staff.id}`, form);
      else await api.post('/staff', form);
      onSaved();
    } catch (err) { setError(apiError(err)); } finally { setBusy(false); }
  }

  return (
    <Modal title={staff ? staff.fullName : 'Nouvel employé'} onClose={onClose}>
      <form onSubmit={submit}>
        <div className="row">
          <div><label>Prénom</label><input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} required /></div>
          <div><label>Nom</label><input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} required /></div>
        </div>
        <label>Email</label><input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
        <div className="row">
          <div><label>Rôle</label><select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as StaffRole })}>{ROLES.map((r) => <option key={r}>{r}</option>)}</select></div>
          <div><label>Statut</label><select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value as StaffStatus })}>{STATUSES.map((s) => <option key={s}>{s}</option>)}</select></div>
        </div>
        <div className="row">
          <div><label>Département</label><input value={form.department} onChange={(e) => setForm({ ...form, department: e.target.value })} /></div>
          <div><label>Date d'embauche</label><input type="date" value={form.hireDate} onChange={(e) => setForm({ ...form, hireDate: e.target.value })} /></div>
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
