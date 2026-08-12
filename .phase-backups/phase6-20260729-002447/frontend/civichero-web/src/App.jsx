import React, { createContext, useContext, useEffect, useMemo, useState } from 'react';
import { Link, Navigate, Route, Routes, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  ROLE_CONFIG,
  initialComplaints,
  users,
  notifications,
  auditRows,
  monthlyTrend,
  departmentPerformance,
} from './demoData.js';

const DemoContext = createContext(null);
const useDemo = () => useContext(DemoContext);

const navByRole = {
  Citizen: [
    ['Dashboard', '/citizen/dashboard', '⌂'],
    ['Report Issue', '/citizen/report', '＋'],
    ['My Complaints', '/citizen/complaints', '▤'],
    ['Nearby Issues', '/citizen/nearby', '⌖'],
    ['Notifications', '/citizen/notifications', '◉'],
    ['Rewards', '/citizen/rewards', '★'],
    ['Profile', '/profile', '●'],
  ],
  Officer: [
    ['Dashboard', '/officer/dashboard', '⌂'],
    ['Work Queue', '/officer/queue', '▤'],
    ['Field Map', '/officer/map', '⌖'],
    ['Notifications', '/citizen/notifications', '◉'],
    ['Profile', '/profile', '●'],
  ],
  Supervisor: [
    ['Dashboard', '/supervisor/dashboard', '⌂'],
    ['Assignments', '/supervisor/assignments', '⇄'],
    ['Escalations', '/supervisor/escalations', '!'],
    ['Performance', '/supervisor/performance', '↗'],
    ['Profile', '/profile', '●'],
  ],
  Admin: [
    ['Dashboard', '/admin/dashboard', '⌂'],
    ['User Management', '/admin/users', '♟'],
    ['Departments', '/admin/departments', '▦'],
    ['Analytics', '/admin/analytics', '↗'],
    ['Audit Logs', '/admin/audit', '≡'],
    ['System Health', '/admin/health', '✓'],
    ['Profile', '/profile', '●'],
  ],
};

const homeByRole = {
  Citizen: '/citizen/dashboard',
  Officer: '/officer/dashboard',
  Supervisor: '/supervisor/dashboard',
  Admin: '/admin/dashboard',
};

function DemoProvider({ children }) {
  const [role, setRole] = useState(() => localStorage.getItem('civichero-demo-role') || 'Citizen');
  const [signedIn, setSignedIn] = useState(() => localStorage.getItem('civichero-demo-login') === 'true');
  const [complaints, setComplaints] = useState(() => {
    try {
      const saved = JSON.parse(localStorage.getItem('civichero-demo-complaints'));
      return Array.isArray(saved) && saved.length ? saved : initialComplaints;
    } catch {
      return initialComplaints;
    }
  });
  const [toast, setToast] = useState('');

  useEffect(() => localStorage.setItem('civichero-demo-role', role), [role]);
  useEffect(() => localStorage.setItem('civichero-demo-login', String(signedIn)), [signedIn]);
  useEffect(() => localStorage.setItem('civichero-demo-complaints', JSON.stringify(complaints)), [complaints]);
  useEffect(() => {
    if (!toast) return undefined;
    const timer = setTimeout(() => setToast(''), 3200);
    return () => clearTimeout(timer);
  }, [toast]);

  const updateComplaint = (id, patch, timelineText) => {
    setComplaints((items) => items.map((item) => {
      if (item.id !== id) return item;
      const nextTimeline = timelineText
        ? [...item.timeline, [timelineText, ROLE_CONFIG[role].name, 'Just now']]
        : item.timeline;
      return { ...item, ...patch, timeline: nextTimeline, updated: 'Just now' };
    }));
  };

  const addComplaint = (data) => {
    const number = 1100 + complaints.length + 1;
    const complaint = {
      id: `CH-2026-${number}`,
      title: data.title,
      category: data.category,
      description: data.description,
      location: data.location,
      ward: data.ward || 'Ward 12',
      department: categoryDepartment(data.category),
      status: 'Submitted',
      priority: data.priority || 'Medium',
      created: '28 Jul 2026',
      updated: 'Just now',
      reporter: ROLE_CONFIG.Citizen.name,
      officer: 'Unassigned',
      votes: 1,
      sla: '48 hours left',
      image: data.category.toLowerCase(),
      latitude: 19.076,
      longitude: 72.878,
      timeline: [['Complaint submitted with GPS location and photo evidence', 'Citizen', 'Just now']],
    };
    setComplaints((items) => [complaint, ...items]);
    return complaint;
  };

  const value = {
    role, setRole, signedIn, setSignedIn, complaints, setComplaints,
    updateComplaint, addComplaint, toast, setToast,
    resetDemo: () => {
      setComplaints(initialComplaints);
      setToast('Demo data restored successfully.');
    },
  };
  return <DemoContext.Provider value={value}>{children}</DemoContext.Provider>;
}

function categoryDepartment(category) {
  const map = {
    Roads: 'Roads & Infrastructure', Streetlights: 'Electrical', Sanitation: 'Solid Waste Management',
    Water: 'Water Supply', Drainage: 'Storm Water Drains', Footpaths: 'Roads & Infrastructure', Parks: 'Gardens',
  };
  return map[category] || 'Civic Operations';
}

function App() {
  return (
    <DemoProvider>
      <Routes>
        <Route path="/" element={<Landing />} />
        <Route path="/login" element={<Login />} />
        <Route path="/*" element={<ProtectedApp />} />
      </Routes>
    </DemoProvider>
  );
}

function ProtectedApp() {
  const { signedIn } = useDemo();
  if (!signedIn) return <Navigate to="/login" replace />;
  return (
    <AppShell>
      <Routes>
        <Route path="/citizen/dashboard" element={<CitizenDashboard />} />
        <Route path="/citizen/report" element={<ReportIssue />} />
        <Route path="/citizen/complaints" element={<ComplaintsList mode="citizen" />} />
        <Route path="/citizen/complaints/:id" element={<ComplaintDetail />} />
        <Route path="/citizen/nearby" element={<NearbyIssues />} />
        <Route path="/citizen/notifications" element={<Notifications />} />
        <Route path="/citizen/rewards" element={<Rewards />} />
        <Route path="/officer/dashboard" element={<OfficerDashboard />} />
        <Route path="/officer/queue" element={<ComplaintsList mode="officer" />} />
        <Route path="/officer/map" element={<FieldMap />} />
        <Route path="/officer/complaints/:id" element={<ComplaintDetail />} />
        <Route path="/supervisor/dashboard" element={<SupervisorDashboard />} />
        <Route path="/supervisor/assignments" element={<Assignments />} />
        <Route path="/supervisor/escalations" element={<Escalations />} />
        <Route path="/supervisor/performance" element={<Performance />} />
        <Route path="/admin/dashboard" element={<AdminDashboard />} />
        <Route path="/admin/users" element={<UserManagement />} />
        <Route path="/admin/departments" element={<Departments />} />
        <Route path="/admin/analytics" element={<Analytics />} />
        <Route path="/admin/audit" element={<AuditLogs />} />
        <Route path="/admin/health" element={<SystemHealth />} />
        <Route path="/profile" element={<Profile />} />
        <Route path="*" element={<RoleHome />} />
      </Routes>
    </AppShell>
  );
}

function RoleHome() {
  const { role } = useDemo();
  return <Navigate to={homeByRole[role]} replace />;
}

function Brand({ light = false }) {
  return (
    <Link to="/" className={`brand ${light ? 'brand-light' : ''}`}>
      <span className="brand-mark"><span>✓</span></span>
      <span><strong>Civic</strong>Hero<small>SMART CITY PLATFORM</small></span>
    </Link>
  );
}

function Landing() {
  const navigate = useNavigate();
  const { setSignedIn } = useDemo();
  const openDemo = () => { setSignedIn(true); navigate('/citizen/dashboard'); };
  return (
    <div className="landing">
      <header className="public-header">
        <Brand />
        <nav><a href="#features">Features</a><a href="#workflow">How it works</a><a href="#impact">Impact</a></nav>
        <div className="header-actions"><button className="btn ghost" onClick={() => navigate('/login')}>Sign in</button><button className="btn primary" onClick={openDemo}>Open live demo</button></div>
      </header>
      <main>
        <section className="hero-section">
          <div className="hero-copy">
            <span className="eyebrow">DIGITAL GOVERNANCE · COMMUNITY POWERED</span>
            <h1>Report civic issues.<br/><span>Track real change.</span></h1>
            <p>CivicHero connects citizens and municipal teams through transparent complaint tracking, intelligent routing, SLA monitoring, and verified resolutions.</p>
            <div className="hero-buttons"><button className="btn primary large" onClick={openDemo}>Explore the platform →</button><button className="btn outline large" onClick={() => document.getElementById('workflow')?.scrollIntoView({ behavior: 'smooth' })}>See how it works</button></div>
            <div className="trust-row"><span>✓ GPS verified reports</span><span>✓ Real-time status</span><span>✓ Resolution proof</span></div>
          </div>
          <div className="hero-visual">
            <div className="city-grid" />
            <div className="map-card">
              <div className="map-toolbar"><span>Live city operations</span><span className="live-dot">LIVE</span></div>
              <MiniMap />
              <div className="floating-issue"><b>CH-2026-1048</b><span>Road repair team dispatched</span><small>Updated 8 minutes ago</small></div>
            </div>
          </div>
        </section>
        <section className="impact-strip" id="impact">
          <Stat value="12,480+" label="Issues reported" />
          <Stat value="9,832" label="Resolved successfully" />
          <Stat value="4.6 hrs" label="Average response" />
          <Stat value="92%" label="Citizen satisfaction" />
        </section>
        <section className="feature-section" id="features">
          <SectionHeading kicker="ONE PLATFORM" title="Built for every civic stakeholder" text="Different portals, one shared source of truth—from citizen reporting to administrative oversight." />
          <div className="feature-grid">
            <Feature icon="⌖" title="Citizen reporting" text="Submit geo-tagged complaints, upload evidence, track timelines, upvote nearby issues and earn civic rewards." />
            <Feature icon="▤" title="Officer work queue" text="Prioritized assignments, SLA countdowns, field maps and verified resolution uploads." />
            <Feature icon="⇄" title="Supervisor control" text="Smart allocation, escalation handling, workload balancing and team performance visibility." />
            <Feature icon="↗" title="Administrative analytics" text="Cross-department KPIs, user governance, audit trails and system health monitoring." />
          </div>
        </section>
        <section className="workflow-section" id="workflow">
          <SectionHeading kicker="TRANSPARENT WORKFLOW" title="From report to resolution" text="Every step is visible, accountable, and time-bound." />
          <div className="steps">
            {[
              ['01', 'Report', 'Citizen submits details, GPS and photo evidence.'],
              ['02', 'Verify & route', 'AI and community signals classify and route the issue.'],
              ['03', 'Assign', 'Supervisor allocates the right officer and tracks SLA.'],
              ['04', 'Resolve', 'Officer completes work and uploads proof.'],
              ['05', 'Confirm', 'Citizen verifies resolution and gives feedback.'],
            ].map(([num, title, text]) => <div className="step" key={num}><span>{num}</span><h3>{title}</h3><p>{text}</p></div>)}
          </div>
        </section>
      </main>
      <footer><Brand light /><span>Mid-term demonstration build · July 2026</span></footer>
    </div>
  );
}

function Login() {
  const { role, setRole, setSignedIn } = useDemo();
  const navigate = useNavigate();
  const [email, setEmail] = useState('demo@civichero.in');
  const submit = (event) => {
    event.preventDefault();
    setSignedIn(true);
    navigate(homeByRole[role]);
  };
  return (
    <div className="auth-page">
      <div className="auth-showcase">
        <Brand light />
        <div><span className="eyebrow light">MID-TERM LIVE DEMO</span><h1>One city.<br/>One civic platform.</h1><p>Use the role selector to demonstrate the complete workflow during evaluation.</p></div>
        <div className="demo-points"><span>● Works without backend</span><span>● Data persists in this browser</span><span>● Switch roles instantly</span></div>
      </div>
      <div className="auth-panel">
        <form className="login-card" onSubmit={submit}>
          <span className="demo-badge">DEMO MODE</span>
          <h2>Welcome to CivicHero</h2>
          <p>Choose a portal and enter the presentation workspace.</p>
          <label>Demo portal</label>
          <div className="role-grid">
            {Object.keys(ROLE_CONFIG).map((item) => <button type="button" key={item} onClick={() => setRole(item)} className={role === item ? 'selected' : ''}><span>{item === 'Citizen' ? '♙' : item === 'Officer' ? '♜' : item === 'Supervisor' ? '♛' : '◆'}</span>{item}</button>)}
          </div>
          <label>Email address</label>
          <input value={email} onChange={(e) => setEmail(e.target.value)} type="email" required />
          <label>Password</label>
          <input defaultValue="CivicHero@2026" type="password" required />
          <button className="btn primary full large" type="submit">Enter {role} portal →</button>
          <small className="form-note">Demo authentication only. No credentials are sent or stored remotely.</small>
        </form>
      </div>
    </div>
  );
}

function AppShell({ children }) {
  const { role, setRole, setSignedIn, toast, setToast, resetDemo } = useDemo();
  const navigate = useNavigate();
  const location = useLocation();
  const profile = ROLE_CONFIG[role];
  const [mobileOpen, setMobileOpen] = useState(false);
  const switchRole = (nextRole) => {
    setRole(nextRole);
    setToast(`Switched to ${nextRole} portal.`);
    navigate(homeByRole[nextRole]);
    setMobileOpen(false);
  };
  return (
    <div className="app-shell">
      <aside className={`sidebar ${mobileOpen ? 'open' : ''}`}>
        <div className="sidebar-brand"><Brand light /></div>
        <div className="portal-label"><span>DEMO WORKSPACE</span><b>{role} Portal</b></div>
        <nav className="side-nav">
          {navByRole[role].map(([label, path, icon]) => (
            <Link key={path} to={path} className={location.pathname === path || (path.includes('complaints') && location.pathname.includes(path)) ? 'active' : ''} onClick={() => setMobileOpen(false)}><i>{icon}</i><span>{label}</span></Link>
          ))}
        </nav>
        <div className="role-switcher">
          <label>Presentation role</label>
          <select value={role} onChange={(e) => switchRole(e.target.value)}>{Object.keys(ROLE_CONFIG).map((item) => <option key={item}>{item}</option>)}</select>
          <button onClick={resetDemo}>↻ Restore demo data</button>
        </div>
        <div className="sidebar-profile"><div className="avatar">{profile.initials}</div><div><b>{profile.name}</b><span>{profile.department}</span></div><button onClick={() => { setSignedIn(false); navigate('/login'); }}>↪</button></div>
      </aside>
      <div className="main-area">
        <header className="topbar">
          <button className="mobile-menu" onClick={() => setMobileOpen(!mobileOpen)}>☰</button>
          <div className="breadcrumb"><span>CivicHero</span><b>{role} workspace</b></div>
          <div className="top-actions"><span className="system-online"><i /> All systems operational</span><button onClick={() => navigate('/citizen/notifications')} className="icon-btn">◉<em>3</em></button><div className="mini-avatar">{profile.initials}</div></div>
        </header>
        <main className="page-content">{children}</main>
      </div>
      {toast && <div className="toast">✓ {toast}</div>}
      {mobileOpen && <div className="scrim" onClick={() => setMobileOpen(false)} />}
    </div>
  );
}

function PageHeader({ eyebrow, title, text, actions }) {
  return <div className="page-header"><div><span>{eyebrow}</span><h1>{title}</h1><p>{text}</p></div>{actions && <div className="page-actions">{actions}</div>}</div>;
}

function MetricCard({ label, value, delta, icon, tone = 'blue' }) {
  return <div className="metric-card"><div className={`metric-icon ${tone}`}>{icon}</div><div><span>{label}</span><strong>{value}</strong><small className={String(delta).startsWith('-') ? 'negative' : ''}>{delta}</small></div></div>;
}

function StatusBadge({ status }) {
  const slug = status.toLowerCase().replace(/\s+/g, '-');
  return <span className={`status ${slug}`}><i />{status}</span>;
}

function PriorityBadge({ priority }) {
  return <span className={`priority ${priority.toLowerCase()}`}>{priority}</span>;
}

function CitizenDashboard() {
  const { complaints } = useDemo();
  const navigate = useNavigate();
  const mine = complaints.filter((c) => c.reporter === ROLE_CONFIG.Citizen.name);
  return (
    <>
      <PageHeader eyebrow="CITIZEN DASHBOARD" title="Good morning, Vaishnavi" text="Here is what is happening with your civic reports and neighbourhood today." actions={<button className="btn primary" onClick={() => navigate('/citizen/report')}>＋ Report new issue</button>} />
      <div className="metric-grid four">
        <MetricCard label="My complaints" value={mine.length + 5} delta="2 active right now" icon="▤" tone="blue" />
        <MetricCard label="Issues resolved" value="6" delta="↑ 2 this month" icon="✓" tone="green" />
        <MetricCard label="Community support" value="248" delta="↑ 31 this week" icon="♟" tone="purple" />
        <MetricCard label="Civic points" value="1,280" delta="Gold citizen · 220 to next" icon="★" tone="amber" />
      </div>
      <div className="dashboard-grid two-one">
        <section className="panel">
          <PanelHeader title="My recent complaints" subtitle="Live status from municipal teams" action={<Link to="/citizen/complaints">View all →</Link>} />
          <div className="complaint-cards">
            {mine.slice(0, 3).map((item) => <ComplaintRow key={item.id} item={item} onClick={() => navigate(`/citizen/complaints/${item.id}`)} />)}
          </div>
        </section>
        <section className="panel neighbourhood-card">
          <PanelHeader title="Ward 12 pulse" subtitle="Last 30 days" />
          <Donut value={78} label="resolution rate" />
          <div className="ward-stats"><div><b>42</b><span>Reported</span></div><div><b>33</b><span>Resolved</span></div><div><b>4.2h</b><span>Avg. response</span></div></div>
          <button className="btn soft full" onClick={() => navigate('/citizen/nearby')}>Explore nearby issues</button>
        </section>
      </div>
      <div className="dashboard-grid one-one">
        <section className="panel">
          <PanelHeader title="Nearby issues need support" subtitle="Upvote to help prioritise community concerns" action={<Link to="/citizen/nearby">Open map →</Link>} />
          {complaints.filter((c) => c.reporter !== ROLE_CONFIG.Citizen.name).slice(0, 3).map((item) => <CompactIssue key={item.id} item={item} />)}
        </section>
        <section className="panel">
          <PanelHeader title="Civic activity" subtitle="Your recent platform events" action={<Link to="/citizen/notifications">All notifications →</Link>} />
          <ActivityList items={notifications.slice(0, 4)} />
        </section>
      </div>
    </>
  );
}

function ReportIssue() {
  const { addComplaint, setToast } = useDemo();
  const navigate = useNavigate();
  const [step, setStep] = useState(1);
  const [form, setForm] = useState({ category: 'Roads', title: '', description: '', location: 'Sion Circle, Ward 12', ward: 'Ward 12', priority: 'High' });
  const [fileName, setFileName] = useState('');
  const update = (key, value) => setForm((old) => ({ ...old, [key]: value }));
  const submit = () => {
    const created = addComplaint({ ...form, title: form.title || 'Damaged road surface near bus stop', description: form.description || 'The road surface is damaged and requires inspection before it becomes unsafe.' });
    setToast(`${created.id} submitted and routed successfully.`);
    navigate(`/citizen/complaints/${created.id}`);
  };
  return (
    <>
      <PageHeader eyebrow="NEW COMPLAINT" title="Report a civic issue" text="Provide clear details so the correct municipal team can respond quickly." />
      <div className="wizard-progress">
        {['Issue details', 'Location & evidence', 'Review & submit'].map((label, index) => <div key={label} className={step >= index + 1 ? 'done' : ''}><span>{step > index + 1 ? '✓' : index + 1}</span><b>{label}</b><i /></div>)}
      </div>
      <div className="form-layout">
        <section className="panel form-panel">
          {step === 1 && <>
            <h2>What issue did you observe?</h2><p>Select a category and describe the problem clearly.</p>
            <label>Issue category</label>
            <div className="category-grid">{['Roads', 'Streetlights', 'Sanitation', 'Water', 'Drainage', 'Parks'].map((cat) => <button key={cat} className={form.category === cat ? 'selected' : ''} onClick={() => update('category', cat)}><span>{categoryIcon(cat)}</span>{cat}</button>)}</div>
            <label>Complaint title</label><input placeholder="Example: Large pothole near college gate" value={form.title} onChange={(e) => update('title', e.target.value)} />
            <label>Description</label><textarea rows="6" placeholder="Describe the issue, its impact, and any safety concern..." value={form.description} onChange={(e) => update('description', e.target.value)} /><div className="field-help">Add landmarks and explain who may be affected.</div>
          </>}
          {step === 2 && <>
            <h2>Where is the issue?</h2><p>We captured your current GPS location. Adjust the address when needed.</p>
            <div className="gps-success">✓ GPS location captured · Accuracy 8 metres</div>
            <MiniMap large />
            <div className="field-row"><div><label>Address / landmark</label><input value={form.location} onChange={(e) => update('location', e.target.value)} /></div><div><label>Ward</label><select value={form.ward} onChange={(e) => update('ward', e.target.value)}><option>Ward 12</option><option>Ward 9</option><option>Ward 18</option></select></div></div>
            <label>Photo evidence</label><label className="upload-zone"><input type="file" accept="image/*" onChange={(e) => setFileName(e.target.files?.[0]?.name || '')} /><span>▧</span><b>{fileName || 'Upload or drag a photo here'}</b><small>JPEG, PNG or WebP · Maximum 5 MB</small></label>
          </>}
          {step === 3 && <>
            <h2>Review your complaint</h2><p>Confirm the information before submitting it to the civic workflow.</p>
            <div className="review-card"><div className={`issue-thumb ${form.category.toLowerCase()}`}>{categoryIcon(form.category)}</div><div><StatusBadge status="Ready to submit" /><h3>{form.title || 'Damaged road surface near bus stop'}</h3><p>{form.description || 'The road surface is damaged and requires inspection before it becomes unsafe.'}</p><div className="meta-row"><span>⌖ {form.location}</span><span>▦ {form.ward}</span><span>▧ {fileName || '1 demo photo'}</span></div></div></div>
            <div className="routing-preview"><span>AI ROUTING PREVIEW</span><div><b>{categoryDepartment(form.category)}</b><small>Expected acknowledgement: within 4 hours</small></div><PriorityBadge priority={form.priority} /></div>
            <label className="check"><input type="checkbox" defaultChecked /> I confirm that the information is accurate and does not contain personal or offensive content.</label>
          </>}
          <div className="wizard-actions"><button className="btn ghost" disabled={step === 1} onClick={() => setStep(step - 1)}>← Back</button>{step < 3 ? <button className="btn primary" onClick={() => setStep(step + 1)}>Continue →</button> : <button className="btn primary" onClick={submit}>✓ Submit complaint</button>}</div>
        </section>
        <aside className="panel help-panel"><span className="help-icon">i</span><h3>Tips for a faster resolution</h3><ul><li>Use a precise title.</li><li>Include a nearby landmark.</li><li>Upload a clear photo.</li><li>Avoid duplicate reports.</li></ul><div className="privacy-note"><b>Privacy protected</b><span>Your contact information is never shown publicly.</span></div></aside>
      </div>
    </>
  );
}

function ComplaintsList({ mode }) {
  const { complaints } = useDemo();
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState('All statuses');
  const filtered = complaints.filter((item) => {
    const searchable = `${item.id} ${item.title} ${item.location} ${item.category}`.toLowerCase();
    return searchable.includes(query.toLowerCase()) && (status === 'All statuses' || item.status === status) && (mode !== 'citizen' || item.reporter === ROLE_CONFIG.Citizen.name);
  });
  return (
    <>
      <PageHeader eyebrow={mode === 'officer' ? 'FIELD OPERATIONS' : 'COMPLAINT TRACKING'} title={mode === 'officer' ? 'Officer work queue' : 'My complaints'} text={mode === 'officer' ? 'Prioritised assignments based on SLA, severity and location.' : 'Review every issue you reported and follow its complete timeline.'} actions={mode === 'citizen' ? <button className="btn primary" onClick={() => navigate('/citizen/report')}>＋ Report issue</button> : <button className="btn outline">↧ Export queue</button>} />
      <section className="panel table-panel">
        <div className="filterbar"><div className="searchbox">⌕<input placeholder="Search by ID, title or location" value={query} onChange={(e) => setQuery(e.target.value)} /></div><select value={status} onChange={(e) => setStatus(e.target.value)}><option>All statuses</option><option>Submitted</option><option>Under Review</option><option>Assigned</option><option>In Progress</option><option>Resolved</option></select><button className="btn soft">More filters</button></div>
        <div className="data-table complaint-table">
          <div className="table-head"><span>Complaint</span><span>Category</span><span>Status</span><span>Priority</span><span>{mode === 'officer' ? 'SLA' : 'Updated'}</span><span /></div>
          {filtered.map((item) => <div className="table-row" key={item.id} onClick={() => navigate(`/${mode === 'officer' ? 'officer' : 'citizen'}/complaints/${item.id}`)}><span className="issue-cell"><span className={`tiny-thumb ${item.image}`}>{categoryIcon(item.category)}</span><span><b>{item.title}</b><small>{item.id} · {item.location}</small></span></span><span>{item.category}</span><span><StatusBadge status={item.status} /></span><span><PriorityBadge priority={item.priority} /></span><span><b className={item.sla.includes('2 hours') ? 'danger-text' : ''}>{mode === 'officer' ? item.sla : item.updated}</b></span><span>›</span></div>)}
        </div>
        <div className="table-footer"><span>Showing {filtered.length} complaints</span><div><button disabled>‹</button><button className="active">1</button><button>2</button><button>›</button></div></div>
      </section>
    </>
  );
}

function ComplaintDetail() {
  const { id } = useParams();
  const { complaints, role, updateComplaint, setToast } = useDemo();
  const navigate = useNavigate();
  const item = complaints.find((c) => c.id === id) || complaints[0];
  const doUpdate = (status, text) => { updateComplaint(item.id, { status }, text); setToast(`${item.id} updated to ${status}.`); };
  return (
    <>
      <button className="back-link" onClick={() => navigate(-1)}>← Back to complaints</button>
      <div className="detail-heading"><div><div className="badge-row"><StatusBadge status={item.status} /><PriorityBadge priority={item.priority} /><span className="id-badge">{item.id}</span></div><h1>{item.title}</h1><p>Reported by {item.reporter} on {item.created}</p></div><div className="detail-actions"><button className="btn outline">↧ Download report</button>{role === 'Citizen' && <button className="btn soft">↑ Upvote · {item.votes}</button>}</div></div>
      <div className="dashboard-grid two-one detail-grid">
        <div className="detail-main">
          <section className="panel"><PanelHeader title="Issue overview" subtitle="Submitted information and evidence" /><div className={`evidence-photo ${item.image}`}><span>{categoryIcon(item.category)}</span><small>PHOTO EVIDENCE · DEMO PREVIEW</small></div><h3>Description</h3><p className="description">{item.description}</p><div className="info-grid"><Info label="Category" value={item.category} /><Info label="Department" value={item.department} /><Info label="Ward" value={item.ward} /><Info label="Community support" value={`${item.votes} upvotes`} /></div><h3>Location</h3><MiniMap large /><div className="location-line">⌖ <div><b>{item.location}</b><span>19.0760° N, 72.8780° E · GPS verified</span></div></div></section>
          <section className="panel"><PanelHeader title="Complaint timeline" subtitle="A transparent record of every action" /><div className="timeline">{item.timeline.map(([event, actor, time], index) => <div key={`${event}-${index}`} className={index === item.timeline.length - 1 ? 'latest' : ''}><span>{index === item.timeline.length - 1 ? '✓' : ''}</span><div><b>{event}</b><p>{actor}</p><small>{time}</small></div></div>)}</div></section>
        </div>
        <aside className="detail-side">
          <section className="panel assignment-card"><span>ASSIGNMENT</span><div className="officer-avatar">{item.officer === 'Unassigned' ? '?' : initials(item.officer)}</div><h3>{item.officer}</h3><p>{item.department}</p><div className="sla-block"><span>SLA status</span><b className={item.sla.includes('2 hours') ? 'danger-text' : ''}>{item.sla}</b><div><i style={{ width: item.sla.includes('2 hours') ? '92%' : '62%' }} /></div></div></section>
          {role === 'Officer' && <section className="panel action-card"><h3>Update field progress</h3><p>Record the latest action for citizens and supervisors.</p><button className="btn soft full" onClick={() => doUpdate('In Progress', 'Field inspection completed; work started')}>Mark In Progress</button><button className="btn primary full" onClick={() => doUpdate('Resolved', 'Resolution completed with photo proof')}>Upload resolution & close</button><button className="btn ghost full">Request escalation</button></section>}
          {role === 'Supervisor' && <section className="panel action-card"><h3>Supervisor actions</h3><button className="btn primary full" onClick={() => { updateComplaint(item.id, { status: 'Assigned', officer: 'Rahul Deshmukh' }, 'Assigned to Rahul Deshmukh'); setToast('Officer assigned successfully.'); }}>Assign officer</button><button className="btn soft full" onClick={() => doUpdate('Under Review', 'Escalated for department review')}>Escalate complaint</button></section>}
          {role === 'Citizen' && <section className="panel action-card"><h3>Was this helpful?</h3><p>Your confirmation improves municipal accountability.</p><button className="btn primary full" onClick={() => setToast('Thank you. Your feedback was recorded.')}>Confirm resolution</button><button className="btn ghost full">Raise a dispute</button></section>}
          <section className="panel transparency-card"><b>Public transparency</b><p>This complaint’s status and timeline can be viewed by nearby citizens. Personal details remain private.</p></section>
        </aside>
      </div>
    </>
  );
}

function NearbyIssues() {
  const { complaints, setToast, setComplaints } = useDemo();
  const upvote = (id) => { setComplaints((items) => items.map((x) => x.id === id ? { ...x, votes: x.votes + 1 } : x)); setToast('Upvote added. This issue gained community priority.'); };
  return (
    <>
      <PageHeader eyebrow="COMMUNITY MAP" title="Issues near you" text="Explore verified reports around Ward 12 and support issues affecting your neighbourhood." actions={<button className="btn primary">⌖ Use my location</button>} />
      <div className="map-layout"><section className="panel map-main"><div className="map-filters"><button className="active">All issues</button><button>Roads</button><button>Sanitation</button><button>Water</button><button>Streetlights</button></div><MiniMap extraLarge /><div className="map-legend"><span><i className="critical" /> Critical</span><span><i className="progress" /> In progress</span><span><i className="resolved" /> Resolved</span></div></section><aside className="panel map-list"><PanelHeader title="Within 5 km" subtitle={`${complaints.length} active and recent reports`} />{complaints.slice(0, 5).map((item) => <div className="nearby-card" key={item.id}><div><StatusBadge status={item.status} /><h4>{item.title}</h4><small>{item.location} · {(Math.random() * 3 + .4).toFixed(1)} km</small></div><button onClick={() => upvote(item.id)}>↑ {item.votes}</button></div>)}</aside></div>
    </>
  );
}

function Notifications() {
  return <><PageHeader eyebrow="ACTIVITY CENTRE" title="Notifications" text="Important complaint updates, SLA alerts and community activity." actions={<button className="btn outline">Mark all as read</button>} /><section className="panel"><div className="notification-tabs"><button className="active">All</button><button>Complaints</button><button>Community</button><button>System</button></div><ActivityList items={[...notifications, ...notifications.slice(1, 3)]} detailed /></section></>;
}

function Rewards() {
  return <><PageHeader eyebrow="CIVIC REWARDS" title="Your community impact" text="Earn points for verified reports, helpful upvotes and resolution feedback." /><div className="reward-hero"><div><span>GOLD CITIZEN</span><h2>1,280 points</h2><p>You are among the top 8% of contributors in Ward 12.</p><div className="reward-progress"><i /></div><small>220 points to Platinum Citizen</small></div><div className="medal">★</div></div><div className="metric-grid three"><MetricCard label="Verified reports" value="8" delta="+100 points each" icon="✓" tone="green" /><MetricCard label="Helpful upvotes" value="34" delta="+5 points each" icon="↑" tone="blue" /><MetricCard label="Resolution feedback" value="6" delta="+20 points each" icon="◉" tone="purple" /></div><section className="panel"><PanelHeader title="Available rewards" subtitle="Redeem civic points with participating partners" /><div className="reward-grid">{[['Local bus day pass','500 points','BUS'],['Public library premium month','750 points','BOOK'],['City garden workshop','900 points','GREEN'],['Municipal sports centre pass','1,100 points','SPORT']].map(([name, points, tag]) => <div className="reward-item" key={name}><span>{tag}</span><h3>{name}</h3><p>{points}</p><button className="btn soft full">Redeem</button></div>)}</div></section></>;
}

function OfficerDashboard() {
  const { complaints } = useDemo();
  const navigate = useNavigate();
  const assigned = complaints.filter((x) => x.officer === 'Rahul Deshmukh' || x.officer === 'Unassigned');
  return <><PageHeader eyebrow="FIELD OPERATIONS" title="Officer command centre" text="Prioritised workload for Roads & Infrastructure · Ward 12." actions={<button className="btn primary" onClick={() => navigate('/officer/queue')}>Open work queue →</button>} /><div className="alert-banner danger"><span>!</span><div><b>1 complaint approaching SLA breach</b><p>Water leakage CH-2026-1015 requires action within 2 hours.</p></div><button onClick={() => navigate('/officer/complaints/CH-2026-1015')}>Review now →</button></div><div className="metric-grid four"><MetricCard label="Assigned today" value="14" delta="5 high priority" icon="▤" tone="blue" /><MetricCard label="In progress" value="6" delta="3 field teams active" icon="↻" tone="amber" /><MetricCard label="Resolved this week" value="27" delta="↑ 18% vs last week" icon="✓" tone="green" /><MetricCard label="SLA compliance" value="91%" delta="Target 90%" icon="◷" tone="purple" /></div><div className="dashboard-grid two-one"><section className="panel"><PanelHeader title="Priority work queue" subtitle="Ordered by urgency and SLA risk" action={<Link to="/officer/queue">View all →</Link>} />{assigned.slice(0, 4).map((item) => <ComplaintRow key={item.id} item={item} onClick={() => navigate(`/officer/complaints/${item.id}`)} officer />)}</section><section className="panel"><PanelHeader title="Today’s route" subtitle="4 field locations · 18.6 km" /><MiniMap /><div className="route-list">{assigned.slice(0, 3).map((item, i) => <div key={item.id}><span>{i + 1}</span><div><b>{item.title}</b><small>{item.location}</small></div></div>)}</div></section></div><div className="dashboard-grid one-one"><section className="panel"><PanelHeader title="Weekly completion" subtitle="Resolved complaints by day" /><BarChart values={[5,8,6,9,12,7,10]} labels={['M','T','W','T','F','S','S']} /></section><section className="panel"><PanelHeader title="Recent activity" subtitle="Updates from your assignments" /><ActivityList items={notifications.slice(0, 4)} /></section></div></>;
}

function FieldMap() {
  const { complaints } = useDemo();
  return <><PageHeader eyebrow="FIELD MAP" title="Assignment locations" text="Plan today’s field route and view nearby civic work." /><div className="map-layout"><section className="panel map-main"><MiniMap extraLarge /></section><aside className="panel map-list"><PanelHeader title="Route sequence" subtitle="Optimised for travel time" />{complaints.slice(0, 5).map((item, i) => <div className="route-card" key={item.id}><span>{i + 1}</span><div><b>{item.title}</b><small>{item.location}</small><StatusBadge status={item.status} /></div></div>)}</aside></div></>;
}

function SupervisorDashboard() {
  const { complaints } = useDemo();
  return <><PageHeader eyebrow="OPERATIONS CONTROL" title="Supervisor overview" text="Manage workload, escalations and department performance across the Central Zone." actions={<button className="btn primary">⇄ Auto-assign queue</button>} /><div className="metric-grid four"><MetricCard label="Open complaints" value="126" delta="↑ 12 since yesterday" icon="▤" tone="blue" /><MetricCard label="Unassigned" value="18" delta="5 need urgent action" icon="?" tone="amber" /><MetricCard label="SLA at risk" value="7" delta="2 critical" icon="!" tone="red" /><MetricCard label="Teams active" value="23/27" delta="85% field capacity" icon="♟" tone="green" /></div><div className="dashboard-grid two-one"><section className="panel"><PanelHeader title="Complaint volume trend" subtitle="Submitted vs resolved · last 12 months" action={<button className="link-button">Download report</button>} /><LineChart values={monthlyTrend} /><div className="chart-legend"><span><i className="blue" /> Submitted</span><span><i className="green" /> Resolved</span></div></section><section className="panel"><PanelHeader title="SLA health" subtitle="Live department performance" /><Donut value={86} label="within SLA" /><div className="sla-legend"><span><i className="green" />108 On track</span><span><i className="amber" />11 At risk</span><span><i className="red" />7 Breached</span></div></section></div><div className="dashboard-grid one-one"><section className="panel"><PanelHeader title="Needs assignment" subtitle="Highest priority unassigned complaints" action={<Link to="/supervisor/assignments">Manage queue →</Link>} />{complaints.filter((x) => x.officer === 'Unassigned').map((item) => <ComplaintRow key={item.id} item={item} />)}</section><section className="panel"><PanelHeader title="Team workload" subtitle="Active assignments by officer" /><Workload /></section></div></>;
}

function Assignments() {
  const { complaints, updateComplaint, setToast } = useDemo();
  const unassigned = complaints.filter((x) => x.officer === 'Unassigned');
  const assign = (id) => { updateComplaint(id, { status: 'Assigned', officer: 'Rahul Deshmukh' }, 'Assigned to Rahul Deshmukh'); setToast('Complaint assigned to Rahul Deshmukh.'); };
  return <><PageHeader eyebrow="WORKLOAD MANAGEMENT" title="Complaint assignments" text="Allocate cases using officer availability, department expertise and ward scope." actions={<button className="btn primary">⚡ Run smart assignment</button>} /><section className="panel"><div className="assignment-layout"><div><h3>Unassigned queue</h3>{unassigned.map((item) => <div className="assignment-item" key={item.id}><div><div className="badge-row"><PriorityBadge priority={item.priority} /><span>{item.id}</span></div><h4>{item.title}</h4><small>{item.department} · {item.ward} · {item.sla}</small></div><button className="btn soft" onClick={() => assign(item.id)}>Assign</button></div>)}</div><div className="officer-capacity"><h3>Officer capacity</h3>{[['Rahul Deshmukh',68,'6 active'],['Sneha Joshi',45,'4 active'],['Amit More',82,'8 active'],['Pooja Nair',54,'5 active']].map(([name, load, label]) => <div className="capacity-row" key={name}><div><span className="avatar small">{initials(name)}</span><b>{name}</b><small>{label}</small></div><div className="capacity-bar"><i style={{width:`${load}%`}} /></div><strong>{load}%</strong></div>)}</div></div></section></>;
}

function Escalations() {
  const { complaints } = useDemo();
  const urgent = complaints.filter((x) => x.priority === 'Critical' || x.sla.includes('hours'));
  return <><PageHeader eyebrow="SLA CONTROL" title="Escalation centre" text="Review breached and at-risk cases before service commitments are missed." /><div className="metric-grid three"><MetricCard label="Critical escalations" value="2" delta="Immediate action needed" icon="!" tone="red" /><MetricCard label="At risk today" value="7" delta="Within next 8 hours" icon="◷" tone="amber" /><MetricCard label="Recovered this week" value="19" delta="↑ 14% improvement" icon="✓" tone="green" /></div><section className="panel">{urgent.map((item) => <div className="escalation-row" key={item.id}><div className="escalation-time"><b>{item.sla}</b><span>SLA REMAINING</span></div><div><div className="badge-row"><PriorityBadge priority={item.priority} /><span>{item.id}</span></div><h3>{item.title}</h3><p>{item.department} · {item.location}</p></div><div><button className="btn primary">Reassign</button><button className="btn soft">Contact team</button></div></div>)}</section></>;
}

function Performance() {
  return <><PageHeader eyebrow="TEAM ANALYTICS" title="Department performance" text="Compare resolution rate, response time and SLA compliance across operational teams." actions={<button className="btn outline">↧ Export report</button>} /><div className="dashboard-grid two-one"><section className="panel"><PanelHeader title="Resolution trend" subtitle="Monthly complaint throughput" /><LineChart values={monthlyTrend} /></section><section className="panel"><PanelHeader title="Citizen satisfaction" subtitle="Based on verified feedback" /><Donut value={92} label="positive rating" /></section></div><section className="panel table-panel"><PanelHeader title="Department scorecard" subtitle="Current month performance" /><div className="data-table performance-table"><div className="table-head"><span>Department</span><span>Resolution rate</span><span>Average response</span><span>SLA compliance</span><span>Trend</span></div>{departmentPerformance.map(([name, rate, response]) => <div className="table-row" key={name}><span><b>{name}</b></span><span><div className="inline-progress"><i style={{width:`${rate}%`}} /></div>{rate}%</span><span>{response}</span><span>{Math.min(rate + 4, 98)}%</span><span className="positive">↗ Improving</span></div>)}</div></section></>;
}

function AdminDashboard() {
  return <><PageHeader eyebrow="PLATFORM ADMINISTRATION" title="City operations dashboard" text="A unified view of citizens, complaints, departments and platform health." actions={<button className="btn outline">↧ Executive report</button>} /><div className="metric-grid four"><MetricCard label="Registered users" value="24,836" delta="↑ 8.4% this month" icon="♟" tone="blue" /><MetricCard label="Total complaints" value="12,480" delta="78.8% resolved" icon="▤" tone="purple" /><MetricCard label="Active departments" value="18" delta="27 operational teams" icon="▦" tone="green" /><MetricCard label="Platform uptime" value="99.98%" delta="All systems healthy" icon="✓" tone="amber" /></div><div className="dashboard-grid two-one"><section className="panel"><PanelHeader title="City complaint trend" subtitle="Volume and resolution performance" /><LineChart values={monthlyTrend} /></section><section className="panel"><PanelHeader title="Status distribution" subtitle="All active complaints" /><Donut value={79} label="resolved" /><div className="sla-legend"><span><i className="green" />9,832 Resolved</span><span><i className="blue" />1,774 Active</span><span><i className="amber" />874 Review</span></div></section></div><div className="dashboard-grid one-one"><section className="panel"><PanelHeader title="Department leaderboard" subtitle="Ranked by SLA compliance" action={<Link to="/admin/analytics">Full analytics →</Link>} />{departmentPerformance.slice(0,4).map(([name, rate, response], i) => <div className="leader-row" key={name}><span>{i+1}</span><div><b>{name}</b><small>Avg. response {response}</small></div><div className="inline-progress"><i style={{width:`${rate}%`}} /></div><strong>{rate}%</strong></div>)}</section><section className="panel"><PanelHeader title="Recent administrative activity" subtitle="Security and governance audit" action={<Link to="/admin/audit">Audit logs →</Link>} />{auditRows.slice(0,4).map((row) => <div className="audit-mini" key={row.join()}><span>✓</span><div><b>{row[2]}</b><small>{row[1]} · {row[3]}</small></div><time>{row[0]}</time></div>)}</section></div></>;
}

function UserManagement() {
  const [query, setQuery] = useState('');
  const [roleFilter, setRoleFilter] = useState('All roles');
  const filtered = users.filter((row) => row.join(' ').toLowerCase().includes(query.toLowerCase()) && (roleFilter === 'All roles' || row[1] === roleFilter));
  return <><PageHeader eyebrow="ACCESS GOVERNANCE" title="User management" text="Manage roles, scopes and account status for all CivicHero users." actions={<button className="btn primary">＋ Add officer</button>} /><div className="metric-grid three"><MetricCard label="Citizens" value="24,128" delta="96.9% of users" icon="♙" tone="blue" /><MetricCard label="Municipal staff" value="682" delta="Across 18 departments" icon="♜" tone="green" /><MetricCard label="Accounts under review" value="26" delta="Fraud and verification queue" icon="!" tone="amber" /></div><section className="panel table-panel"><div className="filterbar"><div className="searchbox">⌕<input placeholder="Search name, role or scope" value={query} onChange={(e) => setQuery(e.target.value)} /></div><select value={roleFilter} onChange={(e) => setRoleFilter(e.target.value)}><option>All roles</option><option>Citizen</option><option>Officer</option><option>Supervisor</option></select><button className="btn soft">Advanced filters</button></div><div className="data-table user-table"><div className="table-head"><span>User</span><span>Role</span><span>Scope</span><span>Status</span><span>Activity</span><span /></div>{filtered.map((row) => <div className="table-row" key={row[0]}><span className="user-cell"><span className="avatar small">{initials(row[0])}</span><span><b>{row[0]}</b><small>{row[0].toLowerCase().replaceAll(' ', '.')}@civichero.in</small></span></span><span><span className="role-pill">{row[1]}</span></span><span>{row[2]}</span><span><StatusBadge status={row[3]} /></span><span>{row[4]}</span><span>•••</span></div>)}</div></section></>;
}

function Departments() {
  const departments = [['Roads & Infrastructure','8 officers','312 open','88%'],['Electrical','6 officers','146 open','94%'],['Solid Waste Management','11 officers','224 open','81%'],['Water Supply','7 officers','198 open','76%'],['Storm Water Drains','5 officers','105 open','84%'],['Gardens & Parks','4 officers','62 open','91%']];
  return <><PageHeader eyebrow="ORGANISATION" title="Department management" text="Configure municipal departments, service categories, ward scopes and operational teams." actions={<button className="btn primary">＋ Add department</button>} /><div className="department-grid">{departments.map(([name, staff, open, rate], i) => <div className="panel department-card" key={name}><div className={`dept-icon tone-${i%4}`}>▦</div><div><h3>{name}</h3><p>{staff} · {open}</p></div><div className="department-rate"><b>{rate}</b><span>SLA compliance</span></div><button>Manage →</button></div>)}</div></>;
}

function Analytics() {
  return <><PageHeader eyebrow="DECISION INTELLIGENCE" title="City analytics" text="Explore complaint patterns, operational bottlenecks and community outcomes." actions={<><select><option>Last 12 months</option><option>Last 90 days</option></select><button className="btn outline">↧ Export</button></>} /><div className="metric-grid four"><MetricCard label="Resolution rate" value="78.8%" delta="↑ 6.2 points YoY" icon="✓" tone="green" /><MetricCard label="Median response" value="4.6h" delta="↓ 1.4h improvement" icon="◷" tone="blue" /><MetricCard label="Repeat locations" value="86" delta="↓ 11% this quarter" icon="⌖" tone="amber" /><MetricCard label="Citizen trust score" value="92/100" delta="↑ 4 points" icon="★" tone="purple" /></div><div className="dashboard-grid two-one"><section className="panel"><PanelHeader title="Complaint and resolution trend" subtitle="Monthly citywide volume" /><LineChart values={monthlyTrend} /></section><section className="panel"><PanelHeader title="Category distribution" subtitle="Share of submitted issues" /><HorizontalBars /></section></div><div className="dashboard-grid one-one"><section className="panel"><PanelHeader title="Ward heatmap" subtitle="Complaint density by geographic cluster" /><HeatMap /></section><section className="panel"><PanelHeader title="Service performance" subtitle="Resolution rate by department" />{departmentPerformance.map(([name, rate]) => <div className="bar-row" key={name}><span>{name}</span><div><i style={{width:`${rate}%`}} /></div><b>{rate}%</b></div>)}</section></div></>;
}

function AuditLogs() {
  return <><PageHeader eyebrow="SECURITY & COMPLIANCE" title="Audit logs" text="Immutable administrative and operational events across the platform." actions={<button className="btn outline">↧ Export CSV</button>} /><section className="panel table-panel"><div className="filterbar"><div className="searchbox">⌕<input placeholder="Search actor, action or resource" /></div><select><option>All actions</option><option>User changes</option><option>Complaint updates</option><option>Security</option></select><button className="btn soft">28 Jul 2026</button></div><div className="data-table audit-table"><div className="table-head"><span>Time</span><span>Actor</span><span>Action</span><span>Resource</span><span>Result</span></div>{auditRows.map((row) => <div className="table-row" key={row.join()}>{row.map((cell, i) => <span key={i}>{i===4 ? <StatusBadge status={cell} /> : i===1 ? <b>{cell}</b> : cell}</span>)}</div>)}</div></section></>;
}

function SystemHealth() {
  return <><PageHeader eyebrow="INFRASTRUCTURE" title="System health" text="Real-time status for application, database, storage and integrations." actions={<button className="btn outline">↻ Refresh checks</button>} /><div className="health-summary"><span>✓</span><div><h2>All systems operational</h2><p>Last checked 28 Jul 2026 at 9:12 AM IST</p></div><b>99.98% uptime</b></div><div className="health-grid">{[['ASP.NET Core API','Operational','42 ms','v1.0.0'],['AWS RDS · MySQL','Operational','68 ms','Encrypted'],['Amazon S3 Storage','Operational','91 ms','ap-south-1'],['Authentication Service','Operational','37 ms','JWT active'],['Notification Engine','Operational','54 ms','Queue healthy'],['Frontend CDN','Operational','29 ms','Vite build']].map(([name,status,latency,detail]) => <div className="panel health-card" key={name}><div><i /><b>{name}</b></div><StatusBadge status={status} /><p><span>Response time</span><b>{latency}</b></p><p><span>Configuration</span><b>{detail}</b></p><small>✓ Last check passed</small></div>)}</div></>;
}

function Profile() {
  const { role, setToast } = useDemo(); const data = ROLE_CONFIG[role];
  return <><PageHeader eyebrow="ACCOUNT" title="My profile" text="Manage your personal information and notification preferences." /><div className="profile-layout"><section className="panel profile-summary"><div className="large-avatar">{data.initials}</div><h2>{data.name}</h2><StatusBadge status="Verified" /><p>{role} · {data.department}</p><div><span>Member since</span><b>July 2026</b></div><div><span>Last login</span><b>Today, 9:02 AM</b></div></section><section className="panel profile-form"><h2>Personal information</h2><div className="field-row"><div><label>Full name</label><input defaultValue={data.name} /></div><div><label>Role</label><input defaultValue={role} disabled /></div></div><div className="field-row"><div><label>Email address</label><input defaultValue="demo@civichero.in" /></div><div><label>Phone number</label><input defaultValue="+91 98765 43210" /></div></div><div className="field-row"><div><label>Department</label><input defaultValue={data.department} disabled /></div><div><label>Ward / scope</label><input defaultValue={data.ward} disabled /></div></div><h3>Notification preferences</h3><label className="toggle-row"><div><b>Complaint status updates</b><span>Email and in-app notification whenever status changes</span></div><input type="checkbox" defaultChecked /></label><label className="toggle-row"><div><b>SLA and escalation alerts</b><span>Urgent operational alerts for assigned responsibilities</span></div><input type="checkbox" defaultChecked /></label><div className="profile-actions"><button className="btn outline">Cancel</button><button className="btn primary" onClick={() => setToast('Profile changes saved locally.')}>Save changes</button></div></section></div></>;
}

function PanelHeader({ title, subtitle, action }) { return <div className="panel-header"><div><h2>{title}</h2><p>{subtitle}</p></div>{action && <div>{action}</div>}</div>; }
function Info({ label, value }) { return <div className="info-item"><span>{label}</span><b>{value}</b></div>; }
function Stat({ value, label }) { return <div><strong>{value}</strong><span>{label}</span></div>; }
function Feature({ icon, title, text }) { return <div className="feature-card"><span>{icon}</span><h3>{title}</h3><p>{text}</p><a>Explore workflow →</a></div>; }
function SectionHeading({ kicker, title, text }) { return <div className="section-heading"><span>{kicker}</span><h2>{title}</h2><p>{text}</p></div>; }
function initials(name) { return name.split(' ').map((x) => x[0]).join('').slice(0,2).toUpperCase(); }
function categoryIcon(category) { return ({ Roads:'▰', Streetlights:'☼', Sanitation:'♲', Water:'≈', Drainage:'≋', Parks:'♣', Footpaths:'▦' })[category] || '◆'; }

function ComplaintRow({ item, onClick, officer }) {
  return <button className="complaint-row" onClick={onClick}><span className={`issue-thumb small ${item.image}`}>{categoryIcon(item.category)}</span><span className="complaint-copy"><span className="badge-row"><StatusBadge status={item.status} /><small>{item.id}</small></span><b>{item.title}</b><small>⌖ {item.location}</small></span><span className="complaint-end"><PriorityBadge priority={item.priority} /><b className={item.sla.includes('2 hours') ? 'danger-text' : ''}>{officer ? item.sla : item.updated}</b><i>›</i></span></button>;
}

function CompactIssue({ item }) { const { setToast, setComplaints } = useDemo(); return <div className="compact-issue"><span className={`tiny-thumb ${item.image}`}>{categoryIcon(item.category)}</span><div><b>{item.title}</b><small>{item.location}</small></div><button onClick={() => { setComplaints((all) => all.map((x) => x.id === item.id ? {...x, votes:x.votes+1}:x)); setToast('Your upvote was added.'); }}>↑ {item.votes}</button></div>; }

function ActivityList({ items, detailed }) { return <div className={`activity-list ${detailed ? 'detailed' : ''}`}>{items.map(([title,text,time,tone], index) => <div className="activity" key={`${title}-${index}`}><span className={tone}>{tone === 'warning' ? '!' : tone === 'success' ? '✓' : 'i'}</span><div><b>{title}</b><p>{text}</p></div><small>{time}</small>{detailed && <button>•••</button>}</div>)}</div>; }

function Donut({ value, label }) { return <div className="donut" style={{ '--value': `${value * 3.6}deg` }}><div><strong>{value}%</strong><span>{label}</span></div></div>; }

function BarChart({ values, labels }) { const max = Math.max(...values); return <div className="bar-chart">{values.map((value,index)=><div key={index}><span style={{height:`${(value/max)*100}%`}}><i>{value}</i></span><small>{labels[index]}</small></div>)}</div>; }

function LineChart({ values }) {
  const width = 680, height = 230, max = Math.max(...values), min = Math.min(...values) - 10;
  const points = values.map((value,index)=>`${(index/(values.length-1))*width},${height-((value-min)/(max-min))*height}`).join(' ');
  const resolved = values.map((value,index)=>`${(index/(values.length-1))*width},${height-(((value*.79)-min*.79)/(max*.79-min*.79))*height}`).join(' ');
  return <div className="line-chart"><svg viewBox={`0 0 ${width} ${height}`} preserveAspectRatio="none"><defs><linearGradient id="area" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor="#1976ed" stopOpacity=".24"/><stop offset="100%" stopColor="#1976ed" stopOpacity="0"/></linearGradient></defs><g className="grid-lines">{[0,1,2,3,4].map(i=><line key={i} x1="0" y1={i*height/4} x2={width} y2={i*height/4}/>)}</g><polygon points={`0,${height} ${points} ${width},${height}`} fill="url(#area)"/><polyline points={points} className="line primary-line"/><polyline points={resolved} className="line resolved-line"/></svg><div className="x-labels">{['Aug','Sep','Oct','Nov','Dec','Jan','Feb','Mar','Apr','May','Jun','Jul'].map(x=><span key={x}>{x}</span>)}</div></div>;
}

function MiniMap({ large, extraLarge }) {
  return <div className={`mini-map ${large ? 'large' : ''} ${extraLarge ? 'extra-large' : ''}`}><div className="road r1"/><div className="road r2"/><div className="road r3"/><div className="road r4"/><div className="block b1"/><div className="block b2"/><div className="block b3"/><div className="block b4"/><span className="map-pin p1">!</span><span className="map-pin p2">✓</span><span className="map-pin p3">!</span><span className="map-pin p4">●</span><span className="you-pin">⌖<small>You</small></span><div className="map-label l1">SION</div><div className="map-label l2">WARD 12</div><div className="map-label l3">CENTRAL ROAD</div><button className="map-zoom">＋<i/>−</button></div>;
}

function Workload() { return <div className="workload">{[['Rahul Deshmukh',68,6],['Sneha Joshi',45,4],['Amit More',82,8],['Pooja Nair',54,5],['Vikram Rao',31,3]].map(([name,value,count])=><div key={name}><span className="avatar small">{initials(name)}</span><div><b>{name}</b><div><i style={{width:`${value}%`}}/></div></div><strong>{count}</strong></div>)}</div>; }

function HorizontalBars() { const rows=[['Roads',28],['Sanitation',22],['Water',18],['Streetlights',14],['Drainage',11],['Other',7]]; return <div className="horizontal-bars">{rows.map(([label,value])=><div key={label}><span>{label}</span><div><i style={{width:`${value*3}%`}}/></div><b>{value}%</b></div>)}</div>; }

function HeatMap() { const vals=[1,2,3,2,1,2,4,5,3,2,1,3,5,5,4,2,1,2,4,3,2,1,1,3,4,5,5,3,2,1,2,3,4,2,1]; return <div><div className="heatmap">{vals.map((v,i)=><span key={i} className={`h${v}`} title={`Ward cluster ${i+1}`} />)}</div><div className="heat-legend"><span>Low</span>{[1,2,3,4,5].map(x=><i className={`h${x}`} key={x}/>)}<span>High</span></div></div>; }

export default App;
