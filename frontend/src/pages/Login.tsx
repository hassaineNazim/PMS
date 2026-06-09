import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { apiError } from '../api/client';

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('admin@demo.com');
  const [password, setPassword] = useState('admin123');
  const [tenantSlug, setTenantSlug] = useState('demo');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError('');
    setBusy(true);
    try {
      await login(email, password, tenantSlug || undefined);
      navigate('/');
    } catch (err) {
      setError(apiError(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="center">
      <form className="card login-card" onSubmit={onSubmit}>
        <h1>🏨 PMS</h1>
        <div className="muted">Gestion hôtelière</div>

        <label>Établissement (slug)</label>
        <input value={tenantSlug} onChange={(e) => setTenantSlug(e.target.value)} placeholder="demo" />

        <label>Email</label>
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />

        <label>Mot de passe</label>
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />

        {error && <div className="error">{error}</div>}

        <button className="btn" style={{ width: '100%', marginTop: 18 }} disabled={busy}>
          {busy ? 'Connexion…' : 'Se connecter'}
        </button>
        <p className="hint">Démo : admin@demo.com / admin123 (établissement « demo »)</p>
      </form>
    </div>
  );
}
