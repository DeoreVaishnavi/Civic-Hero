import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import SuccessPopup from '../../components/common/SuccessPopup.jsx';
import { useGeoLocation } from '../../hooks/useGeoLocation.js';
import { complaintApi } from '../../services/complaintApi.js';

const initialForm = { title: '', description: '', category: '', departmentId: '', wardId: '', latitude: '', longitude: '', address: '', possibleEmergency: false, emergencyReason: '', images: [] };

export default function ReportComplaint() {
  const navigate = useNavigate();
  const geo = useGeoLocation();
  const [metadata, setMetadata] = useState({ categories: [], departments: [], wards: [] });
  const [form, setForm] = useState(initialForm);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => { complaintApi.metadata().then(setMetadata).catch((reason) => setError(reason.message)); }, []);
  const wards = useMemo(() => metadata.wards.filter((ward) => String(ward.departmentId) === String(form.departmentId)), [metadata.wards, form.departmentId]);
  const previews = useMemo(() => form.images.map((file) => ({ file, url: URL.createObjectURL(file) })), [form.images]);
  useEffect(() => () => previews.forEach((item) => URL.revokeObjectURL(item.url)), [previews]);

  const change = (event) => { const { name, value, type, checked } = event.target; setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value })); };
  const chooseImages = (event) => {
    const selected = Array.from(event.target.files || []).slice(0, 5);
    const invalid = selected.find((file) => !['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024);
    if (invalid) { setError('Images must be JPEG, PNG or WebP and no larger than 5 MB each.'); return; }
    setError(''); setForm((current) => ({ ...current, images: selected }));
  };
  const removeImage = (index) => setForm((current) => ({ ...current, images: current.images.filter((_, currentIndex) => currentIndex !== index) }));
  const useLocation = async () => {
    try { const coordinates = await geo.locate(); setForm((current) => ({ ...current, latitude: coordinates.latitude, longitude: coordinates.longitude })); }
    catch { /* Hook already supplies a message. */ }
  };
  const submit = async (event) => {
    event.preventDefault(); setError(''); setSubmitting(true);
    try {
      const result = await complaintApi.create(form);
      setSuccess(`Complaint ${result.complaint.referenceNumber} was submitted successfully.`);
      setTimeout(() => navigate(`/citizen/complaints/${result.complaint.id}`, { replace: true }), 900);
    } catch (reason) { setError(reason.errors?.length ? reason.errors.join(' ') : reason.message); }
    finally { setSubmitting(false); }
  };

  return (
    <section className="page-wrap narrow">
      <div className="page-title-row"><div><p className="section-kicker">Citizen complaint form</p><h2>Report a civic issue</h2><p>Upload clear evidence and provide the exact location so the issue can be routed correctly.</p></div><Link to="/citizen" className="button outline">← Back to dashboard</Link></div>
      <SuccessPopup message={success} onClose={() => setSuccess('')} />
      {error && <div className="alert error">{error}</div>}
      {geo.error && <div className="alert warning">{geo.error}</div>}

      <form onSubmit={submit}>
        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Complaint details</h3><span className="status-pill">Step 1 of 3</span></div>
          <div className="form-grid">
            <label className="form-label">Issue title<input className="input" name="title" value={form.title} onChange={change} minLength="5" maxLength="200" required placeholder="Example: Garbage pile near city park" /></label>
            <label className="form-label">Category<select className="input" name="category" value={form.category} onChange={change} required><option value="">Select category</option>{metadata.categories.map((item) => <option key={item}>{item}</option>)}</select></label>
          </div>
          <label className="form-label" style={{ marginTop: 15 }}>Description<textarea className="input" style={{ minHeight: 125 }} name="description" value={form.description} onChange={change} minLength="20" maxLength="5000" required placeholder="Describe the exact problem, how long it has existed and why it needs attention." /></label>
          <div className="form-grid three" style={{ marginTop: 15 }}>
            <label className="form-label">Department<select className="input" name="departmentId" value={form.departmentId} onChange={(event) => setForm((current) => ({ ...current, departmentId: event.target.value, wardId: '' }))} required><option value="">Select department</option>{metadata.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
            <label className="form-label">Ward / area<select className="input" name="wardId" value={form.wardId} onChange={change} required disabled={!form.departmentId}><option value="">Select ward</option>{wards.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
            <label className="form-label">Landmark / address<input className="input" name="address" value={form.address} onChange={change} maxLength="500" required placeholder="Park Lane, near main gate" /></label>
          </div>
        </section>

        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Image upload and preview</h3><span className="status-pill">Step 2 of 3</span></div>
          <label className="upload-zone" style={{ display: 'block', cursor: 'pointer' }}>
            <span className="feature-icon" style={{ margin: '0 auto' }}>▧</span><strong style={{ marginTop: 12 }}>Upload issue photos</strong><p>Click to select up to five clear JPEG, PNG or WebP images. Maximum 5 MB per image.</p>
            <input type="file" accept="image/jpeg,image/png,image/webp" multiple onChange={chooseImages} hidden />
          </label>
          {previews.length > 0 && <div className="image-preview-grid">{previews.map((item, index) => <div key={`${item.file.name}-${index}`} className="image-preview"><img src={item.url} alt={`Complaint preview ${index + 1}`} /><button type="button" className="remove" onClick={() => removeImage(index)} aria-label={`Remove ${item.file.name}`}>×</button></div>)}</div>}
        </section>

        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Location confirmation</h3><span className="status-pill">Step 3 of 3</span></div>
          <div className="dashboard-grid main-aside">
            <div>
              <div className="form-grid">
                <label className="form-label">Latitude<input className="input" type="number" step="0.0000001" name="latitude" value={form.latitude} onChange={change} min="-90" max="90" required placeholder="19.234500" /></label>
                <label className="form-label">Longitude<input className="input" type="number" step="0.0000001" name="longitude" value={form.longitude} onChange={change} min="-180" max="180" required placeholder="72.678900" /></label>
              </div>
              <button type="button" onClick={useLocation} disabled={geo.loading} className="button outline full" style={{ marginTop: 13 }}>{geo.loading ? 'Capturing location…' : '⌖ Use my current location'}</button>
              <div className="duplicate-card" style={{ marginTop: 14 }}><strong>Duplicate issue protection</strong><p>CivicHero checks nearby complaints using location, text and image similarity. Supporting an existing issue avoids duplicate reports.</p><Link to="/citizen/nearby" className="button ghost small" style={{ marginTop: 8 }}>View common issues</Link></div>
            </div>
            <div className="map-placeholder"><span className="map-pin-dot p2" /><div className="map-overlay-card"><strong>Selected complaint location</strong><p>{form.address || 'Capture GPS or enter coordinates to confirm the location.'}</p></div></div>
          </div>

          <div style={{ marginTop: 15, paddingTop: 15, borderTop: '1px solid var(--civic-line)' }}>
            <label className="check-row"><input type="checkbox" name="possibleEmergency" checked={form.possibleEmergency} onChange={change} /><span><strong>Possible emergency requiring urgent official review</strong><small style={{ display: 'block', marginTop: 3, color: 'var(--civic-muted)' }}>Only a Supervisor or Admin can confirm High or Critical priority.</small></span></label>
            {form.possibleEmergency && <label className="form-label">Emergency reason<textarea className="input" style={{ minHeight: 90 }} name="emergencyReason" value={form.emergencyReason} onChange={change} placeholder="Explain the immediate risk to people, traffic or public infrastructure." /></label>}
          </div>
        </section>

        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10, marginTop: 15 }}><Link to="/citizen" className="button outline large">Cancel</Link><button disabled={submitting} className="button primary large">{submitting ? 'Sending complaint…' : 'Send complaint'}</button></div>
      </form>
    </section>
  );
}
