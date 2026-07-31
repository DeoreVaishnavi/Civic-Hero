import { useMemo, useState } from 'react';
import { civicInitiatives } from '../../data/civicInitiatives.js';

export default function ProjectsPage() {
  const [type, setType] = useState('All');
  const [category, setCategory] = useState('All');
  const [selectedId, setSelectedId] = useState(civicInitiatives[0].id);
  const [following, setFollowing] = useState([]);
  const [message, setMessage] = useState('');

  const filtered = useMemo(() => civicInitiatives.filter((item) => (type === 'All' || item.type === type) && (category === 'All' || item.category === category)), [type, category]);
  const selected = civicInitiatives.find((item) => item.id === selectedId) || filtered[0] || civicInitiatives[0];
  const categories = ['All', ...new Set(civicInitiatives.map((item) => item.category))];

  const follow = () => {
    setFollowing((current) => current.includes(selected.id) ? current.filter((id) => id !== selected.id) : [...current, selected.id]);
    setMessage(following.includes(selected.id) ? 'Project removed from your followed list.' : 'You are now following this initiative.');
    window.setTimeout(() => setMessage(''), 3200);
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

              <div className="mt-8 flex flex-wrap gap-3 border-t border-slate-200 pt-6"><button type="button" onClick={follow} className={`rounded-xl px-5 py-3 text-sm font-black text-white shadow-lg transition hover:-translate-y-0.5 ${following.includes(selected.id) ? 'bg-emerald-600 shadow-emerald-600/20' : 'bg-blue-600 shadow-blue-600/20'}`}>{following.includes(selected.id) ? '✓ Following initiative' : '＋ Follow initiative'}</button><button type="button" onClick={() => { setMessage('Thank you. Your feedback has been recorded for project review.'); window.setTimeout(() => setMessage(''), 3200); }} className="rounded-xl border border-slate-200 bg-white px-5 py-3 text-sm font-black text-slate-700 transition hover:border-blue-300 hover:bg-blue-50 hover:text-blue-700">Give feedback</button></div>
            </div>
          </section>
        </div>
      </div>
    </main>
  );
}

function Filter({ label, value, onChange, options }) { return <label><span className="mb-2 block text-xs font-black uppercase tracking-wider text-slate-500">{label}</span><select value={value} onChange={(event) => onChange(event.target.value)} className="w-full rounded-xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm font-bold text-slate-800 outline-none transition focus:border-blue-400 focus:ring-4 focus:ring-blue-100">{options.map((option) => <option key={option}>{option}</option>)}</select></label>; }
function HeroStat({ value, label }) { return <div className="rounded-2xl border border-white/10 bg-white/10 p-4 backdrop-blur"><strong className="block text-xl font-black">{value}</strong><span className="mt-1 block text-xs text-blue-100">{label}</span></div>; }
function MiniStat({ label, value }) { return <div className="rounded-2xl border border-white/15 bg-white/10 p-4 backdrop-blur"><span className="text-[10px] font-black uppercase tracking-wider text-white/60">{label}</span><strong className="mt-2 block text-sm sm:text-base">{value}</strong></div>; }
function ProgressVisual({ label, index, selected }) { return <div className={`relative h-36 overflow-hidden rounded-2xl bg-gradient-to-br ${selected.visual.gradient}`}><div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,.07)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.07)_1px,transparent_1px)] bg-[length:24px_24px]" /><div className="absolute bottom-5 left-4 right-4 h-2 rounded-full bg-white/15"><span className="block h-full rounded-full bg-white/80" style={{ width: `${Math.min(100, selected.progress + (index - 1) * 22)}%` }} /></div><span className="absolute left-4 top-4 rounded-full bg-black/25 px-3 py-1.5 text-[10px] font-black uppercase tracking-wider text-white backdrop-blur">{label}</span><span className="absolute right-4 top-1/2 -translate-y-1/2 text-4xl">{selected.visual.icon}</span></div>; }
function InfoCard({ title, rows }) { return <div className="rounded-2xl border border-slate-200 p-5"><h3 className="font-black text-slate-900">{title}</h3><div className="mt-4 space-y-4">{rows.map(([value, label]) => <div key={`${value}-${label}`} className="border-b border-slate-100 pb-3 last:border-0 last:pb-0"><strong className="block text-sm text-slate-800">{value}</strong><span className="mt-1 block text-xs text-slate-500">{label}</span></div>)}</div></div>; }
