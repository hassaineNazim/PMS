import { useEffect, useState } from 'react';
import { api, apiError } from '../api/client';
import type { HousekeepingRoomDto, HousekeepingStatus, StaffDto } from '../api/types';
import { PageHeader, Badge } from '../components/ui';

const HK_STATUSES: HousekeepingStatus[] = ['Clean', 'Dirty', 'InProgress', 'Inspected'];

export default function Housekeeping() {
  const [rooms, setRooms] = useState<HousekeepingRoomDto[]>([]);
  const [staff, setStaff] = useState<StaffDto[]>([]);
  const [error, setError] = useState('');

  function load() {
    api.get<HousekeepingRoomDto[]>('/housekeeping/board').then((r) => setRooms(r.data)).catch((e) => setError(apiError(e)));
    api.get<StaffDto[]>('/staff').then((r) => setStaff(r.data)).catch(() => {});
  }
  useEffect(load, []);

  async function setStatus(roomId: string, status: HousekeepingStatus) {
    try { await api.put(`/housekeeping/${roomId}/status`, { status }); load(); } catch (e) { alert(apiError(e)); }
  }
  async function assign(roomId: string, housekeeperId: string) {
    try { await api.put(`/housekeeping/${roomId}/assign`, { housekeeperId: housekeeperId || null }); load(); } catch (e) { alert(apiError(e)); }
  }

  return (
    <>
      <PageHeader title="Housekeeping" />
      <div className="content">
        {error && <div className="error">{error}</div>}
        <div className="card">
          <table>
            <thead><tr><th>Chambre</th><th>Étage</th><th>Commercial</th><th>Ménage</th><th>Assignée à</th><th>Action</th></tr></thead>
            <tbody>
              {rooms.map((r) => (
                <tr key={r.roomId}>
                  <td>{r.number}</td><td>{r.floor ?? '—'}</td>
                  <td><Badge value={r.status} /></td>
                  <td><Badge value={r.housekeepingStatus} /></td>
                  <td>
                    <select value={r.assignedHousekeeperId ?? ''} onChange={(e) => assign(r.roomId, e.target.value)}>
                      <option value="">— Non assignée —</option>
                      {staff.map((s) => <option key={s.id} value={s.id}>{s.fullName}</option>)}
                    </select>
                  </td>
                  <td style={{ whiteSpace: 'nowrap' }}>
                    {HK_STATUSES.map((s) => (
                      <button key={s} className="btn secondary small" style={{ marginRight: 4 }} onClick={() => setStatus(r.roomId, s)}>{s}</button>
                    ))}
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
