import { useCallback, useEffect, useState } from 'react';
import { launchApi } from '../../services/launchApi.js';

export default function LaunchCenter() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    setError('');
    try { setData(await launchApi.readiness()); }
    catch (reason) { setError(reason.message || 'Unable to load launch and handover readiness.'); }
  }, []);

  useEffect(() => { load(); }, [load]);

  return <section className="space-y-6 p-6">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p className="text-sm font-semibold uppercase tracking-[0.18em] text-violet-400">Phase 16 completion</p>
        <h2 className="mt-2 text-3xl font-black text-white">Launch & handover centre</h2>
        <p className="mt-2 max-w-3xl text-slate-400">Technical documentation, training, go-live evidence, maintenance ownership and academic project handover.</p>
      </div>
      <button onClick={load} className="rounded-xl border border-white/15 px-4 py-2 text-sm font-bold text-white hover:bg-white/10">Refresh</button>
    </div>

    {error && <div className="rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-200">{error}</div>}
    {!data && !error && <div className="rounded-xl border border-white/10 bg-white/5 p-5 text-slate-400">Loading Phase 16 artifacts…</div>}

    {data && <>
      <div className={`rounded-2xl border p-5 ${data.preparationScore === 100 ? 'border-emerald-400/30 bg-emerald-400/10' : 'border-amber-400/30 bg-amber-400/10'}`}>
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="text-sm font-black uppercase tracking-wider text-white/70">{data.status}</p>
            <p className="mt-2 text-4xl font-black text-white">{data.preparationScore}%</p>
            <p className="mt-1 text-sm text-white/70">{data.preparedArtifacts} of {data.totalArtifacts} documentation artifacts present</p>
          </div>
          <div className="text-right text-sm text-white/70">
            <p>Environment: <strong className="text-white">{data.environment}</strong></p>
            <p>Stakeholder approval: <strong className="text-amber-200">Not automatically claimed</strong></p>
          </div>
        </div>
      </div>

      <div className="grid gap-4 xl:grid-cols-2">
        {data.artifacts.map((item) => <article key={item.key} className="rounded-2xl border border-white/10 bg-white/5 p-5">
          <div className="flex items-start justify-between gap-3">
            <div><h3 className="font-black text-white">{item.name}</h3><p className="mt-1 text-xs text-slate-500">{item.path}</p></div>
            <span className={`rounded-full px-3 py-1 text-xs font-black ${item.prepared ? 'bg-emerald-400/10 text-emerald-200' : 'bg-amber-400/10 text-amber-100'}`}>{item.prepared ? 'Prepared' : 'Missing'}</span>
          </div>
          <p className="mt-3 text-sm text-slate-400">Audience: {item.audience}</p>
        </article>)}
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <Panel title="Go-live gates">
          <div className="space-y-4">{data.goLiveGates.map((gate) => <div key={gate.key} className="rounded-xl border border-amber-400/20 bg-amber-400/5 p-4">
            <div className="flex items-start justify-between gap-3"><p className="font-bold text-white">{gate.name}</p><span className="rounded-full bg-amber-400/10 px-2 py-1 text-xs font-bold text-amber-200">{gate.status}</span></div>
            <p className="mt-2 text-sm leading-6 text-slate-400">{gate.evidence}</p>
          </div>)}</div>
        </Panel>
        <Panel title="Handover commands">
          <div className="space-y-3">{data.commands.map((command) => <code key={command} className="block overflow-x-auto rounded-xl bg-slate-950 p-3 text-xs text-sky-200">{command}</code>)}</div>
          <div className="mt-5 rounded-xl border border-violet-400/20 bg-violet-400/10 p-4 text-sm leading-6 text-violet-100">Run the scripts from the project root. They create evidence under <code>artifacts/phase16</code> without changing production data.</div>
        </Panel>
      </div>

      <div className="rounded-2xl border border-sky-400/20 bg-sky-400/10 p-5 text-sm leading-6 text-sky-100">{data.signOff.note}</div>
      <div className="rounded-2xl border border-white/10 bg-white/5 p-5 text-sm leading-6 text-slate-300">{data.note}</div>
    </>}
  </section>;
}

function Panel({ title, children }) {
  return <article className="rounded-2xl border border-white/10 bg-white/5 p-5"><h3 className="text-xl font-black text-white">{title}</h3><div className="mt-4">{children}</div></article>;
}
