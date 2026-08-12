import { useAuth } from '../../contexts/AuthContext.jsx';

export default function CitizenDashboard() {
  const { user, expiresAtUtc } = useAuth();

  return (
    <section className="mx-auto max-w-6xl px-6 py-14">
      <div className="rounded-3xl border border-emerald-400/20 bg-emerald-400/5 p-8">
        <p className="text-sm font-bold uppercase tracking-wider text-emerald-300">Authentication successful</p>
        <h1 className="mt-2 text-4xl font-black text-white">Welcome, {user?.fullName}</h1>
        <p className="mt-3 max-w-2xl text-slate-300">The React frontend is authenticated with the ASP.NET Core API and your citizen account is loaded from AWS RDS MySQL.</p>
      </div>

      <div className="mt-8 grid gap-5 md:grid-cols-2 lg:grid-cols-3">
        <Card label="Email" value={user?.email} />
        <Card label="Role" value={user?.role} />
        <Card label="Email verified" value={user?.isEmailVerified ? 'Yes' : 'No'} />
        <Card label="User ID" value={user?.id} />
        <Card label="Token expires" value={expiresAtUtc ? new Date(expiresAtUtc).toLocaleString() : 'Unknown'} />
        <Card label="Next module" value="Role authorization and profiles" />
      </div>
    </section>
  );
}

function Card({ label, value }) {
  return (
    <article className="rounded-2xl border border-white/10 bg-white/5 p-5">
      <p className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</p>
      <p className="mt-2 break-words font-semibold text-white">{String(value ?? 'Not assigned')}</p>
    </article>
  );
}
