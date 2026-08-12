import { useEffect, useState } from 'react';
import AssignmentCard from '../../components/common/AssignmentCard.jsx';
import Pagination from '../../components/common/Pagination.jsx';
import { assignmentApi } from '../../services/assignmentApi.js';

export default function WorkQueue() {
  const [filters, setFilters] = useState({ page: 1, pageSize: 12, status: '', priority: '', search: '', overdueOnly: false });
  const [result, setResult] = useState({ items: [], page: 1, totalPages: 0 });
  const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  useEffect(() => { setLoading(true); setError(''); const timer = setTimeout(() => assignmentApi.mine(filters).then(setResult).catch((reason) => setError(reason.message)).finally(() => setLoading(false)), 200); return () => clearTimeout(timer); }, [filters]);
  return <section className="p-6 lg:p-10"><h2 className="text-3xl font-black text-white">My work queue</h2><p className="mt-2 text-slate-400">Assignments are ordered by priority and SLA deadline.</p>
    <div className="mt-6 grid gap-3 rounded-2xl border border-white/10 bg-white/[0.04] p-4 md:grid-cols-4"><input className="input mt-0 md:col-span-2" placeholder="Search work" value={filters.search} onChange={(e) => setFilters((x) => ({ ...x, search: e.target.value, page: 1 }))} /><select className="input mt-0" value={filters.status} onChange={(e) => setFilters((x) => ({ ...x, status: e.target.value, page: 1 }))}><option value="">All assignment states</option><option>Pending</option><option>Accepted</option><option>Completed</option></select><select className="input mt-0" value={filters.priority} onChange={(e) => setFilters((x) => ({ ...x, priority: e.target.value, page: 1 }))}><option value="">All priorities</option><option>Critical</option><option>High</option><option>Medium</option><option>Low</option></select><label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={filters.overdueOnly} onChange={(e) => setFilters((x) => ({ ...x, overdueOnly: e.target.checked, page: 1 }))} /> SLA overdue only</label></div>
    {error && <div className="mt-6 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}
    {loading ? <p className="mt-8 text-slate-400">Loading work queue…</p> : <>{result.items.length ? <div className="mt-8 grid gap-5 xl:grid-cols-2">{result.items.map((item) => <AssignmentCard key={item.complaintId} item={item} basePath="/officer/assignments" />)}</div> : <div className="mt-8 rounded-2xl border border-white/10 bg-white/[0.04] p-8 text-slate-400">No assignments match these filters.</div>}<Pagination page={result.page} totalPages={result.totalPages} onPageChange={(page) => setFilters((x) => ({ ...x, page }))} /></>}
  </section>;
}
