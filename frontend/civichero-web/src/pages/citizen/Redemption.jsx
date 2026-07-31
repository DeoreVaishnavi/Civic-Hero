import { useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { rewardApi } from '../../services/rewardApi.js';

export default function Redemption() {
  const { rewardId } = useParams();
  const location = useLocation();
  const [points, setPoints] = useState(null);
  const [catalog, setCatalog] = useState([]);
  const [form, setForm] = useState({ contact: '', address: '', confirmed: false });
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null);

  useEffect(() => {
    Promise.all([rewardApi.points(), rewardApi.catalog()])
      .then(([pointData, catalogData]) => { setPoints(pointData); setCatalog(catalogData || []); })
      .catch((reason) => setError(reason.message));
  }, []);

  const reward = useMemo(() => location.state?.reward || catalog.find((item) => String(item.id) === String(rewardId)), [catalog, location.state, rewardId]);
  const available = Number(points?.balance || 0);
  const cost = Number(reward?.pointsCost || 0);
  const remaining = Math.max(0, available - cost);
  const canRedeem = reward && available >= cost && form.confirmed && form.contact.trim() && !submitting;

  const redeem = async (event) => {
    event.preventDefault();
    if (!canRedeem) return;
    setSubmitting(true);
    setError('');
    try { setResult(await rewardApi.redeem(reward.id)); }
    catch (reason) { setError(reason.message); }
    finally { setSubmitting(false); }
  };

  if (!reward && catalog.length > 0) return <section className="page-wrap"><div className="alert error">The selected reward could not be found.</div><Link to="/citizen/rewards" className="button outline" style={{ marginTop: 16 }}>← Back to rewards</Link></section>;

  return (
    <section className="page-wrap narrow">
      <div className="page-title-row"><div><p className="section-kicker">Reward redemption</p><h2>Confirm and claim your reward</h2><p>Review the point deduction and provide the delivery or claim information.</p></div><Link to="/citizen/rewards" className="button outline">← Back to rewards</Link></div>
      {error && <div className="alert error">{error}</div>}

      <form onSubmit={redeem} className="space-y-5">
        <section className="overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-sm"><div className="grid items-center gap-6 bg-gradient-to-r from-blue-50 to-cyan-50 p-6 sm:grid-cols-[180px_1fr]"><div className="grid h-36 place-items-center rounded-2xl bg-gradient-to-br from-rose-200 via-orange-100 to-amber-200 text-6xl shadow-inner">🎁</div><div><span className="status-pill">{reward?.type || 'Reward'}</span><h3 className="mt-4 text-3xl font-black text-slate-950">{reward?.name || 'Loading reward…'}</h3><p className="mt-2 text-sm leading-6 text-slate-600">{reward?.description}</p><strong className="mt-4 block text-lg text-blue-700">Required points: {cost}</strong></div></div></section>

        <section className="rounded-[1.75rem] border border-slate-200 bg-white p-6 shadow-sm"><h3 className="text-lg font-black text-slate-900">Your points</h3><div className="mt-5 space-y-5"><PointBar label="Available points" value={available} max={Math.max(available, cost, 1)} tone="bg-blue-600" /><PointBar label="After redemption" value={remaining} max={Math.max(available, 1)} tone="bg-emerald-500" /></div></section>

        <section className="rounded-[1.75rem] border border-slate-200 bg-white p-6 shadow-sm"><h3 className="text-lg font-black text-slate-900">Reward details</h3><dl className="mt-5 grid gap-4 sm:grid-cols-3"><Detail label="Description" value={reward?.description || '—'} /><Detail label="Validity" value="6 months from issue" /><Detail label="Claim method" value="Digital code or registered delivery" /></dl></section>

        <section className="rounded-[1.75rem] border border-slate-200 bg-white p-6 shadow-sm"><h3 className="text-lg font-black text-slate-900">Delivery or claim information</h3><div className="mt-5 grid gap-4"><label><span className="mb-2 block text-xs font-black uppercase tracking-wider text-slate-500">Email or mobile number</span><input value={form.contact} onChange={(event) => setForm((current) => ({ ...current, contact: event.target.value }))} className="input" placeholder="Enter your email or mobile number" required /></label><label><span className="mb-2 block text-xs font-black uppercase tracking-wider text-slate-500">Delivery address, when required</span><textarea value={form.address} onChange={(event) => setForm((current) => ({ ...current, address: event.target.value }))} className="input min-h-24 resize-y" placeholder="Street, locality and city" /></label></div></section>

        <label className="flex cursor-pointer items-start gap-3 rounded-2xl border border-slate-200 bg-white p-5 shadow-sm"><input type="checkbox" checked={form.confirmed} onChange={(event) => setForm((current) => ({ ...current, confirmed: event.target.checked }))} className="mt-0.5 h-5 w-5 accent-blue-600" /><span><strong className="block text-sm text-slate-900">I confirm that I want to redeem this reward.</strong><small className="mt-1 block text-xs leading-5 text-slate-500">{cost} points will be deducted from your CivicHero balance after successful confirmation.</small></span></label>

        {available < cost && <div className="alert warning">You need {cost - available} more points to redeem this reward.</div>}
        <button disabled={!canRedeem} className="button primary full large">{submitting ? 'Redeeming reward…' : `Redeem now · ${cost} points`}</button>
      </form>

      {result && <SuccessModal reward={reward} result={result} onClose={() => setResult(null)} />}
    </section>
  );
}

function PointBar({ label, value, max, tone }) { return <div><div className="flex items-center justify-between text-sm font-bold text-slate-700"><span>{label}</span><strong>{value}</strong></div><div className="mt-2 h-3 overflow-hidden rounded-full bg-slate-100"><div className={`h-full rounded-full transition-all duration-700 ${tone}`} style={{ width: `${Math.min(100, (value / Math.max(1, max)) * 100)}%` }} /></div></div>; }
function Detail({ label, value }) { return <div className="rounded-2xl bg-slate-50 p-4"><dt className="text-[10px] font-black uppercase tracking-wider text-slate-400">{label}</dt><dd className="mt-2 text-sm font-bold leading-6 text-slate-700">{value}</dd></div>; }
function SuccessModal({ reward, result, onClose }) { return <div className="fixed inset-0 z-[90] grid place-items-center bg-slate-950/50 p-4 backdrop-blur-sm"><div className="w-full max-w-lg rounded-[2rem] bg-white p-8 text-center shadow-2xl"><div className="mx-auto grid h-28 w-28 place-items-center rounded-[2rem] bg-gradient-to-br from-amber-100 to-rose-100 text-6xl">🎁</div><h2 className="mt-6 text-3xl font-black text-slate-950">Redemption successful!</h2><p className="mt-3 text-slate-600">Your <strong>{reward?.name}</strong> has been claimed successfully.</p><div className="mt-6 rounded-2xl bg-slate-50 p-4 text-sm text-slate-600"><span className="block">Redemption code</span><strong className="mt-1 block text-xl text-blue-700">{result?.redemptionCode || 'Generated'}</strong></div><div className="mt-6 grid grid-cols-2 gap-3"><Link to="/citizen/rewards" className="button outline full">Back to rewards</Link><button type="button" onClick={onClose} className="button primary full">Continue</button></div></div></div>; }
