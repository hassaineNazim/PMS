import { useEffect, useState } from 'react';
import {
  AreaChart, Area, BarChart, Bar, LineChart, Line, PieChart, Pie, Cell,
  XAxis, YAxis, Tooltip, ResponsiveContainer, CartesianGrid,
} from 'recharts';
import { api, apiError } from '../api/client';
import type { DashboardStatsDto } from '../api/types';
import { PageHeader, money } from '../components/ui';

const COLORS = ['#22c55e', '#3b82f6', '#f59e0b', '#ef4444'];

export default function Dashboard() {
  const [data, setData] = useState<DashboardStatsDto | null>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api.get<DashboardStatsDto>('/stats/dashboard')
      .then((r) => setData(r.data))
      .catch((e) => setError(apiError(e)));
  }, []);

  if (error) return (<><PageHeader title="Tableau de bord" /><div className="content error">{error}</div></>);
  if (!data) return (<><PageHeader title="Tableau de bord" /><div className="content">Chargement…</div></>);

  const roomsPie = [
    { name: 'Disponibles', value: data.charts.roomsByStatus.available },
    { name: 'Occupées', value: data.charts.roomsByStatus.occupied },
    { name: 'À nettoyer', value: data.charts.roomsByStatus.dirty },
    { name: 'Hors service', value: data.charts.roomsByStatus.outOfService },
  ];

  return (
    <>
      <PageHeader title="Tableau de bord" />
      <div className="content">
        <div className="grid cols-4">
          <Stat label="Taux d'occupation" value={`${data.rooms.occupancyRate}%`} sub={`${data.rooms.occupied}/${data.rooms.total} chambres`} />
          <Stat label="Revenu ce mois" value={money(data.revenue.thisMonth)}
            sub={`${data.revenue.monthGrowth >= 0 ? '▲' : '▼'} ${Math.abs(data.revenue.monthGrowth)}% vs mois dernier`}
            up={data.revenue.monthGrowth >= 0} />
          <Stat label="Réservations actives" value={String(data.reservations.confirmed + data.reservations.checkedIn)}
            sub={`${data.reservations.checkedIn} arrivées en cours`} />
          <Stat label="Clients" value={String(data.guests.total)} sub={`${data.revenue.invoiceCount} factures émises`} />
        </div>

        <div className="grid cols-2" style={{ marginTop: 16 }}>
          <div className="card">
            <h3>Revenu (14 jours)</h3>
            <ResponsiveContainer width="100%" height={240}>
              <AreaChart data={data.charts.revenueByDay}>
                <defs>
                  <linearGradient id="rev" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="5%" stopColor="#3b82f6" stopOpacity={0.6} />
                    <stop offset="95%" stopColor="#3b82f6" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
                <XAxis dataKey="date" stroke="#94a3b8" fontSize={11} />
                <YAxis stroke="#94a3b8" fontSize={11} />
                <Tooltip contentStyle={{ background: '#1e293b', border: '1px solid #334155' }} />
                <Area type="monotone" dataKey="value" stroke="#3b82f6" fill="url(#rev)" />
              </AreaChart>
            </ResponsiveContainer>
          </div>

          <div className="card">
            <h3>Occupation (%)</h3>
            <ResponsiveContainer width="100%" height={240}>
              <LineChart data={data.charts.occupancyByDay}>
                <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
                <XAxis dataKey="date" stroke="#94a3b8" fontSize={11} />
                <YAxis stroke="#94a3b8" fontSize={11} domain={[0, 100]} />
                <Tooltip contentStyle={{ background: '#1e293b', border: '1px solid #334155' }} />
                <Line type="monotone" dataKey="value" stroke="#22c55e" strokeWidth={2} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </div>

          <div className="card">
            <h3>Réservations / jour</h3>
            <ResponsiveContainer width="100%" height={240}>
              <BarChart data={data.charts.reservationsByDay}>
                <CartesianGrid strokeDasharray="3 3" stroke="#334155" />
                <XAxis dataKey="date" stroke="#94a3b8" fontSize={11} />
                <YAxis stroke="#94a3b8" fontSize={11} allowDecimals={false} />
                <Tooltip contentStyle={{ background: '#1e293b', border: '1px solid #334155' }} />
                <Bar dataKey="value" fill="#3b82f6" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>

          <div className="card">
            <h3>Répartition des chambres</h3>
            <ResponsiveContainer width="100%" height={240}>
              <PieChart>
                <Pie data={roomsPie} dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={85} label>
                  {roomsPie.map((_, i) => <Cell key={i} fill={COLORS[i % COLORS.length]} />)}
                </Pie>
                <Tooltip contentStyle={{ background: '#1e293b', border: '1px solid #334155' }} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>
    </>
  );
}

function Stat({ label, value, sub, up }: { label: string; value: string; sub?: string; up?: boolean }) {
  return (
    <div className="card stat">
      <div className="label">{label}</div>
      <div className="value">{value}</div>
      {sub && <div className={`sub ${up === undefined ? 'muted' : up ? 'up' : 'down'}`}>{sub}</div>}
    </div>
  );
}
