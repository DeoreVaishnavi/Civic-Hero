import { useAuth } from '../../contexts/AuthContext.jsx';

export default function CitizenDashboard() {
  const { user } = useAuth();
  return (
    <section className="p-6 lg:p-10">
      <div className="rounded-3xl border border-emerald-400/20 bg-emerald-400/5 p-8">
        <p className="text-sm font-bold uppercase tracking-wider text-emerald-300">Citizen access confirmed</p>
        <h2 className="mt-2 text-4xl font-black text-white">Welcome, {user?.fullName}</h2>
        <p className="mt-3 max-w-2xl text-slate-300">This portal is restricted to Citizen accounts. Complaint reporting and tracking arrive in the next vertical feature phase.</p>
      </div>
      <div className="mt-8 grid gap-5 md:grid-cols-3">
        <Metric label="Account role" value={user?.role} />
        <Metric label="Email status" value={user?.isEmailVerified ? 'Verified' : 'Pending'} />
        <Metric label="Current scope" value="Citizen self-service" />
      </div>
    </section>
  );
}
function Metric({ label, value }) { return <article className="rounded-2xl border border-white/10 bg-white/5 p-5"><p className="text-xs font-bold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-2 font-bold text-white">{value || 'Not assigned'}</p></article>; }
