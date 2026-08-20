import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { getApiHealth, getCorrelationHeaderCheck, getDependencyHealth } from '../../services/healthApi.js';
import { mapApiError } from '../../utils/errorMapper.js';

const features = [
  ['◎', 'Report with evidence', 'Submit GPS-tagged issues with clear images, category, ward and a useful description.'],
  ['✓', 'Verify the resolution', 'Review before-and-after evidence and accept the fix or raise a transparent dispute.'],
  ['⌖', 'Explore city hotspots', 'See nearby complaints, ward activity and heatmap trends without creating duplicates.'],
  ['★', 'Earn civic rewards', 'Receive points and badges for valid reports, helpful verification and community support.'],
];

const steps = [
  ['01', 'Report', 'Add issue details, location and photos.'],
  ['02', 'AI triage', 'The system checks category, priority and duplicates.'],
  ['03', 'Assignment', 'The correct department and field officer receive it.'],
  ['04', 'Resolution', 'Work progress and after-evidence are recorded.'],
  ['05', 'Verify', 'The citizen approves or disputes the final result.'],
];

export default function HomePage() {
  const [status, setStatus] = useState('checking');
  const [health, setHealth] = useState(null);
  const [correlationCheck, setCorrelationCheck] = useState(null);
  const [dependencies, setDependencies] = useState([]);
  const [error, setError] = useState(null);

  const checkHealth = useCallback(async () => {
    setStatus('checking');
    setError(null);
    try {
      const [healthResponse, headerResponse, dependencyResponse] = await Promise.all([
        getApiHealth(),
        getCorrelationHeaderCheck(),
        getDependencyHealth(),
      ]);
      setHealth(healthResponse.data);
      setCorrelationCheck(headerResponse.data);
      setDependencies(dependencyResponse.data?.checks ?? []);
      setStatus('connected');
    } catch (requestError) {
      setHealth(null);
      setCorrelationCheck(null);
      setDependencies([]);
      setError(mapApiError(requestError));
      setStatus('unavailable');
    }
  }, []);

  useEffect(() => { checkHealth(); }, [checkHealth]);

  const dependencyMap = useMemo(
    () => Object.fromEntries(dependencies.map((item) => [item.name, item])),
    [dependencies],
  );

  return (
    <>
      <section className="landing-hero">
        <div className="landing-hero-inner">
          <div className="hero-copy">
            <p className="section-kicker">Transparent city complaint management</p>
            <h1>Report. Verify. <span>Improve your city.</span></h1>
            <p>CivicHero connects citizens, field officers and municipal teams through evidence-based reporting, live status tracking and accountable resolution.</p>
            <div className="hero-actions">
              <Link to={ROUTE_PATHS.anonymousReport} className="button primary large"><span aria-hidden="true">＋</span> Report a complaint</Link>
              <Link to={ROUTE_PATHS.anonymousTrack} className="button outline large"><span aria-hidden="true">⌕</span> Track an issue</Link>
              <button type="button" onClick={checkHealth} disabled={status === 'checking'} className="button ghost large">{status === 'checking' ? 'Checking system…' : 'Check system status'}</button>
            </div>
            <div className="hero-proof">
              <span>GPS and photo evidence</span>
              <span>Role-based workflows</span>
              <span>Citizen verification</span>
              <span>Rewards and leaderboard</span>
            </div>

            <SystemBanner status={status} health={health} correlationCheck={correlationCheck} error={error} />
          </div>

          <div className="hero-dashboard-preview" aria-label="CivicHero dashboard preview">
            <div className="preview-window">
              <div className="preview-topbar">
                <div className="preview-dots"><i /><i /><i /></div>
                <span>Citizen Dashboard</span>
                <span>🔔 3</span>
              </div>
              <div className="preview-body">
                <div className="preview-side">
                  <strong>CivicHero</strong>
                  <span>Dashboard</span><span>Report Issue</span><span>My Complaints</span><span>Heatmap</span><span>Rewards</span>
                </div>
                <div className="preview-content">
                  <div className="preview-stat-row">
                    <div className="preview-stat"><small>Open issues</small><b>18</b></div>
                    <div className="preview-stat"><small>Resolved</small><b>92</b></div>
                    <div className="preview-stat"><small>Your points</small><b>1,250</b></div>
                  </div>
                  <div className="preview-chart">
                    <svg viewBox="0 0 500 160" preserveAspectRatio="none" aria-hidden="true">
                      <polyline points="0,130 75,105 140,117 205,72 270,86 340,54 410,65 500,24" fill="none" stroke="#1769e0" strokeWidth="5" strokeLinecap="round" strokeLinejoin="round" />
                      <polyline points="0,145 75,135 140,128 205,120 270,105 340,98 410,82 500,74" fill="none" stroke="#15966f" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  </div>
                  <div className="preview-list">
                    <div><i /><b>Pothole on Main Street</b><em>In progress</em></div>
                    <div><i /><b>Garbage near city park</b><em>Assigned</em></div>
                    <div><i /><b>Streetlight not working</b><em>Resolved</em></div>
                  </div>
                </div>
              </div>
            </div>
            <div className="float-card"><strong>Ward 3 resolution rate</strong><small>72% of complaints resolved within SLA</small><div className="progress"><i /></div></div>
          </div>
        </div>
      </section>

      <section id="city-impact" className="city-impact">
        <div className="city-impact-inner">
          <Impact value="1,240+" label="Complaints reported" />
          <Impact value="900" label="Issues resolved" />
          <Impact value="36 hrs" label="Average response time" />
          <Impact value="8,000+" label="Active citizens" />
        </div>
      </section>

      <section className="public-section">
        <div className="public-section-inner">
          <div className="public-section-heading"><p className="section-kicker">One platform, complete visibility</p><h2>Everything needed for accountable civic service</h2><p>The visual structure follows your page blueprints while using a cleaner, responsive design system suitable for a real public-service application.</p></div>
          <div className="feature-grid">{features.map(([icon, title, text]) => <article key={title} className="feature-card"><span className="feature-icon">{icon}</span><h3>{title}</h3><p>{text}</p></article>)}</div>
        </div>
      </section>

      <section id="how-it-works" className="public-section soft">
        <div className="public-section-inner">
          <div className="public-section-heading"><p className="section-kicker">Complaint lifecycle</p><h2>From citizen report to verified closure</h2><p>Every important action is visible, timestamped and linked to the responsible role.</p></div>
          <div className="workflow-row">{steps.map(([number, title, text]) => <div key={number} className="workflow-step"><b>{number}</b><h3>{title}</h3><p>{text}</p></div>)}</div>
          <div style={{ textAlign: 'center', marginTop: 42 }}><Link to={ROUTE_PATHS.register} className="button primary large">Create your citizen account</Link></div>
        </div>
      </section>

      <section className="public-section" style={{ paddingTop: 55, paddingBottom: 55 }}>
        <div className="public-section-inner">
          <div className="surface flat" style={{ padding: 20, display: 'grid', gridTemplateColumns: 'repeat(4,1fr)', gap: 12 }}>
            <Dependency label="Frontend" value="React 19 + Vite" status="Healthy" />
            <Dependency label="Backend" value={health?.service || 'ASP.NET Core 8'} status={status === 'connected' ? 'Healthy' : 'Checking'} />
            <Dependency label="Database" value="AWS RDS MySQL 8" status={dependencyMap.database?.status || 'Checking'} />
            <Dependency label="Storage" value="Amazon S3" status={dependencyMap.storage?.status || 'Checking'} />
          </div>
        </div>
      </section>
    </>
  );
}

function Impact({ value, label }) { return <div className="impact-item"><strong>{value}</strong><span>{label}</span></div>; }

function Dependency({ label, value, status }) {
  const healthy = String(status).toLowerCase() === 'healthy';
  return <div style={{ padding: 14, borderRight: '1px solid var(--civic-line)' }}><small style={{ display: 'block', color: 'var(--civic-muted)', fontSize: 8, fontWeight: 900, letterSpacing: 1, textTransform: 'uppercase' }}>{label}</small><strong style={{ display: 'block', marginTop: 6, fontSize: 12 }}>{value}</strong><span style={{ display: 'inline-flex', marginTop: 8, color: healthy ? 'var(--civic-green)' : 'var(--civic-amber)', fontSize: 8, fontWeight: 850 }}>{healthy ? '● Healthy' : '● ' + status}</span></div>;
}

function SystemBanner({ status, health, correlationCheck, error }) {
  if (status === 'checking') return <div className="alert warning" style={{ marginTop: 24 }}>Checking CivicHero API and cloud dependencies…</div>;
  if (status === 'connected') return <div className="alert success" style={{ marginTop: 24 }}><strong>System is connected.</strong> API {health?.version || '1.0.0'} · Correlation ID {correlationCheck?.correlationId || health?.correlationId || 'available'}</div>;
  return <div className="alert error" style={{ marginTop: 24 }}><strong>{error?.title || 'API unavailable'}.</strong> {error?.message || 'Start the backend to use live features.'}</div>;
}
