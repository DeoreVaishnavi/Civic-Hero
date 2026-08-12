import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { dashboardForRole } from '../../utils/roleRouting.js';

export default function AccessDeniedPage() {
  const { user } = useAuth();
  return (
    <section className="flex min-h-screen items-center justify-center bg-slate-950 px-6 text-slate-100">
      <div className="max-w-lg rounded-3xl border border-rose-400/20 bg-rose-400/5 p-8 text-center">
        <p className="text-sm font-bold uppercase tracking-wider text-rose-300">403 · Access denied</p>
        <h1 className="mt-3 text-4xl font-black">This portal is not assigned to your role.</h1>
        <p className="mt-4 text-slate-300">Signed in as {user?.role}. CivicHero blocks unauthorized portal and API access.</p>
        <Link to={dashboardForRole(user?.role)} className="mt-6 inline-block rounded-xl bg-sky-500 px-5 py-3 font-bold text-white">Go to my dashboard</Link>
      </div>
    </section>
  );
}
