import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';

export default function AdminDashboard() {
  const { user } = useAuth();
  return (
    <section className="p-6 lg:p-10">
      <div className="rounded-3xl border border-violet-400/20 bg-violet-400/5 p-8">
        <p className="text-sm font-bold uppercase tracking-wider text-violet-300">Admin policy confirmed</p>
        <h2 className="mt-2 text-4xl font-black text-white">System governance</h2>
        <p className="mt-3 max-w-2xl text-slate-300">Signed in as {user?.role}. Manage user status, roles, departments, wards, and active sessions from one protected screen.</p>
        <Link to="/admin/users" className="mt-6 inline-block rounded-xl bg-violet-500 px-5 py-3 font-bold text-white hover:bg-violet-400">Open user management</Link>
      </div>
      <div className="mt-8 grid gap-5 md:grid-cols-3"><Card label="Role" value={user?.role} /><Card label="Policy" value="AdminOrAbove" /><Card label="Session controls" value="Immediate revocation" /></div>
    </section>
  );
}
function Card({ label, value }) { return <article className="rounded-2xl border border-white/10 bg-white/5 p-5"><p className="text-xs font-bold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-2 font-bold text-white">{value}</p></article>; }
