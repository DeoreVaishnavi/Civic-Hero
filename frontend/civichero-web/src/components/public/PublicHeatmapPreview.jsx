import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { analyticsApi } from '../../services/analyticsApi.js';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { homeHeatmapPreview } from '../../data/civicInitiatives.js';
import { dashboardForRole } from '../../utils/roleRouting.js';

const riskClass = {
  Critical: 'bg-rose-600 shadow-rose-500/40',
  High: 'bg-orange-500 shadow-orange-400/40',
  Medium: 'bg-amber-400 shadow-amber-300/40',
  Low: 'bg-emerald-500 shadow-emerald-400/40',
};

const normalise = (point, index) => ({
  id: point.id || `${point.latitude}-${point.longitude}-${index}`,
  wardName: point.wardName || 'City area',
  latitude: Number(point.latitude),
  longitude: Number(point.longitude),
  complaintCount: Number(point.complaintCount || 0),
  activeCount: Number(point.activeCount || 0),
  resolutionPendingCount: Number(point.resolutionPendingCount || 0),
  solvedCount: Number(point.solvedCount || 0),
  disputedCount: Number(point.disputedCount || 0),
  resolutionRate: Number(point.resolutionRate || 0),
  averageResolutionHours: Number(point.averageResolutionHours || 0),
  dominantCategory: point.dominantCategory || 'Other',
  riskLevel: point.riskLevel || 'Low',
});

export default function PublicHeatmapPreview() {
  const { isAuthenticated, user } = useAuth();
  const [points, setPoints] = useState(homeHeatmapPreview);
  const [selectedId, setSelectedId] = useState(homeHeatmapPreview[0].id);
  const [mode, setMode] = useState('preview');

  useEffect(() => {
    let active = true;
    if (!isAuthenticated) {
      setPoints(homeHeatmapPreview);
      setMode('preview');
      return () => { active = false; };
    }

    analyticsApi.heatmap({})
      .then((response) => {
        if (!active) return;
        const live = (Array.isArray(response) ? response : []).map(normalise).slice(0, 12);
        if (live.length) {
          setPoints(live);
          setSelectedId(live[0].id);
          setMode('live');
        }
      })
      .catch(() => {
        if (active) setMode('preview');
      });
    return () => { active = false; };
  }, [isAuthenticated]);

  const selected = points.find((item) => item.id === selectedId) || points[0];
  const heatmapTarget = !isAuthenticated ? ROUTE_PATHS.login : String(user?.role || '').toLowerCase() === 'citizen' ? ROUTE_PATHS.citizenHeatmap : dashboardForRole(user?.role);
  const plotted = useMemo(() => {
    const lats = points.map((item) => item.latitude);
    const lons = points.map((item) => item.longitude);
    const minLat = Math.min(...lats);
    const maxLat = Math.max(...lats);
    const minLon = Math.min(...lons);
    const maxLon = Math.max(...lons);
    const position = (value, min, max) => max === min ? 50 : 10 + ((value - min) * 80) / (max - min);
    return points.map((item) => ({
      ...item,
      left: position(item.longitude, minLon, maxLon),
      bottom: position(item.latitude, minLat, maxLat),
    }));
  }, [points]);

  return (
    <section id="heatmap" className="relative overflow-hidden bg-slate-950 py-20 text-white sm:py-24">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_12%_20%,rgba(14,165,233,.18),transparent_30%),radial-gradient(circle_at_88%_80%,rgba(16,185,129,.14),transparent_32%)]" />
      <div className="relative mx-auto w-full max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="grid items-end gap-8 lg:grid-cols-[1fr_auto]">
          <div>
            <span className="inline-flex items-center gap-2 rounded-full border border-sky-400/20 bg-sky-400/10 px-4 py-2 text-xs font-black uppercase tracking-[.2em] text-sky-200">
              <span className="h-2 w-2 animate-pulse rounded-full bg-sky-300" /> City intelligence
            </span>
            <h2 className="mt-5 max-w-3xl text-3xl font-black tracking-tight sm:text-4xl lg:text-5xl">See where complaints are concentrated and how quickly they are solved.</h2>
            <p className="mt-4 max-w-2xl text-base leading-7 text-slate-300">Select a hotspot to compare active, resolved, verification-pending and disputed complaints for that area.</p>
          </div>
          <div className="flex flex-wrap gap-3">
            <span className={`inline-flex items-center rounded-full px-4 py-2 text-xs font-black ${mode === 'live' ? 'bg-emerald-400/15 text-emerald-200' : 'bg-amber-400/15 text-amber-200'}`}>
              {mode === 'live' ? '● Live account data' : '● Public preview data'}
            </span>
            <Link to={heatmapTarget} className="rounded-xl bg-white px-5 py-3 text-sm font-black text-slate-950 shadow-xl shadow-black/20 transition hover:-translate-y-0.5 hover:bg-sky-50">
              Open full heatmap →
            </Link>
          </div>
        </div>

        <div className="mt-10 grid overflow-hidden rounded-[2rem] border border-white/10 bg-white/5 shadow-2xl shadow-black/30 backdrop-blur-xl lg:grid-cols-[1.45fr_.75fr]">
          <div className="relative min-h-[470px] overflow-hidden border-b border-white/10 lg:border-b-0 lg:border-r">
            <div className="absolute inset-0 bg-[linear-gradient(29deg,transparent_46%,rgba(255,255,255,.08)_47%,rgba(255,255,255,.08)_50%,transparent_51%),linear-gradient(-35deg,transparent_46%,rgba(255,255,255,.07)_47%,rgba(255,255,255,.07)_50%,transparent_51%),linear-gradient(rgba(148,163,184,.08)_1px,transparent_1px),linear-gradient(90deg,rgba(148,163,184,.08)_1px,transparent_1px)] bg-[length:155px_115px,190px_140px,42px_42px,42px_42px] opacity-80" />
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_45%_48%,rgba(244,63,94,.22),transparent_22%),radial-gradient(circle_at_25%_28%,rgba(245,158,11,.2),transparent_17%),radial-gradient(circle_at_70%_72%,rgba(16,185,129,.16),transparent_18%)]" />
            <div className="absolute left-5 top-5 z-10 flex flex-wrap gap-2 rounded-2xl border border-white/10 bg-slate-950/70 p-3 text-[11px] font-bold backdrop-blur-xl">
              {['Low', 'Medium', 'High', 'Critical'].map((risk) => <span key={risk} className="inline-flex items-center gap-2"><i className={`h-2.5 w-2.5 rounded-full ${riskClass[risk].split(' ')[0]}`} />{risk}</span>)}
            </div>

            {plotted.map((point) => {
              const selectedPoint = selected?.id === point.id;
              const size = Math.min(68, 34 + point.complaintCount * .55);
              return (
                <button
                  key={point.id}
                  type="button"
                  onClick={() => setSelectedId(point.id)}
                  className={`absolute z-20 grid -translate-x-1/2 translate-y-1/2 place-items-center rounded-full border-4 border-white/90 font-black text-white shadow-2xl transition duration-300 hover:scale-110 focus:outline-none focus:ring-4 focus:ring-sky-300/50 ${riskClass[point.riskLevel] || riskClass.Low} ${selectedPoint ? 'scale-110 ring-4 ring-white/30' : ''}`}
                  style={{ left: `${point.left}%`, bottom: `${point.bottom}%`, width: size, height: size }}
                  title={`${point.wardName}: ${point.complaintCount} complaints`}
                >
                  <span className="text-sm">{point.complaintCount}</span>
                </button>
              );
            })}

            <div className="absolute bottom-5 left-5 z-10 rounded-2xl border border-white/10 bg-slate-950/70 px-4 py-3 text-xs text-slate-300 backdrop-blur-xl">
              <strong className="block text-white">Interactive hotspot map</strong>
              Click any numbered circle to inspect the selected area.
            </div>
          </div>

          <aside className="p-6 sm:p-8">
            <p className="text-xs font-black uppercase tracking-[.2em] text-sky-300">Selected area</p>
            <h3 className="mt-3 text-2xl font-black">{selected?.wardName}</h3>
            <p className="mt-2 text-sm text-slate-400">Dominant issue: <strong className="text-white">{selected?.dominantCategory}</strong></p>

            <div className="mt-6 grid grid-cols-2 gap-3">
              <Metric label="Total" value={selected?.complaintCount} tone="sky" />
              <Metric label="Active" value={selected?.activeCount} tone="amber" />
              <Metric label="Solved" value={selected?.solvedCount} tone="emerald" />
              <Metric label="Disputed" value={selected?.disputedCount} tone="rose" />
            </div>

            <div className="mt-6 rounded-2xl border border-white/10 bg-slate-900/70 p-5">
              <div className="flex items-center justify-between gap-3"><span className="text-sm font-bold text-slate-300">Resolution rate</span><strong className="text-xl text-emerald-300">{Number(selected?.resolutionRate || 0).toFixed(0)}%</strong></div>
              <div className="mt-3 h-2.5 overflow-hidden rounded-full bg-white/10"><div className="h-full rounded-full bg-gradient-to-r from-sky-400 to-emerald-400 transition-all duration-700" style={{ width: `${Math.min(100, selected?.resolutionRate || 0)}%` }} /></div>
              <div className="mt-4 grid grid-cols-2 gap-4 text-xs text-slate-400"><span>Verification pending <strong className="block pt-1 text-base text-white">{selected?.resolutionPendingCount}</strong></span><span>Average resolution <strong className="block pt-1 text-base text-white">{Number(selected?.averageResolutionHours || 0).toFixed(1)} hrs</strong></span></div>
            </div>

            {!isAuthenticated && <p className="mt-5 rounded-2xl border border-amber-300/20 bg-amber-300/10 p-4 text-sm leading-6 text-amber-100">Sign in to view live, filterable heatmap data from the CivicHero complaint system.</p>}
          </aside>
        </div>
      </div>
    </section>
  );
}

function Metric({ label, value, tone }) {
  const tones = { sky: 'bg-sky-400/10 text-sky-200', amber: 'bg-amber-400/10 text-amber-200', emerald: 'bg-emerald-400/10 text-emerald-200', rose: 'bg-rose-400/10 text-rose-200' };
  return <div className={`rounded-2xl border border-white/10 p-4 ${tones[tone]}`}><span className="text-[11px] font-black uppercase tracking-wider opacity-80">{label}</span><strong className="mt-2 block text-2xl text-white">{value ?? 0}</strong></div>;
}
