import { useEffect, useState } from 'react';
import { rewardApi } from '../../services/rewardApi.js';

export default function RewardsOverview() {
  const [data, setData] = useState({ points: null, badges: [], catalog: [], history: [], redemptions: [] });
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const load = async () => {
    try {
      const [points, badges, catalog, history, redemptions] = await Promise.all([rewardApi.points(), rewardApi.badges(), rewardApi.catalog(), rewardApi.history(), rewardApi.redemptions()]);
      setData({ points, badges, catalog, history, redemptions });
    } catch (reason) { setError(reason.message); }
  };
  useEffect(() => { load(); }, []);
  const redeem = async (reward) => {
    if (!confirm(`Redeem ${reward.name} for ${reward.pointsCost} points?`)) return;
    try { const result = await rewardApi.redeem(reward.id); setMessage(`Redeemed successfully. Code: ${result.redemptionCode}`); await load(); } catch (reason) { setError(reason.message); }
  };

  const points = data.points;
  return <section className="p-6 lg:p-10">
    <div className="rounded-3xl border border-amber-400/20 bg-gradient-to-br from-amber-400/10 to-sky-400/5 p-8"><p className="text-sm font-bold uppercase tracking-wider text-amber-300">Rewards & gamification</p><div className="mt-4 flex flex-wrap items-end justify-between gap-5"><div><h2 className="text-5xl font-black text-white">{points?.balance ?? '—'} <span className="text-xl text-amber-300">points</span></h2><p className="mt-2 text-slate-300">{points?.tier ?? 'Loading tier'} · City rank #{points?.rank ?? '—'}</p></div><button onClick={() => rewardApi.certificate().catch((reason) => setError(reason.message))} className="rounded-xl bg-amber-400 px-5 py-3 font-black text-slate-950">Download certificate</button></div></div>
    {error && <Alert tone="error">{error}</Alert>}{message && <Alert>{message}</Alert>}
    <h3 className="mt-10 text-2xl font-black text-white">Badges</h3><div className="mt-5 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">{data.badges.map((badge) => <article key={badge.code} className={`rounded-2xl border p-5 ${badge.isUnlocked ? 'border-amber-400/30 bg-amber-400/10' : 'border-white/10 bg-white/5 opacity-70'}`}><div className="text-4xl">{badge.icon}</div><h4 className="mt-3 font-black text-white">{badge.name}</h4><p className="mt-2 text-sm text-slate-400">{badge.description}</p><div className="mt-4 h-2 overflow-hidden rounded-full bg-slate-800"><div className="h-full bg-amber-400" style={{ width: `${Math.min(100, badge.progress * 100 / badge.target)}%` }} /></div><p className="mt-2 text-xs text-slate-500">{badge.progress}/{badge.target} · {badge.isUnlocked ? 'Unlocked' : 'Locked'}</p></article>)}</div>
    <h3 className="mt-10 text-2xl font-black text-white">Reward catalog</h3><div className="mt-5 grid gap-5 xl:grid-cols-3">{data.catalog.map((reward) => <article key={reward.id} className="rounded-2xl border border-white/10 bg-white/5 p-6"><span className="rounded-full bg-sky-400/10 px-3 py-1 text-xs font-bold text-sky-300">{reward.type}</span><h4 className="mt-4 text-xl font-black text-white">{reward.name}</h4><p className="mt-2 min-h-14 text-sm text-slate-400">{reward.description}</p><div className="mt-5 flex items-center justify-between"><strong className="text-amber-300">{reward.pointsCost} points</strong><button disabled={!reward.canRedeem} onClick={() => redeem(reward)} className="rounded-lg bg-sky-500 px-4 py-2 text-sm font-black text-white disabled:cursor-not-allowed disabled:opacity-40">Redeem</button></div></article>)}</div>
    <div className="mt-10 grid gap-6 xl:grid-cols-2"><History title="Points history" items={data.history.map((item) => ({ title:item.reason, meta:new Date(item.createdAt).toLocaleString(), value:`${item.pointsDelta > 0 ? '+' : ''}${item.pointsDelta}` }))} /><History title="Redemptions" items={data.redemptions.map((item) => ({ title:item.rewardName, meta:`${item.status} · ${item.redemptionCode}`, value:`-${item.pointsSpent}` }))} /></div>
  </section>;
}
function History({ title, items }) { return <div className="rounded-2xl border border-white/10 bg-white/5 p-6"><h3 className="text-xl font-black text-white">{title}</h3><div className="mt-4 divide-y divide-white/10">{items.slice(0,10).map((item,index) => <div key={`${item.title}-${index}`} className="flex items-center justify-between gap-4 py-3"><div><p className="font-semibold text-slate-200">{item.title}</p><p className="text-xs text-slate-500">{item.meta}</p></div><strong className={item.value.startsWith('+') ? 'text-emerald-300' : 'text-rose-300'}>{item.value}</strong></div>)}{items.length===0 && <p className="py-5 text-sm text-slate-500">No activity yet.</p>}</div></div>; }
function Alert({ children, tone='success' }) { return <div className={`mt-5 rounded-xl border p-4 ${tone==='error'?'border-rose-400/30 bg-rose-400/10 text-rose-100':'border-emerald-400/30 bg-emerald-400/10 text-emerald-100'}`}>{children}</div>; }
