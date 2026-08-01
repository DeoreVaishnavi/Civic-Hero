import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import StatusBadge from '../../components/common/StatusBadge.jsx';
import { complaintApi } from '../../services/complaintApi.js';

const EMPTY_RESULT = { items: [], page: 1, pageSize: 12, totalCount: 0 };

export default function FollowingComplaints() {
  const [filters, setFilters] = useState({ search: '', status: '', sortBy: 'recently-updated', page: 1, pageSize: 12 });
  const [result, setResult] = useState(EMPTY_RESULT);
  const [loading, setLoading] = useState(true);
  const [busyId, setBusyId] = useState(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const load = async () => {
    setLoading(true);
    try {
      setResult(await complaintApi.following(filters));
      setError('');
    } catch (reason) {
      setError(reason.message || 'Followed complaints could not be loaded.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    const handle = window.setTimeout(load, 250);
    return () => window.clearTimeout(handle);
  }, [filters.search, filters.status, filters.sortBy, filters.page]);

  const unfollow = async (item) => {
    if (!window.confirm(`Stop following ${item.referenceNumber}? You will no longer receive follower updates.`)) return;
    setBusyId(item.id);
    setError('');
    setMessage('');
    try {
      await complaintApi.unfollow(item.id);
      setMessage(`${item.referenceNumber} was removed from your following feed.`);
      await load();
    } catch (reason) {
      setError(reason.message || 'The complaint could not be unfollowed.');
    } finally {
      setBusyId(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil((result.totalCount || 0) / (result.pageSize || 12)));

  return (
    <section className="page-wrap">
      <div className="page-title-row">
        <div>
          <p className="section-kicker">Community following</p>
          <h2>Followed complaints</h2>
          <p>Track issues separately from supporting them and receive notifications when public progress, evidence or comments change.</p>
        </div>
        <Link to="/citizen/nearby" className="button primary">Find community issues</Link>
      </div>

      {message && <div className="alert success">{message}</div>}
      {error && <div className="alert error">{error}</div>}

      <div className="filter-bar">
        <input className="input" value={filters.search} onChange={(event) => setFilters((current) => ({ ...current, search: event.target.value, page: 1 }))} placeholder="Search followed complaints" />
        <select className="input" value={filters.status} onChange={(event) => setFilters((current) => ({ ...current, status: event.target.value, page: 1 }))}>
          <option value="">All statuses</option>
          <option value="Created">Created</option>
          <option value="Assigned">Assigned</option>
          <option value="InProgress">In progress</option>
          <option value="Escalated">Escalated</option>
          <option value="VerificationPending">Verification pending</option>
          <option value="Closed">Closed</option>
        </select>
        <select className="input" value={filters.sortBy} onChange={(event) => setFilters((current) => ({ ...current, sortBy: event.target.value, page: 1 }))}>
          <option value="recently-updated">Recently updated</option>
          <option value="followed">Recently followed</option>
          <option value="most-supported">Most supported</option>
          <option value="oldest">Oldest reported</option>
        </select>
        <button type="button" onClick={() => setFilters({ search: '', status: '', sortBy: 'recently-updated', page: 1, pageSize: 12 })} className="button outline">Reset</button>
      </div>

      {loading ? (
        <div className="surface section-gap"><div className="surface-body">Loading your following feed…</div></div>
      ) : result.items?.length ? (
        <>
          <div className="issue-card-grid section-gap">
            {result.items.map((item) => (
              <article key={item.id} className="issue-card">
                <div className="issue-card-head">
                  <div><h3>{item.title}</h3><p>{item.referenceNumber} · {item.category} · {item.wardName}</p></div>
                  <StatusBadge status={item.status} />
                </div>
                <div style={{ padding: '14px' }}>
                  <p style={{ margin: 0, color: 'var(--civic-muted)', fontSize: 10, lineHeight: 1.7 }}>{item.description}</p>
                  <div className="stat-grid" style={{ gridTemplateColumns: 'repeat(3,1fr)', marginTop: 14 }}>
                    <MiniStat label="Supports" value={item.upvoteCount || 0} />
                    <MiniStat label="Followers" value={item.followerCount || 0} />
                    <MiniStat label="Evidence" value={item.imageCount || 0} />
                  </div>
                  <p className="muted" style={{ marginTop: 12, fontSize: 9 }}>Last activity: {new Date(item.lastActivityAt).toLocaleString()} · Followed: {new Date(item.followedAt).toLocaleString()}</p>
                </div>
                <div className="issue-card-footer">
                  <span className="support-count">{item.departmentName} · {item.priority}</span>
                  <div style={{ display: 'flex', gap: 7 }}>
                    <button type="button" disabled={busyId === item.id} onClick={() => unfollow(item)} className="button outline small">{busyId === item.id ? 'Updating…' : 'Unfollow'}</button>
                    <Link to={`/citizen/complaints/${item.id}`} className="button primary small">View updates</Link>
                  </div>
                </div>
              </article>
            ))}
          </div>

          <div className="page-actions section-gap" style={{ justifyContent: 'center' }}>
            <button type="button" disabled={filters.page <= 1} onClick={() => setFilters((current) => ({ ...current, page: current.page - 1 }))} className="button outline">Previous</button>
            <span className="muted">Page {filters.page} of {totalPages} · {result.totalCount} followed</span>
            <button type="button" disabled={filters.page >= totalPages} onClick={() => setFilters((current) => ({ ...current, page: current.page + 1 }))} className="button outline">Next</button>
          </div>
        </>
      ) : (
        <div className="surface section-gap"><div className="surface-body" style={{ padding: 42, textAlign: 'center' }}><div className="feature-icon" style={{ margin: '0 auto' }}>☆</div><h3 style={{ margin: '15px 0 6px' }}>No followed complaints</h3><p className="muted" style={{ fontSize: 10 }}>Follow a community issue to receive updates without adding a support vote.</p><Link to="/citizen/nearby" className="button primary" style={{ marginTop: 14 }}>Browse nearby issues</Link></div></div>
      )}
    </section>
  );
}

function MiniStat({ label, value }) {
  return <article className="stat-card" style={{ minHeight: 70 }}><small style={{ marginTop: 0 }}>{label}</small><strong style={{ marginTop: 7, fontSize: 17 }}>{value}</strong></article>;
}
