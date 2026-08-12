import { Link } from 'react-router-dom';
import SlaBadge from './SlaBadge.jsx';
import StatusBadge from './StatusBadge.jsx';

export default function AssignmentCard({ item, basePath, actionLabel = 'Open task' }) {
  return (
    <article className="rounded-2xl border border-white/10 bg-white/[0.04] p-5 transition hover:border-sky-400/30">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <span className="font-mono text-xs text-sky-300">{item.referenceNumber}</span>
        <SlaBadge state={item.slaState} remainingMinutes={item.remainingMinutes} />
      </div>
      <div className="mt-4 flex flex-wrap gap-2"><StatusBadge status={item.complaintStatus} /><span className="rounded-full border border-white/10 px-2.5 py-1 text-xs font-bold text-slate-300">{item.priority}</span></div>
      <h3 className="mt-4 text-xl font-black text-white">{item.title}</h3>
      <p className="mt-2 line-clamp-2 text-sm leading-6 text-slate-400">{item.description}</p>
      <dl className="mt-4 grid grid-cols-2 gap-3 text-xs text-slate-400">
        <div><dt className="text-slate-600">Department</dt><dd className="mt-1 text-slate-300">{item.departmentName}</dd></div>
        <div><dt className="text-slate-600">Ward</dt><dd className="mt-1 text-slate-300">{item.wardName}</dd></div>
        <div><dt className="text-slate-600">Officer</dt><dd className="mt-1 text-slate-300">{item.officerName || 'Unassigned'}</dd></div>
        <div><dt className="text-slate-600">Category</dt><dd className="mt-1 text-slate-300">{item.category}</dd></div>
      </dl>
      <div className="mt-5 flex items-center justify-between border-t border-white/10 pt-4">
        <span className="text-xs text-slate-500">{item.address}</span>
        <Link to={`${basePath}/${item.complaintId}`} className="shrink-0 rounded-lg bg-sky-500 px-3 py-2 text-sm font-black text-white hover:bg-sky-400">{actionLabel}</Link>
      </div>
    </article>
  );
}
