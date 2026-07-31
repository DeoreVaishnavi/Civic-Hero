import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { rewardApi } from '../../services/rewardApi.js';

export default function RewardsOverview() {
  const [data, setData] = useState({ points: null, badges: [], catalog: [], history: [], redemptions: [] });
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const load = async () => {
    try {
      const [points, badges, catalog, history, redemptions] = await Promise.all([rewardApi.points(), rewardApi.badges(), rewardApi.catalog(), rewardApi.history(), rewardApi.redemptions()]);
      setData({ points, badges: badges || [], catalog: catalog || [], history: history || [], redemptions: redemptions || [] });
    } catch (reason) { setError(reason.message); }
  };
  useEffect(() => { load(); }, []);

  const redeem = async (reward) => {
    if (!confirm(`Redeem ${reward.name} for ${reward.pointsCost} points?`)) return;
    try { const result = await rewardApi.redeem(reward.id); setMessage(`Reward redeemed. Code: ${result.redemptionCode}`); await load(); }
    catch (reason) { setError(reason.message); }
  };

  const points = data.points;
  const progress = useMemo(() => Math.min(100, ((points?.balance || 0) / Math.max(1, points?.nextTierPoints || 1500)) * 100), [points]);

  return (
    <section className="page-wrap">
      <div className="page-title-row"><div><p className="section-kicker">Civic participation rewards</p><h2>Points, badges and redeemable rewards</h2><p>Valid complaint closure and helpful community actions build your CivicHero reputation.</p></div><Link to="/citizen" className="button outline">← Back to dashboard</Link></div>
      {error && <div className="alert error">{error}</div>}{message && <div className="alert success">{message}</div>}

      <section className="surface reward-summary">
        <div className="podium-avatar">{String(points?.tier || 'CH').slice(0,2).toUpperCase()}</div>
        <div><h2>{points?.balance ?? '—'} total points</h2><p>{points?.tier || 'Citizen'} · City rank #{points?.rank ?? '—'}</p><div className="progress-track" style={{ marginTop: 12 }}><i style={{ width: `${progress}%` }} /></div></div>
        <button type="button" onClick={() => rewardApi.certificate().catch((reason) => setError(reason.message))} className="button primary">Download certificate</button>
      </section>

      <section className="surface section-gap"><div className="surface-header"><h3>Badges and achievements</h3><span className="status-pill amber">Recognition</span></div><div className="surface-body"><div className="badge-grid">{(data.badges.length ? data.badges : fallbackBadges).slice(0,6).map((badge) => <article key={badge.code || badge.name} className="badge-card" style={{ opacity: badge.isUnlocked === false ? .62 : 1 }}><div className="badge-emblem">{badge.icon || '★'}</div><h3>{badge.name}</h3><p>{badge.description}</p><div className="progress-track" style={{ marginTop: 12 }}><i style={{ width: `${Math.min(100, ((badge.progress || 0) / Math.max(1, badge.target || 1)) * 100)}%` }} /></div><small style={{ display: 'block', marginTop: 7, color: 'var(--civic-muted)', fontSize: 7 }}>{badge.isUnlocked ? 'Unlocked' : `${badge.progress || 0}/${badge.target || 1}`}</small></article>)}</div></div></section>

      <section className="surface section-gap"><div className="surface-header"><h3>Reward catalog</h3><span className="muted" style={{ fontSize: 8 }}>Redeem only when your points balance is sufficient</span></div><div className="surface-body"><div className="reward-card-grid">{data.catalog.map((reward, index) => <article key={reward.id} className="reward-card"><div className="reward-art">{['🎁','🎟','📜','🏅'][index % 4]}</div><div className="reward-card-body"><span className="status-pill">{reward.type}</span><h3 style={{ marginTop: 12 }}>{reward.name}</h3><p>{reward.description}</p><div className="reward-card-footer"><strong>{reward.pointsCost} points</strong><button disabled={!reward.canRedeem} onClick={() => redeem(reward)} className="button primary small">Redeem</button></div></div></article>)}{data.catalog.length === 0 && <div className="upload-zone" style={{ gridColumn: '1 / -1' }}><strong>No rewards are currently available</strong><p>An administrator can add active reward catalog items.</p></div>}</div></div></section>

      <div className="dashboard-grid equal section-gap">
        <History title="Points activity" items={data.history.map((item) => ({ title: item.reason, meta: new Date(item.createdAt).toLocaleString(), value: `${item.pointsDelta > 0 ? '+' : ''}${item.pointsDelta}` }))} />
        <History title="Reward history" items={data.redemptions.map((item) => ({ title: item.rewardName, meta: `${item.status} · ${item.redemptionCode}`, value: `-${item.pointsSpent}` }))} />
      </div>
    </section>
  );
}

const fallbackBadges = [
  { code: 'reporter', icon: '🏅', name: 'Top Reporter', description: 'Submit valid civic complaints.', progress: 0, target: 5, isUnlocked: false },
  { code: 'active', icon: '★', name: 'Active Citizen', description: 'Participate in complaint verification.', progress: 0, target: 10, isUnlocked: false },
  { code: 'verifier', icon: '✓', name: 'Verifier', description: 'Confirm completed public work.', progress: 0, target: 5, isUnlocked: false },
];

function History({ title, items }) { return <section className="surface"><div className="surface-header"><h3>{title}</h3></div><div className="surface-body notification-list">{items.slice(0,10).map((item,index) => <div key={`${item.title}-${index}`} className="notification-item"><span>{item.value.startsWith('+') ? '+' : '−'}</span><div><strong>{item.title} <em style={{ float: 'right', color: item.value.startsWith('+') ? 'var(--civic-green)' : 'var(--civic-red)', fontStyle: 'normal' }}>{item.value}</em></strong><small>{item.meta}</small></div></div>)}{items.length === 0 && <p className="muted" style={{ fontSize: 9 }}>No activity yet.</p>}</div></section>; }
