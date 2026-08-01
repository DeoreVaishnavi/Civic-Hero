import { useCallback, useEffect, useMemo, useState } from 'react';
import { rewardApi } from '../../services/rewardApi.js';

const emptyReward = {
  name: '',
  description: '',
  pointsCost: 100,
  type: 'Voucher',
  stockQuantity: 0,
  isActive: true,
};

const emptyAdjustment = { userId: '', pointsDelta: '', reason: '' };
const emptyStock = { rewardId: '', quantity: '', reason: '' };
const emptyDecision = { redemptionId: '', status: 'Approved', reason: '' };

export default function RewardManagement() {
  const [catalog, setCatalog] = useState([]);
  const [redemptions, setRedemptions] = useState([]);
  const [rules, setRules] = useState({ badgeRules: [], tierRules: [] });
  const [rewardForm, setRewardForm] = useState(emptyReward);
  const [editingRewardId, setEditingRewardId] = useState(null);
  const [stockForm, setStockForm] = useState(emptyStock);
  const [adjustment, setAdjustment] = useState(emptyAdjustment);
  const [decision, setDecision] = useState(emptyDecision);
  const [redemptionStatus, setRedemptionStatus] = useState('');
  const [activeSection, setActiveSection] = useState('catalog');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [catalogData, rulesData, redemptionData] = await Promise.all([
        rewardApi.adminCatalog(),
        rewardApi.rewardRules(),
        rewardApi.adminRedemptions({ status: redemptionStatus || undefined, take: 200 }),
      ]);
      setCatalog(Array.isArray(catalogData) ? catalogData : []);
      setRules({
        badgeRules: Array.isArray(rulesData?.badgeRules) ? rulesData.badgeRules : [],
        tierRules: Array.isArray(rulesData?.tierRules) ? rulesData.tierRules : [],
      });
      setRedemptions(Array.isArray(redemptionData) ? redemptionData : []);
    } catch (reason) {
      setError(reason.message || 'Reward administration data could not be loaded.');
    } finally {
      setLoading(false);
    }
  }, [redemptionStatus]);

  useEffect(() => { load(); }, [load]);

  const stats = useMemo(() => ({
    total: catalog.length,
    active: catalog.filter((item) => item.isActive).length,
    lowStock: catalog.filter((item) => item.stockQuantity <= 5).length,
    pending: redemptions.filter((item) => item.status === 'Pending').length,
  }), [catalog, redemptions]);

  const run = async (work, successMessage) => {
    setSaving(true);
    setError('');
    setNotice('');
    try {
      await work();
      setNotice(successMessage);
      await load();
      return true;
    } catch (reason) {
      setError(reason.response?.data?.message || reason.message || 'The operation could not be completed.');
      return false;
    } finally {
      setSaving(false);
    }
  };

  const saveReward = async (event) => {
    event.preventDefault();
    const payload = {
      ...rewardForm,
      pointsCost: Number(rewardForm.pointsCost),
      stockQuantity: Number(rewardForm.stockQuantity),
    };
    const success = await run(
      () => editingRewardId ? rewardApi.updateReward(editingRewardId, payload) : rewardApi.createReward(payload),
      editingRewardId ? 'Reward updated.' : 'Reward created.',
    );
    if (success) {
      setEditingRewardId(null);
      setRewardForm(emptyReward);
    }
  };

  const editReward = (reward) => {
    setEditingRewardId(reward.id);
    setRewardForm({
      name: reward.name,
      description: reward.description,
      pointsCost: reward.pointsCost,
      type: reward.type,
      stockQuantity: reward.stockQuantity,
      isActive: reward.isActive,
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const refillStock = async (event) => {
    event.preventDefault();
    const success = await run(
      () => rewardApi.refillStock(stockForm.rewardId, { quantity: Number(stockForm.quantity), reason: stockForm.reason }),
      'Reward stock refilled.',
    );
    if (success) setStockForm(emptyStock);
  };

  const adjustPoints = async (event) => {
    event.preventDefault();
    const success = await run(
      () => rewardApi.adjustPoints({ userId: Number(adjustment.userId), pointsDelta: Number(adjustment.pointsDelta), reason: adjustment.reason }),
      'Citizen points adjusted and audited.',
    );
    if (success) setAdjustment(emptyAdjustment);
  };

  const updateRedemption = async (event) => {
    event.preventDefault();
    const success = await run(
      () => rewardApi.updateRedemptionStatus(decision.redemptionId, { status: decision.status, reason: decision.reason }),
      'Redemption decision recorded.',
    );
    if (success) setDecision(emptyDecision);
  };

  const saveBadges = () => run(() => rewardApi.saveBadgeRules(rules.badgeRules), 'Badge rules updated.');
  const saveTiers = () => run(() => rewardApi.saveTierRules(rules.tierRules), 'Tier rules updated.');

  const updateBadge = (index, field, value) => setRules((current) => ({
    ...current,
    badgeRules: current.badgeRules.map((rule, ruleIndex) => ruleIndex === index ? { ...rule, [field]: value } : rule),
  }));
  const updateTier = (index, field, value) => setRules((current) => ({
    ...current,
    tierRules: current.tierRules.map((rule, ruleIndex) => ruleIndex === index ? { ...rule, [field]: value } : rule),
  }));

  return (
    <section className="page-wrap">
      <div className="page-title-row">
        <div>
          <p className="section-kicker">Rewards governance</p>
          <h2>Reward, badge and redemption management</h2>
          <p>Manage the reward catalogue, reputation rules, audited point adjustments and fulfilment decisions.</p>
        </div>
        <button className="button outline" type="button" onClick={load} disabled={loading || saving}>↻ Refresh</button>
      </div>

      {error && <div className="alert error" role="alert">{error}</div>}
      {notice && <div className="alert success" role="status">{notice}</div>}

      <div className="stats-grid">
        <Stat label="Catalogue items" value={stats.total} />
        <Stat label="Active rewards" value={stats.active} />
        <Stat label="Low stock" value={stats.lowStock} />
        <Stat label="Pending redemptions" value={stats.pending} />
      </div>

      <div className="filter-bar section-gap">
        {[
          ['catalog', 'Reward catalogue'],
          ['rules', 'Badge & tier rules'],
          ['points', 'Manual points'],
          ['redemptions', 'Redemptions'],
        ].map(([key, label]) => (
          <button key={key} className={`button ${activeSection === key ? 'primary' : 'outline'}`} type="button" onClick={() => setActiveSection(key)}>{label}</button>
        ))}
      </div>

      {loading ? <section className="surface section-gap"><div className="surface-body">Loading reward administration…</div></section> : null}

      {!loading && activeSection === 'catalog' && (
        <div className="dashboard-grid main-aside section-gap">
          <div>
            <section className="surface">
              <div className="surface-header"><h3>Reward catalogue</h3><span className="status-pill">{catalog.length} items</span></div>
              <div className="civic-table-wrap">
                <table className="civic-table">
                  <thead><tr><th>Reward</th><th>Type</th><th>Points</th><th>Stock</th><th>Status</th><th>Actions</th></tr></thead>
                  <tbody>
                    {catalog.map((reward) => (
                      <tr key={reward.id}>
                        <td><strong>{reward.name}</strong><small style={{ display: 'block' }}>{reward.description}</small></td>
                        <td>{reward.type}</td>
                        <td>{reward.pointsCost}</td>
                        <td><strong>{reward.stockQuantity}</strong></td>
                        <td><span className={`status-pill ${reward.isActive ? 'green' : 'amber'}`}>{reward.isActive ? 'Active' : 'Inactive'}</span></td>
                        <td>
                          <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap' }}>
                            <button className="button outline small" type="button" onClick={() => editReward(reward)}>Edit</button>
                            <button className="button outline small" type="button" onClick={() => setStockForm({ rewardId: reward.id, quantity: '', reason: '' })}>Refill</button>
                            <button className="button outline small" type="button" disabled={saving} onClick={() => run(() => reward.isActive ? rewardApi.deactivateReward(reward.id) : rewardApi.activateReward(reward.id), reward.isActive ? 'Reward deactivated.' : 'Reward activated.')}>
                              {reward.isActive ? 'Deactivate' : 'Activate'}
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                    {catalog.length === 0 && <tr><td colSpan="6">No catalogue records were found.</td></tr>}
                  </tbody>
                </table>
              </div>
            </section>

            {stockForm.rewardId && (
              <section className="surface section-gap">
                <div className="surface-header"><h3>Refill stock</h3><button className="button outline small" type="button" onClick={() => setStockForm(emptyStock)}>Cancel</button></div>
                <form className="surface-body form-grid" onSubmit={refillStock}>
                  <Field label="Quantity to add"><input className="input" type="number" min="1" required value={stockForm.quantity} onChange={(event) => setStockForm({ ...stockForm, quantity: event.target.value })} /></Field>
                  <Field label="Audited reason"><input className="input" minLength="10" maxLength="300" required value={stockForm.reason} onChange={(event) => setStockForm({ ...stockForm, reason: event.target.value })} /></Field>
                  <div style={{ alignSelf: 'end' }}><button className="button primary" disabled={saving}>Refill stock</button></div>
                </form>
              </section>
            )}
          </div>

          <section className="surface">
            <div className="surface-header"><h3>{editingRewardId ? 'Edit reward' : 'Create reward'}</h3></div>
            <form className="surface-body dashboard-grid" onSubmit={saveReward}>
              <Field label="Reward name"><input className="input" required maxLength="150" value={rewardForm.name} onChange={(event) => setRewardForm({ ...rewardForm, name: event.target.value })} /></Field>
              <Field label="Description"><textarea className="input" rows="4" required maxLength="1000" value={rewardForm.description} onChange={(event) => setRewardForm({ ...rewardForm, description: event.target.value })} /></Field>
              <Field label="Reward type"><select className="input" value={rewardForm.type} onChange={(event) => setRewardForm({ ...rewardForm, type: event.target.value })}><option>Voucher</option><option>Certificate</option><option>Badge</option><option>Merchandise</option></select></Field>
              <Field label="Points cost"><input className="input" type="number" min="1" required value={rewardForm.pointsCost} onChange={(event) => setRewardForm({ ...rewardForm, pointsCost: event.target.value })} /></Field>
              <Field label="Stock quantity"><input className="input" type="number" min="0" required value={rewardForm.stockQuantity} onChange={(event) => setRewardForm({ ...rewardForm, stockQuantity: event.target.value })} /></Field>
              <label style={{ display: 'flex', gap: 10, alignItems: 'center' }}><input type="checkbox" checked={rewardForm.isActive} onChange={(event) => setRewardForm({ ...rewardForm, isActive: event.target.checked })} /> Active and visible to Citizens</label>
              <button className="button primary full" disabled={saving}>{editingRewardId ? 'Save changes' : 'Create reward'}</button>
              {editingRewardId && <button className="button outline full" type="button" onClick={() => { setEditingRewardId(null); setRewardForm(emptyReward); }}>Cancel editing</button>}
            </form>
          </section>
        </div>
      )}

      {!loading && activeSection === 'rules' && (
        <div className="dashboard-grid equal section-gap">
          <section className="surface">
            <div className="surface-header"><h3>Badge rules</h3><button className="button outline small" type="button" onClick={() => setRules((current) => ({ ...current, badgeRules: [...current.badgeRules, { code: `BADGE_${current.badgeRules.length + 1}`, name: 'New badge', description: 'Describe how this badge is earned.', icon: '★', metric: 'Points', target: 100, isActive: true, displayOrder: (current.badgeRules.length + 1) * 10 }] }))}>+ Add rule</button></div>
            <div className="surface-body dashboard-grid">
              {rules.badgeRules.map((rule, index) => (
                <div key={`${rule.code}-${index}`} className="surface" style={{ padding: 14 }}>
                  <div className="form-grid">
                    <Field label="Code"><input className="input" value={rule.code} onChange={(event) => updateBadge(index, 'code', event.target.value)} /></Field>
                    <Field label="Name"><input className="input" value={rule.name} onChange={(event) => updateBadge(index, 'name', event.target.value)} /></Field>
                    <Field label="Metric"><select className="input" value={rule.metric} onChange={(event) => updateBadge(index, 'metric', event.target.value)}><option>Points</option><option>ClosedComplaints</option><option>ApprovedVerifications</option><option>SubmittedComplaints</option><option>SupportedIssues</option></select></Field>
                    <Field label="Target"><input className="input" type="number" min="1" value={rule.target} onChange={(event) => updateBadge(index, 'target', Number(event.target.value))} /></Field>
                    <Field label="Icon"><input className="input" value={rule.icon} onChange={(event) => updateBadge(index, 'icon', event.target.value)} /></Field>
                    <Field label="Order"><input className="input" type="number" min="0" value={rule.displayOrder} onChange={(event) => updateBadge(index, 'displayOrder', Number(event.target.value))} /></Field>
                  </div>
                  <Field label="Description"><textarea className="input" rows="2" value={rule.description} onChange={(event) => updateBadge(index, 'description', event.target.value)} /></Field>
                  <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: 10 }}><label><input type="checkbox" checked={rule.isActive} onChange={(event) => updateBadge(index, 'isActive', event.target.checked)} /> Active</label><button className="button outline small" type="button" onClick={() => setRules((current) => ({ ...current, badgeRules: current.badgeRules.filter((_, ruleIndex) => ruleIndex !== index) }))}>Remove</button></div>
                </div>
              ))}
              <button className="button primary" type="button" disabled={saving} onClick={saveBadges}>Save badge rules</button>
            </div>
          </section>

          <section className="surface">
            <div className="surface-header"><h3>Tier rules</h3><button className="button outline small" type="button" onClick={() => setRules((current) => ({ ...current, tierRules: [...current.tierRules, { name: 'New tier', minimumPoints: 0 }] }))}>+ Add tier</button></div>
            <div className="surface-body dashboard-grid">
              {[...rules.tierRules].sort((a, b) => a.minimumPoints - b.minimumPoints).map((rule) => {
                const index = rules.tierRules.indexOf(rule);
                return <div key={`${rule.name}-${index}`} className="form-grid"><Field label="Tier name"><input className="input" value={rule.name} onChange={(event) => updateTier(index, 'name', event.target.value)} /></Field><Field label="Minimum points"><input className="input" type="number" min="0" value={rule.minimumPoints} onChange={(event) => updateTier(index, 'minimumPoints', Number(event.target.value))} /></Field><button className="button outline small" type="button" style={{ alignSelf: 'end' }} onClick={() => setRules((current) => ({ ...current, tierRules: current.tierRules.filter((_, ruleIndex) => ruleIndex !== index) }))}>Remove</button></div>;
              })}
              <div className="alert info">One tier must begin at zero points. Citizen tiers and badge progress update immediately after saving.</div>
              <button className="button primary" type="button" disabled={saving} onClick={saveTiers}>Save tier rules</button>
            </div>
          </section>
        </div>
      )}

      {!loading && activeSection === 'points' && (
        <section className="surface section-gap" style={{ maxWidth: 760 }}>
          <div className="surface-header"><h3>Manual points adjustment</h3><span className="status-pill amber">Audited operation</span></div>
          <form className="surface-body dashboard-grid" onSubmit={adjustPoints}>
            <div className="alert info">Use a positive value to add points or a negative value to deduct points. The reason appears in the Citizen’s points history and audit log.</div>
            <Field label="Citizen user ID"><input className="input" type="number" min="1" required value={adjustment.userId} onChange={(event) => setAdjustment({ ...adjustment, userId: event.target.value })} /></Field>
            <Field label="Points adjustment"><input className="input" type="number" min="-10000" max="10000" required value={adjustment.pointsDelta} onChange={(event) => setAdjustment({ ...adjustment, pointsDelta: event.target.value })} /></Field>
            <Field label="Audited reason"><textarea className="input" rows="4" minLength="10" maxLength="250" required value={adjustment.reason} onChange={(event) => setAdjustment({ ...adjustment, reason: event.target.value })} /></Field>
            <button className="button primary" disabled={saving}>Apply points adjustment</button>
          </form>
        </section>
      )}

      {!loading && activeSection === 'redemptions' && (
        <div className="dashboard-grid main-aside section-gap">
          <section className="surface">
            <div className="surface-header"><h3>Redemption administration</h3><select className="input" style={{ maxWidth: 180 }} value={redemptionStatus} onChange={(event) => setRedemptionStatus(event.target.value)}><option value="">All statuses</option><option>Pending</option><option>Approved</option><option>Fulfilled</option><option>Rejected</option><option>Cancelled</option></select></div>
            <div className="civic-table-wrap">
              <table className="civic-table">
                <thead><tr><th>Citizen</th><th>Reward</th><th>Code</th><th>Points</th><th>Status</th><th>Action</th></tr></thead>
                <tbody>
                  {redemptions.map((item) => <tr key={item.id}><td><strong>{item.citizenName}</strong><small style={{ display: 'block' }}>User #{item.userId}</small></td><td>{item.rewardName}</td><td>{item.redemptionCode}</td><td>{item.pointsSpent}</td><td><span className="status-pill">{item.status}</span></td><td><button className="button outline small" type="button" disabled={['Fulfilled', 'Rejected', 'Cancelled'].includes(item.status)} onClick={() => setDecision({ redemptionId: item.id, status: item.status === 'Approved' ? 'Fulfilled' : 'Approved', reason: '' })}>Decide</button></td></tr>)}
                  {redemptions.length === 0 && <tr><td colSpan="6">No redemptions match the selected status.</td></tr>}
                </tbody>
              </table>
            </div>
          </section>

          <section className="surface">
            <div className="surface-header"><h3>Record decision</h3></div>
            <form className="surface-body dashboard-grid" onSubmit={updateRedemption}>
              <Field label="Redemption ID"><input className="input" type="number" min="1" required value={decision.redemptionId} onChange={(event) => setDecision({ ...decision, redemptionId: event.target.value })} /></Field>
              <Field label="Decision"><select className="input" value={decision.status} onChange={(event) => setDecision({ ...decision, status: event.target.value })}><option>Approved</option><option>Fulfilled</option><option>Rejected</option><option>Cancelled</option></select></Field>
              <Field label="Decision reason"><textarea className="input" rows="4" minLength="10" maxLength="300" required value={decision.reason} onChange={(event) => setDecision({ ...decision, reason: event.target.value })} /></Field>
              <div className="alert info">Rejecting or cancelling a pending redemption automatically restores its stock and refunds the Citizen’s points.</div>
              <button className="button primary" disabled={saving}>Save redemption decision</button>
            </form>
          </section>
        </div>
      )}
    </section>
  );
}

function Field({ label, children }) {
  return <label className="field"><span>{label}</span>{children}</label>;
}

function Stat({ label, value }) {
  return <article className="stat-card"><span>{label}</span><strong>{value}</strong></article>;
}
