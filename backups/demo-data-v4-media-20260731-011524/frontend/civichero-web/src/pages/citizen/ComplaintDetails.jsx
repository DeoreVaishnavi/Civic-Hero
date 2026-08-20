import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import StatusBadge from '../../components/common/StatusBadge.jsx';
import { complaintApi } from '../../services/complaintApi.js';

export default function ComplaintDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [detail, setDetail] = useState(null);
  const [metadata, setMetadata] = useState({ categories: [], departments: [], wards: [] });
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState(null);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const wards = useMemo(() => metadata.wards.filter((ward) => String(ward.departmentId) === String(form?.departmentId)), [metadata.wards, form?.departmentId]);

  const load = async () => {
    try {
      const response = await complaintApi.getById(id); setDetail(response);
      const item = response.complaint;
      setForm({ title: item.title, description: item.description, category: item.category, departmentId: item.departmentId, wardId: item.wardId, latitude: item.latitude, longitude: item.longitude, address: item.address });
    } catch (reason) { setError(reason.message); }
  };
  useEffect(() => { load(); complaintApi.metadata().then(setMetadata).catch(() => {}); }, [id]);
  const item = detail?.complaint;

  const save = async (event) => {
    event.preventDefault(); setError('');
    try { const response = await complaintApi.update(id, form); setDetail(response); setEditing(false); setMessage('Complaint updated.'); }
    catch (reason) { setError(reason.errors?.join(' ') || reason.message); }
  };
  const withdraw = async () => {
    if (!confirm('Withdraw this complaint?')) return;
    try { setDetail(await complaintApi.withdraw(id)); setMessage('Complaint withdrawn.'); }
    catch (reason) { setError(reason.message); }
  };
  const toggleUpvote = async () => {
    try { if (item.hasUpvoted) await complaintApi.removeUpvote(id); else await complaintApi.upvote(id); await load(); }
    catch (reason) { setError(reason.message); }
  };
  const download = async (image) => {
    try { const response = await complaintApi.downloadImage(id, image.id); const url = URL.createObjectURL(response.data); const link = document.createElement('a'); link.href = url; link.download = image.fileName; link.click(); URL.revokeObjectURL(url); }
    catch (reason) { setError(reason.message); }
  };

  if (!detail && !error) return <section className="page-wrap"><div className="surface"><div className="surface-body">Loading complaint…</div></div></section>;
  if (!detail) return <section className="page-wrap"><div className="alert error">{error}</div></section>;

  return (
    <section className="page-wrap narrow">
      <div className="page-title-row"><div><p className="section-kicker">Complaint details</p><h2>{item.title}</h2><p>{item.referenceNumber} · {item.category} · {item.wardName}</p></div><div className="page-actions"><StatusBadge status={item.status} /><button type="button" onClick={() => navigate(-1)} className="button outline">← Back</button></div></div>
      {message && <div className="alert success">{message}</div>}{error && <div className="alert error">{error}</div>}

      <section className="surface">
        <div className="surface-header"><h3>Before images</h3><span className="muted" style={{ fontSize: 8 }}>Click an evidence file to download</span></div>
        <div className="surface-body">
          {detail.images.length ? <div className="image-preview-grid">{detail.images.slice(0,6).map((image, index) => <button type="button" key={image.id} onClick={() => download(image)} className="image-preview" style={{ border: 0, padding: 0 }}><div className="issue-photo" style={{ width: '100%', height: '100%' }}>Evidence {index + 1}<br />{image.fileName}</div></button>)}</div> : <div className="upload-zone"><strong>No evidence images available</strong><p>Images uploaded with the complaint will appear here.</p></div>}
        </div>
      </section>

      <section className="surface section-gap">
        <div className="surface-header"><h3>Complaint information</h3><div className="page-actions">{item.canEdit && <button type="button" onClick={() => setEditing((value) => !value)} className="button outline small">{editing ? 'Cancel editing' : 'Edit complaint'}</button>}{item.canWithdraw && <button type="button" onClick={withdraw} className="button danger small">Withdraw</button>}</div></div>
        <div className="surface-body">
          <div className="stat-grid" style={{ gridTemplateColumns: 'repeat(4,1fr)' }}><Info label="Category" value={item.category} /><Info label="Priority" value={item.priority} /><Info label="Department" value={item.departmentName} /><Info label="Ward" value={item.wardName} /></div>
          <p style={{ margin: '18px 0 0', color: '#3d4c61', fontSize: 11, lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>{item.description}</p>
        </div>
      </section>

      {editing && <form onSubmit={save} className="surface form-section section-gap"><div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Edit complaint</h3></div><label className="form-label">Title<input className="input" value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required /></label><label className="form-label" style={{ marginTop: 13 }}>Description<textarea className="input" style={{ minHeight: 120 }} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} required /></label><div className="form-grid three" style={{ marginTop: 13 }}><label className="form-label">Category<select className="input" value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })}>{metadata.categories.map((value) => <option key={value}>{value}</option>)}</select></label><label className="form-label">Department<select className="input" value={form.departmentId} onChange={(e) => setForm({ ...form, departmentId: e.target.value, wardId: '' })}>{metadata.departments.map((value) => <option key={value.id} value={value.id}>{value.name}</option>)}</select></label><label className="form-label">Ward<select className="input" value={form.wardId} onChange={(e) => setForm({ ...form, wardId: e.target.value })}>{wards.map((value) => <option key={value.id} value={value.id}>{value.name}</option>)}</select></label></div><button className="button primary" style={{ marginTop: 15 }}>Save changes</button></form>}

      <div className="dashboard-grid main-aside section-gap">
        <section className="surface">
          <div className="surface-header"><h3>Location / map</h3></div>
          <div className="surface-body"><div className="map-placeholder"><span className="map-pin-dot p2" /><div className="map-overlay-card"><strong>{item.address}</strong><p>{item.latitude}, {item.longitude}</p></div></div></div>
        </section>
        <section className="surface">
          <div className="surface-header"><h3>Community support</h3></div>
          <div className="surface-body"><strong style={{ fontSize: 28 }}>{item.upvoteCount || 0}</strong><p className="muted" style={{ marginTop: 4, fontSize: 9 }}>Citizens supporting this complaint</p><button type="button" onClick={toggleUpvote} className={`button ${item.hasUpvoted ? 'success' : 'primary'} full`}>{item.hasUpvoted ? '✓ You supported this issue' : '👍 Support this issue'}</button></div>
        </section>
      </div>

      <section className="surface section-gap">
        <div className="surface-header"><h3>Complaint timeline</h3></div>
        <div className="surface-body timeline-list">{detail.timeline.map((event) => <div key={event.id} className="timeline-item complete"><span className="timeline-dot" /><h4>{readable(event.eventType)} <small style={{ color: 'var(--civic-muted)', fontWeight: 500 }}>· {new Date(event.timestamp).toLocaleString()}</small></h4><p>{event.description} · By {event.actorName}</p></div>)}</div>
      </section>
    </section>
  );
}

function Info({ label, value }) { return <article className="stat-card" style={{ minHeight: 82 }}><small style={{ marginTop: 0 }}>{label}</small><strong style={{ fontSize: 15, marginTop: 8 }}>{value || '—'}</strong></article>; }
function readable(value='') { return String(value).replace(/([a-z])([A-Z])/g, '$1 $2'); }
