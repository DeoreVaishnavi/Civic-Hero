import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import CivicIcon from '../../components/ui/CivicIcon.jsx';
import GovernmentShowcase from '../../components/public/GovernmentShowcase.jsx';
import PublicHeatmapPreview from '../../components/public/PublicHeatmapPreview.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { complaintApi } from '../../services/complaintApi.js';

const features = [
  { icon: 'complaints', title: 'Evidence-based reporting', text: 'Submit GPS-tagged complaints with clear images, ward, category and a useful description.', tone: 'from-blue-600 to-cyan-500' },
  { icon: 'verify', title: 'Citizen verification', text: 'Compare before-and-after evidence, accept completed work or raise a transparent dispute.', tone: 'from-emerald-600 to-teal-500' },
  { icon: 'map', title: 'Live city intelligence', text: 'Explore nearby issues, ward-level heatmaps and resolution performance without creating duplicates.', tone: 'from-violet-600 to-fuchsia-500' },
  { icon: 'rewards', title: 'Rewards and recognition', text: 'Earn points, badges and community rank for honest reporting and helpful participation.', tone: 'from-amber-500 to-orange-500' },
];

const steps = [
  ['01', 'Report', 'Add the issue, location and supporting photos.'],
  ['02', 'Triage', 'CivicHero checks category, priority, fraud risk and possible duplicates.'],
  ['03', 'Assign', 'The correct department and field officer receive the case.'],
  ['04', 'Resolve', 'Work progress, notes and after-evidence are recorded.'],
  ['05', 'Verify', 'The citizen accepts the work or raises a dispute for review.'],
];

const fallbackTrendingIssues = [
  { title: 'Garbage near Park Lane', ward: 'Ward 3', status: 'In progress', supports: 120, icon: 'recycle' },
  { title: 'Pothole on Market Road', ward: 'Ward 5', status: 'Assigned', supports: 86, icon: 'road' },
  { title: 'Streetlight outage', ward: 'Ward 1', status: 'Resolved', supports: 54, icon: 'light' },
];

export default function HomePage() {
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();
  const [trendingIssues, setTrendingIssues] = useState(fallbackTrendingIssues);
  const [supportingId, setSupportingId] = useState(null);
  const [supportMessage, setSupportMessage] = useState('');
  const isCitizen = String(user?.role || '').toLowerCase() === 'citizen';

  useEffect(() => {
    let active = true;
    complaintApi.publicFeed({ page: 1, pageSize: 3, sortBy: 'most-supported' })
      .then((data) => {
        if (active && data?.items?.length) setTrendingIssues(data.items);
      })
      .catch(() => { /* Keep the visual fallback when the API is unavailable. */ });
    return () => { active = false; };
  }, []);

  const toggleSupport = async (item) => {
    if (!item.id) {
      navigate(ROUTE_PATHS.publicIssues);
      return;
    }
    if (!isAuthenticated) {
      navigate(ROUTE_PATHS.login, { state: { from: ROUTE_PATHS.publicIssues } });
      return;
    }
    if (!isCitizen) {
      setSupportMessage('Only Citizen accounts can support complaints.');
      return;
    }

    setSupportingId(item.id);
    setSupportMessage('');
    try {
      const response = item.hasUpvoted
        ? await complaintApi.removeUpvote(item.id)
        : await complaintApi.upvote(item.id);
      setTrendingIssues((current) => current.map((row) => row.id === item.id
        ? { ...row, hasUpvoted: !row.hasUpvoted, upvoteCount: response?.upvoteCount ?? Math.max(0, (row.upvoteCount || 0) + (row.hasUpvoted ? -1 : 1)) }
        : row));
      setSupportMessage(item.hasUpvoted ? 'Support removed.' : 'Thank you. Your support was recorded.');
    } catch (reason) {
      setSupportMessage(reason.message || 'Unable to update support.');
    } finally {
      setSupportingId(null);
    }
  };

  return (
    <>
      <section className="relative overflow-hidden bg-gradient-to-br from-slate-950 via-blue-950 to-blue-800 text-white">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_18%_24%,rgba(56,189,248,.18),transparent_28%),radial-gradient(circle_at_83%_78%,rgba(16,185,129,.12),transparent_30%)]" />
        <div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,.045)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.045)_1px,transparent_1px)] bg-[length:42px_42px] [mask-image:linear-gradient(to_right,black,transparent_85%)]" />
        <div className="relative mx-auto grid min-h-[700px] w-full max-w-7xl items-center gap-14 px-4 py-20 sm:px-6 lg:grid-cols-[1.05fr_.95fr] lg:px-8 lg:py-24">
          <div>
            <span className="inline-flex items-center gap-2 rounded-full border border-cyan-300/20 bg-cyan-300/10 px-4 py-2 text-xs font-black uppercase tracking-[.2em] text-cyan-100 backdrop-blur">
              <span className="h-2 w-2 animate-pulse rounded-full bg-cyan-300" /> Transparent civic governance
            </span>
            <h1 className="mt-7 max-w-3xl text-5xl font-black leading-[1.02] tracking-[-.04em] sm:text-6xl lg:text-7xl">Report. Verify. <span className="bg-gradient-to-r from-cyan-300 to-emerald-300 bg-clip-text text-transparent">Improve your city.</span></h1>
            <p className="mt-6 max-w-2xl text-lg leading-8 text-blue-100/80">CivicHero connects citizens, field officers and municipal teams through evidence-based reporting, live heatmaps, accountable resolution and citizen verification.</p>
            <div className="mt-9 flex flex-wrap gap-3">
              <Link to={ROUTE_PATHS.anonymousReport} className="inline-flex min-h-14 items-center justify-center gap-2 rounded-2xl bg-white px-6 py-4 text-sm font-black text-blue-800 shadow-2xl shadow-black/20 transition duration-300 hover:-translate-y-1 hover:bg-cyan-50"><CivicIcon name="plus" size={19} /> Report complaint</Link>
              <Link to={ROUTE_PATHS.anonymousTrack} className="inline-flex min-h-14 items-center justify-center gap-2 rounded-2xl border border-white/15 bg-white/10 px-6 py-4 text-sm font-black text-white backdrop-blur transition duration-300 hover:-translate-y-1 hover:bg-white/15"><CivicIcon name="track" size={19} /> Track an issue</Link>
            </div>
            <div className="mt-7 flex flex-wrap gap-x-6 gap-y-3 text-sm font-bold text-blue-100/75">{['GPS and photo evidence', 'Live status updates', 'Citizen verification', 'Rewards and leaderboard'].map((item) => <span key={item} className="inline-flex items-center gap-2"><i className="grid h-5 w-5 place-items-center rounded-full bg-emerald-400/15 text-emerald-200"><CivicIcon name="verify" size={13} /></i>{item}</span>)}</div>
          </div>

          <HeroPortalPreview />
        </div>
      </section>

      <section id="city-impact" className="relative z-10 -mt-8 px-4 sm:px-6 lg:px-8">
        <div className="mx-auto grid w-full max-w-6xl overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-2xl shadow-slate-900/10 sm:grid-cols-2 lg:grid-cols-4">
          <Impact value="1,240+" label="Complaints reported" icon="complaints" />
          <Impact value="900" label="Issues resolved" icon="verify" />
          <Impact value="36 hrs" label="Average response" icon="clock" />
          <Impact value="8,000+" label="Active citizens" icon="users" />
        </div>
      </section>

      <section className="bg-white py-20 sm:py-24">
        <div className="mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="mx-auto max-w-3xl text-center"><p className="text-xs font-black uppercase tracking-[.22em] text-blue-600">One platform, complete visibility</p><h2 className="mt-4 text-3xl font-black tracking-tight text-slate-950 sm:text-4xl lg:text-5xl">Everything needed for accountable civic service.</h2><p className="mt-5 text-base leading-7 text-slate-600">Every page is organised around the citizen, officer, supervisor and administrator actions shown in your wireframes.</p></div>
          <div className="mt-12 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">{features.map((feature) => <article key={feature.title} className="group rounded-3xl border border-slate-200 bg-white p-6 shadow-sm transition duration-300 hover:-translate-y-2 hover:border-blue-200 hover:shadow-2xl hover:shadow-blue-950/10"><div className={`grid h-14 w-14 place-items-center rounded-2xl bg-gradient-to-br ${feature.tone} text-2xl font-black text-white shadow-lg transition group-hover:rotate-6 group-hover:scale-110`}><CivicIcon name={feature.icon} size={27} /></div><h3 className="mt-6 text-lg font-black text-slate-900">{feature.title}</h3><p className="mt-3 text-sm leading-6 text-slate-500">{feature.text}</p></article>)}</div>
        </div>
      </section>

      <PublicHeatmapPreview />
      <GovernmentShowcase />

      <section id="how-it-works" className="bg-slate-50 py-20 sm:py-24">
        <div className="mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8">
          <div className="grid gap-12 lg:grid-cols-[.72fr_1.28fr]">
            <div><p className="text-xs font-black uppercase tracking-[.22em] text-blue-600">Complaint lifecycle</p><h2 className="mt-4 text-3xl font-black tracking-tight text-slate-950 sm:text-4xl">From citizen report to verified closure.</h2><p className="mt-5 text-base leading-7 text-slate-600">Every important action is visible, timestamped and linked to the responsible role.</p><Link to={ROUTE_PATHS.register} className="mt-8 inline-flex rounded-xl bg-blue-600 px-5 py-3 text-sm font-black text-white shadow-lg shadow-blue-600/20 transition hover:-translate-y-0.5 hover:bg-blue-700">Create your citizen account</Link></div>
            <div className="grid gap-4 sm:grid-cols-2">{steps.map(([number, title, text], index) => <article key={number} className={`relative rounded-3xl border border-slate-200 bg-white p-6 shadow-sm transition hover:-translate-y-1 hover:shadow-xl ${index === steps.length - 1 ? 'sm:col-span-2' : ''}`}><span className="text-xs font-black uppercase tracking-[.2em] text-blue-600">Step {number}</span><h3 className="mt-3 text-xl font-black text-slate-900">{title}</h3><p className="mt-2 text-sm leading-6 text-slate-500">{text}</p></article>)}</div>
          </div>
        </div>
      </section>

      <section className="bg-white py-20">
        <div className="mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8">
          <section className="rounded-[2rem] border border-slate-200 bg-white p-6 shadow-xl shadow-slate-900/5 sm:p-8">
            <div className="flex flex-wrap items-center justify-between gap-4"><div><p className="text-xs font-black uppercase tracking-[.18em] text-blue-600">Trending complaints</p><h2 className="mt-2 text-2xl font-black text-slate-950">Issues receiving community attention</h2></div><Link to={ROUTE_PATHS.publicIssues} className="rounded-xl bg-blue-50 px-4 py-3 text-sm font-black text-blue-700 transition hover:bg-blue-100">View issues <CivicIcon name="arrow" size={15} /></Link></div>
            {supportMessage && <p className="mt-4 rounded-xl border border-blue-100 bg-blue-50 px-4 py-3 text-sm font-bold text-blue-800">{supportMessage}</p>}
            <div className="mt-6 grid gap-3 lg:grid-cols-3">{trendingIssues.map((item) => <article key={item.id || item.title} className="rounded-2xl border border-slate-200 p-4 transition hover:-translate-y-1 hover:border-blue-200 hover:bg-blue-50/40 hover:shadow-lg"><div className="flex items-center gap-4"><div className="grid h-14 w-14 shrink-0 place-items-center rounded-2xl bg-slate-100 text-blue-700"><CivicIcon name={item.icon || publicIssueIcon(item.category)} size={25} /></div><div className="min-w-0 flex-1"><h3 className="truncate font-black text-slate-900">{item.title}</h3><p className="mt-1 text-xs text-slate-500">{item.wardName || item.ward} · {String(item.status || '').replace(/([a-z])([A-Z])/g, '$1 $2')}</p></div><div className="text-right"><strong className="block text-lg text-slate-900">{item.upvoteCount ?? item.supports ?? 0}</strong><span className="text-[10px] font-black uppercase tracking-wider text-slate-400">Supports</span></div></div><button type="button" onClick={() => toggleSupport(item)} disabled={supportingId === item.id} className={`mt-4 w-full rounded-xl px-4 py-2.5 text-xs font-black transition disabled:opacity-60 ${item.hasUpvoted ? 'border border-emerald-200 bg-emerald-50 text-emerald-700' : 'bg-blue-600 text-white hover:bg-blue-700'}`}>{supportingId === item.id ? <><span className="loading-spinner" /> Updating…</> : item.hasUpvoted ? <><CivicIcon name="verify" size={16} /> Supported</> : <><CivicIcon name="support" size={16} /> Support issue</>}</button></article>)}</div>
          </section>
        </div>
      </section>
    </>
  );
}

function HeroPortalPreview() {
  return (
    <div className="relative min-h-[530px] [perspective:1400px]">
      <div className="absolute inset-x-0 top-7 overflow-hidden rounded-[2rem] border border-white/15 bg-white/95 text-slate-900 shadow-2xl shadow-black/35 [transform:rotateY(-5deg)_rotateX(2deg)] transition duration-700 hover:[transform:rotateY(-1deg)_rotateX(0deg)]">
        <div className="flex h-14 items-center justify-between border-b border-slate-200 bg-white px-5"><div className="flex items-center gap-2"><i className="h-3 w-3 rounded-full bg-rose-400" /><i className="h-3 w-3 rounded-full bg-amber-400" /><i className="h-3 w-3 rounded-full bg-emerald-400" /></div><strong className="text-sm">Citizen Dashboard</strong><span className="rounded-full bg-blue-100 px-3 py-1 text-xs font-black text-blue-700"><span className="status-dot" aria-hidden="true" /> Live</span></div>
        <div className="grid min-h-[430px] grid-cols-[105px_1fr]">
          <aside className="bg-slate-950 p-4 text-white"><strong className="text-sm">Civic<span className="text-cyan-300">Hero</span></strong><div className="mt-7 space-y-2 text-[10px] font-bold text-slate-400">{['Dashboard', 'Report issue', 'Complaints', 'Heatmap', 'Rewards'].map((item, index) => <span key={item} className={`block rounded-lg px-3 py-2 ${index === 0 ? 'bg-blue-600 text-white' : ''}`}>{item}</span>)}</div></aside>
          <div className="bg-slate-50 p-5"><div className="grid grid-cols-3 gap-3"><PreviewStat label="Open issues" value="18" tone="bg-amber-100 text-amber-700" /><PreviewStat label="Resolved" value="92" tone="bg-emerald-100 text-emerald-700" /><PreviewStat label="Your points" value="1,250" tone="bg-blue-100 text-blue-700" /></div><div className="mt-4 grid gap-4 sm:grid-cols-[1.2fr_.8fr]"><div className="rounded-2xl border border-slate-200 bg-white p-4"><div className="flex items-center justify-between"><strong className="text-xs">Complaint activity</strong><span className="text-[9px] text-slate-400">Past 30 days</span></div><svg className="mt-5 h-28 w-full" viewBox="0 0 500 150" preserveAspectRatio="none" aria-hidden="true"><defs><linearGradient id="heroChart" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor="#2563eb" stopOpacity=".28" /><stop offset="1" stopColor="#2563eb" stopOpacity="0" /></linearGradient></defs><path d="M0 130 L70 110 L130 118 L200 78 L270 90 L340 54 L410 66 L500 25 L500 150 L0 150Z" fill="url(#heroChart)" /><polyline points="0,130 70,110 130,118 200,78 270,90 340,54 410,66 500,25" fill="none" stroke="#2563eb" strokeWidth="5" strokeLinecap="round" strokeLinejoin="round" /></svg></div><div className="relative overflow-hidden rounded-2xl bg-slate-200 p-4"><div className="absolute inset-0 bg-[linear-gradient(29deg,transparent_46%,white_47%,white_50%,transparent_51%),linear-gradient(-36deg,transparent_46%,white_47%,white_50%,transparent_51%)] bg-[length:100px_80px,130px_100px]" />{[{l:'24%',t:'30%',c:'bg-rose-500'},{l:'56%',t:'53%',c:'bg-amber-400'},{l:'72%',t:'25%',c:'bg-emerald-500'}].map((pin,index)=><i key={index} className={`absolute h-7 w-7 rounded-full border-4 border-white shadow ${pin.c}`} style={{left:pin.l,top:pin.t}} />)}<span className="absolute bottom-3 left-3 rounded-lg bg-white/90 px-3 py-2 text-[9px] font-black shadow">Heatmap preview</span></div></div><div className="mt-4 space-y-2">{[['Pothole on Main Street','In progress'],['Garbage near city park','Assigned'],['Streetlight not working','Resolved']].map(([title,state]) => <div key={title} className="flex items-center justify-between rounded-xl border border-slate-200 bg-white px-4 py-3 text-[10px]"><strong>{title}</strong><span className="rounded-full bg-blue-50 px-2 py-1 font-black text-blue-700">{state}</span></div>)}</div></div>
        </div>
      </div>
      <div className="absolute -bottom-1 -left-5 rounded-2xl border border-white/15 bg-white/10 p-4 text-white shadow-2xl backdrop-blur-xl animate-civic-float"><p className="text-[10px] font-black uppercase tracking-wider text-cyan-100">Ward 3 resolution rate</p><strong className="mt-1 block text-2xl">72%</strong><div className="mt-2 h-2 w-40 overflow-hidden rounded-full bg-white/10"><i className="block h-full w-[72%] rounded-full bg-gradient-to-r from-cyan-300 to-emerald-300" /></div></div>
    </div>
  );
}

function PreviewStat({ label, value, tone }) { return <div className="rounded-xl border border-slate-200 bg-white p-3"><span className={`inline-flex rounded-lg px-2 py-1 text-[9px] font-black ${tone}`}>{label}</span><strong className="mt-3 block text-xl">{value}</strong></div>; }
function Impact({ value, label, icon }) { return <div className="flex items-center gap-4 border-b border-slate-200 p-6 last:border-0 sm:border-b-0 sm:border-r"><div className="grid h-12 w-12 shrink-0 place-items-center rounded-2xl bg-blue-50 text-blue-700"><CivicIcon name={icon} size={24} /></div><div><strong className="block text-2xl font-black text-slate-950">{value}</strong><span className="mt-1 block text-xs font-bold text-slate-500">{label}</span></div></div>; }
function publicIssueIcon(category = '') { const value = String(category).toLowerCase(); if (value.includes('garbage') || value.includes('waste')) return 'recycle'; if (value.includes('road') || value.includes('pothole')) return 'road'; if (value.includes('light')) return 'light'; if (value.includes('water')) return 'water'; return 'location'; }
