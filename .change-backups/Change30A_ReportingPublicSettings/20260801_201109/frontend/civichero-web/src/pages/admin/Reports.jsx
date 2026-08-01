import { useState } from 'react';
import { analyticsApi } from '../../services/analyticsApi.js';
import { ANALYTICS_REPORTS } from '../../types/analytics.types.js';
import { downloadBlob, toIsoBoundary } from '../../utils/exportHelper.js';

export default function Reports() {
  const [busy,setBusy] = useState(''); const [message,setMessage] = useState(''); const [dates,setDates] = useState({from:'',to:''});
  const exportOne = async report => { setBusy(report); setMessage(''); try { const result = await analyticsApi.exportReport(report,{from:toIsoBoundary(dates.from),to:toIsoBoundary(dates.to,true)}); downloadBlob(result.blob,result.fileName); setMessage(`${report} report downloaded.`); } catch(e) { setMessage(e.message || 'Export failed.'); } finally { setBusy(''); } };
  return <section className="p-6 lg:p-10"><div><p className="text-sm font-bold uppercase tracking-[.16em] text-violet-300">Administrative reporting</p><h2 className="mt-2 text-3xl font-black text-white">Export centre</h2><p className="mt-2 text-slate-400">Generate CSV reports directly from live AWS RDS data.</p></div>
    <div className="mt-6 flex flex-wrap gap-4 rounded-2xl border border-white/10 bg-white/5 p-4"><label className="text-sm text-slate-400">From<input type="date" value={dates.from} onChange={e=>setDates(x=>({...x,from:e.target.value}))} className="ml-2 rounded-lg border border-white/10 bg-slate-950 px-3 py-2 text-white" /></label><label className="text-sm text-slate-400">To<input type="date" value={dates.to} onChange={e=>setDates(x=>({...x,to:e.target.value}))} className="ml-2 rounded-lg border border-white/10 bg-slate-950 px-3 py-2 text-white" /></label></div>
    {message && <p className="mt-4 rounded-xl border border-sky-400/20 bg-sky-400/10 p-3 text-sm text-sky-100">{message}</p>}
    <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">{ANALYTICS_REPORTS.map(report=><article key={report.key} className="rounded-2xl border border-white/10 bg-white/[.04] p-5"><div className="rounded-xl bg-violet-400/10 p-3 text-2xl">↧</div><h3 className="mt-4 font-black text-white">{report.label}</h3><p className="mt-2 min-h-10 text-sm text-slate-400">{report.description}</p><button disabled={Boolean(busy)} onClick={()=>exportOne(report.key)} className="mt-5 w-full rounded-xl bg-violet-500 px-4 py-2.5 font-bold text-white hover:bg-violet-400 disabled:opacity-50">{busy===report.key?'Preparing…':'Download CSV'}</button></article>)}</div>
  </section>;
}
