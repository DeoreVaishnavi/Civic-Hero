import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { rewardApi } from '../../services/rewardApi.js';

export default function Leaderboard() {
  const [rows, setRows] = useState([]);
  const [error, setError] = useState('');
  const [filters, setFilters] = useState({ range: 'All time', ward: 'All wards', category: 'All categories' });
  useEffect(() => { rewardApi.leaderboard(50).then((data) => setRows(Array.isArray(data) ? data : data?.items || [])).catch((reason) => setError(reason.message)); }, []);
  const top = useMemo(() => rows.slice(0, 3), [rows]);

  return (
    <section className="page-wrap">
      <div className="page-title-row"><div><p className="section-kicker">Positive civic competition</p><h2>Community leaderboard</h2><p>Top contributors improving the city through valid complaints, verification and helpful participation.</p></div><Link to="/citizen" className="button outline">← Back to dashboard</Link></div>
      {error && <div className="alert error">{error}</div>}
      <div className="filter-bar"><select className="input" value={filters.range} onChange={(e) => setFilters({ ...filters, range: e.target.value })}><option>All time</option><option>This month</option><option>This year</option></select><select className="input" value={filters.ward} onChange={(e) => setFilters({ ...filters, ward: e.target.value })}><option>All wards</option></select><select className="input" value={filters.category} onChange={(e) => setFilters({ ...filters, category: e.target.value })}><option>All categories</option></select><button className="button primary">Apply filters</button></div>

      <div className="dashboard-grid main-aside section-gap">
        <div>
          <section className="surface"><div className="surface-header"><h3>Top 3 citizens</h3><span className="status-pill amber">★ Community heroes</span></div><div className="surface-body"><div className="podium">{[top[1], top[0], top[2]].map((row, index) => <Podium key={row?.userId || index} row={row} first={index === 1} fallbackRank={index === 1 ? 1 : index === 0 ? 2 : 3} />)}</div></div></section>
          <section className="surface section-gap"><div className="surface-header"><h3>Leaderboard list</h3><span className="muted" style={{ fontSize: 8 }}>Only valid closed complaints earn points</span></div><div className="civic-table-wrap"><table className="civic-table"><thead><tr><th>Rank</th><th>Citizen</th><th>Tier</th><th>Closed issues</th><th>Points</th><th>Action</th></tr></thead><tbody>{rows.map((row) => <tr key={row.userId} style={{ background: row.isCurrentUser ? '#eef5ff' : undefined }}><td><strong>#{row.rank}</strong></td><td><strong>{row.citizenName}</strong>{row.isCurrentUser && <span className="status-pill" style={{ marginLeft: 7 }}>You</span>}</td><td>{row.tier}</td><td>{row.closedComplaints}</td><td><strong>{row.points}</strong></td><td><button className="button outline small">View profile</button></td></tr>)}{rows.length === 0 && <tr><td colSpan="6">Leaderboard data will appear after citizens earn points.</td></tr>}</tbody></table></div></section>
        </div>
        <aside className="dashboard-grid">
          <section className="surface"><div className="surface-header"><h3>User profile preview</h3></div><div className="surface-body" style={{ textAlign: 'center' }}><div className="podium-avatar">{top[0]?.citizenName?.split(' ').map((x) => x[0]).slice(0,2).join('') || 'CH'}</div><h3 style={{ marginBottom: 4 }}>{top[0]?.citizenName || 'Top citizen'}</h3><p className="muted" style={{ marginTop: 0, fontSize: 9 }}>{top[0]?.points ?? '—'} points · {top[0]?.tier || 'Citizen'}</p><div style={{ display: 'grid', gap: 7, margin: '16px 0' }}><span className="status-pill amber">★ Top reporter</span><span className="status-pill green">✓ Verifier</span><span className="status-pill">Active citizen</span></div><button className="button primary full">Follow citizen</button><button className="button outline full" style={{ marginTop: 8 }}>View profile</button></div></section>
          <section className="surface"><div className="surface-header"><h3>How points work</h3></div><div className="surface-body notification-list"><Notice title="Valid complaint" text="+10 points" /><Notice title="Verified closure" text="+20 points" /><Notice title="Support an issue" text="+2 points" /><Notice title="Helpful comment" text="+3 points" /></div></section>
        </aside>
      </div>
    </section>
  );
}

function Podium({ row, first, fallbackRank }) { return <article className={`podium-card ${first ? 'first' : ''}`}><div className="podium-avatar">{row?.citizenName?.split(' ').map((x) => x[0]).slice(0,2).join('') || fallbackRank}</div><h3>{row?.citizenName || `Rank ${fallbackRank}`}</h3><p>#{row?.rank || fallbackRank} · {row?.points ?? '—'} points</p></article>; }
function Notice({ title, text }) { return <div className="notification-item"><span>★</span><div><strong>{title}</strong><small>{text}</small></div></div>; }
