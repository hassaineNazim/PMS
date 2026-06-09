import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

const links = [
  { to: '/', label: 'Tableau de bord', end: true },
  { to: '/reservations', label: 'Réservations' },
  { to: '/rooms', label: 'Chambres' },
  { to: '/housekeeping', label: 'Housekeeping' },
  { to: '/guests', label: 'Clients' },
  { to: '/invoices', label: 'Factures' },
  { to: '/cash', label: 'Caisse' },
  { to: '/rates', label: 'Tarifs' },
  { to: '/reports', label: 'Rapports' },
  { to: '/staff', label: 'Personnel' },
  { to: '/settings', label: 'Paramètres' },
];

export default function Layout() {
  const { user, logout } = useAuth();
  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="brand">🏨 PMS</div>
        <nav>
          {links.map((l) => (
            <NavLink key={l.to} to={l.to} end={l.end}>
              {l.label}
            </NavLink>
          ))}
        </nav>
        <div className="spacer" />
        <div className="user">
          <div style={{ fontWeight: 600 }}>{user?.fullName}</div>
          <div className="muted">{user?.role}</div>
          <button className="btn secondary small" style={{ marginTop: 10 }} onClick={logout}>
            Déconnexion
          </button>
        </div>
      </aside>
      <div className="main">
        <Outlet />
      </div>
    </div>
  );
}
