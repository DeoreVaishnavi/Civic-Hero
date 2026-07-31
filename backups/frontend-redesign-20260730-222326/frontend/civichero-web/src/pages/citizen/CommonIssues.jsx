import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useGeoLocation } from '../../hooks/useGeoLocation.js';
import { complaintApi } from '../../services/complaintApi.js';
import StatusBadge from '../../components/common/StatusBadge.jsx';

export default function CommonIssues() {
  const geo = useGeoLocation();
  const [radiusKm, setRadiusKm] = useState(5);
  const [items, setItems] = useState([]);
  const [sort, setSort] = useState('MostSupported');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  const search = async () => {
    try {
      const coordinates = await geo.locate();
      const result = await complaintApi.nearby({ ...coordinates, radiusKm });
      setItems(Array.isArray(result) ? result : result?.items || []);
      setError('');
    } catch (reason) { setError(reason.message); }
  };

  const support = async (item) => {
    try {
      await complaintApi.upvote(item.id);
      setItems((current) => current.map((row) => row.id === item.id ? { ...row, upvoteCount: (row.upvoteCount || 0) + 1, hasUpvoted: true } : row));
      setMessage(`You supported ${item.referenceNumber || item.title}.`);
    } catch (reason) { setError(reason.message); }
  };

  const sorted = [...items].sort((a, b) => sort === 'MostSupported' ? (b.upvoteCount || 0) - (a.upvoteCount || 0) : new Date(b.createdAt) - new Date(a.createdAt));

  return (
    <section className="page-wrap narrow">
      <div className="page-title-row"><div><p className="section-kicker">Avoid duplicate reports</p><h2>Common issues in your area</h2><p>Find existing complaints near your location and support them instead of creating duplicates.</p></div><Link to="/citizen/report" className="button primary">＋ Report new issue</Link></div>
      {error && <div className="alert error">{error || geo.error}</div>}
      {message && <div className="alert success">{message}</div>}

      <div className="filter-bar">
        <select className="input" aria-label="Ward filter"><option>All wards</option></select>
        <select className="input" aria-label="Category filter"><option>All categories</option></select>
        <select className="input" value={radiusKm} onChange={(e) => setRadiusKm(Number(e.target.value))}><option value="1">Within 1 km</option><option value="5">Within 5 km</option><option value="10">Within 10 km</option><option value="25">Within 25 km</option></select>
        <select className="input" value={sort} onChange={(e) => setSort(e.target.value)}><option value="MostSupported">Most supported</option><option value="Newest">Newest first</option></select>
        <button type="button" onClick={search} disabled={geo.loading} className="button primary">{geo.loading ? 'Locating…' : '⌖ Find nearby issues'}</button>
      </div>

      {sorted.length ? <div className="issue-card-grid section-gap">{sorted.map((item) => <article key={item.id} className="issue-card">
        <div className="issue-card-head"><div><h3>{item.title}</h3><p>{item.referenceNumber} · {item.category} · {item.wardName || 'Ward'}</p></div><StatusBadge status={item.status} /></div>
        <div className="issue-card-images"><div className="issue-photo">Before image</div><div className="issue-photo">Area view</div><div className="issue-photo">Map</div></div>
        <div style={{ padding: '12px 14px 0' }}><p style={{ margin: 0, color: 'var(--civic-muted)', fontSize: 9, lineHeight: 1.6 }}>{item.description}</p></div>
        <div className="issue-card-footer"><span className="support-count">{item.upvoteCount || 0} supports · {item.distanceKm ?? '—'} km away</span><div style={{ display: 'flex', gap: 7 }}><button type="button" onClick={() => support(item)} disabled={item.hasUpvoted} className={`button ${item.hasUpvoted ? 'success' : 'outline'} small`}>{item.hasUpvoted ? '✓ Supported' : '👍 Support'}</button><Link to={`/citizen/complaints/${item.id}`} className="button primary small">View details</Link></div></div>
      </article>)}</div> : <div className="surface section-gap"><div className="surface-body" style={{ padding: 42, textAlign: 'center' }}><div className="feature-icon" style={{ margin: '0 auto' }}>⌖</div><h3 style={{ margin: '15px 0 6px' }}>Search your current area</h3><p className="muted" style={{ fontSize: 10 }}>Allow location access to discover active complaints nearby.</p><button type="button" onClick={search} className="button primary">Find nearby issues</button></div></div>}
    </section>
  );
}
