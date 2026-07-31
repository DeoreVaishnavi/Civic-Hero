import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { assignmentApi } from '../../services/assignmentApi.js';
import StatusBadge from '../../components/common/StatusBadge.jsx';

export default function SupervisorDashboard() {
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  useEffect(() => { assignmentApi.supervisorDashboard().then(setData).catch((reason) => setError(reason.message)); }, []);
  const items = data?.priorityItems || [];
  const selected = items[0];

  return (
    <section className="page-wrap">
      <div className="surface summary-banner">
        <div className="summary-user"><div className="avatar">SV</div><div><p className="section-kicker">Supervisor summary</p><h2>Operations and dispute control</h2><p>Balance workload, manage SLA risk and review escalated cases.</p></div></div>
        <div className="summary-meta"><span>Unassigned: {data?.unassigned ?? '—'}</span><span>Pending: {data?.pending ?? '—'}</span><span>Overdue: {data?.overdue ?? '—'}</span><span>SLA: {data?.slaCompliancePercent ?? '—'}%</span></div>
      </div>
      {error && <div className="alert error section-gap">{error}</div>}

      <div className="quick-actions section-gap">
        <Quick to="/supervisor/assignments" icon="≡" title="Assignment queue" text="Assign or reassign officers" />
        <Quick to="/supervisor/overdue" icon="!" title="Overdue work" text="Intervene in SLA breaches" />
        <Quick to="/supervisor/disputes" icon="⚖" title="Dispute review" text="Review citizen disputes" />
        <Quick to="/supervisor/analytics" icon="↗" title="Team analytics" text="Measure workload and performance" />
        <Quick to="/supervisor/initiative-engagement" icon="★" title="Initiative feedback" text="Review follows and citizen feedback" />
      </div>

      <div className="stat-grid section-gap">
        <Stat icon="○" label="Unassigned" value={data?.unassigned ?? '—'} hint={`${data?.reassignmentPending ?? '—'} need reassignment`} tone="amber" />
        <Stat icon="↻" label="In progress" value={data?.inProgress ?? '—'} hint={`${data?.pending ?? '—'} awaiting acceptance`} />
        <Stat icon="✓" label="Completed this week" value={data?.completedThisWeek ?? '—'} hint="Submitted for citizen verification" tone="green" />
        <Stat icon="!" label="Overdue" value={data?.overdue ?? '—'} hint="Requires supervisor intervention" tone="red" />
      </div>

      <section className="surface section-gap">
        <div className="surface-header"><h3>Highest SLA risk</h3><Link to="/supervisor/overdue" className="button danger small">View overdue queue</Link></div>
        <div className="civic-table-wrap"><table className="civic-table"><thead><tr><th>Complaint</th><th>Ward</th><th>Officer</th><th>Priority</th><th>SLA state</th><th>Status</th><th>Action</th></tr></thead><tbody>{items.map((item) => <tr key={item.complaintId}><td><strong>{item.title}</strong><br />{item.referenceNumber}</td><td>{item.wardName}</td><td>{item.officerName || 'Unassigned'}</td><td><span className="status-pill amber">{item.priority}</span></td><td>{item.slaState}</td><td><StatusBadge status={item.complaintStatus} /></td><td><Link to={`/supervisor/assignments/${item.complaintId}`} className="button primary small">Manage</Link></td></tr>)}{!items.length && <tr><td colSpan="7">No active assignment risk.</td></tr>}</tbody></table></div>
      </section>

      {selected && <div className="dashboard-grid main-aside section-gap">
        <section className="surface"><div className="surface-header"><h3>Selected complaint details</h3></div><div className="surface-body"><h3 style={{ marginTop: 0 }}>{selected.title}</h3><p className="muted" style={{ fontSize: 9 }}>{selected.referenceNumber} · {selected.category} · {selected.wardName}</p><p style={{ fontSize: 11, lineHeight: 1.7 }}>{selected.description}</p><div className="page-actions"><Link to={`/supervisor/assignments/${selected.complaintId}`} className="button primary">Manage assignment</Link><Link to="/supervisor/disputes" className="button outline">Open disputes</Link></div></div></section>
        <section className="surface"><div className="surface-header"><h3>Map / location</h3></div><div className="surface-body"><div className="map-placeholder"><span className="map-pin-dot p2" /><div className="map-overlay-card"><strong>{selected.wardName}</strong><p>{selected.address}</p></div></div></div></section>
      </div>}

      <div className="dashboard-grid equal section-gap">
        <section className="surface"><div className="surface-header"><h3>AI verification queue</h3><Link to="/supervisor/visual-verification" className="button ghost small">Review queue →</Link></div><div className="surface-body"><div className="notification-list"><Notice icon="◎" title="Before vs after comparison" text="Review suspicious or low-confidence visual evidence." /><Notice icon="!" title="Emergency review" text="Confirm urgent reports before priority escalation." /></div></div></section>
        <section className="surface"><div className="surface-header"><h3>Dispute decisions</h3><Link to="/supervisor/disputes" className="button ghost small">Open cases →</Link></div><div className="surface-body"><div className="notification-list"><Notice icon="⚖" title="Citizen dispute queue" text="Compare before, after and proof images before deciding." /><Notice icon="↻" title="Rework requests" text="Send incomplete work back to the field officer." /></div></div></section>
      </div>
    </section>
  );
}

function Quick({ to, icon, title, text }) { return <Link to={to} className="quick-action"><span>{icon}</span><div><strong>{title}</strong><small>{text}</small></div></Link>; }
function Stat({ icon, label, value, hint, tone='' }) { return <article className={`stat-card ${tone}`}><span className="stat-icon">{icon}</span><small>{label}</small><strong>{value}</strong><p>{hint}</p></article>; }
function Notice({ icon, title, text }) { return <div className="notification-item"><span>{icon}</span><div><strong>{title}</strong><small>{text}</small></div></div>; }
