import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import AssignmentCard from '../../components/common/AssignmentCard.jsx';
import MetricCard from '../../components/common/MetricCard.jsx';
import { assignmentApi } from '../../services/assignmentApi.js';

export default function OfficerDashboard() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  useEffect(() => { assignmentApi.officerDashboard().then(setData).catch((reason) => setError(reason.message)); }, []);

  return <section className="p-6 lg:p-10">
    <div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-sm font-black uppercase tracking-widest text-sky-300">Field operations</p><h2 className="mt-2 text-4xl font-black text-white">Officer command centre</h2><p className="mt-2 text-slate-400">Accept work, report progress and submit verified resolution evidence.</p></div><Link to="/officer/assignments" className="rounded-xl bg-sky-500 px-5 py-3 font-black text-white hover:bg-sky-400">Open work queue</Link></div>
    {error && <div className="mt-6 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}
    {!data ? <p className="mt-8 text-slate-400">Loading officer dashboard…</p> : <>
      <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4"><MetricCard label="Awaiting response" value={data.pending} hint="Accept before assignment SLA" tone="amber" /><MetricCard label="In progress" value={data.inProgress} hint="Active field assignments" /><MetricCard label="Completed this week" value={data.completedThisWeek} hint="Submitted for verification" tone="emerald" /><MetricCard label="SLA overdue" value={data.overdue} hint={`${data.slaCompliancePercent}% compliance`} tone="rose" /></div>
      <div className="mt-10 flex items-center justify-between"><h3 className="text-2xl font-black text-white">Priority work</h3><Link to="/officer/assignments" className="text-sm font-bold text-sky-300">View all →</Link></div>
      {data.priorityItems.length ? <div className="mt-5 grid gap-5 xl:grid-cols-2">{data.priorityItems.map((item) => <AssignmentCard key={item.complaintId} item={item} basePath="/officer/assignments" />)}</div> : <div className="mt-5 rounded-2xl border border-white/10 bg-white/[0.04] p-8 text-slate-400">No active assignments. Your queue is clear.</div>}
    </>}
  </section>;
}
