import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import CivicIcon from '../../components/ui/CivicIcon.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { getAdminOverview } from '../../services/adminApi.js';

export default function AdminDashboard() {
  const { user } = useAuth();
  const [stats, setStats] = useState(null);
  const [error, setError] = useState('');
  useEffect(() => { getAdminOverview().then(setStats).catch((reason) => setError(reason.message)); }, []);

  return (
    <section className="page-wrap">
      <div className="surface summary-banner"><div className="summary-user"><div className="avatar">AD</div><div><p className="section-kicker">Admin summary</p><h2>System governance dashboard</h2><p>Signed in as {user?.role}. Manage users, operational controls, analytics and audit evidence.</p></div></div><div className="page-actions"><Link to="/admin/governance" className="button primary">Open governance</Link><Link to="/admin/system-health" className="button outline">System health</Link></div></div>
      {error && <div className="alert error section-gap">{error}</div>}

      <div className="stat-grid six section-gap">
        <Stat icon="department" label="Departments" value={stats?.activeDepartments ?? '—'} hint="Active master records" />
        <Stat icon="ward" label="Wards" value={stats?.activeWards ?? '—'} hint="Geographic areas" />
        <Stat icon="category" label="Categories" value={stats?.activeCategories ?? '—'} hint="Complaint categories" tone="violet" />
        <Stat icon="users" label="Active users" value={stats?.activeUsers ?? '—'} hint="All enabled accounts" tone="green" />
        <Stat icon="alert" label="Open complaints" value={stats?.openComplaints ?? '—'} hint="Requires processing" tone="amber" />
        <Stat icon="audit" label="Audit events today" value={stats?.auditEventsToday ?? '—'} hint="Traceable actions" tone="red" />
      </div>

      <div className="dashboard-grid main-aside section-gap">
        <section className="surface"><div className="surface-header"><h3>Complaint analytics</h3><div className="page-actions"><button className="button outline small">30 days</button><button className="button ghost small">90 days</button><button className="button ghost small">1 year</button></div></div><div className="surface-body"><div className="chart-placeholder"><svg className="chart-line" viewBox="0 0 600 220" preserveAspectRatio="none" aria-hidden="true"><polyline points="0,175 70,135 140,155 215,95 285,112 360,66 435,88 520,45 600,62" fill="none" stroke="#1769e0" strokeWidth="6" strokeLinecap="round" /><polyline points="0,190 70,175 140,168 215,150 285,143 360,123 435,117 520,100 600,91" fill="none" stroke="#15966f" strokeWidth="4" strokeLinecap="round" /><polyline points="0,202 70,192 140,180 215,178 285,166 360,158 435,145 520,140 600,125" fill="none" stroke="#d38a10" strokeWidth="4" strokeLinecap="round" /></svg><div className="chart-legend"><span>Garbage</span><span>Roads</span><span>Streetlights</span></div></div></div></section>
        <section className="surface"><div className="surface-header"><h3>Complaint heatmap</h3><Link to="/admin/analytics" className="button ghost small">Full analytics <CivicIcon name="arrow" size={15} /></Link></div><div className="surface-body"><div className="map-placeholder"><span className="map-pin-dot p1" /><span className="map-pin-dot p2" /><span className="map-pin-dot p3" /><span className="map-pin-dot p4" /><div className="map-overlay-card"><strong>Ward hotspot view</strong><p>Open analytics for category, SLA and date-range filters.</p></div></div></div></section>
      </div>

      <section className="surface section-gap">
        <div className="surface-header"><h3>Operational management</h3><Link to="/admin/users" className="button primary small">Manage users</Link></div>
        <div className="quick-actions" style={{ padding: 15 }}>
          <Quick to="/admin/users" icon="users" title="User and officer management" text="Activate, deactivate and assign roles" />
          <Quick to="/admin/ai-review" icon="track" title="AI review queue" text="Review suspicious complaints" />
          <Quick to="/admin/audit-logs" icon="audit" title="Audit logs" text="Inspect secure action history" />
          <Quick to="/admin/reports" icon="report" title="Generate reports" text="Export operational evidence" />
        </div>
      </section>

      <div className="dashboard-grid equal section-gap">
        <section className="surface"><div className="surface-header"><h3>Department work tracking</h3><Link to="/admin/analytics" className="button ghost small">View performance <CivicIcon name="arrow" size={15} /></Link></div><div className="civic-table-wrap"><table className="civic-table" style={{ minWidth: 480 }}><thead><tr><th>Department</th><th>Workload</th><th>Completed</th><th>Pending</th></tr></thead><tbody><tr><td><strong>Roads</strong></td><td>Live analytics</td><td>See dashboard</td><td>Open queue</td></tr><tr><td><strong>Sanitation</strong></td><td>Live analytics</td><td>See dashboard</td><td>Open queue</td></tr><tr><td><strong>Electrical</strong></td><td>Live analytics</td><td>See dashboard</td><td>Open queue</td></tr></tbody></table></div></section>
        <section className="surface"><div className="surface-header"><h3>System alerts</h3><Link to="/admin/system-health" className="button ghost small">Open health centre <CivicIcon name="arrow" size={15} /></Link></div><div className="surface-body notification-list"><Notice icon="health" title="Database and storage" text="Use system health to verify AWS RDS and Amazon S3 dependencies." /><Notice icon="bell" title="Notification channels" text="Review SignalR, email, SMS and queue delivery status." /><Notice icon="audit" title="Security controls" text="Inspect lockouts, permissions and audit activity." /></div></section>
      </div>
    </section>
  );
}

function Stat({ icon, label, value, hint, tone='' }) { return <article className={`stat-card ${tone}`}><span className="stat-icon"><CivicIcon name={icon} size={20} /></span><small>{label}</small><strong>{value}</strong><p>{hint}</p></article>; }
function Quick({ to, icon, title, text }) { return <Link to={to} className="quick-action"><span><CivicIcon name={icon} size={21} /></span><div><strong>{title}</strong><small>{text}</small></div></Link>; }
function Notice({ icon, title, text }) { return <div className="notification-item"><span><CivicIcon name={icon} size={19} /></span><div><strong>{title}</strong><small>{text}</small></div></div>; }
