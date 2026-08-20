const riskClass = { Critical: 'bg-rose-500/90', High: 'bg-orange-400/90', Medium: 'bg-amber-300/90', Low: 'bg-emerald-400/80' };
export default function HeatMap({ points = [] }) {
  if (!points.length) return <p className="rounded-xl border border-dashed border-white/10 p-6 text-center text-sm text-slate-500">No geographic points for this period.</p>;
  const latitudes = points.map(p => Number(p.latitude)); const longitudes = points.map(p => Number(p.longitude));
  const minLat = Math.min(...latitudes), maxLat = Math.max(...latitudes), minLon = Math.min(...longitudes), maxLon = Math.max(...longitudes);
  const position = (value, min, max) => max === min ? 50 : 8 + (value-min) * 84 / (max-min);
  return <div>
    <div className="relative h-80 overflow-hidden rounded-2xl border border-white/10 bg-[radial-gradient(circle_at_center,rgba(14,165,233,.12),transparent_60%),linear-gradient(rgba(148,163,184,.08)_1px,transparent_1px),linear-gradient(90deg,rgba(148,163,184,.08)_1px,transparent_1px)] bg-[size:auto,32px_32px,32px_32px]">
      {points.slice(0,80).map((point,index) => <button key={`${point.latitude}-${point.longitude}-${index}`} type="button" title={`${point.wardName || 'Area'} · ${point.complaintCount} complaints · ${point.riskLevel}`} className={`absolute -translate-x-1/2 translate-y-1/2 rounded-full border-2 border-white/70 shadow-lg ${riskClass[point.riskLevel] || riskClass.Low}`} style={{ left: `${position(Number(point.longitude),minLon,maxLon)}%`, bottom: `${position(Number(point.latitude),minLat,maxLat)}%`, width: `${Math.min(44,14+Number(point.complaintCount)*3)}px`, height: `${Math.min(44,14+Number(point.complaintCount)*3)}px` }} />)}
      <span className="absolute bottom-3 left-3 rounded-lg bg-slate-950/80 px-3 py-2 text-xs text-slate-400">Heatmap-ready GPS clusters</span>
    </div>
    <div className="mt-3 flex flex-wrap gap-3 text-xs text-slate-400">{Object.keys(riskClass).map(level => <span key={level} className="inline-flex items-center gap-2"><i className={`h-2.5 w-2.5 rounded-full ${riskClass[level]}`} />{level}</span>)}</div>
  </div>;
}
