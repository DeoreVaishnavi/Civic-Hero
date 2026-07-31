import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import StatusBadge from '../../components/common/StatusBadge.jsx';
import { complaintApi } from '../../services/complaintApi.js';

export default function ComplaintTimeline() {
  const { id } = useParams();
  const [detail, setDetail] = useState(null);
  const [error, setError] = useState('');
  useEffect(() => { complaintApi.getById(id).then(setDetail).catch((reason) => setError(reason.message)); }, [id]);
  const item = detail?.complaint;
  if (!item) return <section className="page-wrap narrow">{error ? <div className="alert error">{error}</div> : <div className="surface"><div className="surface-body">Loading timeline…</div></div>}</section>;
  return <section className="page-wrap narrow">
    <div className="page-title-row"><div><p className="section-kicker">Complaint process history</p><h2>{item.title}</h2><p>{item.referenceNumber} · Current status: {readable(item.status)}</p></div><div className="page-actions"><StatusBadge status={item.status} /><Link to={`/citizen/complaints/${id}`} className="button outline">← Back to complaint</Link></div></div>
    <section className="surface"><div className="surface-header"><h3>Timeline</h3><span className="status-pill green">Audit trail</span></div><div className="surface-body timeline-list">{detail.timeline.map((event,index) => <div key={event.id} className="timeline-item complete"><span className="timeline-dot" /><h4>{readable(event.eventType)} <small style={{ color:'var(--civic-muted)', fontWeight:500 }}>· {new Date(event.timestamp).toLocaleString()}</small></h4><p>{event.description} · By {event.actorName}</p>{index === 0 && <div className="issue-photo" style={{ marginTop: 10, maxWidth: 430, minHeight: 130 }}>Original complaint evidence</div>}</div>)}</div></section>
    <section className="surface section-gap"><div className="surface-header"><h3>Final status</h3></div><div className="surface-body"><div className={`alert ${['Closed','ClosedAuto'].includes(item.status) ? 'success' : 'warning'}`}><strong>{readable(item.status)}</strong> — {['Closed','ClosedAuto'].includes(item.status) ? 'The complaint process is complete.' : 'The complaint is still moving through its workflow.'}</div></div></section>
  </section>;
}
function readable(value='') { return String(value).replace(/([a-z])([A-Z])/g,'$1 $2'); }
