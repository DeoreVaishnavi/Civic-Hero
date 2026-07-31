import { useEffect, useMemo, useState } from 'react';
import { initiativeApi } from '../../services/initiativeApi.js';

export default function InitiativeEngagement() {
  const [data, setData] = useState({ items: [], summaries: [], totalCount: 0 });
  const [type, setType] = useState('All');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    initiativeApi.supervisorActivity({ page: 1, pageSize: 200, type })
      .then((result) => { if (active) setData(result); })
      .catch((reason) => { if (active) setError(reason.message || 'Unable to load initiative activity.'); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [type]);

  const visibleItems = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return data.items || [];
    return (data.items || []).filter((item) => [item.initiativeTitle, item.citizenName, item.citizenEmail, item.category, item.feedback, item.activityType]
      .filter(Boolean)
      .some((value) => String(value).toLowerCase().includes(query)));
  }, [data.items, search]);

  return (
    <section className="page-wrap">
      <div className="surface summary-banner">
        <div className="summary-user"><div className="avatar">IF</div><div><p className="section-kicker">Citizen participation</p><h2>Initiative follows and feedback</h2><p>Review which public initiatives citizens follow and the feedback they send to the supervisor team.</p></div></div>
        <div className="summary-meta"><span>Activity: {data.totalCount ?? 0}</span><span>Visible: {visibleItems.length}</span><span>Initiatives: {data.summaries?.length ?? 0}</span></div>
      </div>

      {error && <div className="alert error section-gap">{error}</div>}

      <div className="stat-grid section-gap">
        {(data.summaries || []).map((item) => <article key={item.initiativeId} className="stat-card">
          <span className="stat-icon">◎</span>
          <small>{item.initiativeTitle}</small>
          <strong>{item.followerCount}</strong>
          <p>{item.feedbackCount} feedback · {item.feedbackCount ? `${item.averageRating}/5 average` : 'No rating yet'}</p>
        </article>)}
      </div>

      <section className="surface section-gap">
        <div className="surface-header"><div><h3>Citizen activity</h3><p className="muted" style={{ margin: '.35rem 0 0', fontSize: 9 }}>Follow, unfollow and feedback submissions are recorded here.</p></div></div>
        <div className="surface-body">
          <div style={{ display: 'grid', gap: 12, gridTemplateColumns: 'minmax(0,1fr) minmax(180px,.3fr)', marginBottom: 18 }}>
            <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search citizen, initiative or feedback" className="civic-input" />
            <select value={type} onChange={(event) => setType(event.target.value)} className="civic-input"><option>All</option><option>Feedback</option><option>Followed</option><option>Unfollowed</option></select>
          </div>

          {loading ? <p className="muted">Loading initiative activity…</p> : <div className="notification-list">
            {visibleItems.map((item) => <article key={item.id} className="notification-item" style={{ gridTemplateColumns: '42px 1fr', padding: '16px 0' }}>
              <span style={{ width: 38, height: 38 }}>{item.activityType === 'Feedback' ? '★' : item.activityType === 'Followed' ? '+' : '−'}</span>
              <div>
                <div style={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'space-between', gap: 8 }}><strong style={{ fontSize: 11 }}>{item.citizenName} · {item.activityType}</strong><small>{new Date(item.createdAt).toLocaleString()}</small></div>
                <small style={{ marginTop: 5 }}>{item.initiativeTitle}{item.citizenEmail ? ` · ${item.citizenEmail}` : ''}</small>
                {item.activityType === 'Feedback' && <div style={{ marginTop: 10, borderRadius: 12, background: '#f8fafc', padding: 12 }}><strong style={{ fontSize: 9 }}>{item.rating}/5 · {item.category || 'General'}</strong><p style={{ margin: '6px 0 0', fontSize: 10, color: '#475569' }}>{item.feedback}</p></div>}
              </div>
            </article>)}
            {!visibleItems.length && <p className="muted">No initiative activity matches the selected filter.</p>}
          </div>}
        </div>
      </section>

      <style>{`.civic-input{width:100%;border:1px solid #dbe3ec;border-radius:12px;background:white;padding:11px 13px;color:#26364d;outline:none}.civic-input:focus{border-color:#4f8ef7;box-shadow:0 0 0 4px rgba(79,142,247,.12)}@media(max-width:700px){.surface-body>div:first-child{grid-template-columns:1fr!important}}`}</style>
    </section>
  );
}
