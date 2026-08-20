import { useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { assignmentApi } from '../../services/assignmentApi.js';

export default function UploadResolution() {
  const { complaintId } = useParams();
  const navigate = useNavigate();
  const [notes, setNotes] = useState('');
  const [contractor, setContractor] = useState('');
  const [cost, setCost] = useState('');
  const [evidence, setEvidence] = useState([]);
  const [location, setLocation] = useState({ latitude: '', longitude: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const previews = evidence.filter((file) => file.type.startsWith('image/')).map((file) => ({ file, url: URL.createObjectURL(file) }));

  const capture = () => navigator.geolocation?.getCurrentPosition(
    (position) => setLocation({ latitude: position.coords.latitude, longitude: position.coords.longitude }),
    () => setError('Location could not be captured. You may submit evidence without GPS.'),
  );
  const submit = async (event) => {
    event.preventDefault(); setBusy(true); setError('');
    try { await assignmentApi.complete(complaintId, { notes: `${notes}${contractor ? `\nContractor: ${contractor}` : ''}${cost ? `\nCost: ${cost}` : ''}`, evidence, ...location }); navigate(`/officer/assignments/${complaintId}`, { replace: true }); }
    catch (reason) { setError(reason.message); }
    finally { setBusy(false); }
  };

  return (
    <section className="page-wrap narrow">
      <div className="page-title-row"><div><p className="section-kicker">Resolution evidence</p><h2>Upload completed-work proof</h2><p>Add after-work images, work details and the completion location for citizen verification.</p></div><Link to={`/officer/assignments/${complaintId}`} className="button outline">← Back to assignment</Link></div>
      {error && <div className="alert error">{error}</div>}
      <form onSubmit={submit}>
        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Complaint summary</h3><span className="status-pill amber">In progress</span></div>
          <div className="summary-meta"><span>Complaint #{complaintId}</span><span>Before images available in assignment details</span><span>Citizen verification follows submission</span></div>
        </section>

        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Upload completion evidence</h3><span className="status-pill">1–5 files</span></div>
          <label className="upload-zone" style={{ display: 'block', cursor: 'pointer' }}><span className="feature-icon" style={{ margin: '0 auto' }}>▧</span><strong style={{ marginTop: 12 }}>Select photos, video or documents</strong><p>At least one clear image is required. You may also attach MP4, WebM, MOV, PDF, DOC or DOCX evidence.</p><input type="file" accept="image/jpeg,image/png,image/webp,video/mp4,video/webm,video/quicktime,application/pdf,.doc,.docx" multiple required hidden onChange={(e) => setEvidence(Array.from(e.target.files || []).slice(0,5))} /></label>
          {previews.length > 0 && <div className="image-preview-grid">{previews.map((item, index) => <div key={`${item.file.name}-${index}`} className="image-preview"><img src={item.url} alt={`Resolution preview ${index + 1}`} /></div>)}</div>}
          {evidence.length > 0 && <p className="muted">Selected: {evidence.map((file) => file.name).join(', ')}</p>}
        </section>

        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Work details</h3></div>
          <label className="form-label">Description of work<textarea className="input" style={{ minHeight: 130 }} required minLength="10" value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Describe the work completed, material used and final condition." /></label>
          <div className="form-grid" style={{ marginTop: 14 }}><label className="form-label">Contractor name (optional)<input className="input" value={contractor} onChange={(e) => setContractor(e.target.value)} placeholder="Contractor or municipal team" /></label><label className="form-label">Cost reference (optional)<input className="input" value={cost} onChange={(e) => setCost(e.target.value)} placeholder="₹ / reference amount" /></label></div>
        </section>

        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Location confirmation</h3></div>
          <div className="dashboard-grid main-aside"><div><p className="muted" style={{ fontSize: 10 }}>Capture GPS at the work site. This supports evidence verification and audit records.</p><button type="button" onClick={capture} className="button outline full">⌖ Capture completion location</button>{location.latitude && <div className="alert success">Captured: {Number(location.latitude).toFixed(6)}, {Number(location.longitude).toFixed(6)}</div>}</div><div className="map-placeholder"><span className="map-pin-dot p2" /><div className="map-overlay-card"><strong>Completion location</strong><p>{location.latitude ? `${Number(location.latitude).toFixed(6)}, ${Number(location.longitude).toFixed(6)}` : 'Capture GPS to confirm the work site.'}</p></div></div></div>
        </section>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 15 }}><Link to={`/officer/assignments/${complaintId}`} className="button outline large">Cancel</Link><button disabled={busy || evidence.length < 1} className="button success large">{busy ? 'Uploading evidence…' : 'Submit resolution for verification'}</button></div>
      </form>
    </section>
  );
}
