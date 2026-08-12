import { useEffect, useState } from 'react';
import AssignmentCard from '../../components/common/AssignmentCard.jsx';
import { assignmentApi } from '../../services/assignmentApi.js';

export default function OverdueAssignments() {
  const [items, setItems] = useState([]); const [error, setError] = useState('');
  useEffect(() => { assignmentApi.overdue().then(setItems).catch((reason) => setError(reason.message)); }, []);
  return <section className="p-6 lg:p-10"><p className="text-sm font-black uppercase tracking-widest text-rose-300">SLA escalation</p><h2 className="mt-2 text-3xl font-black text-white">Overdue assignments</h2><p className="mt-2 text-slate-400">Pending acceptance or resolution deadlines that have passed.</p>{error && <div className="mt-6 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}{items.length ? <div className="mt-8 grid gap-5 xl:grid-cols-2">{items.map((item) => <AssignmentCard key={item.complaintId} item={item} basePath="/supervisor/assignments" actionLabel="Intervene" />)}</div> : <div className="mt-8 rounded-2xl border border-emerald-400/20 bg-emerald-400/5 p-8 text-emerald-100">No overdue assignments in your scope.</div>}</section>;
}
