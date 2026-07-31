import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import StatusBadge from '../../components/common/StatusBadge.jsx';
import { complaintApi } from '../../services/complaintApi.js';

export default function UpvotePage() {
  const { id } = useParams();
  const [detail, setDetail] = useState(null);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const load = () => complaintApi.getById(id).then(setDetail).catch((reason) => setError(reason.message));
  useEffect(() => { load(); }, [id]);
  const support = async () => { try { await complaintApi.upvote(id); setMessage('You supported this issue successfully.'); await load(); } catch (reason) { setError(reason.message); } };
  const item = detail?.complaint;

  if (!item) return <section className="page-wrap narrow">{error ? <div className="alert error">{error}</div> : <div className="surface"><div className="surface-body">Loading issue…</div></div>}</section>;
  return <section className="page-wrap narrow">
    <div className="page-title-row"><div><p className="section-kicker">Support existing complaint</p><h2>{item.title}</h2><p>{item.category} · {item.wardName} · {item.referenceNumber}</p></div><div className="page-actions"><StatusBadge status={item.status} /><Link to="/citizen/nearby" className="button outline">← Back to common issues</Link></div></div>
    {error && <div className="alert error">{error}</div>}{message && <div className="alert success">{message}</div>}
    <section className="surface"><div className="surface-header"><h3>Image gallery</h3><span className="muted" style={{ fontSize: 8 }}>Complaint evidence</span></div><div className="surface-body"><div className="image-preview-grid">{(detail.images.length ? detail.images.slice(0,3) : [1,2,3]).map((image,index) => <div key={image.id || index} className="issue-photo" style={{ minHeight: 150 }}>Evidence image {index+1}</div>)}</div></div></section>
    <section className="surface section-gap"><div className="surface-header"><h3>Details</h3></div><div className="surface-body"><p style={{ margin: 0, fontSize: 11, lineHeight: 1.8 }}>{item.description}</p></div></section>
    <div className="dashboard-grid main-aside section-gap"><section className="surface"><div className="surface-header"><h3>Location / map</h3></div><div className="surface-body"><div className="map-placeholder"><span className="map-pin-dot p2" /><div className="map-overlay-card"><strong>{item.address}</strong><p>{item.latitude}, {item.longitude}</p></div></div></div></section><section className="surface"><div className="surface-header"><h3>Support section</h3></div><div className="surface-body"><strong style={{ fontSize: 32 }}>{item.upvoteCount || 0}</strong><p className="muted" style={{ fontSize: 9 }}>Citizens already supporting this complaint</p><button type="button" onClick={support} disabled={item.hasUpvoted} className={`button ${item.hasUpvoted ? 'success' : 'primary'} full large`}>{item.hasUpvoted ? '✓ Already supported' : '👍 Support this issue'}</button><Link to={`/citizen/complaints/${id}`} className="button outline full" style={{ marginTop: 8 }}>View full complaint</Link></div></section></div>
  </section>;
}
