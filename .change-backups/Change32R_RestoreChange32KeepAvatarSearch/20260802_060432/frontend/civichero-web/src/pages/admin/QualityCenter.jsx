import { useCallback, useEffect, useState } from 'react';
import { qualityApi } from '../../services/qualityApi.js';

export default function QualityCenter() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setError('');
    try { setData(await qualityApi.readiness()); }
    catch (reason) { setError(reason.message || 'Unable to load quality readiness.'); }
  }, []);

  useEffect(() => { void load(); }, [load]);

  return <section className="space-y-6 p-6">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-blue-700">Release assurance</p><h2 className="mt-2 text-3xl font-black text-slate-900">Quality centre</h2><p className="mt-2 max-w-3xl text-slate-600">Testing inventory, coverage targets and release gates for CivicHero Phase 14.</p></div>
      <button onClick={load} className="cv-btn-secondary">Refresh</button>
    </div>

    {error && <div className="cv-alert cv-alert-error">{error}</div>}
    {!data && !error && <div className="cv-card rounded-xl border p-5 text-slate-600">Loading quality configuration…</div>}

    {data && <>
      <div className="cv-alert cv-alert-warning rounded-2xl p-5"><p className="font-black">{data.readiness}</p><p className="mt-2 text-sm">{data.note}</p></div>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        {Object.entries(data.coverageTargets).map(([key, value]) => <Metric key={key} label={readable(key)} value={`${value}%`} />)}
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        {data.categories.map((item) => <article key={item.key} className="cv-card rounded-2xl border p-5"><div className="flex items-start justify-between gap-3"><div><h3 className="text-lg font-black text-slate-900">{item.name}</h3><p className="mt-1 text-sm text-slate-600">{item.tool}</p></div><span className="cv-status-blue">{item.status}</span></div><p className="mt-4 text-sm text-slate-700">Target: {item.target}</p></article>)}
      </div>
      <div className="cv-card rounded-2xl border p-5"><h3 className="text-xl font-black text-slate-900">Release gates</h3><ol className="mt-4 space-y-3">{data.releaseGates.map((gate, index) => <li key={gate} className="flex gap-3 text-sm text-slate-700"><span className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-blue-100 text-xs font-black text-blue-800">{index + 1}</span><span>{gate}</span></li>)}</ol></div>
    </>}
  </section>;
}

function Metric({ label, value }) { return <article className="cv-card rounded-2xl border p-5"><p className="text-xs font-semibold uppercase tracking-wider text-slate-600">{label}</p><p className="mt-2 text-3xl font-black text-slate-900">{value}</p></article>; }
function readable(value) { return value.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^./, (letter) => letter.toUpperCase()); }
