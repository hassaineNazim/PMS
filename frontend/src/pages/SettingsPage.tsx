import { useEffect, useState, type FormEvent } from 'react';
import { api, apiError } from '../api/client';
import type { TenantSettingsDto } from '../api/types';
import { PageHeader } from '../components/ui';

export default function SettingsPage() {
  const [s, setS] = useState<TenantSettingsDto | null>(null);
  const [error, setError] = useState('');
  const [saved, setSaved] = useState(false);

  useEffect(() => { api.get<TenantSettingsDto>('/settings').then((r) => setS(r.data)).catch((e) => setError(apiError(e))); }, []);

  async function submit(e: FormEvent) {
    e.preventDefault(); setError(''); setSaved(false);
    try { const r = await api.put<TenantSettingsDto>('/settings', s); setS(r.data); setSaved(true); }
    catch (err) { setError(apiError(err)); }
  }
  function set<K extends keyof TenantSettingsDto>(k: K, v: TenantSettingsDto[K]) { setS((p) => p && { ...p, [k]: v }); }

  if (!s) return (<><PageHeader title="Paramètres" /><div className="content">{error || 'Chargement…'}</div></>);

  return (
    <>
      <PageHeader title="Paramètres de l'établissement" />
      <div className="content">
        <form onSubmit={submit} style={{ maxWidth: 720 }}>
          <div className="card" style={{ marginBottom: 16 }}>
            <h3>Identité</h3>
            <div className="row">
              <div><label>Nom commercial</label><input value={s.name} onChange={(e) => set('name', e.target.value)} /></div>
              <div><label>Raison sociale</label><input value={s.legalName} onChange={(e) => set('legalName', e.target.value)} /></div>
            </div>
            <label>Adresse</label><input value={s.address ?? ''} onChange={(e) => set('address', e.target.value)} />
            <div className="row">
              <div><label>Ville</label><input value={s.city ?? ''} onChange={(e) => set('city', e.target.value)} /></div>
              <div><label>Pays</label><input value={s.country ?? ''} onChange={(e) => set('country', e.target.value)} /></div>
            </div>
            <div className="row">
              <div><label>Téléphone</label><input value={s.phone ?? ''} onChange={(e) => set('phone', e.target.value)} /></div>
              <div><label>Email</label><input value={s.contactEmail ?? ''} onChange={(e) => set('contactEmail', e.target.value)} /></div>
            </div>
            <div className="row">
              <div><label>Devise</label><input value={s.currency} onChange={(e) => set('currency', e.target.value)} maxLength={3} /></div>
              <div><label>TVA par défaut (%)</label><input type="number" step="0.01" value={s.defaultTaxRate} onChange={(e) => set('defaultTaxRate', +e.target.value)} /></div>
            </div>
          </div>

          <div className="card" style={{ marginBottom: 16 }}>
            <h3>Conformité fiscale (DGI)</h3>
            <div className="row">
              <div><label>NIF</label><input value={s.taxId ?? ''} onChange={(e) => set('taxId', e.target.value)} /></div>
              <div><label>NIS</label><input value={s.statId ?? ''} onChange={(e) => set('statId', e.target.value)} /></div>
            </div>
            <div className="row">
              <div><label>RC</label><input value={s.tradeRegister ?? ''} onChange={(e) => set('tradeRegister', e.target.value)} /></div>
              <div><label>Article d'imposition</label><input value={s.taxArticle ?? ''} onChange={(e) => set('taxArticle', e.target.value)} /></div>
            </div>
            <label style={{ display: 'flex', gap: 8, alignItems: 'center', marginTop: 14 }}>
              <input type="checkbox" style={{ width: 'auto' }} checked={s.fiscalStampEnabled} onChange={(e) => set('fiscalStampEnabled', e.target.checked)} />
              Droit de timbre sur les paiements en espèces
            </label>
            <div className="row">
              <div><label>Taux timbre (DA / 100 DA)</label><input type="number" step="0.01" value={s.fiscalStampRate} onChange={(e) => set('fiscalStampRate', +e.target.value)} /></div>
              <div><label>Timbre minimum (DA)</label><input type="number" step="0.01" value={s.fiscalStampMinimum} onChange={(e) => set('fiscalStampMinimum', +e.target.value)} /></div>
            </div>
          </div>

          <div className="card" style={{ marginBottom: 16 }}>
            <h3>Suppléments de pension (par personne / nuit)</h3>
            <div className="row">
              <div><label>Petit-déjeuner</label><input type="number" value={s.breakfastSupplement} onChange={(e) => set('breakfastSupplement', +e.target.value)} /></div>
              <div><label>Demi-pension</label><input type="number" value={s.halfBoardSupplement} onChange={(e) => set('halfBoardSupplement', +e.target.value)} /></div>
              <div><label>Pension complète</label><input type="number" value={s.fullBoardSupplement} onChange={(e) => set('fullBoardSupplement', +e.target.value)} /></div>
            </div>
          </div>

          {error && <div className="error">{error}</div>}
          {saved && <div style={{ color: 'var(--green)', marginBottom: 8 }}>Paramètres enregistrés ✓</div>}
          <button className="btn">Enregistrer</button>
        </form>
      </div>
    </>
  );
}
