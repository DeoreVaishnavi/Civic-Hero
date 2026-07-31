import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { civicInitiatives } from '../../data/civicInitiatives.js';
import { initiativeApi } from '../../services/initiativeApi.js';

const emptyFeedback = { rating: 5, category: 'General', feedback: '' };

export default function ProjectsPage() {
  const [type, setType] = useState('All');
  const [category, setCategory] = useState('All');
  const [selectedId, setSelectedId] = useState(civicInitiatives[0].id);
  const [engagement, setEngagement] = useState({ followerCount: 0, feedbackCount: 0, averageRating: 0, isFollowing: false });
  const [loadingEngagement, setLoadingEngagement] = useState(false);
  const [busyAction, setBusyAction] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [showFeedback, setShowFeedback] = useState(false);
  const [feedbackForm, setFeedbackForm] = useState(emptyFeedback);
  const { isAuthenticated, user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const filtered = useMemo(() => civicInitiatives.filter((item) => (type === 'All' || item.type === type) && (category === 'All' || item.category === category)), [type, category]);
  const selected = civicInitiatives.find((item) => item.id === selectedId) || filtered[0] || civicInitiatives[0];
  const categories = ['All', ...new Set(civicInitiatives.map((item) => item.category))];

  useEffect(() => {
    let active = true;
    setLoadingEngagement(true);
    setError('');
    initiativeApi.engagement(selected.id)
      .then((result) => { if (active) setEngagement(result); })
      .catch((reason) => { if (active) setError(reason.message || 'Unable to load initiative engagement.'); })
      .finally(() => { if (active) setLoadingEngagement(false); });
    return () => { active = false; };
  }, [selected.id, isAuthenticated]);

  const showMessage = (text) => {
    setMessage(text);
    window.setTimeout(() => setMessage(''), 3500);
  };

  const requireCitizen = () => {
    if (!isAuthenticated) {
      navigate('/login', { state: { from: `${location.pathname}${location.search}` } });
      return false;
    }
    if ((user?.role || '').toLowerCase() !== 'citizen') {
      setError('Only Citizen accounts can follow initiatives or submit feedback.');
      return false;
    }
    return true;
  };

  const follow = async () => {
    if (!requireCitizen()) return;
    try {
      setBusyAction('follow');
      setError('');
      const result = await initiativeApi.toggleFollow(selected.id);
      setEngagement(result);
      showMessage(result.isFollowing ? 'You are now following this initiative. Supervisors can see this activity.' : 'Initiative removed from your followed list.');
    } catch (reason) {
      setError(reason.message || 'Unable to update initiative follow status.');
    } finally {
      setBusyAction('');
    }
  };

  const openFeedback = () => {
    if (!requireCitizen()) return;
    setFeedbackForm(emptyFeedback);
    setShowFeedback(true);
  };

  const submitFeedback = async (event) => {
    event.preventDefault();
    const trimmed = feedbackForm.feedback.trim();
    if (trimmed.length < 10) {
      setError('Please enter at least 10 characters in your feedback.');
      return;
    }
    try {
      setBusyAction('feedback');
      setError('');
      const result = await initiativeApi.submitFeedback(selected.id, { ...feedbackForm, feedback: trimmed });
      setEngagement(result.engagement);
      setShowFeedback(false);
      setFeedbackForm(emptyFeedback);
      showMessage('Thank you. Your feedback was recorded and sent to the supervisor team.');
    } catch (reason) {
      setError(reason.message || 'Unable to submit feedback.');
    } finally {
      setBusyAction('');
    }
  };

  return (
    <main className="min-h-screen bg-slate-50 py-10 sm:py-14">
      <div className="mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8">
        <section className="relative overflow-hidden rounded-[2rem] bg-gradient-to-br from-slate-950 via-blue-950 to-blue-700 px-6 py-10 text-white shadow-2xl sm:px-10 lg:px-14 lg:py-14">
          <div className="absolute -right-16 -top-20 h-80 w-80 rounded-full bg-cyan-400/15 blur-3xl" />
          <div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,.045)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.045)_1px,transparent_1px)] bg-[length:42px_42px]" />
          <div className="relative grid items-center gap-10 lg:grid-cols-[1fr_.7fr]">
            <div><p className="text-xs font-black uppercase tracking-[.22em] text-cyan-200">Public transparency tracker</p><h1 className="mt-4 text-4xl font-black tracking-tight sm:text-5xl">Government schemes and projects</h1><p className="mt-5 max-w-2xl text-base leading-7 text-blue-100">Track budgets, departments, milestones, contractors, ward coverage and visible public impact in one place.</p></div>
            <div className="grid grid-cols-2 gap-3"><HeroStat value="4" label="Active initiatives" /><HeroStat value="₹43.4 Cr" label="Combined budget" /><HeroStat value="69%" label="Average progress" /><HeroStat value="8.5 lakh" label="Estimated beneficiaries" /></div>
          </div>
        </section>

        <section className="mt-8 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:p-5">
          <div className="grid gap-3 md:grid-cols-[1fr_1fr_auto]">
            <Filter label="Initiative type" value={type} onChange={setType} options={['All', 'Project', 'Scheme']} />
            <Filter label="Category" value={category} onChange={setCategory} options={categories} />
            <button type="button" onClick={() => { setType('All'); setCategory('All'); }} className="self-end rounded-xl bg-blue-600 px-5 py-3 text-sm font-black text-white shadow-lg shadow-blue-600/20 transition hover:bg-blue-700">Reset filters</button>
          </div>
        </section>

        {message && <div className="mt-6 rounded-2xl border border-emerald-200 bg-emerald-50 p-4 text-sm font-bold text-emerald-800">✓ {message}</div>}
        {error && <div className="mt-6 rounded-2xl border border-rose-200 bg-rose-50 p-4 text-sm font-bold text-rose-800">{error}</div>}

        <div className="mt-8 grid gap-8 lg:grid-cols-[.72fr_1.28fr]">
          <section className="space-y-4">
            <div className="flex items-center justify-between"><div><p className="text-xs font-black uppercase tracking-[.18em] text-blue-600">Initiative list</p><h2 className="mt-1 text-2xl font-black text-slate-950">{filtered.length} results</h2></div></div>
            {filtered.map((item) => <button type="button" key={item.id} onClick={() => setSelectedId(item.id)} className={`w-full overflow-hidden rounded-2xl border bg-white text-left shadow-sm transition duration-300 hover:-translate-y-1 hover:shadow-xl ${selected.id === item.id ? 'border-blue-400 ring-4 ring-blue-100' : 'border-slate-200'}`}>
              <div className={`h-28 bg-gradient-to-br ${item.visual.gradient} p-5 text-white`}><div className="flex items-start justify-between"><span className="text-4xl">{item.visual.icon}</span><span className="rounded-full bg-white/15 px-3 py-1.5 text-[10px] font-black uppercase tracking-wider backdrop-blur">{item.type}</span></div></div>
              <div className="p-5"><h3 className="text-lg font-black text-slate-900">{item.shortTitle}</h3><p className="mt-2 text-sm leading-6 text-slate-500">{item.ward}</p><div className="mt-4 flex items-center justify-between text-xs font-bold text-slate-600"><span>{item.status}</span><strong>{item.progress}%</strong></div><div className="mt-2 h-2 overflow-hidden rounded-full bg-slate-100"><div className="h-full rounded-full bg-blue-600" style={{ width: `${item.progress}%` }} /></div></div>
            </button>)}
          </section>

          <section className="overflow-hidden rounded-[2rem] border border-slate-200 bg-white shadow-xl shadow-slate-900/5">
            <div className={`relative overflow-hidden bg-gradient-to-br ${selected.visual.gradient} p-7 text-white sm:p-10`}>
              <div className="absolute -right-10 -top-14 h-56 w-56 rounded-full bg-white/10 blur-2xl" />
              <div className="relative flex flex-wrap items-start justify-between gap-5"><div><span className="inline-flex rounded-full border border-white/15 bg-white/10 px-4 py-2 text-xs font-black uppercase tracking-[.18em] backdrop-blur">{selected.type} · {selected.status}</span><h2 className="mt-5 max-w-3xl text-3xl font-black sm:text-4xl">{selected.title}</h2><p className="mt-4 max-w-2xl text-sm leading-7 text-white/75">{selected.summary}</p></div><div className="text-6xl">{selected.visual.icon}</div></div>
              <div className="relative mt-8 grid grid-cols-2 gap-3 sm:grid-cols-4"><MiniStat label="Budget" value={selected.budget} /><MiniStat label="Spent" value={selected.spent} /><MiniStat label="Progress" value={`${selected.progress}%`} /><MiniStat label="Impact" value={selected.impact} /></div>
            </div>

            <div className="p-6 sm:p-9">
              <div className="mb-7 grid gap-3 sm:grid-cols-3">
                <EngagementStat label="Followers" value={loadingEngagement ? '…' : engagement.followerCount} />
                <EngagementStat label="Feedback received" value={loadingEngagement ? '…' : engagement.feedbackCount} />
                <EngagementStat label="Citizen rating" value={loadingEngagement ? '…' : (engagement.feedbackCount ? `${engagement.averageRating}/5` : 'No rating yet')} />
              </div>

              <div className="grid gap-8 xl:grid-cols-[1fr_.8fr]">
                <div>
                  <h3 className="text-xl font-black text-slate-950">Live progress and milestones</h3>
                  <div className="mt-5 grid gap-4 sm:grid-cols-3"><ProgressVisual label="Before" index={0} selected={selected} /><ProgressVisual label="In progress" index={1} selected={selected} /><ProgressVisual label="Current" index={2} selected={selected} /></div>
                  <div className="mt-6 rounded-2xl border border-slate-200 p-5"><div className="flex items-center justify-between text-sm font-black text-slate-700"><span>Project timeline</span><span>{selected.progress}% complete</span></div><div className="relative mt-7 h-1 rounded-full bg-slate-200"><div className="absolute inset-y-0 left-0 rounded-full bg-blue-600" style={{ width: `${selected.progress}%` }} />{selected.milestones.map((milestone, index) => <span key={milestone.label} className={`absolute top-1/2 grid h-7 w-7 -translate-x-1/2 -translate-y-1/2 place-items-center rounded-full border-4 border-white text-[10px] font-black shadow ${milestone.complete ? 'bg-emerald-500 text-white' : 'bg-slate-200 text-slate-500'}`} style={{ left: `${(index / (selected.milestones.length - 1)) * 100}%` }}>{milestone.complete ? '✓' : index + 1}</span>)}</div><div className="mt-5 grid grid-cols-4 gap-2 text-center text-[10px] font-bold text-slate-500">{selected.milestones.map((item) => <span key={item.label}>{item.label}</span>)}</div></div>
                </div>

                <aside className="space-y-5">
                  <InfoCard title="Contractor and department" rows={[[selected.contractor, 'Implementation partner'], [selected.officer, 'Field officer'], [selected.department, 'Responsible department']]} />
                  <InfoCard title="Schedule" rows={[[selected.startDate, 'Start date'], [selected.targetDate, 'Target completion'], [selected.beneficiaries, 'Beneficiaries']]} />
                  <div className="relative h-48 overflow-hidden rounded-2xl border border-slate-200 bg-slate-100"><div className="absolute inset-0 bg-[linear-gradient(29deg,transparent_46%,white_47%,white_50%,transparent_51%),linear-gradient(-36deg,transparent_46%,white_47%,white_50%,transparent_51%),linear-gradient(rgba(100,116,139,.12)_1px,transparent_1px),linear-gradient(90deg,rgba(100,116,139,.12)_1px,transparent_1px)] bg-[length:120px_90px,150px_110px,36px_36px,36px_36px]" /><span className="absolute left-1/2 top-1/2 grid h-14 w-14 -translate-x-1/2 -translate-y-1/2 place-items-center rounded-full bg-rose-500 text-2xl text-white shadow-xl">⌖</span><div className="absolute bottom-3 left-3 right-3 rounded-xl bg-white/90 p-3 text-xs font-bold text-slate-700 shadow backdrop-blur">{selected.ward}</div></div>
                </aside>
              </div>

              <div className="mt-8 flex flex-wrap gap-3 border-t border-slate-200 pt-6">
                <button type="button" onClick={follow} disabled={busyAction === 'follow'} className={`rounded-xl px-5 py-3 text-sm font-black text-white shadow-lg transition hover:-translate-y-0.5 disabled:cursor-wait disabled:opacity-60 ${engagement.isFollowing ? 'bg-emerald-600 shadow-emerald-600/20' : 'bg-blue-600 shadow-blue-600/20'}`}>{busyAction === 'follow' ? 'Saving…' : engagement.isFollowing ? '✓ Following initiative' : '＋ Follow initiative'}</button>
                <button type="button" onClick={openFeedback} className="rounded-xl border border-slate-200 bg-white px-5 py-3 text-sm font-black text-slate-700 transition hover:border-blue-300 hover:bg-blue-50 hover:text-blue-700">Give feedback</button>
                <span className="self-center text-xs font-semibold text-slate-500">Follow activity and feedback are visible to the supervisor team.</span>
              </div>
            </div>
          </section>
        </div>
      </div>

      {showFeedback && <div className="fixed inset-0 z-[100] grid place-items-center bg-slate-950/70 p-4 backdrop-blur-sm" role="dialog" aria-modal="true" aria-labelledby="initiative-feedback-title">
        <form onSubmit={submitFeedback} className="w-full max-w-xl rounded-[2rem] bg-white p-6 shadow-2xl sm:p-8">
          <div className="flex items-start justify-between gap-4"><div><p className="text-xs font-black uppercase tracking-[.18em] text-blue-600">Citizen feedback</p><h2 id="initiative-feedback-title" className="mt-2 text-2xl font-black text-slate-950">{selected.shortTitle}</h2><p className="mt-2 text-sm text-slate-500">Your response will be recorded and sent to supervisors for review.</p></div><button type="button" onClick={() => setShowFeedback(false)} className="grid h-10 w-10 place-items-center rounded-full bg-slate-100 text-xl font-black text-slate-600" aria-label="Close feedback form">×</button></div>
          <div className="mt-6 grid gap-5 sm:grid-cols-2">
            <label><span className="mb-2 block text-xs font-black uppercase tracking-wider text-slate-500">Rating</span><select value={feedbackForm.rating} onChange={(event) => setFeedbackForm((current) => ({ ...current, rating: Number(event.target.value) }))} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none focus:border-blue-400 focus:ring-4 focus:ring-blue-100"><option value={5}>5 — Excellent</option><option value={4}>4 — Good</option><option value={3}>3 — Average</option><option value={2}>2 — Needs improvement</option><option value={1}>1 — Poor</option></select></label>
            <label><span className="mb-2 block text-xs font-black uppercase tracking-wider text-slate-500">Feedback type</span><select value={feedbackForm.category} onChange={(event) => setFeedbackForm((current) => ({ ...current, category: event.target.value }))} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold outline-none focus:border-blue-400 focus:ring-4 focus:ring-blue-100"><option>General</option><option>Progress</option><option>Quality</option><option>Transparency</option><option>Safety</option><option>Suggestion</option></select></label>
          </div>
          <label className="mt-5 block"><span className="mb-2 block text-xs font-black uppercase tracking-wider text-slate-500">Your feedback</span><textarea required minLength={10} maxLength={1500} rows={6} value={feedbackForm.feedback} onChange={(event) => setFeedbackForm((current) => ({ ...current, feedback: event.target.value }))} placeholder="Describe what is working, what should improve, or what the supervisor should review." className="w-full resize-y rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm leading-6 outline-none focus:border-blue-400 focus:ring-4 focus:ring-blue-100" /><span className="mt-2 block text-right text-xs text-slate-400">{feedbackForm.feedback.length}/1500</span></label>
          <div className="mt-6 flex justify-end gap-3"><button type="button" onClick={() => setShowFeedback(false)} className="rounded-xl border border-slate-200 px-5 py-3 text-sm font-black text-slate-700">Cancel</button><button type="submit" disabled={busyAction === 'feedback'} className="rounded-xl bg-blue-600 px-5 py-3 text-sm font-black text-white shadow-lg shadow-blue-600/20 disabled:cursor-wait disabled:opacity-60">{busyAction === 'feedback' ? 'Sending…' : 'Send to supervisor'}</button></div>
        </form>
      </div>}
    </main>
  );
}

function Filter({ label, value, onChange, options }) { return <label><span className="mb-2 block text-xs font-black uppercase tracking-wider text-slate-500">{label}</span><select value={value} onChange={(event) => onChange(event.target.value)} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-800 outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100">{options.map((option) => <option key={option}>{option}</option>)}</select></label>; }
function HeroStat({ value, label }) { return <div className="rounded-2xl border border-white/10 bg-white/10 p-4 backdrop-blur"><strong className="block text-xl font-black">{value}</strong><span className="mt-1 block text-xs text-blue-100">{label}</span></div>; }
function MiniStat({ label, value }) { return <div className="rounded-2xl border border-white/15 bg-white/10 p-4 backdrop-blur"><span className="text-[10px] font-black uppercase tracking-wider text-white/60">{label}</span><strong className="mt-2 block text-sm sm:text-base">{value}</strong></div>; }
function EngagementStat({ label, value }) { return <div className="rounded-2xl border border-blue-100 bg-blue-50/70 p-4"><span className="text-[10px] font-black uppercase tracking-[.14em] text-blue-600">{label}</span><strong className="mt-2 block text-xl font-black text-slate-950">{value}</strong></div>; }
function ProgressVisual({ label, index, selected }) { return <div className={`relative h-36 overflow-hidden rounded-2xl bg-gradient-to-br ${selected.visual.gradient}`}><div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,.07)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.07)_1px,transparent_1px)] bg-[length:24px_24px]" /><div className="absolute bottom-5 left-4 right-4 h-2 rounded-full bg-white/15"><span className="block h-full rounded-full bg-white/80" style={{ width: `${Math.min(100, selected.progress + (index - 1) * 22)}%` }} /></div><span className="absolute left-4 top-4 rounded-full bg-black/25 px-3 py-1.5 text-[10px] font-black uppercase tracking-wider text-white backdrop-blur">{label}</span><span className="absolute right-4 top-1/2 -translate-y-1/2 text-4xl">{selected.visual.icon}</span></div>; }
function InfoCard({ title, rows }) { return <div className="rounded-2xl border border-slate-200 p-5"><h3 className="font-black text-slate-900">{title}</h3><div className="mt-4 space-y-4">{rows.map(([value, label]) => <div key={`${value}-${label}`} className="border-b border-slate-100 pb-3 last:border-0 last:pb-0"><strong className="block text-sm text-slate-800">{value}</strong><span className="mt-1 block text-xs text-slate-500">{label}</span></div>)}</div></div>; }
