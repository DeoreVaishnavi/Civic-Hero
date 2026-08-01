import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import CivicHeatmapMap from '../maps/CivicHeatmapMap.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { analyticsApi } from '../../services/analyticsApi.js';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { homeHeatmapPreview } from '../../data/civicInitiatives.js';
import { dashboardForRole } from '../../utils/roleRouting.js';

const normalise = (point, index) => ({
  id: point.id || `${point.latitude}-${point.longitude}-${index}`,
  wardId: point.wardId,
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

const publicPreviewPoints = homeHeatmapPreview.map(normalise);
const publicMapConfig = {
  enabled: false,
  defaultCenter: { latitude: 19.0760, longitude: 72.8777 },
  defaultZoom: 10.5,
};

export default function PublicHeatmapPreview() {
  const { isAuthenticated, user } = useAuth();
  const [points, setPoints] = useState(publicPreviewPoints);
  const [selectedId, setSelectedId] = useState(publicPreviewPoints[0]?.id);
  const [mode, setMode] = useState('preview');

  useEffect(() => {
    let active = true;
    if (!isAuthenticated) {
      setPoints(publicPreviewPoints);
      setSelectedId(publicPreviewPoints[0]?.id);
      setMode('preview');
      return () => { active = false; };
    }

    analyticsApi.heatmap({})
      .then((response) => {
        if (!active) return;
        const live = (Array.isArray(response) ? response : []).map(normalise).slice(0, 30);
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
  const heatmapTarget = !isAuthenticated
    ? ROUTE_PATHS.login
    : String(user?.role || '').toLowerCase() === 'citizen'
      ? ROUTE_PATHS.citizenHeatmap
      : dashboardForRole(user?.role);

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
            <p className="mt-4 max-w-2xl text-base leading-7 text-slate-300">Explore complaint hotspots on a real street map. Select a marker to compare active, resolved, verification-pending and disputed complaints.</p>
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
          <div className="relative min-h-[470px] overflow-hidden border-b border-white/10 bg-slate-200 lg:border-b-0 lg:border-r">
            <CivicHeatmapMap
              points={points}
              config={publicMapConfig}
              selectedPoint={selected}
              onSelect={(point) => setSelectedId(point.id)}
              className="public-heatmap-real-map"
            />
            <div className="pointer-events-none absolute left-5 top-5 z-[600] rounded-2xl border border-white/70 bg-white/90 px-4 py-3 text-xs text-slate-700 shadow-xl backdrop-blur-xl">
              <strong className="block text-slate-950">Interactive complaint map</strong>
              Pan, zoom or select a numbered hotspot.
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

            {!isAuthenticated && <p className="mt-5 rounded-2xl border border-amber-300/20 bg-amber-300/10 p-4 text-sm leading-6 text-amber-100">The public preview uses sample hotspot counts on a real map. Sign in to view live, filterable complaint data.</p>}
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
