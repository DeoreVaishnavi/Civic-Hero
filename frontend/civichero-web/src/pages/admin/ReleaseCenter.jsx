import { useCallback, useEffect, useState } from 'react';
import { releaseApi } from '../../services/releaseApi.js';

export default function ReleaseCenter() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setError('');
    try { setData(await releaseApi.readiness()); }
    catch (reason) { setError(reason.message || 'Unable to load production readiness.'); }
  }, []);

  useEffect(() => { load(); }, [load]);

  return <section className="space-y-6 p-6">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p className="text-sm font-semibold uppercase tracking-[0.18em] text-emerald-400">Phase 15 operations</p>
        <h2 className="mt-2 text-3xl font-black text-white">Release centre</h2>
        <p className="mt-2 max-w-3xl text-slate-400">Production configuration checks, deployment artifacts and go-live commands without exposing secret values.</p>
      </div>
      <button onClick={load} className="rounded-xl border border-white/15 px-4 py-2 text-sm font-bold text-white hover:bg-white/10">Refresh</button>
    </div>

    {error && <div className="rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-200">{error}</div>}
    {!data && !error && <div className="rounded-xl border border-white/10 bg-white/5 p-5 text-slate-400">Checking deployment readiness…</div>}

    {data && <>
      <div className={`rounded-2xl border p-5 ${data.readinessScore === 100 ? 'border-emerald-400/30 bg-emerald-400/10' : 'border-amber-400/30 bg-amber-400/10'}`}>
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div><p className="text-sm font-black uppercase tracking-wider text-white/70">{data.status}</p><p className="mt-2 text-4xl font-black text-white">{data.readinessScore}%</p><p className="mt-1 text-sm text-white/70">{data.readyChecks} of {data.totalChecks} configuration checks ready</p></div>
          <div className="text-right text-sm text-white/70"><p>Environment: <strong className="text-white">{data.runtime.environment}</strong></p><p>Version: <strong className="text-white">{data.runtime.version}</strong></p><p>Container: <strong className="text-white">{data.runtime.containerized ? 'Yes' : 'No'}</strong></p></div>
        </div>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        {data.checks.map((check) => <article key={check.key} className="rounded-2xl border border-white/10 bg-white/5 p-5">
          <div className="flex items-start justify-between gap-3"><h3 className="font-black text-white">{check.name}</h3><span className={`rounded-full px-3 py-1 text-xs font-black ${check.ready ? 'bg-emerald-400/10 text-emerald-200' : 'bg-amber-400/10 text-amber-100'}`}>{check.status}</span></div>
          <p className="mt-3 text-sm leading-6 text-slate-400">{check.detail}</p>
        </article>)}
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <Panel title="Deployment artifacts"><ul className="space-y-3">{data.artifacts.map((item) => <li key={item} className="flex gap-3 text-sm text-slate-300"><span className="text-emerald-400">✓</span><span>{item}</span></li>)}</ul></Panel>
        <Panel title="Release commands"><div className="space-y-3">{data.deploymentCommands.map((command) => <code key={command} className="block overflow-x-auto rounded-xl bg-slate-950 p-3 text-xs text-sky-200">{command}</code>)}</div></Panel>
      </div>

      <div className="rounded-2xl border border-sky-400/20 bg-sky-400/10 p-5 text-sm leading-6 text-sky-100">{data.note}</div>
    </>}
  </section>;
}

function Panel({ title, children }) { return <article className="rounded-2xl border border-white/10 bg-white/5 p-5"><h3 className="text-xl font-black text-white">{title}</h3><div className="mt-4">{children}</div></article>; }
