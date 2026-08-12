import { Link } from 'react-router-dom';

export default function EmptyState({ title, message, actionLabel, actionTo }) {
  return (
    <div className="rounded-3xl border border-dashed border-white/15 bg-white/[0.03] p-10 text-center">
      <h3 className="text-2xl font-black text-white">{title}</h3>
      <p className="mx-auto mt-3 max-w-xl text-slate-400">{message}</p>
      {actionTo && <Link to={actionTo} className="mt-6 inline-flex rounded-xl bg-sky-500 px-5 py-3 font-bold text-white hover:bg-sky-400">{actionLabel}</Link>}
    </div>
  );
}
