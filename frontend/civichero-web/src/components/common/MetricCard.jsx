export default function MetricCard({ label, value, hint, tone = 'sky' }) {
  const tones = {
    sky: 'border-sky-400/20 bg-sky-400/[0.06] text-sky-300',
    emerald: 'border-emerald-400/20 bg-emerald-400/[0.06] text-emerald-300',
    amber: 'border-amber-400/20 bg-amber-400/[0.06] text-amber-300',
    rose: 'border-rose-400/20 bg-rose-400/[0.06] text-rose-300',
  };
  return <article className={`rounded-2xl border p-5 ${tones[tone] || tones.sky}`}><p className="text-xs font-black uppercase tracking-widest opacity-80">{label}</p><p className="mt-3 text-4xl font-black text-white">{value}</p><p className="mt-2 text-xs opacity-80">{hint}</p></article>;
}
