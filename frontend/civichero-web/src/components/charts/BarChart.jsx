export default function BarChart({ data = [], labelKey = 'name', valueKey = 'count', suffix = '', emptyText = 'No data for this period.' }) {
  const max = Math.max(1, ...data.map((item) => Number(item[valueKey]) || 0));
  if (!data.length) return <p className="rounded-xl border border-dashed border-white/10 p-6 text-center text-sm text-slate-500">{emptyText}</p>;
  return <div className="space-y-4">{data.map((item) => {
    const value = Number(item[valueKey]) || 0;
    return <div key={`${item[labelKey]}-${value}`}>
      <div className="mb-1 flex items-center justify-between gap-3 text-sm"><span className="truncate text-slate-300">{item[labelKey]}</span><strong className="text-white">{value}{suffix}</strong></div>
      <div className="h-2.5 overflow-hidden rounded-full bg-white/10"><div className="h-full rounded-full bg-sky-400 transition-all" style={{ width: `${Math.max(2, value * 100 / max)}%` }} /></div>
    </div>;
  })}</div>;
}
