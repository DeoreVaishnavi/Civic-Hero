import { useEffect, useMemo, useState } from 'react';
import { aiApi } from '../../services/aiApi.js';
import { complaintApi } from '../../services/complaintApi.js';

const card = 'rounded-2xl border border-white/10 bg-white/[0.04] p-5 shadow-xl shadow-black/10';
const input = 'w-full rounded-xl border border-white/10 bg-slate-950 px-3 py-2.5 text-sm text-white outline-none focus:border-sky-400';

export default function SupervisorAiReviewQueue() {
  const [queue, setQueue] = useState([]);
  const [metadata, setMetadata] = useState({ categories: [], departments: [], wards: [] });
  const [busy, setBusy] = useState(null);
  const [error, setError] = useState('');
  const [routingItem, setRoutingItem] = useState(null);
  const [routing, setRouting] = useState({ category: '', departmentId: '', wardId: '', notes: '' });

  const load = async () => {
    setError('');
    try {
      const [items, meta] = await Promise.all([aiApi.reviewQueue(), complaintApi.metadata()]);
      setQueue(items ?? []);
      setMetadata(meta ?? { categories: [], departments: [], wards: [] });
    } catch (reason) {
      setError(reason?.errors?.join(' ') || reason?.message || 'Unable to load the AI review queue.');
    }
  };

  useEffect(() => { load(); }, []);

  const summary = useMemo(() => ({
    total: queue.length,
    fraud: queue.filter((item) => item.fraudScore >= 0.65).length,
    duplicate: queue.filter((item) => item.duplicateScore >= 0.55).length,
    routing: queue.filter((item) => item.predictedDepartmentId && item.predictedDepartmentId !== item.departmentId).length,
  }), [queue]);

  const submitDecision = async (item, payload) => {
    setBusy(item.complaintId);
    setError('');
    try {
      await aiApi.decide(item.complaintId, payload);
      setRoutingItem(null);
      await load();
    } catch (reason) {
      setError(reason?.errors?.join(' ') || reason?.message || 'The review decision could not be saved.');
    } finally {
      setBusy(null);
    }
  };

  const askNotes = (label) => {
    const notes = window.prompt(label, '')?.trim() ?? '';
    if (notes.length < 5) {
      if (notes) setError('Please enter at least 5 characters for the decision reason.');
      return null;
    }
    return notes;
  };

  const clearValid = (item) => {
    const notes = askNotes('Why is this complaint valid?');
    if (notes) submitDecision(item, { decision: 'Clear', notes });
  };

  const rejectInvalid = (item) => {
    const notes = askNotes('Reason for rejecting this complaint as invalid:');
    if (notes && window.confirm('Reject this complaint after human review?'))
      submitDecision(item, { decision: 'RejectInvalid', notes });
  };

  const escalate = (item) => {
    const notes = askNotes('Why does this complaint require Admin review?');
    if (notes) submitDecision(item, { decision: 'Escalate', notes });
  };

  const merge = (item) => {
    const suggested = item.duplicateComplaintId ? String(item.duplicateComplaintId) : '';
    const raw = window.prompt('Canonical complaint ID:', suggested);
    if (!raw) return;
    const mergeIntoComplaintId = Number(raw);
    if (!Number.isInteger(mergeIntoComplaintId) || mergeIntoComplaintId <= 0 || mergeIntoComplaintId === item.complaintId) {
      setError('Enter a different valid complaint ID for the merge.');
      return;
    }
    const notes = askNotes('Reason for merging these complaints:');
    if (notes) submitDecision(item, { decision: 'Merge', notes, mergeIntoComplaintId });
  };

  const openRouting = (item) => {
    setRoutingItem(item);
    setRouting({
      category: item.currentCategory || item.predictedCategory || '',
      departmentId: String(item.departmentId),
      wardId: String(item.wardId),
      notes: '',
    });
  };

  const scopedWards = useMemo(() => metadata.wards?.filter((ward) =>
    String(ward.departmentId) === String(routing.departmentId)) ?? [], [metadata.wards, routing.departmentId]);

  const saveRouting = async (event) => {
    event.preventDefault();
    if (!routingItem) return;
    if (routing.notes.trim().length < 5) {
      setError('Routing override reason must contain at least 5 characters.');
      return;
    }
    await submitDecision(routingItem, {
      decision: 'OverrideRouting',
      notes: routing.notes.trim(),
      category: routing.category,
      departmentId: Number(routing.departmentId),
      wardId: Number(routing.wardId),
    });
  };

  return <section className="p-6 lg:p-10">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p className="text-sm font-bold uppercase tracking-wider text-violet-300">Human-in-the-loop AI</p>
        <h2 className="mt-2 text-3xl font-black text-white">AI triage review queue</h2>
        <p className="mt-2 max-w-4xl text-slate-400">Review only complaints inside your assigned department and ward. AI indicators are advisory; every decision is recorded in the complaint timeline and audit log.</p>
      </div>
      <button onClick={load} className="rounded-xl border border-white/10 px-4 py-2.5 font-bold text-slate-200 hover:bg-white/5">Refresh queue</button>
    </div>

    {error && <div className="mt-5 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}

    <div className="mt-7 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <Metric label="Pending review" value={summary.total} />
      <Metric label="High fraud risk" value={summary.fraud} />
      <Metric label="Possible duplicates" value={summary.duplicate} />
      <Metric label="Routing mismatch" value={summary.routing} />
    </div>

    <div className="mt-7 space-y-5">
      {queue.length === 0 && <div className={`${card} text-center text-slate-400`}>No AI reviews are pending inside your operational scope.</div>}
      {queue.map((item) => <article key={item.complaintId} className={card}>
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="font-mono text-xs text-sky-300">CH-{String(item.complaintId).padStart(6, '0')}</p>
            <h3 className="mt-1 text-xl font-black text-white">{item.title}</h3>
            <p className="mt-1 text-sm text-slate-400">{item.citizenName} · {item.status}</p>
          </div>
          <span className={`rounded-full px-3 py-1 text-xs font-black ${item.fraudScore >= 0.65 ? 'bg-rose-400/15 text-rose-200' : 'bg-amber-400/15 text-amber-200'}`}>
            {item.fraudVerdict} · {Math.round(item.fraudScore * 100)}%
          </span>
        </div>

        <div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
          <Info label="Current routing" value={`${item.departmentName} / ${item.wardName}`} />
          <Info label="Current category" value={item.currentCategory} />
          <Info label="AI suggestion" value={`${item.predictedCategory}${item.predictedDepartmentName ? ` / ${item.predictedDepartmentName}` : ''}`} />
          <Info label="Duplicate indicator" value={`${item.duplicateStatus} · ${Math.round(item.duplicateScore * 100)}%${item.duplicateComplaintId ? ` · CH-${String(item.duplicateComplaintId).padStart(6, '0')}` : ''}`} />
        </div>

        <p className="mt-4 rounded-xl bg-slate-950/60 p-4 text-sm leading-6 text-slate-300">{item.reasoning}</p>

        <div className="mt-5 flex flex-wrap gap-2">
          <Action disabled={busy === item.complaintId} onClick={() => clearValid(item)} kind="success">Clear valid</Action>
          <Action disabled={busy === item.complaintId} onClick={() => openRouting(item)}>Override routing</Action>
          <Action disabled={busy === item.complaintId} onClick={() => merge(item)}>Merge duplicate</Action>
          <Action disabled={busy === item.complaintId} onClick={() => rejectInvalid(item)} kind="danger">Reject invalid</Action>
          <Action disabled={busy === item.complaintId} onClick={() => escalate(item)} kind="warning">Escalate to Admin</Action>
          <Action disabled={busy === item.complaintId} onClick={() => submitDecision(item, { decision: 'Reanalyze', notes: 'Supervisor requested a fresh AI analysis.' })}>Reanalyse</Action>
        </div>

        {routingItem?.complaintId === item.complaintId && <form onSubmit={saveRouting} className="mt-5 rounded-2xl border border-sky-400/20 bg-sky-400/[0.05] p-5">
          <h4 className="font-black text-white">Routing override within your scope</h4>
          <p className="mt-1 text-sm text-slate-400">A Supervisor cannot route a complaint outside the assigned department or ward. Use escalation when another department must decide.</p>
          <div className="mt-4 grid gap-4 md:grid-cols-3">
            <label className="text-sm font-bold text-slate-300">Category
              <select className={`${input} mt-2`} value={routing.category} onChange={(event) => setRouting({ ...routing, category: event.target.value })}>
                {[...new Set([routing.category, ...(metadata.categories ?? [])].filter(Boolean))].map((category) => <option key={category} value={category}>{category}</option>)}
              </select>
            </label>
            <label className="text-sm font-bold text-slate-300">Department
              <select className={`${input} mt-2 opacity-75`} value={routing.departmentId} disabled>
                {(metadata.departments ?? []).filter((department) => String(department.id) === String(routing.departmentId)).map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
              </select>
            </label>
            <label className="text-sm font-bold text-slate-300">Ward
              <select className={`${input} mt-2`} value={routing.wardId} onChange={(event) => setRouting({ ...routing, wardId: event.target.value })}>
                {scopedWards.map((ward) => <option key={ward.id} value={ward.id}>{ward.name}</option>)}
              </select>
            </label>
          </div>
          <label className="mt-4 block text-sm font-bold text-slate-300">Reason
            <textarea className={`${input} mt-2`} rows="3" value={routing.notes} onChange={(event) => setRouting({ ...routing, notes: event.target.value })} placeholder="Explain why the AI routing suggestion is being overridden." />
          </label>
          <div className="mt-4 flex gap-2">
            <button type="submit" disabled={busy === item.complaintId} className="rounded-xl bg-sky-500 px-4 py-2 font-black text-white disabled:opacity-50">Save routing decision</button>
            <button type="button" onClick={() => setRoutingItem(null)} className="rounded-xl border border-white/10 px-4 py-2 font-bold text-slate-300">Cancel</button>
          </div>
        </form>}
      </article>)}
    </div>
  </section>;
}

function Metric({ label, value }) {
  return <div className={card}><p className="text-xs font-bold uppercase tracking-wider text-slate-400">{label}</p><p className="mt-2 text-3xl font-black text-white">{value}</p></div>;
}

function Info({ label, value }) {
  return <div className="rounded-xl border border-white/10 bg-slate-950/60 p-3"><p className="text-xs font-bold uppercase text-slate-500">{label}</p><p className="mt-1 text-sm font-bold text-slate-100">{value || '—'}</p></div>;
}

function Action({ children, onClick, disabled, kind = 'normal' }) {
  const classes = {
    normal: 'border border-white/10 bg-white/5 text-slate-200 hover:bg-white/10',
    success: 'bg-emerald-500 text-white hover:bg-emerald-400',
    danger: 'bg-rose-500 text-white hover:bg-rose-400',
    warning: 'bg-amber-500 text-slate-950 hover:bg-amber-400',
  };
  return <button disabled={disabled} onClick={onClick} className={`rounded-xl px-3 py-2 text-sm font-black disabled:opacity-50 ${classes[kind]}`}>{children}</button>;
}
