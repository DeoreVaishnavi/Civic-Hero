import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { complaintApi } from '../../services/complaintApi.js';

const STATUS_OPTIONS = [
  'Created', 'AiTriage', 'Assigned', 'InProgress', 'Escalated', 'Resolved',
  'VerificationPending', 'Disputed', 'Appealed', 'Closed', 'ClosedAuto',
];

const CATEGORY_VISUALS = {
  Pothole: ['🛣️', 'from-amber-400 via-orange-500 to-rose-500'],
  Garbage: ['♻️', 'from-emerald-400 via-teal-500 to-cyan-600'],
  Streetlight: ['💡', 'from-yellow-300 via-amber-400 to-orange-500'],
  'Water Leakage': ['💧', 'from-cyan-400 via-blue-500 to-indigo-600'],
  Drainage: ['🌧️', 'from-sky-400 via-blue-500 to-slate-700'],
  'Road Damage': ['🚧', 'from-orange-400 via-red-500 to-rose-700'],
  'Public Safety': ['🛡️', 'from-violet-400 via-purple-500 to-indigo-700'],
  'Illegal Dumping': ['🗑️', 'from-lime-400 via-emerald-500 to-teal-700'],
  Other: ['📍', 'from-slate-400 via-slate-600 to-slate-800'],
};

function visualFor(category) {
  return CATEGORY_VISUALS[category] || CATEGORY_VISUALS.Other;
}

function statusLabel(value = '') {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}

function statusTone(value = '') {
  const normalized = value.toLowerCase();
  if (normalized.includes('closed') || normalized === 'resolved') return 'bg-emerald-50 text-emerald-700 ring-emerald-200';
  if (normalized.includes('disputed') || normalized.includes('appealed')) return 'bg-rose-50 text-rose-700 ring-rose-200';
  if (normalized.includes('assigned') || normalized.includes('progress')) return 'bg-blue-50 text-blue-700 ring-blue-200';
  if (normalized.includes('verification')) return 'bg-violet-50 text-violet-700 ring-violet-200';
  return 'bg-amber-50 text-amber-700 ring-amber-200';
}

function formatDate(value) {
  if (!value) return 'Recently reported';
  return new Intl.DateTimeFormat('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(value));
}

export default function PublicIssuesPage() {
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();
  const [metadata, setMetadata] = useState({ categories: [], wards: [] });
  const [result, setResult] = useState({ items: [], page: 1, pageSize: 9, totalCount: 0, totalPages: 0 });
  const [filters, setFilters] = useState({ page: 1, pageSize: 9, search: '', category: '', status: '', wardId: '', sortBy: 'newest' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [supportingId, setSupportingId] = useState(null);

  const isCitizen = String(user?.role || '').toLowerCase() === 'citizen';
  const reportPath = isAuthenticated && isCitizen ? ROUTE_PATHS.reportComplaint : ROUTE_PATHS.anonymousReport;

  useEffect(() => {
    complaintApi.metadata().then((data) => setMetadata(data || { categories: [], wards: [] })).catch(() => {});
  }, []);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');

    const timer = window.setTimeout(async () => {
      try {
        const query = {
          ...filters,
          wardId: filters.wardId || undefined,
          category: filters.category || undefined,
          status: filters.status || undefined,
          search: filters.search.trim() || undefined,
        };
        const data = await complaintApi.publicFeed(query);
        if (active) setResult(data || { items: [], page: 1, pageSize: 9, totalCount: 0, totalPages: 0 });
      } catch (reason) {
        if (active) setError(reason.message || 'Unable to load community issues.');
      } finally {
        if (active) setLoading(false);
      }
    }, 300);

    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [filters]);

  const activeCount = useMemo(
    () => result.items.filter((item) => !['Closed', 'ClosedAuto', 'Resolved'].includes(item.status)).length,
    [result.items],
  );

  const toggleSupport = async (item) => {
    setMessage('');
    setError('');

    if (!isAuthenticated) {
      navigate(ROUTE_PATHS.login, { state: { from: ROUTE_PATHS.publicIssues } });
      return;
    }

    if (!isCitizen) {
      setError('Only a Citizen account can support community complaints.');
      return;
    }

    setSupportingId(item.id);
    try {
      const response = item.hasUpvoted
        ? await complaintApi.removeUpvote(item.id)
        : await complaintApi.upvote(item.id);
      const nextCount = response?.upvoteCount ?? Math.max(0, (item.upvoteCount || 0) + (item.hasUpvoted ? -1 : 1));
      setResult((current) => ({
        ...current,
        items: current.items.map((row) => row.id === item.id
          ? { ...row, upvoteCount: nextCount, hasUpvoted: !row.hasUpvoted }
          : row),
      }));
      setMessage(item.hasUpvoted ? 'Support removed.' : 'Your support has been recorded.');
    } catch (reason) {
      setError(reason.message || 'Unable to update support.');
    } finally {
      setSupportingId(null);
    }
  };

  const resetFilters = () => setFilters({ page: 1, pageSize: 9, search: '', category: '', status: '', wardId: '', sortBy: 'newest' });

  return (
    <div className="min-h-screen bg-slate-50">
      <section className="relative overflow-hidden bg-gradient-to-br from-slate-950 via-blue-950 to-blue-800 text-white">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_12%_20%,rgba(56,189,248,.18),transparent_28%),radial-gradient(circle_at_86%_74%,rgba(16,185,129,.13),transparent_32%)]" />
        <div className="relative mx-auto grid w-full max-w-7xl gap-10 px-4 py-16 sm:px-6 lg:grid-cols-[1fr_auto] lg:items-end lg:px-8 lg:py-20">
          <div>
            <p className="text-xs font-black uppercase tracking-[.22em] text-cyan-200">Community issue feed</p>
            <h1 className="mt-4 max-w-4xl text-4xl font-black tracking-tight sm:text-5xl">See what your city is reporting, support important issues, and encourage action.</h1>
            <p className="mt-5 max-w-3xl text-base leading-7 text-blue-100/80">Every visible complaint is searchable and sortable. Supporting an existing complaint helps departments understand community priority and reduces duplicate reports.</p>
          </div>
          <Link to={reportPath} className="inline-flex min-h-14 items-center justify-center rounded-2xl bg-white px-6 py-4 text-sm font-black text-blue-800 shadow-2xl shadow-black/20 transition hover:-translate-y-1 hover:bg-cyan-50">＋ File a complaint</Link>
        </div>
      </section>

      <section className="relative z-10 -mt-7 px-4 sm:px-6 lg:px-8">
        <div className="mx-auto grid w-full max-w-7xl gap-4 rounded-[1.75rem] border border-slate-200 bg-white p-5 shadow-2xl shadow-slate-900/10 sm:grid-cols-3">
          <FeedMetric label="Visible complaints" value={result.totalCount} icon="▤" />
          <FeedMetric label="Active on this page" value={activeCount} icon="●" />
          <FeedMetric label="Community supports" value={result.items.reduce((sum, item) => sum + (item.upvoteCount || 0), 0)} icon="♥" />
        </div>
      </section>

      <main className="mx-auto w-full max-w-7xl px-4 py-12 sm:px-6 lg:px-8 lg:py-16">
        <section className="rounded-[1.75rem] border border-slate-200 bg-white p-5 shadow-sm sm:p-6">
          <div className="grid gap-3 lg:grid-cols-[1.5fr_repeat(4,minmax(0,1fr))_auto]">
            <label className="relative">
              <span className="sr-only">Search complaints</span>
              <input
                value={filters.search}
                onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value, page: 1 }))}
                className="h-12 w-full rounded-xl border border-slate-200 bg-slate-50 px-4 text-sm font-semibold text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-blue-400 focus:bg-white focus:ring-4 focus:ring-blue-100"
                placeholder="Search title, description or address"
              />
            </label>
            <FilterSelect value={filters.category} onChange={(value) => setFilters((current) => ({ ...current, category: value, page: 1 }))} label="Category">
              <option value="">All categories</option>
              {(metadata.categories || []).map((item) => <option key={item} value={item}>{item}</option>)}
            </FilterSelect>
            <FilterSelect value={filters.status} onChange={(value) => setFilters((current) => ({ ...current, status: value, page: 1 }))} label="Status">
              <option value="">All statuses</option>
              {STATUS_OPTIONS.map((item) => <option key={item} value={item}>{statusLabel(item)}</option>)}
            </FilterSelect>
            <FilterSelect value={filters.wardId} onChange={(value) => setFilters((current) => ({ ...current, wardId: value, page: 1 }))} label="Ward">
              <option value="">All wards</option>
              {(metadata.wards || []).map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
            </FilterSelect>
            <FilterSelect value={filters.sortBy} onChange={(value) => setFilters((current) => ({ ...current, sortBy: value, page: 1 }))} label="Sort">
              <option value="newest">Newest first</option>
              <option value="oldest">Oldest first</option>
              <option value="most-supported">Most supported</option>
              <option value="recently-updated">Recently updated</option>
            </FilterSelect>
            <button type="button" onClick={resetFilters} className="h-12 rounded-xl border border-slate-200 px-4 text-sm font-black text-slate-600 transition hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700">Reset</button>
          </div>
        </section>

        {message && <div className="mt-6 rounded-2xl border border-emerald-200 bg-emerald-50 px-5 py-4 text-sm font-bold text-emerald-800">{message}</div>}
        {error && <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 px-5 py-4 text-sm font-bold text-rose-800">{error}</div>}

        {loading ? (
          <div className="mt-8 grid gap-6 lg:grid-cols-2">
            {[1, 2, 3, 4].map((item) => <div key={item} className="h-80 animate-pulse rounded-[1.75rem] border border-slate-200 bg-white" />)}
          </div>
        ) : result.items?.length ? (
          <div className="mt-8 grid gap-6 lg:grid-cols-2">
            {result.items.map((item) => {
              const [icon, gradient] = visualFor(item.category);
              return (
                <article key={item.id} className="group overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-sm transition duration-300 hover:-translate-y-1 hover:border-blue-200 hover:shadow-2xl hover:shadow-blue-950/10">
                  <div className={`relative h-40 overflow-hidden bg-gradient-to-br ${gradient}`}>
                    <div className="absolute inset-0 opacity-25 [background-image:linear-gradient(rgba(255,255,255,.35)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.35)_1px,transparent_1px)] [background-size:34px_34px]" />
                    <div className="absolute -right-8 -top-10 h-40 w-40 rounded-full bg-white/20 blur-2xl" />
                    <span className="absolute left-5 top-5 grid h-16 w-16 place-items-center rounded-2xl border border-white/30 bg-white/20 text-3xl shadow-xl backdrop-blur">{icon}</span>
                    <span className={`absolute right-5 top-5 rounded-full px-3 py-1.5 text-xs font-black ring-1 ${statusTone(item.status)}`}>{statusLabel(item.status)}</span>
                    <div className="absolute bottom-5 left-5 right-5 flex items-end justify-between gap-4 text-white">
                      <div><p className="text-xs font-black uppercase tracking-[.16em] text-white/75">{item.category}</p><p className="mt-1 font-mono text-xs font-bold">{item.referenceNumber}</p></div>
                      <span className="rounded-xl border border-white/25 bg-black/15 px-3 py-2 text-xs font-black backdrop-blur">{item.upvoteCount || 0} supports</span>
                    </div>
                  </div>

                  <div className="p-5 sm:p-6">
                    <h2 className="text-xl font-black text-slate-950">{item.title}</h2>
                    <p className="mt-3 line-clamp-3 min-h-[72px] text-sm leading-6 text-slate-600">{item.description}</p>
                    <div className="mt-5 grid gap-2 text-xs font-semibold text-slate-500 sm:grid-cols-2">
                      <span className="rounded-xl bg-slate-50 px-3 py-2.5">⌖ {item.wardName || 'Ward not assigned'}</span>
                      <span className="rounded-xl bg-slate-50 px-3 py-2.5">🏢 {item.departmentName || 'Municipal department'}</span>
                      <span className="rounded-xl bg-slate-50 px-3 py-2.5 sm:col-span-2">📍 {item.address || 'Location available in complaint record'}</span>
                    </div>
                    <div className="mt-5 flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 pt-5">
                      <div className="text-xs text-slate-400"><strong className="text-slate-600">Reported</strong> {formatDate(item.createdAt)} · {item.imageCount || 0} evidence files</div>
                      <div className="flex flex-wrap gap-2">
                        {isAuthenticated && isCitizen && (
                          <Link to={`/citizen/complaints/${item.id}`} className="rounded-xl border border-slate-200 px-4 py-2.5 text-xs font-black text-slate-600 transition hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700">View details</Link>
                        )}
                        <button
                          type="button"
                          onClick={() => toggleSupport(item)}
                          disabled={supportingId === item.id}
                          className={`rounded-xl px-4 py-2.5 text-xs font-black transition disabled:cursor-wait disabled:opacity-60 ${item.hasUpvoted ? 'border border-emerald-200 bg-emerald-50 text-emerald-700 hover:bg-emerald-100' : 'bg-blue-600 text-white shadow-lg shadow-blue-600/20 hover:-translate-y-0.5 hover:bg-blue-700'}`}
                        >
                          {supportingId === item.id ? 'Updating…' : item.hasUpvoted ? '✓ Supported' : '♥ Support issue'}
                        </button>
                      </div>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        ) : (
          <section className="mt-8 rounded-[1.75rem] border border-dashed border-slate-300 bg-white px-6 py-16 text-center">
            <div className="mx-auto grid h-16 w-16 place-items-center rounded-2xl bg-blue-50 text-3xl">⌕</div>
            <h2 className="mt-5 text-2xl font-black text-slate-900">No complaints match these filters</h2>
            <p className="mt-2 text-sm text-slate-500">Reset the filters or file a new complaint when the issue is not already listed.</p>
            <div className="mt-6 flex justify-center gap-3"><button type="button" onClick={resetFilters} className="rounded-xl border border-slate-200 px-5 py-3 text-sm font-black text-slate-700">Reset filters</button><Link to={reportPath} className="rounded-xl bg-blue-600 px-5 py-3 text-sm font-black text-white">File complaint</Link></div>
          </section>
        )}

        <PublicPagination page={result.page || 1} totalPages={result.totalPages || 0} onPageChange={(page) => setFilters((current) => ({ ...current, page }))} />
      </main>
    </div>
  );
}

function FilterSelect({ label, value, onChange, children }) {
  return (
    <label>
      <span className="sr-only">{label}</span>
      <select value={value} onChange={(event) => onChange(event.target.value)} className="h-12 w-full rounded-xl border border-slate-200 bg-slate-50 px-3 text-sm font-bold text-slate-700 outline-none transition focus:border-blue-400 focus:bg-white focus:ring-4 focus:ring-blue-100">
        {children}
      </select>
    </label>
  );
}

function FeedMetric({ label, value, icon }) {
  return <div className="flex items-center gap-4 rounded-2xl bg-slate-50 p-4"><span className="grid h-12 w-12 place-items-center rounded-2xl bg-blue-600 text-xl font-black text-white shadow-lg shadow-blue-600/20">{icon}</span><div><strong className="block text-2xl font-black text-slate-950">{Number(value || 0).toLocaleString('en-IN')}</strong><span className="text-xs font-bold text-slate-500">{label}</span></div></div>;
}

function PublicPagination({ page, totalPages, onPageChange }) {
  if (!totalPages || totalPages <= 1) return null;
  return (
    <div className="mt-10 flex flex-wrap items-center justify-center gap-3">
      <button type="button" disabled={page <= 1} onClick={() => onPageChange(page - 1)} className="rounded-xl border border-slate-200 bg-white px-5 py-3 text-sm font-black text-slate-700 shadow-sm disabled:opacity-40">← Previous</button>
      <span className="rounded-xl bg-slate-900 px-5 py-3 text-sm font-black text-white">Page {page} of {totalPages}</span>
      <button type="button" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)} className="rounded-xl border border-slate-200 bg-white px-5 py-3 text-sm font-black text-slate-700 shadow-sm disabled:opacity-40">Next →</button>
    </div>
  );
}
