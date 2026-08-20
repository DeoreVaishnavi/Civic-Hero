export default function LineChart({ data = [], xKey = 'period', series = [{ key: 'created', label: 'Created' }] }) {
  if (!data.length) return <p className="rounded-xl border border-dashed border-white/10 p-6 text-center text-sm text-slate-500">No trend data for this period.</p>;
  const width = 760, height = 250, padX = 42, padY = 28;
  const max = Math.max(1, ...data.flatMap((item) => series.map((entry) => Number(item[entry.key]) || 0)));
  const point = (value, index) => `${padX + index * ((width - padX * 2) / Math.max(1, data.length - 1))},${height - padY - (Number(value) || 0) * (height - padY * 2) / max}`;
  return <div>
    <div className="mb-4 flex flex-wrap gap-4 text-xs text-slate-400">{series.map((entry, index) => <span key={entry.key} className="inline-flex items-center gap-2"><i className={`h-2.5 w-2.5 rounded-full ${index === 0 ? 'bg-sky-400' : index === 1 ? 'bg-emerald-400' : 'bg-amber-400'}`} />{entry.label}</span>)}</div>
    <div className="overflow-x-auto"><svg viewBox={`0 0 ${width} ${height}`} className="min-w-[620px]" role="img" aria-label="Analytics trend chart">
      {[0, .25, .5, .75, 1].map(tick => <line key={tick} x1={padX} x2={width-padX} y1={padY + tick*(height-padY*2)} y2={padY + tick*(height-padY*2)} stroke="rgba(148,163,184,.16)" />)}
      {series.map((entry, index) => <polyline key={entry.key} fill="none" stroke={index === 0 ? '#38bdf8' : index === 1 ? '#34d399' : '#fbbf24'} strokeWidth="4" strokeLinecap="round" strokeLinejoin="round" points={data.map((item, i) => point(item[entry.key], i)).join(' ')} />)}
      {data.map((item, index) => <text key={item[xKey]} x={padX + index * ((width-padX*2)/Math.max(1,data.length-1))} y={height-6} textAnchor="middle" fill="#94a3b8" fontSize="11">{String(item[xKey]).replace(' 20',' ’')}</text>)}
    </svg></div>
  </div>;
}
