import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import CivicIcon from '../../components/ui/CivicIcon.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { complaintApi } from '../../services/complaintApi.js';
import { rewardApi } from '../../services/rewardApi.js';
import { useComplaintStore } from '../../store/complaintStore.js';

export default function CitizenDashboard() {
  const { user } = useAuth();
  const { dashboard, setDashboard } = useComplaintStore();
  const [error, setError] = useState('');
  const [points, setPoints] = useState(null);

  useEffect(() => {
    complaintApi.dashboard().then(setDashboard).catch((reason) => setError(reason.message));
    rewardApi.points().then(setPoints).catch(() => undefined);
  }, [setDashboard]);

  const recent = dashboard?.recentComplaints || [];
  const first = recent[0];

  return (
    <section className="page-wrap">
      <div className="surface summary-banner">
        <div className="summary-user">
          <div className="avatar">{(user?.fullName || 'C').split(' ').map((part) => part[0]).slice(0,2).join('').toUpperCase()}</div>
          <div><p className="section-kicker">Citizen summary</p><h2>{user?.fullName || 'Citizen'}</h2><p>{user?.email || 'Your CivicHero citizen account'}</p></div>
        </div>
        <div className="summary-meta"><span>Ward: {user?.wardName || user?.wardId || 'Not selected'}</span><span>Points: {points?.balance ?? '—'}</span><span>Rank: #{points?.rank ?? '—'}</span><span>Tier: {points?.tier || 'Citizen'}</span></div>
      </div>

      {error && <div className="alert error section-gap">{error}</div>}

      <div className="quick-actions section-gap">
        <Quick to="/citizen/report" icon="plus" title="Report complaint" text="Add issue, photos and GPS" />
        <Quick to="/citizen/nearby" icon="search" title="Nearby issues" text="Support existing complaints" />
        <Quick to="/citizen/heatmap" icon="map" title="City heatmap" text="Explore ward hotspots" />
        <Quick to="/citizen/leaderboard" icon="rewards" title="Leaderboard" text="See top civic contributors" />
      </div>

      <div className="stat-grid six section-gap">
        <Stat icon="complaints" label="Total complaints" value={dashboard?.total ?? '—'} hint="All reports" />
        <Stat icon="clock" label="Open" value={dashboard?.open ?? '—'} hint="Awaiting action" tone="amber" />
        <Stat icon="progress" label="In progress" value={dashboard?.inProgress ?? '—'} hint="Work underway" />
        <Stat icon="verify" label="Resolved" value={dashboard?.resolved ?? '—'} hint="Ready or closed" tone="green" />
        <Stat icon="support" label="Community support" value={dashboard?.totalUpvotes ?? '—'} hint="Total upvotes" tone="violet" />
        <Stat icon="rewards" label="CivicHero points" value={points?.balance ?? '—'} hint="Rewards balance" tone="amber" />
      </div>

      <div className="surface section-gap">
        <div className="surface-header"><h3>My complaints</h3><Link to="/citizen/complaints" className="button ghost small">View all complaints <CivicIcon name="arrow" size={15} /></Link></div>
        <div className="surface-body complaint-list">
          {recent.length ? recent.slice(0,4).map((item) => (
            <div key={item.id} className="complaint-list-row">
              <div className="issue-thumbnail"><CivicIcon name={categoryIcon(item.category)} size={20} /></div>
              <div><h4>{item.title}</h4><p>{item.referenceNumber} · {item.category} · {item.wardName || 'Ward'} · {new Date(item.createdAt).toLocaleDateString()}</p></div>
              <div className="row-actions"><span className={`status-pill ${statusTone(item.status)}`}><span className="status-dot" aria-hidden="true" />{readable(item.status)}</span><Link to={`/citizen/complaints/${item.id}`} className="button outline small">View</Link></div>
            </div>
          )) : <div style={{ padding: 20, textAlign: 'center' }}><strong>No complaints yet.</strong><p className="muted" style={{ fontSize: 10 }}>Your reported civic issues will appear here.</p><Link to="/citizen/report" className="button primary small">Report your first issue</Link></div>}
        </div>
      </div>

      <div className="dashboard-grid main-aside section-gap">
        <div className="dashboard-grid">
          <section className="surface">
            <div className="surface-header"><h3>Complaint timeline</h3>{first && <Link to={`/citizen/complaints/${first.id}`} className="button ghost small">Open complaint</Link>}</div>
            <div className="surface-body timeline-list">
              <Timeline title="Reported" text={first ? `${first.referenceNumber} submitted successfully` : 'Your complaint is submitted with evidence'} complete />
              <Timeline title="Assigned" text="The correct department and officer receive the issue" complete={Boolean(first && !['Created','AiTriage'].includes(first.status))} />
              <Timeline title="In progress" text="Field work and progress updates are recorded" complete={Boolean(first && ['InProgress','Resolved','VerificationPending','Closed'].includes(first.status))} />
              <Timeline title="Resolved and verified" text="You review evidence and confirm the result" complete={Boolean(first && ['Closed','ClosedAuto'].includes(first.status))} />
            </div>
          </section>

          <section className="surface">
            <div className="surface-header"><h3>Heatmap preview</h3><Link to="/citizen/heatmap" className="button outline small">View heatmap</Link></div>
            <div className="surface-body">
              <div className="map-placeholder"><span className="map-pin-dot p1" /><span className="map-pin-dot p2" /><span className="map-pin-dot p3" /><span className="map-pin-dot p4" /><div className="map-overlay-card"><strong>Ward activity</strong><p>High complaint concentration near the centre. Use filters on the full heatmap page.</p><div className="legend" style={{ marginTop: 9 }}><span><i />High</span><span><i />Medium</span><span><i />Low</span></div></div></div>
            </div>
          </section>
        </div>

        <aside className="dashboard-grid">
          <section className="surface">
            <div className="surface-header"><h3>Notifications</h3><Link to="/citizen/notifications" className="button ghost small">See all</Link></div>
            <div className="surface-body notification-list">
              <Notice icon="progress" title="Status updates" text="See assignment and work progress in real time." />
              <Notice icon="alert" title="Dispute alerts" text="Respond when a resolution needs your review." />
              <Notice icon="rewards" title="Reward alerts" text="Points and badges appear after valid closure." />
            </div>
          </section>
          <section className="surface">
            <div className="surface-header"><h3>Common issues</h3></div>
            <div className="surface-body"><p className="muted" style={{ marginTop: 0, fontSize: 10 }}>Support an existing nearby complaint instead of creating a duplicate.</p><Link to="/citizen/nearby" className="button outline full">Find nearby complaints</Link></div>
          </section>
          <section className="surface">
            <div className="surface-header"><h3>Points & rewards</h3></div>
            <div className="surface-body"><strong style={{ fontSize: 25 }}>{points?.balance ?? '—'} points</strong><p className="muted" style={{ fontSize: 9 }}>{points?.tier || 'Citizen tier'} · City rank #{points?.rank ?? '—'}</p><div className="progress-track" style={{ margin: '13px 0' }}><i style={{ width: `${Math.min(100, ((points?.balance || 0) % 500) / 5)}%` }} /></div><Link to="/citizen/rewards" className="button primary full">View and redeem rewards</Link></div>
          </section>
        </aside>
      </div>
    </section>
  );
}

function Quick({ to, icon, title, text }) { return <Link to={to} className="quick-action"><span><CivicIcon name={icon} size={21} /></span><div><strong>{title}</strong><small>{text}</small></div></Link>; }
function Stat({ icon, label, value, hint, tone='' }) { return <article className={`stat-card ${tone}`}><span className="stat-icon"><CivicIcon name={icon} size={20} /></span><small>{label}</small><strong>{value}</strong><p>{hint}</p></article>; }
function Timeline({ title, text, complete=false }) { return <div className={`timeline-item ${complete ? 'complete' : ''}`}><span className="timeline-dot" /><h4>{title}</h4><p>{text}</p></div>; }
function Notice({ icon, title, text }) { return <div className="notification-item"><span><CivicIcon name={icon} size={19} /></span><div><strong>{title}</strong><small>{text}</small></div></div>; }
function readable(value='') { return String(value).replace(/([a-z])([A-Z])/g, '$1 $2'); }
function statusTone(status='') { if (['Closed','ClosedAuto','Resolved'].includes(status)) return 'green'; if (['InProgress','VerificationPending'].includes(status)) return 'amber'; if (['Disputed','Escalated'].includes(status)) return 'red'; return ''; }
function categoryIcon(category='') { const value=String(category).toLowerCase(); if (value.includes('garbage')) return 'recycle'; if (value.includes('road')||value.includes('pothole')) return 'road'; if (value.includes('light')) return 'light'; if (value.includes('water')) return 'water'; return 'complaints'; }
