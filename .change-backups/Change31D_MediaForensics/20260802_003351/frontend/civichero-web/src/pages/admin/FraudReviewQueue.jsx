import { useEffect, useMemo, useState } from 'react';
import { aiApi } from '../../services/aiApi.js';

const card = 'rounded-2xl border border-white/10 bg-white/5 p-5';
const input = 'w-full rounded-xl border border-white/10 bg-slate-950 px-4 py-3 text-white outline-none focus:border-sky-400';

export default function FraudReviewQueue() {
  const [queue, setQueue] = useState([]);
  const [metrics, setMetrics] = useState(null);
  const [hotspots, setHotspots] = useState([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(null);
  const [sample, setSample] = useState({ title: 'Large pothole near school gate', description: 'A deep pothole is causing accidents during school hours.', category: 'Pothole', latitude: 19.076, longitude: 72.8777 });
  const [sandbox, setSandbox] = useState(null);

  const load = async () => {
    setError('');
    try {
      const [reviewItems, metricData, hotspotData] = await Promise.all([aiApi.reviewQueue(), aiApi.metrics(), aiApi.hotspots(30)]);
      setQueue(reviewItems ?? []);
      setMetrics(metricData);
      setHotspots(hotspotData ?? []);
    } catch (reason) { setError(reason.message); }
  };

  useEffect(() => { load(); }, []);

  const decide = async (complaintId, decision) => {
    const notes = window.prompt(`Notes for ${decision}:`, '') ?? '';
    let mergeIntoComplaintId = null;
    if (decision === 'Merge') {
      const raw = window.prompt('Parent complaint ID:');
      if (!raw) return;
      mergeIntoComplaintId = Number(raw);
    }
    setBusy(complaintId);
    setError('');
    try { await aiApi.decide(complaintId, { decision, notes, mergeIntoComplaintId }); await load(); }
    catch (reason) { setError(reason.message); }
    finally { setBusy(null); }
  };

  const runSandbox = async () => {
    setBusy('sandbox'); setError('');
    try {
      const [classification, duplicate, fraud, priority] = await Promise.all([
        aiApi.classify(sample), aiApi.duplicateCheck(sample), aiApi.fraudCheck(sample), aiApi.priority(sample),
      ]);
      setSandbox({ classification, duplicate, fraud, priority });
    } catch (reason) { setError(reason.message); }
    finally { setBusy(null); }
  };

  const topHotspots = useMemo(() => hotspots.slice(0, 8), [hotspots]);

  return <section className="p-6 lg:p-10">
    <p className="text-sm font-bold uppercase tracking-wider text-violet-300">Intelligence & governance</p>
    <h2 className="mt-2 text-3xl font-black text-white">AI triage command centre</h2>
    <p className="mt-2 max-w-4xl text-slate-400">Review advisory classification, duplicate, fraud and priority decisions before they affect municipal workflow.</p>
    {error && <div className="mt-5 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}

    <div className="mt-7 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
      <Metric label="Triaged" value={metrics?.triagedComplaints ?? '—'} />
      <Metric label="Manual review" value={metrics?.manualReviewPending ?? '—'} />
      <Metric label="Fraud queue" value={metrics?.fraudReviewCount ?? '—'} />
      <Metric label="Merged" value={metrics?.confirmedDuplicates ?? '—'} />
      <Metric label="Confidence" value={metrics ? `${Math.round(metrics.averageConfidence * 100)}%` : '—'} helper={metrics ? `${metrics.activeProvider} · ${metrics.model}` : ''} />
    </div>

    <div className="mt-8 grid gap-6 2xl:grid-cols-[1.4fr_.8fr]">
      <div className={card}>
        <div className="flex items-center justify-between"><div><h3 className="text-xl font-black text-white">Manual review queue</h3><p className="text-sm text-slate-400">Only uncertain or high-risk analyses appear here.</p></div><button onClick={load} className="rounded-lg border border-white/10 px-3 py-2 text-sm font-bold text-slate-200">Refresh</button></div>
        <div className="mt-5 space-y-4">
          {queue.length === 0 && <Empty text="No AI decisions currently require manual review." />}
          {queue.map((item) => <article key={item.complaintId} className="rounded-xl border border-white/10 bg-slate-950/70 p-5">
            <div className="flex flex-wrap items-start justify-between gap-3"><div><p className="font-mono text-xs text-sky-300">CH-{String(item.complaintId).padStart(6,'0')}</p><h4 className="mt-1 text-lg font-black text-white">{item.title}</h4><p className="text-sm text-slate-400">{item.citizenName} · {item.status}</p></div><span className={`rounded-full px-3 py-1 text-xs font-black ${item.fraudScore >= .65 ? 'bg-rose-400/15 text-rose-200' : 'bg-amber-400/15 text-amber-200'}`}>{item.fraudVerdict} {Math.round(item.fraudScore * 100)}%</span></div>
            <div className="mt-4 grid gap-3 sm:grid-cols-3"><Mini label="Category" value={item.predictedCategory} /><Mini label="Priority" value={item.predictedPriority} /><Mini label="Duplicate" value={`${item.duplicateStatus} · ${Math.round(item.duplicateScore * 100)}%`} /></div>
            <p className="mt-4 text-sm leading-6 text-slate-300">{item.reasoning}</p>
            {item.escalatedToAdmin && <div className="mt-4 rounded-xl border border-amber-400/30 bg-amber-400/10 p-3 text-sm text-amber-100"><span className="font-black">Escalated by Supervisor:</span> {item.escalationNotes || 'Administrator decision required.'}</div>}
            <div className="mt-4 flex flex-wrap gap-2"><Action disabled={busy===item.complaintId} onClick={()=>decide(item.complaintId,'Clear')}>Clear</Action><Action disabled={busy===item.complaintId} onClick={()=>decide(item.complaintId,'Merge')}>Merge</Action><Action danger disabled={busy===item.complaintId} onClick={()=>decide(item.complaintId,'ConfirmFraud')}>Confirm fraud</Action><Action disabled={busy===item.complaintId} onClick={()=>decide(item.complaintId,'Reanalyze')}>Reanalyse</Action></div>
          </article>)}
        </div>
      </div>

      <div className="space-y-6">
        <div className={card}><h3 className="text-xl font-black text-white">30-day hotspots</h3><p className="text-sm text-slate-400">Grid-based risk clusters generated from complaint density and severity.</p><div className="mt-4 space-y-3">{topHotspots.length===0&&<Empty text="No hotspot data yet." />}{topHotspots.map((spot,index)=><div key={`${spot.latitude}-${spot.longitude}`} className="flex items-center justify-between rounded-xl bg-slate-950/70 p-3"><div><p className="font-bold text-white">#{index+1} {spot.dominantCategory}</p><p className="text-xs text-slate-400">{spot.latitude}, {spot.longitude} · {spot.complaintCount} complaints</p></div><span className="text-sm font-black text-amber-300">{spot.riskLevel} {Math.round(spot.riskScore*100)}%</span></div>)}</div></div>
      </div>
    </div>

    <div className={`${card} mt-8`}><h3 className="text-xl font-black text-white">AI sandbox</h3><p className="text-sm text-slate-400">Test classification, duplicate, fraud and priority endpoints without creating a complaint.</p><div className="mt-5 grid gap-4 lg:grid-cols-2"><label className="text-sm font-bold text-slate-300">Title<input className={`${input} mt-2`} value={sample.title} onChange={e=>setSample({...sample,title:e.target.value})}/></label><label className="text-sm font-bold text-slate-300">Category<input className={`${input} mt-2`} value={sample.category} onChange={e=>setSample({...sample,category:e.target.value})}/></label><label className="text-sm font-bold text-slate-300 lg:col-span-2">Description<textarea rows="4" className={`${input} mt-2`} value={sample.description} onChange={e=>setSample({...sample,description:e.target.value})}/></label></div><button onClick={runSandbox} disabled={busy==='sandbox'} className="mt-4 rounded-xl bg-violet-500 px-5 py-3 font-black text-white hover:bg-violet-400 disabled:opacity-50">{busy==='sandbox'?'Analysing…':'Run AI analysis'}</button>{sandbox&&<div className="mt-5 grid gap-3 md:grid-cols-2 xl:grid-cols-4"><Mini label="Classification" value={`${sandbox.classification.category} · ${Math.round(sandbox.classification.confidence*100)}%`} /><Mini label="Duplicate" value={`${sandbox.duplicate.status} · ${Math.round(sandbox.duplicate.score*100)}%`} /><Mini label="Fraud" value={`${sandbox.fraud.verdict} · ${Math.round(sandbox.fraud.score*100)}%`} /><Mini label="Priority" value={`${sandbox.priority.priority} · ${Math.round(sandbox.priority.score*100)}%`} /></div>}</div>
  </section>;
}
function Metric({label,value,helper}){return <div className={card}><p className="text-xs font-bold uppercase tracking-wider text-slate-400">{label}</p><p className="mt-2 text-3xl font-black text-white">{value}</p>{helper&&<p className="mt-1 truncate text-xs text-slate-500">{helper}</p>}</div>}
function Mini({label,value}){return <div className="rounded-xl border border-white/10 bg-white/5 p-3"><p className="text-xs font-bold uppercase text-slate-500">{label}</p><p className="mt-1 font-bold text-slate-100">{value}</p></div>}
function Action({children,onClick,danger,disabled}){return <button disabled={disabled} onClick={onClick} className={`rounded-lg px-3 py-2 text-sm font-black disabled:opacity-50 ${danger?'bg-rose-500 text-white':'border border-white/10 bg-white/5 text-slate-200 hover:bg-white/10'}`}>{children}</button>}
function Empty({text}){return <div className="rounded-xl border border-dashed border-white/10 p-6 text-center text-sm text-slate-500">{text}</div>}
