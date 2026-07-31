import { useState } from 'react';
import { Link } from 'react-router-dom';

export default function Heatmap() {
  const [filters, setFilters] = useState({ category: 'All', range: '30', ward: 'All' });
  const [message, setMessage] = useState('');
  const apply = () => setMessage(`Heatmap filters applied: ${filters.category}, past ${filters.range} days, ${filters.ward}.`);

  return (
    <section className="page-wrap">
      <div className="page-title-row"><div><p className="section-kicker">Geographic complaint intelligence</p><h2>City complaint heatmap</h2><p>Visualize complaint concentration across wards and inspect high-activity zones.</p></div><Link to="/citizen" className="button outline">← Back to dashboard</Link></div>
      {message && <div className="alert success">{message}</div>}
      <div className="filter-bar"><select className="input" value={filters.category} onChange={(e) => setFilters({ ...filters, category: e.target.value })}><option>All</option><option>Garbage</option><option>Potholes</option><option>Streetlights</option><option>Water</option></select><select className="input" value={filters.range} onChange={(e) => setFilters({ ...filters, range: e.target.value })}><option value="30">Past 30 days</option><option value="90">Past 90 days</option><option value="365">Past year</option></select><select className="input" value={filters.ward} onChange={(e) => setFilters({ ...filters, ward: e.target.value })}><option>All wards</option><option>Ward 1</option><option>Ward 2</option><option>Ward 3</option><option>Ward 4</option></select><button type="button" onClick={apply} className="button primary">Apply filters</button></div>

      <section className="surface section-gap"><div className="surface-header"><h3>Heatmap</h3><div className="legend"><span><i />High complaints</span><span><i />Medium complaints</span><span><i />Low complaints</span></div></div><div className="surface-body"><div className="map-placeholder large"><span className="map-pin-dot p1" /><span className="map-pin-dot p2" /><span className="map-pin-dot p3" /><span className="map-pin-dot p4" /><div style={{ position: 'absolute', left: '34%', top: '22%', width: 190, height: 120, borderRadius: '50%', background: 'radial-gradient(circle, rgba(216,74,92,.72), rgba(211,138,16,.42) 42%, rgba(21,150,111,.16) 72%, transparent 74%)', zIndex: 1 }} /><div className="map-overlay-card"><strong>Selected ward details</strong><p>Click a ward in the production map to load complaint totals, category distribution and active cases.</p><div className="notification-list" style={{ marginTop: 8 }}><Notice title="Garbage" text="High concentration" /><Notice title="Potholes" text="Medium concentration" /><Notice title="Streetlights" text="Low concentration" /></div></div></div></div></section>

      <div className="dashboard-grid equal section-gap">
        <section className="surface"><div className="surface-header"><h3>Complaints by category</h3></div><div className="surface-body"><div className="bar-chart"><Bar label="Garbage" height="145px" /><Bar label="Potholes" height="110px" /><Bar label="Lights" height="72px" /><Bar label="Water" height="95px" /><Bar label="Other" height="55px" /></div></div></section>
        <section className="surface"><div className="surface-header"><h3>Complaint trend</h3></div><div className="surface-body"><div className="chart-placeholder"><svg className="chart-line" viewBox="0 0 600 220" preserveAspectRatio="none" aria-hidden="true"><polyline points="0,175 70,150 140,125 210,140 280,90 350,105 430,66 510,82 600,43" fill="none" stroke="#1769e0" strokeWidth="6" strokeLinecap="round" /><polyline points="0,192 70,182 140,174 210,165 280,155 350,142 430,135 510,122 600,110" fill="none" stroke="#15966f" strokeWidth="4" strokeLinecap="round" /></svg><div className="chart-legend"><span>Complaints</span><span>Resolved</span></div></div></div></section>
      </div>

      <div className="page-actions section-gap" style={{ justifyContent: 'flex-end' }}><button className="button outline">View full report</button><button className="button primary">Export data</button></div>
    </section>
  );
}

function Bar({ label, height }) { return <div className="bar-item"><i style={{ '--height': height }} /><span>{label}</span></div>; }
function Notice({ title, text }) { return <div className="notification-item"><span>•</span><div><strong>{title}</strong><small>{text}</small></div></div>; }
