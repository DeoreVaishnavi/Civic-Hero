import { Link } from 'react-router-dom';
import SlaBadge from './SlaBadge.jsx';
import StatusBadge from './StatusBadge.jsx';

export default function AssignmentCard({ item, basePath, actionLabel = 'Open task' }) {
  return (
    <article className="issue-card">
      <div className="issue-card-head"><div><span style={{ color: 'var(--civic-blue)', fontFamily: 'ui-monospace, monospace', fontSize: 8, fontWeight: 850 }}>{item.referenceNumber}</span><h3 style={{ marginTop: 6 }}>{item.title}</h3><p>{item.category} · {item.wardName} · {item.departmentName}</p></div><SlaBadge state={item.slaState} remainingMinutes={item.remainingMinutes} /></div>
      <div style={{ padding: '0 14px' }}><div style={{ display: 'flex', gap: 7, flexWrap: 'wrap' }}><StatusBadge status={item.complaintStatus} /><span className="status-pill amber">{item.priority}</span></div><p style={{ minHeight: 43, color: 'var(--civic-muted)', fontSize: 9, lineHeight: 1.6 }}>{item.description}</p></div>
      <div className="issue-card-footer"><span style={{ color: 'var(--civic-muted)', fontSize: 8 }}>{item.officerName || 'Unassigned'} · {item.address}</span><Link to={`${basePath}/${item.complaintId}`} className="button primary small">{actionLabel}</Link></div>
    </article>
  );
}
