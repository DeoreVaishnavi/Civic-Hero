import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { useGeoLocation } from '../../hooks/useGeoLocation.js';
import { complaintApi } from '../../services/complaintApi.js';
import StatusBadge from '../../components/common/StatusBadge.jsx';

export default function CommonIssues() {
  const geo = useGeoLocation();
  const [radiusKm, setRadiusKm] = useState(5);
  const [items, setItems] = useState([]);
  const [followedIds, setFollowedIds] = useState(new Set());
  const [busyId, setBusyId] = useState(null);
  const [sort, setSort] = useState('MostSupported');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  useEffect(() => {
    complaintApi.followingIds()
      .then((result) => setFollowedIds(new Set(result?.complaintIds || [])))
      .catch(() => {});
  }, []);

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
      if (item.hasUpvoted) await complaintApi.removeUpvote(item.id);
      else await complaintApi.upvote(item.id);
      setItems((current) => current.map((row) => row.id === item.id
        ? { ...row, upvoteCount: Math.max(0, (row.upvoteCount || 0) + (row.hasUpvoted ? -1 : 1)), hasUpvoted: !row.hasUpvoted }
        : row));
      setMessage(`${item.hasUpvoted ? 'Support removed from' : 'You supported'} ${item.referenceNumber || item.title}.`);
    } catch (reason) { setError(reason.message); }
  };

  const toggleFollow = async (item) => {
    setBusyId(item.id);
    setError('');
    setMessage('');
    try {
      const isFollowing = followedIds.has(item.id);
      const result = isFollowing ? await complaintApi.unfollow(item.id) : await complaintApi.follow(item.id);
      setFollowedIds((current) => {
        const next = new Set(current);
        if (result?.isFollowing) next.add(item.id); else next.delete(item.id);
        return next;
      });
      setMessage(result?.isFollowing
        ? `${item.referenceNumber || item.title} added to your following feed.`
        : `${item.referenceNumber || item.title} removed from your following feed.`);
    } catch (reason) {
      setError(reason.message || 'Follow status could not be updated.');
    } finally {
      setBusyId(null);
    }
  };

  const sorted = [...items].sort((a, b) => sort === 'MostSupported'
    ? (b.upvoteCount || 0) - (a.upvoteCount || 0)
    : new Date(b.createdAt) - new Date(a.createdAt));

  return (
    <section className="page-wrap narrow">
      <div className="page-title-row"><div><p className="section-kicker">Avoid duplicate reports</p><h2>Common issues in your area</h2><p>Support an existing complaint to raise its community priority, or follow it only to receive future updates.</p></div><div className="page-actions"><Link to="/citizen/following" className="button outline">Following feed</Link><Link to="/citizen/report" className="button primary">＋ Report new issue</Link></div></div>
      {error && <div className="alert error">{error || geo.error}</div>}
      {message && <div className="alert success">{message}</div>}

      <div className="filter-bar">
        <select className="input" aria-label="Ward filter"><option>All wards</option></select>
        <select className="input" aria-label="Category filter"><option>All categories</option></select>
        <select className="input" value={radiusKm} onChange={(event) => setRadiusKm(Number(event.target.value))}><option value="1">Within 1 km</option><option value="5">Within 5 km</option><option value="10">Within 10 km</option><option value="25">Within 25 km</option></select>
        <select className="input" value={sort} onChange={(event) => setSort(event.target.value)}><option value="MostSupported">Most supported</option><option value="Newest">Newest first</option></select>
        <button type="button" onClick={search} disabled={geo.loading} className="button primary">{geo.loading ? 'Locating…' : '⌖ Find nearby issues'}</button>
      </div>

      {sorted.length ? <div className="issue-card-grid section-gap">{sorted.map((item) => {
        const isFollowing = followedIds.has(item.id);
        return <article key={item.id} className="issue-card">
          <div className="issue-card-head"><div><h3>{item.title}</h3><p>{item.referenceNumber} · {item.category} · {item.wardName || 'Ward'}</p></div><StatusBadge status={item.status} /></div>
          <div className="issue-card-images"><div className="issue-photo">Before image</div><div className="issue-photo">Area view</div><div className="issue-photo">Map</div></div>
          <div style={{ padding: '12px 14px 0' }}><p style={{ margin: 0, color: 'var(--civic-muted)', fontSize: 9, lineHeight: 1.6 }}>{item.description}</p></div>
          <div className="issue-card-footer"><span className="support-count">{item.upvoteCount || 0} supports · {item.distanceKm ?? '—'} km away</span><div style={{ display: 'flex', flexWrap: 'wrap', gap: 7 }}><button type="button" onClick={() => support(item)} className={`button ${item.hasUpvoted ? 'success' : 'outline'} small`}>{item.hasUpvoted ? '✓ Supported' : '👍 Support'}</button><button type="button" onClick={() => toggleFollow(item)} disabled={busyId === item.id} className={`button ${isFollowing ? 'success' : 'outline'} small`}>{busyId === item.id ? 'Updating…' : isFollowing ? '✓ Following' : '☆ Follow'}</button><Link to={`/citizen/complaints/${item.id}`} className="button primary small">View details</Link></div></div>
        </article>;
      })}</div> : <div className="surface section-gap"><div className="surface-body" style={{ padding: 42, textAlign: 'center' }}><div className="feature-icon" style={{ margin: '0 auto' }}>⌖</div><h3 style={{ margin: '15px 0 6px' }}>Search your current area</h3><p className="muted" style={{ fontSize: 10 }}>Allow location access to discover active complaints nearby.</p><button type="button" onClick={search} className="button primary">Find nearby issues</button></div></div>}
    </section>
  );
}
