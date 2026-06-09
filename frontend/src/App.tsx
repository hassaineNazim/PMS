import { Routes, Route } from 'react-router-dom';
import ProtectedRoute from './components/ProtectedRoute';
import Layout from './components/Layout';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Reservations from './pages/Reservations';
import Rooms from './pages/Rooms';
import Guests from './pages/Guests';
import Invoices from './pages/Invoices';
import Staff from './pages/Staff';
import CashRegister from './pages/CashRegister';
import Housekeeping from './pages/Housekeeping';
import Rates from './pages/Rates';
import Reports from './pages/Reports';
import SettingsPage from './pages/SettingsPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        element={
          <ProtectedRoute>
            <Layout />
          </ProtectedRoute>
        }
      >
        <Route path="/" element={<Dashboard />} />
        <Route path="/reservations" element={<Reservations />} />
        <Route path="/rooms" element={<Rooms />} />
        <Route path="/housekeeping" element={<Housekeeping />} />
        <Route path="/guests" element={<Guests />} />
        <Route path="/invoices" element={<Invoices />} />
        <Route path="/cash" element={<CashRegister />} />
        <Route path="/rates" element={<Rates />} />
        <Route path="/reports" element={<Reports />} />
        <Route path="/staff" element={<Staff />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Route>
    </Routes>
  );
}
