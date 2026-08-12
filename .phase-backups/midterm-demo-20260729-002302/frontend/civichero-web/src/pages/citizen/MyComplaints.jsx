import { useEffect, useState } from 'react';
import ComplaintCard from '../../components/common/ComplaintCard.jsx';
import EmptyState from '../../components/common/EmptyState.jsx';
import Pagination from '../../components/common/Pagination.jsx';
import { complaintApi } from '../../services/complaintApi.js';

export default function MyComplaints() {
  const [result, setResult] = useState({ items: [], page: 1, totalPages: 0 });
  const [filters, setFilters] = useState({ page: 1, pageSize: 12, status: '', search: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    setLoading(true); setError('');
    const timer = setTimeout(() => complaintApi.mine(filters).then(setResult).catch((reason) => setError(reason.message)).finally(() => setLoading(false)), 250);
    return () => clearTimeout(timer);
  }, [filters]);

  return (
    <section className="p-6 lg:p-10">
      <div className="flex flex-wrap items-end justify-between gap-4"><div><h2 className="text-3xl font-black text-white">My complaints</h2><p className="mt-2 text-slate-400">Search and track every issue you have reported.</p></div><a href="/citizen/report" className="rounded-xl bg-sky-500 px-5 py-3 font-black text-white">Report complaint</a></div>
      <div className="mt-6 grid gap-4 rounded-2xl border border-white/10 bg-white/[0.04] p-4 md:grid-cols-[1fr_220px]">
        <input className="input mt-0" placeholder="Search title, description or address" value={filters.search} onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value, page: 1 }))} />
        <select className="input mt-0" value={filters.status} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value, page: 1 }))}><option value="">All statuses</option>{['Created', 'Assigned', 'InProgress', 'Resolved', 'VerificationPending', 'Closed', 'Withdrawn'].map((item) => <option key={item}>{item}</option>)}</select>
      </div>
      {error && <div className="mt-6 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}
      {loading ? <div className="mt-8 text-slate-400">Loading complaints…</div> : (
        <>{result.items.length > 0 ? <div className="mt-8 grid gap-5 xl:grid-cols-2">{result.items.map((item) => <ComplaintCard key={item.id} complaint={item} />)}</div> : <div className="mt-8"><EmptyState title="No matching complaints" message="Change the filters or submit a new civic issue." actionLabel="Report complaint" actionTo="/citizen/report" /></div>}<Pagination page={result.page} totalPages={result.totalPages} onPageChange={(page) => setFilters((current) => ({ ...current, page }))} /></>
      )}
    </section>
  );
}
