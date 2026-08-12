import { useAuth } from '../../contexts/AuthContext.jsx';

export default function OfficerDashboard() {
  const { user } = useAuth();
  return (
    <section className="p-6 lg:p-10">
      <div className="rounded-3xl border border-sky-400/20 bg-sky-400/5 p-8">
        <p className="text-sm font-bold uppercase tracking-wider text-sky-300">Officer scope enforced</p>
        <h2 className="mt-2 text-4xl font-black text-white">Field operations dashboard</h2>
        <p className="mt-3 text-slate-300">Your future work queue will only show complaints assigned to your department and ward.</p>
      </div>
      <div className="mt-8 grid gap-5 md:grid-cols-3">
        <Card label="Department" value={user?.departmentName || `ID ${user?.departmentId ?? '—'}`} />
        <Card label="Ward" value={user?.wardName || `ID ${user?.wardId ?? '—'}`} />
        <Card label="Access" value="Assigned work only" />
      </div>
    </section>
  );
}
function Card({ label, value }) { return <article className="rounded-2xl border border-white/10 bg-white/5 p-5"><p className="text-xs font-bold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-2 font-bold text-white">{value}</p></article>; }
