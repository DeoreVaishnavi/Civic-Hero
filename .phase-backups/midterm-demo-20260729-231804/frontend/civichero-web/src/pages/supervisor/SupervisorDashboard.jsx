import { useAuth } from '../../contexts/AuthContext.jsx';

export default function SupervisorDashboard() {
  const { user } = useAuth();
  return (
    <section className="p-6 lg:p-10">
      <div className="rounded-3xl border border-amber-400/20 bg-amber-400/5 p-8">
        <p className="text-sm font-bold uppercase tracking-wider text-amber-300">Supervisor policy active</p>
        <h2 className="mt-2 text-4xl font-black text-white">Department oversight</h2>
        <p className="mt-3 text-slate-300">Supervisor access is scoped to {user?.departmentName || `department ${user?.departmentId ?? 'not assigned'}`} and supports cross-ward team coordination.</p>
      </div>
      <div className="mt-8 grid gap-5 md:grid-cols-3"><Card label="Department" value={user?.departmentName || 'Not assigned'} /><Card label="Ward scope" value={user?.wardName || 'All department wards'} /><Card label="Permission" value="SupervisorOrAbove" /></div>
    </section>
  );
}
function Card({ label, value }) { return <article className="rounded-2xl border border-white/10 bg-white/5 p-5"><p className="text-xs font-bold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-2 font-bold text-white">{value}</p></article>; }
