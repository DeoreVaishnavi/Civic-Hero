import { useCallback, useEffect, useState } from 'react';
import BarChart from '../../components/charts/BarChart.jsx';
import HeatMap from '../../components/charts/HeatMap.jsx';
import LineChart from '../../components/charts/LineChart.jsx';
import MetricCard from '../../components/common/MetricCard.jsx';
import { analyticsApi } from '../../services/analyticsApi.js';
import { toIsoBoundary } from '../../utils/exportHelper.js';

const defaultDates = () => { const to = new Date(); const from = new Date(); from.setDate(to.getDate()-89); return { from: from.toISOString().slice(0,10), to: to.toISOString().slice(0,10) }; };

export default function AnalyticsDashboard() {
  const [dates,setDates] = useState(defaultDates); const [data,setData] = useState(null); const [loading,setLoading] = useState(true); const [error,setError] = useState('');
  const filters = { from: toIsoBoundary(dates.from), to: toIsoBoundary(dates.to,true) };
  const load = useCallback(async () => { setLoading(true); setError(''); try { const [overview,complaints,departments,sla,satisfaction,heatmap] = await Promise.all([analyticsApi.overview(filters),analyticsApi.complaints(filters),analyticsApi.departments(filters),analyticsApi.sla(filters),analyticsApi.satisfaction(filters),analyticsApi.heatmap(filters)]); setData({overview,complaints,departments,sla,satisfaction,heatmap}); } catch (e) { setError(e.message || 'Unable to load analytics.'); } finally { setLoading(false); } }, [dates.from, dates.to]);
  useEffect(() => { load(); }, [load]);
  return <section className="p-6 lg:p-10">
    <div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-sm font-bold uppercase tracking-[.16em] text-sky-300">Operational intelligence</p><h2 className="mt-2 text-3xl font-black text-white">City analytics command centre</h2><p className="mt-2 text-slate-400">Complaint outcomes, SLA, satisfaction, department ranking and geographic density.</p></div><div className="flex flex-wrap items-end gap-3 rounded-2xl border border-white/10 bg-white/5 p-3"><DateField label="From" value={dates.from} onChange={from=>setDates(x=>({...x,from}))}/><DateField label="To" value={dates.to} onChange={to=>setDates(x=>({...x,to}))}/><button onClick={load} className="rounded-xl bg-sky-500 px-4 py-2.5 text-sm font-bold text-white hover:bg-sky-400">Refresh</button></div></div>
    {error && <div className="mt-6 rounded-2xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}
    {loading ? <div className="mt-8 text-slate-400">Calculating live analytics from AWS RDS…</div> : data && <>
      <div className="mt-8 grid gap-4 sm:grid-cols-2 xl:grid-cols-4"><MetricCard label="Total complaints" value={data.overview.totalComplaints} helper={`${data.overview.openComplaints} currently open`} /><MetricCard label="Resolution rate" value={`${data.overview.resolutionRate}%`} helper={`${data.overview.closedComplaints} valid closures`} /><MetricCard label="SLA compliance" value={`${data.overview.slaCompliance}%`} helper={`${data.sla.currentlyOverdue} currently overdue`} /><MetricCard label="Citizen rating" value={`${data.overview.satisfactionScore || 0}/5`} helper={`${data.satisfaction.ratingCount} verified ratings`} /></div>
      <div className="mt-6 grid gap-6 xl:grid-cols-[1.55fr_1fr]"><Panel title="Complaint trend" subtitle="Created, closed and escalated by month"><LineChart data={data.complaints.monthlyTrend} series={[{key:'created',label:'Created'},{key:'closed',label:'Closed'},{key:'escalated',label:'Escalated'}]} /></Panel><Panel title="Status distribution" subtitle={`${data.complaints.total} complaints in the selected period`}><BarChart data={data.complaints.byStatus} /></Panel></div>
      <div className="mt-6 grid gap-6 xl:grid-cols-2"><Panel title="Department ranking" subtitle="Resolution performance"><div className="overflow-x-auto"><table className="min-w-full text-sm"><thead className="text-left text-xs uppercase tracking-wider text-slate-500"><tr><th className="pb-3">Rank</th><th>Department</th><th>Resolved</th><th>SLA</th><th>Rating</th></tr></thead><tbody className="divide-y divide-white/10">{data.departments.slice(0,8).map(row=><tr key={row.departmentId}><td className="py-3 font-black text-sky-300">#{row.rank}</td><td className="font-semibold text-white">{row.departmentName}</td><td>{row.resolutionRate}%</td><td>{row.slaCompliance}%</td><td>{row.satisfactionScore || 0}/5</td></tr>)}</tbody></table></div></Panel><Panel title="Ward and hotspot map" subtitle="Cluster size represents complaint density"><HeatMap points={data.heatmap} /></Panel></div>
    </>}
  </section>;
}
function Panel({title,subtitle,children}) { return <article className="rounded-3xl border border-white/10 bg-white/[.04] p-6"><h3 className="text-lg font-black text-white">{title}</h3><p className="mb-6 mt-1 text-sm text-slate-500">{subtitle}</p>{children}</article>; }
function DateField({label,value,onChange}) { return <label className="text-xs font-bold uppercase tracking-wider text-slate-500">{label}<input type="date" value={value} onChange={e=>onChange(e.target.value)} className="mt-1 block rounded-lg border border-white/10 bg-slate-950 px-3 py-2 text-sm normal-case text-white" /></label>; }
