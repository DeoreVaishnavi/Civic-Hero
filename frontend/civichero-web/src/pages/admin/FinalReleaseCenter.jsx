import { useCallback, useEffect, useState } from 'react';
import { finalReleaseApi } from '../../services/finalReleaseApi.js';

const statusClass = (status) => {
  if (status === 'Passed') return 'border-emerald-400/25 bg-emerald-400/10 text-emerald-200';
  if (status === 'Failed') return 'border-rose-400/25 bg-rose-400/10 text-rose-200';
  if (status === 'Skipped') return 'border-amber-400/25 bg-amber-400/10 text-amber-200';
  return 'border-slate-400/20 bg-white/5 text-slate-300';
};

export default function FinalReleaseCenter() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const load = useCallback(async () => {
    setError('');
    try { setData(await finalReleaseApi.readiness()); }
    catch (reason) { setError(reason.message || 'Unable to load final release evidence.'); }
  }, []);
  useEffect(() => { load(); }, [load]);

  return <section className="space-y-6 p-6">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-400">Phase 18</p><h2 className="mt-2 text-3xl font-black text-white">Final release centre</h2><p className="mt-2 max-w-3xl text-slate-400">Real evidence for testing, migrations, security, performance, Docker, UAT, backup recovery and release approval.</p></div>
      <button onClick={load} className="rounded-xl border border-white/15 px-4 py-2 font-bold text-white hover:bg-white/10">Refresh</button>
    </div>
    {error && <div className="rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-200">{error}</div>}
    {!data && !error && <div className="rounded-xl border border-white/10 bg-white/5 p-5 text-slate-400">Loading release evidence…</div>}
    {data && <>
      <div className={`rounded-2xl border p-5 ${data.approved ? 'border-emerald-400/30 bg-emerald-400/10' : 'border-amber-400/30 bg-amber-400/10'}`}>
        <div className="flex flex-wrap items-center justify-between gap-3"><div><p className={`text-xl font-black ${data.approved ? 'text-emerald-100' : 'text-amber-100'}`}>{data.status}</p><p className="mt-2 text-sm text-slate-300">{data.note}</p></div><span className={`rounded-full px-4 py-2 text-sm font-black ${data.approved ? 'bg-emerald-400 text-emerald-950' : 'bg-amber-300 text-amber-950'}`}>{data.approved ? 'APPROVED' : 'BLOCKED'}</span></div>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5"><Metric label="Release" value={data.releaseVersion} /><Metric label="Required gates" value={data.summary.totalRequired} /><Metric label="Passed" value={data.summary.passed} /><Metric label="Failed" value={data.summary.failed} /><Metric label="Skipped" value={data.summary.skipped} /></div>
      <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="grid gap-3 text-sm text-slate-300 md:grid-cols-2"><p><span className="font-bold text-white">Git commit:</span> {data.gitCommit}</p><p><span className="font-bold text-white">Generated:</span> {data.generatedAtUtc ? new Date(data.generatedAtUtc).toLocaleString() : 'Not generated'}</p><p><span className="font-bold text-white">Evidence:</span> {data.evidenceFile}</p><p><span className="font-bold text-white">Total gates:</span> {data.summary.totalGates}</p></div></div>
      <div className="grid gap-4 lg:grid-cols-2">{data.gates?.map((gate) => <article key={gate.key} className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex items-start justify-between gap-3"><div><h3 className="font-black text-white">{gate.name}</h3><p className="mt-1 text-xs uppercase tracking-wider text-slate-500">{gate.required ? 'Required' : 'Optional'} · {Math.round(gate.durationSeconds || 0)}s</p></div><span className={`rounded-full border px-3 py-1 text-xs font-black ${statusClass(gate.status)}`}>{gate.status}</span></div><p className="mt-4 text-sm text-slate-300">{gate.detail}</p>{gate.evidencePath && <p className="mt-3 break-all text-xs text-sky-300">{gate.evidencePath}</p>}</article>)}</div>
      {(!data.gates || data.gates.length === 0) && <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><h3 className="font-black text-white">No release run found</h3><p className="mt-2 text-sm text-slate-400">Run scripts/release/run-phase18-release-gates.ps1 to create actual evidence. Phase 18 remains blocked until every required gate passes.</p></div>}
      <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><h3 className="text-xl font-black text-white">Required evidence</h3><ul className="mt-4 grid gap-2 md:grid-cols-2">{data.requiredEvidence.map((item) => <li key={item} className="rounded-xl bg-slate-950/50 px-4 py-3 text-sm text-slate-300">{item}</li>)}</ul></div>
    </>}
  </section>;
}
function Metric({ label, value }) { return <article className="rounded-2xl border border-white/10 bg-white/5 p-5"><p className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-2 break-words text-2xl font-black text-white">{value}</p></article>; }
