import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import ComplaintCard from '../../components/common/ComplaintCard.jsx';
import EmptyState from '../../components/common/EmptyState.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { complaintApi } from '../../services/complaintApi.js';
import { useComplaintStore } from '../../store/complaintStore.js';

export default function CitizenDashboard() {
  const { user } = useAuth();
  const { dashboard, setDashboard } = useComplaintStore();
  const [error, setError] = useState('');

  useEffect(() => {
    complaintApi.dashboard().then(setDashboard).catch((reason) => setError(reason.message));
  }, [setDashboard]);

  return (
    <section className="p-6 lg:p-10">
      <div className="flex flex-wrap items-end justify-between gap-5 rounded-3xl border border-sky-400/20 bg-sky-400/5 p-8">
        <div>
          <p className="text-sm font-bold uppercase tracking-wider text-sky-300">Citizen complaint centre</p>
          <h2 className="mt-2 text-4xl font-black text-white">Welcome, {user?.fullName}</h2>
          <p className="mt-3 text-slate-300">Report an issue with GPS and images, then follow every update.</p>
        </div>
        <Link to="/citizen/report" className="rounded-xl bg-sky-500 px-5 py-3 font-black text-white hover:bg-sky-400">Report complaint</Link>
      </div>

      {error && <div className="mt-6 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}
      <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <Metric label="Total" value={dashboard?.total ?? '—'} />
        <Metric label="Open" value={dashboard?.open ?? '—'} />
        <Metric label="In progress" value={dashboard?.inProgress ?? '—'} />
        <Metric label="Resolved" value={dashboard?.resolved ?? '—'} />
        <Metric label="Community upvotes" value={dashboard?.totalUpvotes ?? '—'} />
      </div>

      <div className="mt-10 flex items-center justify-between">
        <h3 className="text-2xl font-black text-white">Recent complaints</h3>
        <Link to="/citizen/complaints" className="text-sm font-bold text-sky-300">View all →</Link>
      </div>
      <div className="mt-5 grid gap-5 xl:grid-cols-2">
        {(dashboard?.recentComplaints || []).map((complaint) => <ComplaintCard key={complaint.id} complaint={complaint} />)}
      </div>
      {dashboard && dashboard.recentComplaints.length === 0 && (
        <div className="mt-5"><EmptyState title="No complaints yet" message="Your submitted civic issues will appear here." actionLabel="Report your first complaint" actionTo="/citizen/report" /></div>
      )}
    </section>
  );
}

function Metric({ label, value }) {
  return <article className="rounded-2xl border border-white/10 bg-white/5 p-5"><p className="text-xs font-bold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-2 text-3xl font-black text-white">{value}</p></article>;
}
