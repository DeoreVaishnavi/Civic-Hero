import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import CivicHeatmapMap from '../../components/maps/CivicHeatmapMap.jsx';
import { analyticsApi } from '../../services/analyticsApi.js';
import { mapApi } from '../../services/mapApi.js';

const categories = [
  '', 'Pothole', 'Garbage', 'Streetlight', 'Water Leakage', 'Drainage',
  'Road Damage', 'Public Safety', 'Illegal Dumping', 'Other',
];

const statuses = [
  ['', 'All statuses'],
  ['Created', 'Reported'],
  ['Assigned', 'Assigned'],
  ['InProgress', 'In progress'],
  ['Resolved', 'Resolution uploaded'],
  ['VerificationPending', 'Awaiting verification'],
  ['Disputed', 'Disputed'],
  ['Closed', 'Solved and closed'],
  ['ClosedAuto', 'Auto-closed'],
];

const riskTone = { Critical: 'danger', High: 'danger', Medium: 'warning', Low: 'success' };

export default function Heatmap() {
  const [draftFilters, setDraftFilters] = useState({ category: '', status: '', range: '30', wardId: '' });
  const [filters, setFilters] = useState(draftFilters);
  const [points, setPoints] = useState([]);
  const [selectedPoint, setSelectedPoint] = useState(null);
  const [mapConfig, setMapConfig] = useState(null);
  const [wardOptions, setWardOptions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  useEffect(() => {
    mapApi.config()
      .then(setMapConfig)
      .catch(() => setMapConfig({ enabled: false }));
  }, []);

  const loadHeatmap = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const to = new Date();
      const from = new Date(to);
      from.setDate(from.getDate() - Number(filters.range || 30));
      const data = await analyticsApi.heatmap({
        from: from.toISOString(),
        to: to.toISOString(),
        category: filters.category,
        status: filters.status,
        wardId: filters.wardId,
      });
      const nextPoints = Array.isArray(data) ? data : [];
      setPoints(nextPoints);
      setSelectedPoint((current) => {
        if (!nextPoints.length) return null;
        if (!current) return nextPoints[0];
        return nextPoints.find((point) => point.latitude === current.latitude && point.longitude === current.longitude) || nextPoints[0];
      });

      const wards = Array.from(
        new Map(nextPoints.filter((point) => point.wardId).map((point) => [String(point.wardId), point.wardName || `Ward ${point.wardId}`])).entries(),
      ).map(([id, name]) => ({ id, name })).sort((a, b) => a.name.localeCompare(b.name));
      if (wards.length) setWardOptions((current) => current.length >= wards.length ? current : wards);
    } catch (requestError) {
      setError(requestError.message || 'Unable to load complaint heatmap data.');
      setPoints([]);
      setSelectedPoint(null);
    } finally {
      setLoading(false);
    }
  }, [filters]);

  useEffect(() => { loadHeatmap(); }, [loadHeatmap]);

  const summary = useMemo(() => summarize(points), [points]);
  const wardRows = useMemo(() => summarizeWards(points), [points]);
  const categoryRows = useMemo(() => aggregateSlices(points, 'byCategory'), [points]);
  const statusRows = useMemo(() => aggregateSlices(points, 'byStatus'), [points]);

  const applyFilters = () => {
    setFilters(draftFilters);
    setMessage('Heatmap filters applied to live complaint data.');
    window.setTimeout(() => setMessage(''), 2600);
  };

  const clearFilters = () => {
    const cleared = { category: '', status: '', range: '30', wardId: '' };
    setDraftFilters(cleared);
    setFilters(cleared);
  };

  return (
    <section className="page-wrap heatmap-page">
      <div className="page-title-row">
        <div>
          <p className="section-kicker">Geographic complaint intelligence</p>
          <h2>City complaint heatmap</h2>
          <p>See how many complaints were reported in each area, how many are active, and how many were solved.</p>
        </div>
        <Link to="/citizen" className="button outline">← Back to dashboard</Link>
      </div>

      {message && <div className="alert success">{message}</div>}
      {error && <div className="alert danger">{error}</div>}

      <section className="surface heatmap-filter-card">
        <div className="surface-header"><h3>Filter map data</h3><span className="heatmap-live-label">● Live from CivicHero database</span></div>
        <div className="surface-body heatmap-filters">
          <label><span>Category</span><select className="input" value={draftFilters.category} onChange={(event) => setDraftFilters({ ...draftFilters, category: event.target.value })}>{categories.map((category) => <option key={category || 'all'} value={category}>{category || 'All categories'}</option>)}</select></label>
          <label><span>Status</span><select className="input" value={draftFilters.status} onChange={(event) => setDraftFilters({ ...draftFilters, status: event.target.value })}>{statuses.map(([value, label]) => <option key={value || 'all'} value={value}>{label}</option>)}</select></label>
          <label><span>Date range</span><select className="input" value={draftFilters.range} onChange={(event) => setDraftFilters({ ...draftFilters, range: event.target.value })}><option value="7">Past 7 days</option><option value="30">Past 30 days</option><option value="90">Past 90 days</option><option value="365">Past year</option><option value="730">Past 2 years</option></select></label>
          <label><span>Ward</span><select className="input" value={draftFilters.wardId} onChange={(event) => setDraftFilters({ ...draftFilters, wardId: event.target.value })}><option value="">All wards</option>{wardOptions.map((ward) => <option key={ward.id} value={ward.id}>{ward.name}</option>)}</select></label>
          <div className="heatmap-filter-actions"><button type="button" className="button outline" onClick={clearFilters}>Clear</button><button type="button" className="button primary" onClick={applyFilters} disabled={loading}>{loading ? 'Loading…' : 'Apply filters'}</button></div>
        </div>
      </section>

      <div className="heatmap-metrics section-gap">
        <Metric label="Complaints shown" value={summary.total} helper={`${points.length} geographic clusters`} />
        <Metric label="Active complaints" value={summary.active} helper="Still being processed" tone="warning" />
        <Metric label="Solved complaints" value={summary.solved} helper={`${summary.resolutionRate}% resolution rate`} tone="success" />
        <Metric label="Under verification" value={summary.pending} helper={`${summary.disputed} disputed`} tone="violet" />
      </div>

      <div className="heatmap-main-grid section-gap">
        <section className="surface heatmap-map-card">
          <div className="surface-header">
            <div><h3>Complaint density map</h3><small>Click a numbered hotspot to inspect that area.</small></div>
            <div className="heatmap-risk-legend"><span><i className="low" />Low</span><span><i className="medium" />Medium</span><span><i className="high" />High</span><span><i className="critical" />Critical</span></div>
          </div>
          <div className="surface-body heatmap-map-body">
            {loading ? <div className="heatmap-loading">Loading geographic complaint data…</div> : <CivicHeatmapMap points={points} config={mapConfig} selectedPoint={selectedPoint} onSelect={setSelectedPoint} />}
          </div>
        </section>

        <AreaDetails point={selectedPoint} />
      </div>

      <div className="dashboard-grid equal section-gap heatmap-analysis-grid">
        <BreakdownCard title="Complaints by category" rows={categoryRows} total={summary.total} />
        <BreakdownCard title="Complaint status" rows={statusRows} total={summary.total} />
      </div>

      <section className="surface section-gap">
        <div className="surface-header"><div><h3>Ward resolution performance</h3><small>Compare reported, active and solved complaint counts by ward.</small></div><button type="button" className="button outline small" onClick={loadHeatmap}>Refresh data</button></div>
        <div className="heatmap-ward-table">
          <div className="heatmap-ward-head"><span>Ward</span><span>Total</span><span>Active</span><span>Solved</span><span>Resolution</span><span>Hotspot risk</span></div>
          {wardRows.length ? wardRows.map((row) => <div className="heatmap-ward-row" key={row.id}><strong>{row.name}</strong><span>{row.total}</span><span>{row.active}</span><span className="solved">{row.solved}</span><span><b>{row.rate}%</b><i><em style={{ width: `${row.rate}%` }} /></i></span><span className={`heatmap-risk-pill ${riskTone[row.risk] || 'success'}`}>{row.risk}</span></div>) : <p className="heatmap-table-empty">No ward data matches these filters.</p>}
        </div>
      </section>
    </section>
  );
}

function Metric({ label, value, helper, tone = 'blue' }) {
  return <article className={`heatmap-metric tone-${tone}`}><span>{label}</span><strong>{Number(value || 0).toLocaleString()}</strong><small>{helper}</small></article>;
}

function AreaDetails({ point }) {
  if (!point) return <aside className="surface heatmap-area-card"><div className="surface-header"><h3>Selected area</h3></div><div className="heatmap-empty">Select a hotspot to view its complaint and resolution details.</div></aside>;
  return (
    <aside className="surface heatmap-area-card">
      <div className="surface-header"><div><h3>{point.wardName || 'Selected area'}</h3><small>{Number(point.latitude).toFixed(4)}, {Number(point.longitude).toFixed(4)}</small></div><span className={`heatmap-risk-pill ${riskTone[point.riskLevel] || 'success'}`}>{point.riskLevel} risk</span></div>
      <div className="surface-body">
        <div className="area-count-grid"><Count label="Total" value={point.complaintCount} /><Count label="Active" value={point.activeCount} /><Count label="Solved" value={point.solvedCount} good /><Count label="Disputed" value={point.disputedCount} /></div>
        <div className="resolution-block"><div><span>Area resolution rate</span><strong>{point.resolutionRate || 0}%</strong></div><div className="resolution-track"><i style={{ width: `${Math.min(100, Number(point.resolutionRate || 0))}%` }} /></div><small>Average time to resolution: {formatHours(point.averageResolutionHours)}</small></div>
        <div className="area-detail-list"><Detail label="Dominant category" value={point.dominantCategory || 'Other'} /><Detail label="Awaiting verification" value={point.resolutionPendingCount || 0} /><Detail label="Latest status" value={friendlyStatus(point.latestComplaintStatus)} /><Detail label="Last updated" value={formatDate(point.lastUpdatedAt)} /></div>
        {point.latestComplaintId ? <Link className="button primary full" to={`/citizen/complaints/${point.latestComplaintId}`}>View latest complaint</Link> : null}
      </div>
    </aside>
  );
}

function Count({ label, value, good }) { return <div className={good ? 'good' : ''}><strong>{value || 0}</strong><span>{label}</span></div>; }
function Detail({ label, value }) { return <div><span>{label}</span><strong>{value}</strong></div>; }

function BreakdownCard({ title, rows, total }) {
  const visible = rows.slice(0, 8);
  return <section className="surface"><div className="surface-header"><h3>{title}</h3></div><div className="surface-body heatmap-breakdown">{visible.length ? visible.map((row) => { const percentage = total ? Math.round((row.count / total) * 100) : 0; return <div key={row.name}><div><span>{friendlyStatus(row.name)}</span><strong>{row.count}</strong></div><i><em style={{ width: `${percentage}%` }} /></i><small>{percentage}% of complaints shown</small></div>; }) : <p className="heatmap-table-empty">No data available.</p>}</div></section>;
}

function summarize(points) {
  const total = points.reduce((sum, point) => sum + Number(point.complaintCount || 0), 0);
  const active = points.reduce((sum, point) => sum + Number(point.activeCount || 0), 0);
  const solved = points.reduce((sum, point) => sum + Number(point.solvedCount || 0), 0);
  const pending = points.reduce((sum, point) => sum + Number(point.resolutionPendingCount || 0), 0);
  const disputed = points.reduce((sum, point) => sum + Number(point.disputedCount || 0), 0);
  return { total, active, solved, pending, disputed, resolutionRate: total ? Math.round((solved / total) * 100) : 0 };
}

function summarizeWards(points) {
  const wards = new Map();
  points.forEach((point) => {
    const id = String(point.wardId ?? point.wardName ?? 'unassigned');
    const row = wards.get(id) || { id, name: point.wardName || 'Unassigned ward', total: 0, active: 0, solved: 0, heat: 0 };
    row.total += Number(point.complaintCount || 0);
    row.active += Number(point.activeCount || 0);
    row.solved += Number(point.solvedCount || 0);
    row.heat += Number(point.heatScore || 0);
    wards.set(id, row);
  });
  return Array.from(wards.values()).map((row) => ({ ...row, rate: row.total ? Math.round((row.solved / row.total) * 100) : 0, risk: row.heat >= 25 ? 'Critical' : row.heat >= 12 ? 'High' : row.heat >= 5 ? 'Medium' : 'Low' })).sort((a, b) => b.total - a.total);
}

function aggregateSlices(points, key) {
  const totals = new Map();
  points.forEach((point) => (point[key] || []).forEach((slice) => totals.set(slice.name, (totals.get(slice.name) || 0) + Number(slice.count || 0))));
  return Array.from(totals.entries()).map(([name, count]) => ({ name, count })).sort((a, b) => b.count - a.count);
}

function friendlyStatus(value = '') {
  return String(value).replace(/([a-z])([A-Z])/g, '$1 $2').replace(/_/g, ' ').replace(/^./, (letter) => letter.toUpperCase());
}

function formatDate(value) {
  if (!value) return 'Not available';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'Not available' : date.toLocaleString();
}

function formatHours(value) {
  const hours = Number(value || 0);
  if (!hours) return 'Not enough solved cases';
  if (hours < 24) return `${Math.round(hours)} hours`;
  return `${(hours / 24).toFixed(1)} days`;
}
