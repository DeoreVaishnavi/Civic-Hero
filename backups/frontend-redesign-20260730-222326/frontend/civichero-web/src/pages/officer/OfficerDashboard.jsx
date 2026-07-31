import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { assignmentApi } from '../../services/assignmentApi.js';
import StatusBadge from '../../components/common/StatusBadge.jsx';

export default function OfficerDashboard() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState('All');
  useEffect(() => { assignmentApi.officerDashboard().then(setData).catch((reason) => setError(reason.message)); }, []);

  const items = useMemo(() => {
    const rows = data?.priorityItems || [];
    if (filter === 'All') return rows;
    if (filter === 'Overdue') return rows.filter((item) => String(item.slaState).toLowerCase().includes('overdue'));
    if (filter === 'In progress') return rows.filter((item) => item.complaintStatus === 'InProgress');
    return rows.filter((item) => String(item.priority).toLowerCase() === 'high' || String(item.priority).toLowerCase() === 'critical');
  }, [data, filter]);
  const selected = items[0] || data?.priorityItems?.[0];

  return (
    <section className="page-wrap">
      <div className="surface summary-banner">
        <div className="summary-user"><div className="avatar">OF</div><div><p className="section-kicker">Officer summary</p><h2>Field operations dashboard</h2><p>Accept assigned work, update progress and upload resolution evidence.</p></div></div>
        <div className="summary-meta"><span>Pending: {data?.pending ?? '—'}</span><span>In progress: {data?.inProgress ?? '—'}</span><span>Overdue: {data?.overdue ?? '—'}</span><span>SLA: {data?.slaCompliancePercent ?? '—'}%</span></div>
      </div>
      {error && <div className="alert error section-gap">{error}</div>}

      <div className="stat-grid section-gap">
        <Stat icon="⌛" label="Awaiting response" value={data?.pending ?? '—'} hint="Accept before assignment SLA" tone="amber" />
        <Stat icon="↻" label="In progress" value={data?.inProgress ?? '—'} hint="Active field assignments" />
        <Stat icon="✓" label="Completed this week" value={data?.completedThisWeek ?? '—'} hint="Submitted for citizen review" tone="green" />
        <Stat icon="!" label="SLA overdue" value={data?.overdue ?? '—'} hint="Requires immediate attention" tone="red" />
      </div>

      <section className="surface section-gap">
        <div className="surface-header"><h3>Quick filters</h3><Link to="/officer/assignments" className="button ghost small">Open complete work queue →</Link></div>
        <div className="surface-body" style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>{['All','High priority','In progress','Overdue'].map((value) => <button type="button" key={value} onClick={() => setFilter(value)} className={`button ${filter === value ? 'primary' : 'outline'} small`}>{value}</button>)}</div>
      </section>

      <section className="surface section-gap">
        <div className="surface-header"><h3>List of assigned complaints</h3><span className="status-pill">{items.length} visible</span></div>
        <div className="civic-table-wrap"><table className="civic-table"><thead><tr><th>Complaint</th><th>Category</th><th>Ward</th><th>Priority</th><th>SLA</th><th>Status</th><th>Action</th></tr></thead><tbody>{items.map((item) => <tr key={item.complaintId}><td><strong>{item.title}</strong><br /><span>{item.referenceNumber}</span></td><td>{item.category}</td><td>{item.wardName}</td><td><span className="status-pill amber">{item.priority}</span></td><td>{item.slaState}</td><td><StatusBadge status={item.complaintStatus} /></td><td><Link to={`/officer/assignments/${item.complaintId}`} className="button primary small">View details</Link></td></tr>)}{items.length === 0 && <tr><td colSpan="7">No assignments match this filter.</td></tr>}</tbody></table></div>
      </section>

      {selected && <section className="surface section-gap">
        <div className="surface-header"><h3>Complaint action panel</h3><span className="status-pill amber">{selected.priority}</span></div>
        <div className="surface-body dashboard-grid main-aside">
          <div><h3 style={{ margin: 0, fontSize: 18 }}>{selected.title}</h3><p className="muted" style={{ fontSize: 9 }}>{selected.referenceNumber} · {selected.category} · {selected.wardName} · {selected.departmentName}</p><p style={{ fontSize: 11, lineHeight: 1.75 }}>{selected.description}</p><div className="page-actions"><Link to={`/officer/assignments/${selected.complaintId}`} className="button outline">Open full details</Link><Link to={`/officer/assignments/${selected.complaintId}/resolve`} className="button success">Upload resolution</Link></div></div>
          <div className="map-placeholder"><span className="map-pin-dot p2" /><div className="map-overlay-card"><strong>Field location</strong><p>{selected.address}</p></div></div>
        </div>
      </section>}
    </section>
  );
}

function Stat({ icon, label, value, hint, tone='' }) { return <article className={`stat-card ${tone}`}><span className="stat-icon">{icon}</span><small>{label}</small><strong>{value}</strong><p>{hint}</p></article>; }
