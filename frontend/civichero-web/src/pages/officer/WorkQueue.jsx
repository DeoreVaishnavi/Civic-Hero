import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import AssignmentCard from '../../components/common/AssignmentCard.jsx';
import Pagination from '../../components/common/Pagination.jsx';
import CivicIcon from '../../components/ui/CivicIcon.jsx';
import { assignmentApi } from '../../services/assignmentApi.js';

const normalizeResult = (value) => {
  if (Array.isArray(value)) {
    return { items: value, page: 1, totalPages: value.length ? 1 : 0 };
  }

  const items = Array.isArray(value?.items) ? value.items : [];
  return {
    ...value,
    items,
    page: Number(value?.page) > 0 ? Number(value.page) : 1,
    totalPages: Number(value?.totalPages) >= 0 ? Number(value.totalPages) : 0,
  };
};

export default function WorkQueue() {
  const [searchParams] = useSearchParams();
  const headerSearch = searchParams.get('search') || '';
  const [filters, setFilters] = useState({ page: 1, pageSize: 12, status: '', priority: '', search: headerSearch, overdueOnly: false });
  const [result, setResult] = useState({ items: [], page: 1, totalPages: 0 });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    setFilters((current) => current.search === headerSearch
      ? current
      : { ...current, search: headerSearch, page: 1 });
  }, [headerSearch]);

  useEffect(() => {
    let active = true;
    const timer = window.setTimeout(() => {
      setLoading(true);
      setError('');
      assignmentApi.mine(filters)
        .then((value) => {
          if (active) setResult(normalizeResult(value));
        })
        .catch((reason) => {
          if (active) {
            setResult({ items: [], page: 1, totalPages: 0 });
            setError(reason?.message || 'Unable to load the work queue.');
          }
        })
        .finally(() => {
          if (active) setLoading(false);
        });
    }, 200);

    return () => {
      active = false;
      window.clearTimeout(timer);
    };
  }, [filters]);

  return (
    <section className="page-wrap">
      <div className="surface">
        <div className="surface-header">
          <div>
            <p className="section-kicker">Officer workspace</p>
            <h2>My work queue</h2>
            <p className="muted">Assignments are ordered by priority and SLA deadline.</p>
          </div>
          <Link to="/officer" className="button outline"><CivicIcon name="arrow" size={17} className="icon-back" /> Back to dashboard</Link>
        </div>

        <div className="surface-body">
          <div className="form-grid">
            <label className="form-label">
              <span>Search work</span>
              <input
                className="input"
                placeholder="Complaint title, reference or address"
                value={filters.search}
                onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value, page: 1 }))}
              />
            </label>
            <label className="form-label">
              <span>Assignment state</span>
              <select className="input" value={filters.status} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value, page: 1 }))}>
                <option value="">All assignment states</option>
                <option>Pending</option><option>Accepted</option><option>Completed</option>
              </select>
            </label>
            <label className="form-label">
              <span>Priority</span>
              <select className="input" value={filters.priority} onChange={(event) => setFilters((current) => ({ ...current, priority: event.target.value, page: 1 }))}>
                <option value="">All priorities</option>
                <option>Critical</option><option>High</option><option>Medium</option><option>Low</option>
              </select>
            </label>
            <label className="form-label" style={{ display: 'flex', flexDirection: 'row', alignItems: 'center', gap: 10, alignSelf: 'end' }}>
              <input type="checkbox" checked={filters.overdueOnly} onChange={(event) => setFilters((current) => ({ ...current, overdueOnly: event.target.checked, page: 1 }))} />
              <span>SLA overdue only</span>
            </label>
          </div>
        </div>
      </div>

      {error && (
        <div className="surface section-gap">
          <div className="surface-body">
            <div className="alert error" role="alert">{error}</div>
            <div className="page-actions">
              <button type="button" className="button primary" onClick={() => setFilters((current) => ({ ...current }))}>Retry</button>
              <Link to="/officer" className="button outline">Back to dashboard</Link>
            </div>
          </div>
        </div>
      )}

      {loading ? (
        <div className="surface section-gap"><div className="surface-body">Loading work queue…</div></div>
      ) : result.items.length ? (
        <>
          <div className="section-gap issue-card-grid">
            {result.items.map((item, index) => (
              <AssignmentCard
                key={item?.complaintId || item?.assignmentId || `${item?.referenceNumber || 'assignment'}-${index}`}
                item={item}
                basePath="/officer/assignments"
                actionLabel="View details"
              />
            ))}
          </div>
          <Pagination page={result.page} totalPages={result.totalPages} onPageChange={(page) => setFilters((current) => ({ ...current, page }))} />
        </>
      ) : (
        <div className="surface section-gap">
          <div className="surface-body">
            <h3>No assignments match these filters</h3>
            <p className="muted">Clear the filters or return to the Officer Dashboard.</p>
            <div className="page-actions">
              <button type="button" className="button primary" onClick={() => setFilters({ page: 1, pageSize: 12, status: '', priority: '', search: '', overdueOnly: false })}>Clear filters</button>
              <Link to="/officer" className="button outline">Back to dashboard</Link>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
