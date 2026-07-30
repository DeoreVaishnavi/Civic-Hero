import { Link } from 'react-router-dom';
import StatusBadge from './StatusBadge.jsx';

export default function ComplaintCard({ complaint, basePath = '/citizen/complaints' }) {
  return (
    <article className="rounded-2xl border border-white/10 bg-white/[0.04] p-5 transition hover:-translate-y-0.5 hover:border-sky-400/30">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <span className="font-mono text-xs text-sky-300">{complaint.referenceNumber}</span>
        <StatusBadge status={complaint.status} />
      </div>
      <h3 className="mt-4 text-xl font-black text-white">{complaint.title}</h3>
      <p className="mt-2 line-clamp-2 text-sm leading-6 text-slate-400">{complaint.description}</p>
      <div className="mt-4 grid grid-cols-2 gap-3 text-xs text-slate-400">
        <span>{complaint.category}</span>
        <span className="text-right">{complaint.wardName}</span>
        <span>{complaint.imageCount} image(s)</span>
        <span className="text-right">▲ {complaint.upvoteCount}</span>
      </div>
      <div className="mt-5 flex items-center justify-between border-t border-white/10 pt-4">
        <time className="text-xs text-slate-500">{new Date(complaint.createdAt).toLocaleDateString()}</time>
        <Link to={`${basePath}/${complaint.id}`} className="rounded-lg bg-sky-500 px-3 py-2 text-sm font-bold text-white hover:bg-sky-400">View details</Link>
      </div>
    </article>
  );
}
