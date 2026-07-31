import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import axiosInstance from '../../api/axiosInstance.js';
const unwrap = (response) => response.data?.data ?? response.data;

export default function DisputePage() {
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    axiosInstance.get('/disputes/mine').then((response) => {
      const data = unwrap(response);
      const next = data.items ?? data ?? [];
      setItems(next);
      setSelectedId(next[0]?.id ?? null);
    }).catch((reason) => setError(reason.message || 'Unable to load disputes.'));
  }, []);

  const selected = useMemo(() => items.find((item) => item.id === selectedId) || items[0], [items, selectedId]);
  const pending = items.filter((item) => String(item.status).toLowerCase().includes('pending')).length;
  const resolved = items.length - pending;

  return (
    <section className="page-wrap">
      <div className="page-title-row"><div><p className="section-kicker">Citizen review and escalation</p><h2>Disputes and appeals</h2><p>Review submitted disputes, supporting evidence and supervisor decisions.</p></div><Link to="/citizen/complaints" className="button outline">View my complaints</Link></div>
      {error && <div className="alert error">{error}</div>}

      <div className="stat-grid" style={{ gridTemplateColumns: 'repeat(3,minmax(0,1fr))' }}><div className="stat-card"><span className="stat-icon">!</span><small>Total disputes</small><strong>{items.length}</strong><p>All dispute and appeal records</p></div><div className="stat-card amber"><span className="stat-icon">◷</span><small>Pending review</small><strong>{pending}</strong><p>Waiting for supervisor decision</p></div><div className="stat-card green"><span className="stat-icon">✓</span><small>Decision recorded</small><strong>{resolved}</strong><p>Resolved or closed cases</p></div></div>

      <div className="mt-6 grid gap-6 xl:grid-cols-[.72fr_1.28fr]">
        <section className="overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-sm"><div className="border-b border-slate-200 p-5"><h3 className="text-lg font-black text-slate-900">My dispute list</h3></div><div className="divide-y divide-slate-100">{items.map((item) => <button type="button" key={item.id} onClick={() => setSelectedId(item.id)} className={`w-full p-5 text-left transition hover:bg-blue-50/50 ${selected?.id === item.id ? 'bg-blue-50' : ''}`}><div className="flex items-start justify-between gap-3"><div><h4 className="font-black text-slate-900">{item.complaintReference ?? `Complaint ${item.complaintId}`}</h4><p className="mt-2 line-clamp-2 text-sm leading-6 text-slate-500">{item.reason ?? item.description ?? 'Citizen requested a review of the submitted resolution.'}</p></div><span className="status-pill amber">{item.status}</span></div></button>)}{items.length === 0 && <div className="p-10 text-center text-sm text-slate-500">You have no dispute records.</div>}</div></section>

        <section className="overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-sm">
          {selected ? <><div className="border-b border-slate-200 bg-gradient-to-r from-slate-50 to-blue-50 p-6"><p className="text-xs font-black uppercase tracking-[.18em] text-blue-600">Selected dispute</p><div className="mt-3 flex flex-wrap items-center justify-between gap-4"><div><h3 className="text-2xl font-black text-slate-950">{selected.complaintReference ?? `Complaint ${selected.complaintId}`}</h3><p className="mt-2 text-sm text-slate-500">Status: {selected.status}</p></div><span className="status-pill red">Under review</span></div></div>
            <div className="p-6"><div className="grid gap-4 sm:grid-cols-3"><Evidence label="Before" icon="📷" tone="from-slate-200 to-slate-300" /><Evidence label="Officer evidence" icon="✓" tone="from-blue-100 to-cyan-200" /><Evidence label="Citizen proof" icon="!" tone="from-amber-100 to-rose-200" /></div><div className="mt-6 rounded-2xl border border-slate-200 p-5"><h4 className="font-black text-slate-900">Citizen reason</h4><p className="mt-3 text-sm leading-7 text-slate-600">{selected.reason ?? selected.description ?? 'The submitted resolution did not fully address the reported issue.'}</p></div><div className="mt-6 grid gap-4 sm:grid-cols-3"><Detail label="Raised on" value={selected.raisedAt ? new Date(selected.raisedAt).toLocaleString() : 'Recorded'} /><Detail label="Review level" value={selected.reviewLevel || 'Supervisor'} /><Detail label="Decision" value={selected.decision || 'Pending'} /></div><div className="mt-6 flex flex-wrap gap-3"><Link to={`/citizen/complaints/${selected.complaintId}`} className="button primary">View complaint</Link><Link to="/citizen/notifications" className="button outline">View status alerts</Link></div></div></> : <div className="grid min-h-[430px] place-items-center p-10 text-center"><div><div className="mx-auto grid h-20 w-20 place-items-center rounded-3xl bg-slate-100 text-3xl">!</div><h3 className="mt-5 text-xl font-black text-slate-900">No dispute selected</h3><p className="mt-2 text-sm text-slate-500">Select a dispute to view the review details.</p></div></div>}
        </section>
      </div>
    </section>
  );
}

function Evidence({ label, icon, tone }) { return <div className={`relative h-40 overflow-hidden rounded-2xl bg-gradient-to-br ${tone}`}><div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,.35)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.35)_1px,transparent_1px)] bg-[length:25px_25px]" /><span className="absolute left-4 top-4 rounded-full bg-white/80 px-3 py-1.5 text-[10px] font-black uppercase tracking-wider text-slate-700 shadow">{label}</span><span className="absolute bottom-5 right-5 text-5xl">{icon}</span></div>; }
function Detail({ label, value }) { return <div className="rounded-2xl bg-slate-50 p-4"><span className="text-[10px] font-black uppercase tracking-wider text-slate-400">{label}</span><strong className="mt-2 block text-sm text-slate-800">{value}</strong></div>; }
