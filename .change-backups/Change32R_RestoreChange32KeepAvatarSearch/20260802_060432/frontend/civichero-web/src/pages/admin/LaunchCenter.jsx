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

  useEffect(() => { void load(); }, [load]);

  return <section className="space-y-6 p-6">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p className="text-sm font-semibold uppercase tracking-[0.18em] text-violet-700">Phase 16 completion</p>
        <h2 className="mt-2 text-3xl font-black text-slate-900">Launch & handover centre</h2>
        <p className="mt-2 max-w-3xl text-slate-600">Technical documentation, training, go-live evidence, maintenance ownership and academic project handover.</p>
      </div>
      <button onClick={load} className="cv-btn-secondary">Refresh</button>
    </div>

    {error && <div className="cv-alert cv-alert-error">{error}</div>}
    {!data && !error && <div className="cv-card rounded-xl border p-5 text-slate-600">Loading Phase 16 artifacts…</div>}

    {data && <>
      <div className={`cv-alert rounded-2xl p-5 ${data.preparationScore === 100 ? 'cv-alert-success' : 'cv-alert-warning'}`}>
        <div className="flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="text-sm font-black uppercase tracking-wider">{data.status}</p>
            <p className="mt-2 text-4xl font-black">{data.preparationScore}%</p>
            <p className="mt-1 text-sm">{data.preparedArtifacts} of {data.totalArtifacts} documentation artifacts present</p>
          </div>
          <div className="text-right text-sm">
            <p>Environment: <strong>{data.environment}</strong></p>
            <p>Stakeholder approval: <strong>Not automatically claimed</strong></p>
          </div>
        </div>
      </div>

      <div className="grid gap-4 xl:grid-cols-2">
        {data.artifacts.map((item) => <article key={item.key} className="cv-card rounded-2xl border p-5">
          <div className="flex items-start justify-between gap-3">
            <div><h3 className="font-black text-slate-900">{item.name}</h3><p className="mt-1 text-xs text-slate-600">{item.path}</p></div>
            <span className={item.prepared ? 'cv-status-green' : 'cv-status-amber'}>{item.prepared ? 'Prepared' : 'Missing'}</span>
          </div>
          <p className="mt-3 text-sm text-slate-700">Audience: {item.audience}</p>
        </article>)}
      </div>

      <div className="grid gap-6 xl:grid-cols-2">
        <Panel title="Go-live gates">
          <div className="space-y-4">{data.goLiveGates.map((gate) => <div key={gate.key} className="cv-alert cv-alert-warning rounded-xl p-4">
            <div className="flex items-start justify-between gap-3"><p className="font-bold">{gate.name}</p><span className="cv-status-amber">{gate.status}</span></div>
            <p className="mt-2 text-sm leading-6">{gate.evidence}</p>
          </div>)}</div>
        </Panel>
        <Panel title="Handover commands">
          <div className="space-y-3">{data.commands.map((command) => <code key={command} className="cv-dark-panel block overflow-x-auto rounded-xl p-3 text-xs text-sky-100">{command}</code>)}</div>
          <div className="cv-alert cv-alert-info mt-5 rounded-xl p-4 text-sm leading-6">Run the scripts from the project root. They create evidence under <code>artifacts/phase16</code> without changing production data.</div>
        </Panel>
      </div>

      <div className="cv-alert cv-alert-info rounded-2xl p-5 text-sm leading-6">{data.signOff.note}</div>
      <div className="cv-card rounded-2xl border p-5 text-sm leading-6 text-slate-700">{data.note}</div>
    </>}
  </section>;
}

function Panel({ title, children }) {
  return <article className="cv-card rounded-2xl border p-5"><h3 className="text-xl font-black text-slate-900">{title}</h3><div className="mt-4">{children}</div></article>;
}
