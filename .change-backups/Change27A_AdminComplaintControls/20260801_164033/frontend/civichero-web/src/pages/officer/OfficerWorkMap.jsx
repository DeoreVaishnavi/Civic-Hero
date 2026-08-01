import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import OfficerAssignmentMap from '../../components/maps/OfficerAssignmentMap.jsx';
import SlaBadge from '../../components/common/SlaBadge.jsx';
import StatusBadge from '../../components/common/StatusBadge.jsx';
import { assignmentApi } from '../../services/assignmentApi.js';

const asArray = (value) => (Array.isArray(value) ? value : []);

export default function OfficerWorkMap() {
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState(null);
  const [priority, setPriority] = useState('');
  const [status, setStatus] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [lastUpdated, setLastUpdated] = useState(null);

  const load = useCallback(async (showLoader = true) => {
    if (showLoader) setLoading(true);
    setError('');
    try {
      const value = asArray(await assignmentApi.officerMap());
      setItems(value);
      setLastUpdated(new Date());
      setSelected((current) => value.find((item) => Number(item.complaintId) === Number(current?.complaintId)) || value[0] || null);
    } catch (reason) {
      setError(reason?.message || 'Unable to load the Officer work map.');
    } finally {
      if (showLoader) setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
    const timer = window.setInterval(() => load(false), 30000);
    return () => window.clearInterval(timer);
  }, [load]);

  const filtered = useMemo(() => items.filter((item) => {
    if (priority && String(item?.priority).toLowerCase() !== priority.toLowerCase()) return false;
    if (status && String(item?.assignmentStatus).toLowerCase() !== status.toLowerCase()) return false;
    return true;
  }), [items, priority, status]);

  useEffect(() => {
    if (selected && !filtered.some((item) => Number(item.complaintId) === Number(selected.complaintId))) setSelected(filtered[0] || null);
  }, [filtered, selected]);

  return (
    <section className="page-wrap">
      <div className="page-title-row">
        <div><p className="section-kicker">Officer field operations</p><h2>Live assignment map</h2><p>Active assignments refresh every 30 seconds while this page is open.</p></div>
        <div className="page-actions"><button type="button" className="button outline" onClick={() => load()} disabled={loading}>Refresh</button><Link to="/officer/assignments" className="button primary">Work queue</Link></div>
      </div>

      {error && <div className="alert error" role="alert">{error}</div>}
      <section className="surface section-gap">
        <div className="surface-header">
          <div><h3>Map filters</h3><p className="muted">{filtered.length} of {items.length} assignments visible{lastUpdated ? ` · Updated ${lastUpdated.toLocaleTimeString()}` : ''}</p></div>
        </div>
        <div className="surface-body form-grid">
          <label className="form-label">Priority<select className="input" value={priority} onChange={(event) => setPriority(event.target.value)}><option value="">All priorities</option>{['Low', 'Medium', 'High', 'Critical'].map((value) => <option key={value}>{value}</option>)}</select></label>
          <label className="form-label">Assignment state<select className="input" value={status} onChange={(event) => setStatus(event.target.value)}><option value="">All active states</option><option value="Pending">Pending</option><option value="Accepted">Accepted</option></select></label>
        </div>
      </section>

      <div className="dashboard-grid main-aside section-gap">
        <section className="surface">
          <div className="surface-header"><h3>Assigned work locations</h3><span className="status-pill">Auto refresh</span></div>
          <div className="surface-body" style={{ padding: 0 }}>{loading ? <div style={{ padding: 24 }}>Loading map…</div> : <OfficerAssignmentMap assignments={filtered} selectedId={selected?.complaintId} onSelect={setSelected} />}</div>
        </section>

        <aside className="surface">
          <div className="surface-header"><h3>Selected assignment</h3></div>
          <div className="surface-body">
            {selected ? <>
              <p className="section-kicker">{selected.referenceNumber}</p>
              <h3>{selected.title}</h3>
              <div className="page-actions"><StatusBadge status={selected.complaintStatus} /><span className="status-pill amber">{selected.priority}</span></div>
              <p className="muted">{selected.category} · {selected.wardName}</p>
              <p>{selected.address || 'Address unavailable'}</p>
              <SlaBadge state={selected.slaState} remainingMinutes={selected.remainingMinutes} />
              {selected.estimatedCompletionAt && <p className="muted">ETA: {new Date(selected.estimatedCompletionAt).toLocaleString()}</p>}
              <Link to={`/officer/assignments/${selected.complaintId}`} className="button primary full">Open assignment</Link>
            </> : <p className="muted">No active assignment matches the selected filters.</p>}
          </div>
        </aside>
      </div>
    </section>
  );
}
