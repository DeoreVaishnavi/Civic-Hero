import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { civicInitiatives } from '../../data/civicInitiatives.js';

export default function GovernmentShowcase() {
  const [index, setIndex] = useState(0);
  const [paused, setPaused] = useState(false);
  const slide = civicInitiatives[index];

  useEffect(() => {
    if (paused) return undefined;
    const timer = window.setInterval(() => setIndex((current) => (current + 1) % civicInitiatives.length), 6500);
    return () => window.clearInterval(timer);
  }, [paused]);

  const move = (direction) => setIndex((current) => (current + direction + civicInitiatives.length) % civicInitiatives.length);

  return (
    <section id="schemes-projects" className="overflow-hidden bg-white py-20 sm:py-24">
      <div className="mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="grid items-end gap-8 lg:grid-cols-[1fr_auto]">
          <div>
            <p className="text-xs font-black uppercase tracking-[.22em] text-blue-600">Government schemes and projects</p>
            <h2 className="mt-4 max-w-3xl text-3xl font-black tracking-tight text-slate-950 sm:text-4xl lg:text-5xl">A cinematic view of public work in progress.</h2>
            <p className="mt-4 max-w-2xl text-base leading-7 text-slate-600">Follow budgets, milestones, departments and visible citizen impact through an auto-playing 3D project showcase.</p>
          </div>
          <Link to="/projects" className="inline-flex items-center justify-center rounded-xl border border-slate-200 bg-white px-5 py-3 text-sm font-black text-slate-800 shadow-sm transition hover:-translate-y-0.5 hover:border-blue-300 hover:text-blue-700">Explore all initiatives →</Link>
        </div>

        <div className="mt-10 overflow-hidden rounded-[2rem] border border-slate-200 bg-slate-950 shadow-2xl shadow-slate-900/20">
          <div className="grid lg:grid-cols-[1.25fr_.75fr]">
            <div className={`relative min-h-[540px] overflow-hidden bg-gradient-to-br ${slide.visual.gradient} p-6 text-white sm:p-10`}>
              <div className={`absolute -left-24 top-10 h-72 w-72 rounded-full ${slide.visual.glow} blur-3xl`} />
              <div className="absolute -right-16 bottom-10 h-72 w-72 rounded-full bg-white/10 blur-3xl" />
              <div className="absolute inset-0 bg-[linear-gradient(rgba(255,255,255,.045)_1px,transparent_1px),linear-gradient(90deg,rgba(255,255,255,.045)_1px,transparent_1px)] bg-[length:36px_36px] [mask-image:linear-gradient(to_bottom,black,transparent_90%)]" />

              <div className="relative z-10 flex items-center justify-between gap-4">
                <span className="rounded-full border border-white/15 bg-white/10 px-4 py-2 text-xs font-black uppercase tracking-[.2em] backdrop-blur-xl">{slide.type} · {slide.status}</span>
                <button type="button" onClick={() => setPaused((value) => !value)} className="grid h-11 w-11 place-items-center rounded-full border border-white/15 bg-white/10 text-lg backdrop-blur-xl transition hover:scale-105 hover:bg-white/20" aria-label={paused ? 'Play slideshow' : 'Pause slideshow'}>{paused ? '▶' : 'Ⅱ'}</button>
              </div>

              <div className="relative z-10 mt-12 max-w-2xl">
                <p className="text-sm font-bold text-white/70">{slide.department}</p>
                <h3 className="mt-3 text-4xl font-black leading-tight tracking-tight sm:text-5xl">{slide.title}</h3>
                <p className="mt-5 max-w-xl text-base leading-7 text-white/75">{slide.summary}</p>
              </div>

              <ProjectVisual slide={slide} />

              <div className="relative z-10 mt-8 grid grid-cols-3 gap-3">
                <VisualMetric label="Budget" value={slide.budget} />
                <VisualMetric label="Progress" value={`${slide.progress}%`} />
                <VisualMetric label="Impact" value={slide.impact} />
              </div>
            </div>

            <aside className="flex flex-col bg-white p-6 sm:p-9">
              <div className="flex items-start justify-between gap-4">
                <div><p className="text-xs font-black uppercase tracking-[.18em] text-blue-600">Now showing</p><h3 className="mt-2 text-2xl font-black text-slate-950">{slide.shortTitle}</h3></div>
                <span className="text-4xl" aria-hidden="true">{slide.visual.icon}</span>
              </div>

              <dl className="mt-7 space-y-4 text-sm">
                <Detail label="Coverage" value={slide.ward} />
                <Detail label="Field officer" value={slide.officer} />
                <Detail label="Target completion" value={slide.targetDate} />
                <Detail label="Beneficiaries" value={slide.beneficiaries} />
              </dl>

              <div className="mt-7">
                <div className="flex items-center justify-between text-sm font-bold text-slate-700"><span>Overall progress</span><strong>{slide.progress}%</strong></div>
                <div className="mt-3 h-3 overflow-hidden rounded-full bg-slate-100"><div className="h-full rounded-full bg-gradient-to-r from-blue-600 via-cyan-500 to-emerald-400 transition-all duration-700" style={{ width: `${slide.progress}%` }} /></div>
              </div>

              <div className="mt-7 space-y-3">
                {slide.milestones.map((milestone) => <div key={milestone.label} className="flex items-center gap-3 rounded-xl border border-slate-200 px-4 py-3"><span className={`grid h-7 w-7 place-items-center rounded-full text-xs font-black ${milestone.complete ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-400'}`}>{milestone.complete ? '✓' : '○'}</span><span className="text-sm font-bold text-slate-700">{milestone.label}</span></div>)}
              </div>

              <div className="mt-auto pt-8">
                <div className="flex items-center justify-between gap-3">
                  <button type="button" onClick={() => move(-1)} className="grid h-12 w-12 place-items-center rounded-xl border border-slate-200 text-xl font-black text-slate-700 transition hover:border-blue-300 hover:bg-blue-50">←</button>
                  <div className="flex flex-1 justify-center gap-2">{civicInitiatives.map((item, dotIndex) => <button key={item.id} type="button" onClick={() => setIndex(dotIndex)} className={`h-2.5 rounded-full transition-all ${dotIndex === index ? 'w-9 bg-blue-600' : 'w-2.5 bg-slate-200 hover:bg-slate-300'}`} aria-label={`Show ${item.title}`} />)}</div>
                  <button type="button" onClick={() => move(1)} className="grid h-12 w-12 place-items-center rounded-xl border border-slate-200 text-xl font-black text-slate-700 transition hover:border-blue-300 hover:bg-blue-50">→</button>
                </div>
                <div className="mt-5 h-1 overflow-hidden rounded-full bg-slate-100"><div key={`${index}-${paused}`} className={`h-full bg-blue-600 ${paused ? '' : 'animate-showcase-progress'}`} /></div>
              </div>
            </aside>
          </div>
        </div>
      </div>
    </section>
  );
}

function ProjectVisual({ slide }) {
  return (
    <div className="relative z-10 mt-10 h-56 [perspective:1400px] sm:h-64">
      <div className="absolute inset-x-5 bottom-0 top-2 animate-civic-float [transform-style:preserve-3d] [transform:rotateX(58deg)_rotateZ(-6deg)]">
        <div className="absolute inset-0 rounded-[2.5rem] border border-white/20 bg-white/10 shadow-2xl shadow-black/30 backdrop-blur-sm [transform:translateZ(0)]" />
        <div className="absolute inset-x-[8%] bottom-[10%] top-[14%] overflow-hidden rounded-[2rem] border border-white/15 bg-slate-950/50 [transform:translateZ(34px)]">
          <div className="absolute inset-0 bg-[linear-gradient(90deg,transparent,rgba(255,255,255,.13),transparent)] bg-[length:45%_100%] animate-civic-scan" />
          <div className="absolute bottom-0 left-0 right-0 h-2/5 bg-gradient-to-t from-black/70 to-transparent" />
          <div className="absolute left-[8%] top-[16%] h-16 w-12 rounded-t-lg bg-white/15 shadow-[60px_18px_0_rgba(255,255,255,.12),125px_-2px_0_rgba(255,255,255,.1),190px_22px_0_rgba(255,255,255,.12),260px_4px_0_rgba(255,255,255,.1)]" />
          <div className="absolute bottom-[15%] left-[6%] right-[6%] h-1.5 rounded-full bg-white/20"><span className={`block h-full rounded-full ${slide.visual.accent}`} style={{ width: `${slide.progress}%` }} /></div>
          <div className="absolute left-6 top-5 flex items-center gap-2 rounded-full border border-white/15 bg-black/25 px-3 py-1.5 text-[10px] font-black uppercase tracking-wider"><span className="h-2 w-2 animate-pulse rounded-full bg-rose-400" /> Live project view</div>
          <div className="absolute bottom-7 right-7 grid h-16 w-16 place-items-center rounded-2xl border border-white/15 bg-white/10 text-4xl shadow-xl backdrop-blur-xl">{slide.visual.icon}</div>
        </div>
      </div>
      <div className="absolute left-[8%] top-[20%] h-14 w-24 animate-civic-orbit rounded-2xl border border-white/15 bg-white/10 p-3 text-[10px] font-bold text-white/80 shadow-xl backdrop-blur-xl"><strong className="block text-sm text-white">{slide.progress}%</strong>Milestone progress</div>
      <div className="absolute right-[5%] top-[8%] h-14 w-28 animate-civic-float-delayed rounded-2xl border border-white/15 bg-white/10 p-3 text-[10px] font-bold text-white/80 shadow-xl backdrop-blur-xl"><strong className="block text-sm text-white">{slide.budget}</strong>Approved budget</div>
    </div>
  );
}

function VisualMetric({ label, value }) { return <div className="rounded-2xl border border-white/15 bg-white/10 p-4 backdrop-blur-xl"><span className="text-[10px] font-black uppercase tracking-wider text-white/60">{label}</span><strong className="mt-2 block text-sm text-white sm:text-base">{value}</strong></div>; }
function Detail({ label, value }) { return <div className="flex items-start justify-between gap-5 border-b border-slate-100 pb-4"><dt className="text-slate-500">{label}</dt><dd className="text-right font-black text-slate-800">{value}</dd></div>; }
