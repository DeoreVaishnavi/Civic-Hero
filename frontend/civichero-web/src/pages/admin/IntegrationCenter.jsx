
import { useCallback, useEffect, useState } from 'react';
import { integrationApi } from '../../services/integrationApi.js';

export default function IntegrationCenter() {
  const [data, setData] = useState(null); const [error, setError] = useState(''); const [message, setMessage] = useState(''); const [busy, setBusy] = useState('');
  const load = useCallback(async () => { setError(''); try { setData(await integrationApi.readiness()); } catch (reason) { setError(reason.message || 'Unable to load integration readiness.'); } }, []);
  useEffect(() => { load(); }, [load]);
  const run = async (name) => { setBusy(name); setError(''); setMessage(''); try { const result = name === 'cache' ? await integrationApi.testCache() : await integrationApi.testMessaging(); setMessage(result.message || `${name === 'cache' ? 'Cache' : 'Messaging'} test completed.`); await load(); } catch (reason) { setError(reason.message || 'Integration test failed.'); } finally { setBusy(''); } };
  return <section className="space-y-6 p-6">
    <div className="flex flex-wrap items-start justify-between gap-4"><div><p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-400">Phase 17</p><h2 className="mt-2 text-3xl font-black text-white">Integration centre</h2><p className="mt-2 text-slate-400">Validate cache, messaging, background automation and final runtime dependencies.</p></div><button onClick={load} className="rounded-xl border border-white/15 px-4 py-2 font-bold text-white">Refresh</button></div>
    {error && <div className="rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-200">{error}</div>}{message && <div className="rounded-xl border border-emerald-400/30 bg-emerald-400/10 p-4 text-emerald-200">{message}</div>}
    {data && <><div className="grid gap-4 sm:grid-cols-3"><Metric label="Overall status" value={data.overallStatus} /><Metric label="Dependencies" value={data.dependencies?.length ?? 0} /><Metric label="2FA" value={data.twoFactorAvailable ? 'Available' : 'Unavailable'} /></div>
      <div className="grid gap-4 lg:grid-cols-2">{data.dependencies?.map((item) => <article key={item.name} className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex items-center justify-between gap-3"><h3 className="font-bold text-white">{item.name}</h3><span className={`rounded-full px-3 py-1 text-xs font-bold ${item.status === 'Ready' ? 'bg-emerald-400/10 text-emerald-200' : item.status === 'Unavailable' ? 'bg-rose-400/10 text-rose-200' : 'bg-amber-400/10 text-amber-200'}`}>{item.status}</span></div><p className="mt-3 text-sm text-slate-400">{item.detail}</p></article>)}</div>
      <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><h3 className="text-xl font-bold text-white">Automation workers</h3><div className="mt-4 flex flex-wrap gap-2">{data.automationWorkers?.map((worker) => <span key={worker} className="rounded-full bg-sky-400/10 px-3 py-2 text-sm font-semibold text-sky-200">{worker}</span>)}</div></div>
      <div className="flex flex-wrap gap-3"><button disabled={busy} onClick={() => run('cache')} className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white disabled:opacity-60">{busy === 'cache' ? 'Testing cache…' : 'Test cache round-trip'}</button><button disabled={busy} onClick={() => run('messaging')} className="rounded-xl bg-violet-500 px-5 py-3 font-bold text-white disabled:opacity-60">{busy === 'messaging' ? 'Publishing event…' : 'Publish RabbitMQ test event'}</button></div></>}
  </section>;
}
function Metric({ label, value }) { return <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><p className="text-xs font-semibold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-2 text-2xl font-black text-white">{value}</p></div>; }
