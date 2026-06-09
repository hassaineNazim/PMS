import { useEffect, useState, type FormEvent, type CSSProperties } from 'react';
import dayjs from 'dayjs';
import { api, apiError } from '../api/client';
import type {
  ReservationDto, GuestDto, AvailableRoomDto, PagedResult, CheckInResult,
  MealPlan, FolioDto, ChargeCategory, PaymentMethod, PaymentType,
} from '../api/types';
import { MEAL_PLAN_LABELS } from '../api/types';
import { PageHeader, Modal, Badge, money } from '../components/ui';

const MEAL_PLANS: MealPlan[] = ['RoomOnly', 'BedAndBreakfast', 'HalfBoard', 'FullBoard'];

export default function Reservations() {
  const [items, setItems] = useState<ReservationDto[]>([]);
  const [creating, setCreating] = useState(false);
  const [folioFor, setFolioFor] = useState<ReservationDto | null>(null);
  const [error, setError] = useState('');

  function load() {
    api.get<ReservationDto[]>('/reservations').then((r) => setItems(r.data)).catch((e) => setError(apiError(e)));
  }
  useEffect(load, []);

  async function checkIn(id: string) {
    try {
      const res = await api.post<CheckInResult>(`/checkin/${id}`, {});
      alert(`Check-in OK — facture ${res.data.invoiceNumber} (${money(res.data.invoiceTotal)}).`);
      load();
    } catch (e) { alert(apiError(e)); }
  }
  async function checkOut(id: string) { try { await api.post(`/checkout/${id}`); load(); } catch (e) { alert(apiError(e)); } }
  async function cancel(id: string) {
    if (!confirm('Annuler cette réservation ?')) return;
    try { await api.post(`/reservations/${id}/cancel`); load(); } catch (e) { alert(apiError(e)); }
  }

  return (
    <>
      <PageHeader title="Réservations">
        <button className="btn" onClick={() => setCreating(true)}>+ Nouvelle réservation</button>
      </PageHeader>
      <div className="content">
        {error && <div className="error">{error}</div>}
        <div className="card">
          <table>
            <thead><tr><th>Client</th><th>Chambre</th><th>Pension</th><th>Arrivée</th><th>Départ</th><th>Total</th><th>Statut</th><th></th></tr></thead>
            <tbody>
              {items.map((r) => (
                <tr key={r.id}>
                  <td>{r.guestName}</td>
                  <td>{r.roomNumber} <span className="muted">({r.roomType})</span></td>
                  <td>{MEAL_PLAN_LABELS[r.mealPlan]}</td>
                  <td>{r.checkIn}</td><td>{r.checkOut}</td>
                  <td>{money(r.totalAmount)}</td>
                  <td><Badge value={r.status} /></td>
                  <td style={{ textAlign: 'right', whiteSpace: 'nowrap' }}>
                    <button className="btn secondary small" onClick={() => setFolioFor(r)}>Folio</button>{' '}
                    {r.status === 'Confirmed' && <button className="btn small" onClick={() => checkIn(r.id)}>Check-in</button>}
                    {r.status === 'CheckedIn' && <button className="btn small" onClick={() => checkOut(r.id)}>Check-out</button>}{' '}
                    {r.status === 'Confirmed' && <button className="btn danger small" onClick={() => cancel(r.id)}>Annuler</button>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
      {creating && <NewReservation onClose={() => setCreating(false)} onSaved={() => { setCreating(false); load(); }} />}
      {folioFor && <FolioModal reservation={folioFor} onClose={() => { setFolioFor(null); load(); }} />}
    </>
  );
}

function NewReservation({ onClose, onSaved }: { onClose: () => void; onSaved: () => void }) {
  const [guests, setGuests] = useState<GuestDto[]>([]);
  const [guestId, setGuestId] = useState('');
  const [checkIn, setCheckIn] = useState(dayjs().format('YYYY-MM-DD'));
  const [checkOut, setCheckOut] = useState(dayjs().add(1, 'day').format('YYYY-MM-DD'));
  const [mealPlan, setMealPlan] = useState<MealPlan>('RoomOnly');
  const [available, setAvailable] = useState<AvailableRoomDto[] | null>(null);
  const [roomId, setRoomId] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    api.get<PagedResult<GuestDto>>('/guests', { params: { pageSize: 200 } }).then((r) => setGuests(r.data.items));
  }, []);

  async function searchAvailability() {
    setError(''); setAvailable(null); setRoomId('');
    try {
      const res = await api.post<AvailableRoomDto[]>('/reservations/availability', { checkIn, checkOut });
      setAvailable(res.data);
    } catch (e) { setError(apiError(e)); }
  }

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!guestId || !roomId) { setError('Sélectionnez un client et une chambre.'); return; }
    setBusy(true); setError('');
    try {
      await api.post('/reservations', { guestId, roomId, checkIn, checkOut, adults: 1, children: 0, mealPlan });
      onSaved();
    } catch (err) { setError(apiError(err)); } finally { setBusy(false); }
  }

  return (
    <Modal title="Nouvelle réservation" onClose={onClose}>
      <form onSubmit={submit}>
        <label>Client</label>
        <select value={guestId} onChange={(e) => setGuestId(e.target.value)} required>
          <option value="">— Choisir —</option>
          {guests.map((g) => <option key={g.id} value={g.id}>{g.fullName}</option>)}
        </select>
        <div className="row">
          <div><label>Arrivée</label><input type="date" value={checkIn} onChange={(e) => setCheckIn(e.target.value)} /></div>
          <div><label>Départ</label><input type="date" value={checkOut} onChange={(e) => setCheckOut(e.target.value)} /></div>
        </div>
        <label>Formule de pension</label>
        <select value={mealPlan} onChange={(e) => setMealPlan(e.target.value as MealPlan)}>
          {MEAL_PLANS.map((m) => <option key={m} value={m}>{MEAL_PLAN_LABELS[m]}</option>)}
        </select>
        <button type="button" className="btn secondary" style={{ marginTop: 12 }} onClick={searchAvailability}>
          Rechercher les chambres disponibles
        </button>
        {available && (
          <div style={{ marginTop: 14 }}>
            {available.length === 0 && <div className="muted">Aucune chambre disponible sur cette période.</div>}
            {available.map((r) => (
              <label key={r.roomId} style={{ display: 'flex', alignItems: 'center', gap: 10, marginTop: 6, cursor: 'pointer' }}>
                <input type="radio" name="room" style={{ width: 'auto' }} checked={roomId === r.roomId} onChange={() => setRoomId(r.roomId)} />
                <span>Ch. {r.number} ({r.type}) — {money(r.pricePerNight)}/nuit · {money(r.estimatedTotal)}</span>
              </label>
            ))}
          </div>
        )}
        {error && <div className="error">{error}</div>}
        <div style={{ marginTop: 18, display: 'flex', gap: 10, justifyContent: 'flex-end' }}>
          <button type="button" className="btn secondary" onClick={onClose}>Annuler</button>
          <button className="btn" disabled={busy || !roomId}>{busy ? '…' : 'Réserver'}</button>
        </div>
      </form>
    </Modal>
  );
}

const CHARGE_CATEGORIES: ChargeCategory[] = ['MiniBar', 'Restaurant', 'RoomService', 'Laundry', 'Telephone', 'Spa', 'Other'];
const METHODS: PaymentMethod[] = ['Cash', 'CIB', 'Edahabia', 'BankTransfer', 'Cheque', 'Other'];
const PTYPES: PaymentType[] = ['Deposit', 'Balance', 'Refund'];

function FolioModal({ reservation, onClose }: { reservation: ReservationDto; onClose: () => void }) {
  const [folio, setFolio] = useState<FolioDto | null>(null);
  const [error, setError] = useState('');
  // charge form
  const [cat, setCat] = useState<ChargeCategory>('Restaurant');
  const [label, setLabel] = useState('');
  const [qty, setQty] = useState(1);
  const [unit, setUnit] = useState(0);
  // payment form
  const [amount, setAmount] = useState(0);
  const [method, setMethod] = useState<PaymentMethod>('Cash');
  const [ptype, setPtype] = useState<PaymentType>('Balance');

  function load() {
    api.get<FolioDto>(`/reservations/${reservation.id}/folio`).then((r) => {
      setFolio(r.data);
      setAmount(Math.max(0, r.data.balanceDue));
    }).catch((e) => setError(apiError(e)));
  }
  useEffect(load, [reservation.id]);

  async function addCharge() {
    if (!label) return;
    try {
      await api.post('/charges', { reservationId: reservation.id, category: cat, label, quantity: qty, unitPrice: unit });
      setLabel(''); setQty(1); setUnit(0); load();
    } catch (e) { alert(apiError(e)); }
  }
  async function delCharge(id: string) { try { await api.delete(`/charges/${id}`); load(); } catch (e) { alert(apiError(e)); } }

  async function addPayment() {
    try {
      const res = await api.post('/payments', { reservationId: reservation.id, amount, method, type: ptype });
      if (res.data.stampDuty > 0) alert(`Paiement enregistré. Timbre fiscal : ${money(res.data.stampDuty)}`);
      load();
    } catch (e) { alert(apiError(e)); }
  }
  async function delPayment(id: string) { try { await api.delete(`/payments/${id}`); load(); } catch (e) { alert(apiError(e)); } }

  async function policeForm() {
    try {
      const res = await api.get(`/reports/police-form/${reservation.id}`, { responseType: 'blob' });
      const url = URL.createObjectURL(res.data);
      const a = document.createElement('a'); a.href = url; a.download = `fiche-police.pdf`; a.click();
      URL.revokeObjectURL(url);
    } catch (e) { alert(apiError(e)); }
  }

  return (
    <Modal title={`Folio — ${reservation.guestName} (Ch. ${reservation.roomNumber})`} onClose={onClose}>
      {error && <div className="error">{error}</div>}
      {!folio ? <div>Chargement…</div> : (
        <div style={{ fontSize: 13 }}>
          <div className="card" style={{ marginBottom: 12 }}>
            <Line k="Chambre" v={money(folio.roomSubtotal, folio.currency)} />
            {folio.mealPlanSubtotal > 0 && <Line k="Pension" v={money(folio.mealPlanSubtotal, folio.currency)} />}
            {folio.extrasSubtotal > 0 && <Line k="Extras" v={money(folio.extrasSubtotal, folio.currency)} />}
            <Line k={`TVA (${folio.taxRate}%)`} v={money(folio.taxAmount, folio.currency)} />
            {folio.stampDuty > 0 && <Line k="Timbre fiscal" v={money(folio.stampDuty, folio.currency)} />}
            <Line k="TOTAL" v={money(folio.total, folio.currency)} bold />
            <Line k="Payé" v={money(folio.amountPaid, folio.currency)} />
            <Line k="Reste à payer" v={money(folio.balanceDue, folio.currency)} bold danger={folio.balanceDue > 0} />
          </div>

          <h3 style={{ margin: '10px 0 6px' }}>Extras / consommations</h3>
          {folio.charges.map((c) => (
            <div key={c.id} style={rowStyle}>
              <span>{c.label} <span className="muted">({c.category}) ×{c.quantity}</span></span>
              <span>{money(c.total, folio.currency)} <button className="btn danger small" onClick={() => delCharge(c.id)}>×</button></span>
            </div>
          ))}
          <div className="row" style={{ marginTop: 6 }}>
            <select value={cat} onChange={(e) => setCat(e.target.value as ChargeCategory)}>{CHARGE_CATEGORIES.map((c) => <option key={c}>{c}</option>)}</select>
            <input placeholder="Désignation" value={label} onChange={(e) => setLabel(e.target.value)} />
            <input type="number" min={1} style={{ maxWidth: 60 }} value={qty} onChange={(e) => setQty(+e.target.value)} />
            <input type="number" min={0} placeholder="P.U." value={unit} onChange={(e) => setUnit(+e.target.value)} />
            <button className="btn small" type="button" onClick={addCharge}>Ajouter</button>
          </div>

          <h3 style={{ margin: '14px 0 6px' }}>Paiements</h3>
          {folio.payments.map((p) => (
            <div key={p.id} style={rowStyle}>
              <span>{p.type} · {p.method}{p.stampDuty > 0 ? ` (timbre ${money(p.stampDuty, folio.currency)})` : ''}</span>
              <span>{money(p.amount, folio.currency)} <button className="btn danger small" onClick={() => delPayment(p.id)}>×</button></span>
            </div>
          ))}
          <div className="row" style={{ marginTop: 6 }}>
            <input type="number" min={0} value={amount} onChange={(e) => setAmount(+e.target.value)} />
            <select value={method} onChange={(e) => setMethod(e.target.value as PaymentMethod)}>{METHODS.map((m) => <option key={m}>{m}</option>)}</select>
            <select value={ptype} onChange={(e) => setPtype(e.target.value as PaymentType)}>{PTYPES.map((t) => <option key={t}>{t}</option>)}</select>
            <button className="btn small" type="button" onClick={addPayment}>Encaisser</button>
          </div>

          <div style={{ marginTop: 18, display: 'flex', gap: 10, justifyContent: 'space-between' }}>
            <button className="btn secondary" type="button" onClick={policeForm}>Fiche de police (PDF)</button>
            <button className="btn" type="button" onClick={onClose}>Fermer</button>
          </div>
        </div>
      )}
    </Modal>
  );
}

const rowStyle: CSSProperties = { display: 'flex', justifyContent: 'space-between', padding: '4px 0', borderBottom: '1px solid var(--border)' };

function Line({ k, v, bold, danger }: { k: string; v: string; bold?: boolean; danger?: boolean }) {
  return (
    <div style={{ display: 'flex', justifyContent: 'space-between', padding: '3px 0', fontWeight: bold ? 700 : 400, color: danger ? 'var(--red)' : undefined }}>
      <span>{k}</span><span>{v}</span>
    </div>
  );
}
