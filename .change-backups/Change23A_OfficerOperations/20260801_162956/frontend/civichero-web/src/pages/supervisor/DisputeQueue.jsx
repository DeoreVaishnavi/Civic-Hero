import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { disputeApi } from '../../services/disputeApi.js';

export default function DisputeQueue() {
  const [items, setItems] = useState([]);
  const [error, setError] = useState('');
  useEffect(() => { disputeApi.queue(false).then(setItems).catch((e) => setError(e.message || 'Unable to load disputes.')); }, []);
  return <section className="space-y-5 p-6 lg:p-10"><div><p className="text-sm font-semibold uppercase tracking-[.18em] text-sky-400">Department review</p><h2 className="mt-2 text-3xl font-black text-white">Dispute queue</h2><p className="mt-2 text-slate-400">Review Citizen challenges, request investigation, reopen work, or escalate the case.</p></div>{error && <p className="rounded-xl bg-rose-500/10 p-4 text-rose-200">{error}</p>}<div className="space-y-3">{items.length === 0 ? <p className="rounded-2xl border border-white/10 bg-white/5 p-6 text-slate-400">No disputes require supervisor action.</p> : items.map((item) => <Link key={item.id} to={`/supervisor/disputes/${item.id}`} className="block rounded-2xl border border-white/10 bg-white/5 p-5 hover:bg-white/10"><div className="flex justify-between gap-3"><h3 className="font-bold text-white">{item.referenceNumber || `Complaint ${item.complaintId}`}</h3><span className="text-sm text-amber-200">{item.status}</span></div><p className="mt-2 text-sm text-slate-400">{item.citizenRemarks}</p><p className="mt-2 text-xs text-slate-500">Cycle {item.cycleNumber}</p></Link>)}</div></section>;
}
