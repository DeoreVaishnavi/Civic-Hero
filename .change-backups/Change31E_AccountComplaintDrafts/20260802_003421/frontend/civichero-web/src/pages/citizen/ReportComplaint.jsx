import { useEffect, useMemo, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import SuccessPopup from '../../components/common/SuccessPopup.jsx';
import ComplaintLocationPicker from '../../components/maps/ComplaintLocationPicker.jsx';
import { useGeoLocation } from '../../hooks/useGeoLocation.js';
import { complaintApi } from '../../services/complaintApi.js';

const DRAFT_KEY = 'civichero:citizen-complaint-draft:v2';
const allowedEvidenceTypes = [
  'image/jpeg', 'image/png', 'image/webp',
  'video/mp4', 'video/webm', 'video/quicktime',
  'application/pdf', 'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
];

const initialForm = {
  title: '',
  description: '',
  category: '',
  citizenSeverity: 'Medium',
  departmentId: '',
  wardId: '',
  latitude: '',
  longitude: '',
  address: '',
  landmark: '',
  possibleEmergency: false,
  emergencyReason: '',
  evidence: [],
};

function validationErrors(form, metadata) {
  const errors = [];
  const latitude = Number(form.latitude);
  const longitude = Number(form.longitude);
  const department = metadata.departments.find((item) => String(item.id) === String(form.departmentId));
  const ward = metadata.wards.find((item) => String(item.id) === String(form.wardId));
  const totalBytes = form.evidence.reduce((sum, file) => sum + file.size, 0);

  if (form.title.trim().length < 5 || form.title.trim().length > 200) errors.push('Issue title must contain 5 to 200 characters.');
  if (form.description.trim().length < 20 || form.description.trim().length > 5000) errors.push('Description must contain 20 to 5000 characters.');
  if (!metadata.categories.includes(form.category)) errors.push('Select a valid complaint category.');
  if (!['Low', 'Medium', 'High', 'Critical'].includes(form.citizenSeverity)) errors.push('Select the reported severity.');
  if (!department) errors.push('Select an active department.');
  if (!ward || String(ward.departmentId) !== String(form.departmentId)) errors.push('Select a ward belonging to the chosen department.');
  if (form.address.trim().length < 5 || form.address.trim().length > 400) errors.push('Address must contain 5 to 400 characters.');
  if (form.landmark.trim().length > 100) errors.push('Landmark cannot exceed 100 characters.');
  if (form.latitude === '' || !Number.isFinite(latitude) || latitude < -90 || latitude > 90) errors.push('Enter a valid latitude between -90 and 90.');
  if (form.longitude === '' || !Number.isFinite(longitude) || longitude < -180 || longitude > 180) errors.push('Enter a valid longitude between -180 and 180.');
  if (form.possibleEmergency && (form.emergencyReason.trim().length < 10 || form.emergencyReason.trim().length > 1000)) errors.push('Emergency reason must contain 10 to 1000 characters.');
  if (form.evidence.length > 8) errors.push('Upload a maximum of eight evidence files.');
  if (totalBytes > 25 * 1024 * 1024) errors.push('Total evidence size cannot exceed 25 MB.');

  form.evidence.forEach((file) => {
    if (!allowedEvidenceTypes.includes(file.type)) errors.push(`${file.name} is not a supported evidence format.`);
    if (file.size <= 0 || file.size > 15 * 1024 * 1024) errors.push(`${file.name} must be between 1 byte and 15 MB.`);
  });
  return [...new Set(errors)];
}

function analysisFingerprint(form) {
  return JSON.stringify({
    title: form.title.trim(), description: form.description.trim(), category: form.category,
    latitude: Number(form.latitude), longitude: Number(form.longitude),
  });
}

function formatPercent(value) {
  const number = Number(value);
  return Number.isFinite(number) ? `${Math.round(number * 100)}%` : '—';
}

export default function ReportComplaint() {
  const navigate = useNavigate();
  const geo = useGeoLocation();
  const cameraInputRef = useRef(null);
  const [metadata, setMetadata] = useState({ categories: [], departments: [], wards: [] });
  const [metadataLoading, setMetadataLoading] = useState(true);
  const [metadataError, setMetadataError] = useState('');
  const [form, setForm] = useState(initialForm);
  const [draftReady, setDraftReady] = useState(false);
  const [draftMessage, setDraftMessage] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [analyzing, setAnalyzing] = useState(false);
  const [analysis, setAnalysis] = useState(null);
  const [analyzedFingerprint, setAnalyzedFingerprint] = useState('');
  const [duplicateOverride, setDuplicateOverride] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const loadMetadata = async () => {
    setMetadataLoading(true);
    setMetadataError('');
    try {
      const result = await complaintApi.metadata();
      setMetadata({
        categories: result?.categories || [],
        departments: result?.departments || [],
        wards: result?.wards || [],
      });
    } catch (reason) {
      setMetadataError(reason.message || 'Unable to load complaint categories and areas.');
    } finally {
      setMetadataLoading(false);
    }
  };

  useEffect(() => { loadMetadata(); }, []);

  useEffect(() => {
    try {
      const stored = JSON.parse(localStorage.getItem(DRAFT_KEY) || 'null');
      if (stored?.form) {
        setForm((current) => ({ ...current, ...stored.form, evidence: [] }));
        setDraftMessage(`Draft restored${stored.savedAt ? ` from ${new Date(stored.savedAt).toLocaleString()}` : ''}. Evidence files must be selected again for security.`);
      }
    } catch {
      localStorage.removeItem(DRAFT_KEY);
    } finally {
      setDraftReady(true);
    }
  }, []);

  useEffect(() => {
    if (!draftReady) return undefined;
    const timer = window.setTimeout(() => {
      const { evidence, ...draftForm } = form;
      localStorage.setItem(DRAFT_KEY, JSON.stringify({ form: draftForm, savedAt: new Date().toISOString() }));
      setDraftMessage('Draft saved automatically in this browser.');
    }, 800);
    return () => window.clearTimeout(timer);
  }, [form, draftReady]);

  const wards = useMemo(
    () => metadata.wards.filter((ward) => String(ward.departmentId) === String(form.departmentId)),
    [metadata.wards, form.departmentId],
  );

  const previews = useMemo(
    () => form.evidence
      .filter((file) => file.type.startsWith('image/') || file.type.startsWith('video/'))
      .map((file) => ({ file, url: URL.createObjectURL(file) })),
    [form.evidence],
  );
  useEffect(() => () => previews.forEach((item) => URL.revokeObjectURL(item.url)), [previews]);

  const change = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
    setAnalyzedFingerprint('');
    setDuplicateOverride(false);
    setError('');
  };

  const appendEvidence = (selected) => {
    const incoming = Array.from(selected || []);
    const combined = [...form.evidence, ...incoming];
    const invalid = incoming.find((file) => !allowedEvidenceTypes.includes(file.type) || file.size <= 0 || file.size > 15 * 1024 * 1024);
    if (invalid) {
      setError('Evidence must be JPEG, PNG, WebP, MP4, WebM, MOV, PDF, DOC or DOCX, with a maximum size of 15 MB each.');
      return;
    }
    if (combined.length > 8) {
      setError('Select a maximum of eight evidence files.');
      return;
    }
    if (combined.reduce((sum, file) => sum + file.size, 0) > 25 * 1024 * 1024) {
      setError('The total evidence size cannot exceed 25 MB.');
      return;
    }
    setForm((current) => ({ ...current, evidence: combined }));
    setError('');
  };

  const removeEvidence = (index) => setForm((current) => ({
    ...current,
    evidence: current.evidence.filter((_, currentIndex) => currentIndex !== index),
  }));

  const useLocation = async () => {
    try {
      const coordinates = await geo.locate();
      setForm((current) => ({
        ...current,
        latitude: Number(coordinates.latitude).toFixed(7),
        longitude: Number(coordinates.longitude).toFixed(7),
      }));
      setAnalyzedFingerprint('');
      setError('');
    } catch {
      // The geolocation hook already provides a useful error message.
    }
  };

  const setMapLocation = (latitude, longitude) => {
    setForm((current) => ({
      ...current,
      latitude: Number(latitude).toFixed(7),
      longitude: Number(longitude).toFixed(7),
    }));
    setAnalyzedFingerprint('');
    setDuplicateOverride(false);
  };

  const saveDraftNow = () => {
    const { evidence, ...draftForm } = form;
    localStorage.setItem(DRAFT_KEY, JSON.stringify({ form: draftForm, savedAt: new Date().toISOString() }));
    setDraftMessage('Draft saved. You can safely leave this page and continue later in this browser.');
  };

  const clearDraft = () => {
    localStorage.removeItem(DRAFT_KEY);
    setForm(initialForm);
    setAnalysis(null);
    setAnalyzedFingerprint('');
    setDraftMessage('Draft cleared.');
  };

  const runAiReview = async () => {
    const latitude = Number(form.latitude);
    const longitude = Number(form.longitude);
    if (form.title.trim().length < 5 || form.description.trim().length < 20 || !Number.isFinite(latitude) || !Number.isFinite(longitude)) {
      setError('Enter the title, description and exact location before requesting AI suggestions.');
      return null;
    }

    setAnalyzing(true);
    setError('');
    const payload = {
      title: form.title.trim(),
      description: form.description.trim(),
      category: form.category || null,
      latitude,
      longitude,
    };
    try {
      const result = await complaintApi.preSubmissionReview(payload);
      setAnalysis({ ...result, warning: '' });
      setAnalyzedFingerprint(analysisFingerprint(form));
      return result;
    } catch (reason) {
      const result = { classification: null, duplicate: null, priority: null, warning: reason.message || 'AI suggestions are currently unavailable. You may continue with manual review.' };
      setAnalysis(result);
      setAnalyzedFingerprint(analysisFingerprint(form));
      return result;
    } finally {
      setAnalyzing(false);
    }
  };

  const useSuggestedCategory = () => {
    const suggested = analysis?.classification?.category;
    if (suggested && metadata.categories.includes(suggested)) {
      setForm((current) => ({ ...current, category: suggested }));
      setAnalyzedFingerprint('');
    }
  };

  const supportExisting = async (complaintId) => {
    try {
      await complaintApi.upvote(complaintId);
      navigate(`/citizen/complaints/${complaintId}`);
    } catch (reason) {
      setError(reason.message || 'The existing complaint could not be supported.');
    }
  };

  const submit = async (event) => {
    event.preventDefault();
    if (submitting || analyzing) return;
    setError('');
    setSuccess('');
    setUploadProgress(0);

    if (metadataLoading || metadataError) {
      setError(metadataError ? 'Complaint metadata is unavailable. Retry loading it before submitting.' : 'Complaint categories and areas are still loading.');
      return;
    }

    const errors = validationErrors(form, metadata);
    if (errors.length) {
      setError(errors.join(' '));
      globalThis.scrollTo?.({ top: 0, behavior: 'smooth' });
      return;
    }

    const fingerprint = analysisFingerprint(form);
    if (analyzedFingerprint !== fingerprint) {
      await runAiReview();
      setError('AI pre-submission checks have been refreshed. Review the suggestions below, then click Send complaint again.');
      globalThis.scrollTo?.({ top: 0, behavior: 'smooth' });
      return;
    }

    const duplicateScore = Number(analysis?.duplicate?.score || 0);
    const matchingComplaintId = analysis?.duplicate?.matchingComplaintId || analysis?.duplicate?.matches?.[0]?.complaintId;
    if (duplicateScore >= 0.75 && matchingComplaintId && !duplicateOverride) {
      setError('A strong possible duplicate was found. Support the existing issue, or confirm that this is a different issue before submitting.');
      globalThis.scrollTo?.({ top: 0, behavior: 'smooth' });
      return;
    }

    setSubmitting(true);
    try {
      const request = {
        ...form,
        title: form.title.trim(),
        description: form.description.trim(),
        address: form.address.trim(),
        landmark: form.landmark.trim(),
        emergencyReason: form.emergencyReason.trim(),
        latitude: Number(form.latitude),
        longitude: Number(form.longitude),
      };
      const result = await complaintApi.create(request, {
        onUploadProgress: (progressEvent) => {
          if (!progressEvent.total) return;
          setUploadProgress(Math.min(100, Math.round((progressEvent.loaded * 100) / progressEvent.total)));
        },
      });
      if (!result?.complaint?.id || !result?.complaint?.referenceNumber) throw new Error('The server did not confirm the new complaint.');
      localStorage.removeItem(DRAFT_KEY);
      setUploadProgress(100);
      setSuccess(`Complaint ${result.complaint.referenceNumber} was submitted successfully.`);
      globalThis.setTimeout(() => navigate(`/citizen/complaints/${result.complaint.id}`, { replace: true }), 900);
    } catch (reason) {
      const details = reason.errors?.length ? reason.errors.join(' ') : reason.message;
      setError(reason.isTimeout
        ? 'Submission took too long. Check My Complaints before retrying so you do not create a duplicate.'
        : details || 'The complaint could not be submitted.');
      globalThis.scrollTo?.({ top: 0, behavior: 'smooth' });
    } finally {
      setSubmitting(false);
    }
  };

  const duplicateMatches = analysis?.duplicate?.matches || [];
  const strongestDuplicate = analysis?.duplicate?.matchingComplaintId || duplicateMatches[0]?.complaintId;
  const lowConfidence = analysis?.classification && Number(analysis.classification.confidence) < 0.7;
  const submitLabel = submitting
    ? (uploadProgress > 0 && uploadProgress < 100 ? `Uploading evidence ${uploadProgress}%…` : 'Saving complaint…')
    : 'Send complaint';

  return (
    <section className="page-wrap narrow">
      <div className="page-title-row">
        <div>
          <p className="section-kicker">Citizen complaint form</p>
          <h2>Report a civic issue</h2>
          <p>Save a draft, select the exact map location, add supporting media and review AI suggestions before submission.</p>
        </div>
        <div className="page-actions">
          <button type="button" onClick={saveDraftNow} className="button outline">Save draft</button>
          <Link to="/citizen" className="button outline">← Dashboard</Link>
        </div>
      </div>

      <SuccessPopup message={success} onClose={() => setSuccess('')} />
      {error && <div className="alert error" role="alert">{error}</div>}
      {draftMessage && <div className="alert info"><span>{draftMessage}</span> <button type="button" className="button ghost small" onClick={clearDraft}>Clear draft</button></div>}
      {geo.error && <div className="alert warning">{geo.error}</div>}
      {metadataError && <div className="alert error"><span>{metadataError}</span> <button type="button" className="button ghost small" onClick={loadMetadata}>Retry</button></div>}

      <form onSubmit={submit} noValidate>
        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Complaint details</h3><span className="status-pill">Step 1 of 4</span></div>
          <div className="form-grid">
            <label className="form-label">Issue title<input className="input" name="title" value={form.title} onChange={change} minLength="5" maxLength="200" required placeholder="Example: Garbage pile blocking the footpath" /></label>
            <label className="form-label">Category<select className="input" name="category" value={form.category} onChange={change} required disabled={metadataLoading || Boolean(metadataError)}><option value="">Select category</option>{metadata.categories.map((item) => <option key={item}>{item}</option>)}</select></label>
          </div>
          <label className="form-label" style={{ marginTop: 15 }}>Description<textarea className="input" style={{ minHeight: 125 }} name="description" value={form.description} onChange={change} minLength="20" maxLength="5000" required placeholder="Describe the problem, its duration, impact and visible risks." /></label>
          <div className="form-grid three" style={{ marginTop: 15 }}>
            <label className="form-label">Reported severity<select className="input" name="citizenSeverity" value={form.citizenSeverity} onChange={change}><option>Low</option><option>Medium</option><option>High</option><option>Critical</option></select><small className="muted">This is your assessment; official priority is decided through triage.</small></label>
            <label className="form-label">Department<select className="input" name="departmentId" value={form.departmentId} onChange={(event) => { setForm((current) => ({ ...current, departmentId: event.target.value, wardId: '' })); setAnalyzedFingerprint(''); }} required><option value="">Select department</option>{metadata.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
            <label className="form-label">Ward / area<select className="input" name="wardId" value={form.wardId} onChange={change} required disabled={!form.departmentId}><option value="">Select ward</option>{wards.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
          </div>
          <div className="form-grid" style={{ marginTop: 15 }}>
            <label className="form-label">Address<input className="input" name="address" value={form.address} onChange={change} minLength="5" maxLength="400" required placeholder="Road, locality and nearby area" /></label>
            <label className="form-label">Landmark<input className="input" name="landmark" value={form.landmark} onChange={change} maxLength="100" placeholder="Near school gate, bus stop, shop, etc." /></label>
          </div>
        </section>

        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Evidence and camera</h3><span className="status-pill">Step 2 of 4</span></div>
          <div className="form-grid">
            <label className="upload-zone" style={{ display: 'block', cursor: 'pointer' }}>
              <strong>Upload images, video or documents</strong>
              <p>Up to eight files. JPEG, PNG, WebP, MP4, WebM, MOV, PDF, DOC or DOCX. Maximum 15 MB each and 25 MB total.</p>
              <input type="file" accept="image/jpeg,image/png,image/webp,video/mp4,video/webm,video/quicktime,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document" multiple onChange={(event) => { appendEvidence(event.target.files); event.target.value = ''; }} hidden />
            </label>
            <div className="upload-zone" style={{ display: 'grid', placeItems: 'center' }}>
              <strong>Take a photo now</strong>
              <p>Open the device camera using the dedicated rear-camera workflow.</p>
              <button type="button" className="button primary" onClick={() => cameraInputRef.current?.click()}>Open camera</button>
              <input ref={cameraInputRef} type="file" accept="image/jpeg,image/png,image/webp" capture="environment" onChange={(event) => { appendEvidence(event.target.files); event.target.value = ''; }} hidden />
            </div>
          </div>
          {form.evidence.length > 0 && <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3" style={{ marginTop: 16 }}>
            {form.evidence.map((file, index) => {
              const preview = previews.find((item) => item.file === file);
              return <article key={`${file.name}-${file.lastModified}-${index}`} className="rounded-2xl border border-slate-200 bg-white p-3 shadow-sm">
                {file.type.startsWith('image/') && preview && <img src={preview.url} alt={file.name} className="h-40 w-full rounded-xl object-cover" />}
                {file.type.startsWith('video/') && preview && <video src={preview.url} controls className="h-40 w-full rounded-xl bg-slate-950 object-cover" />}
                {!file.type.startsWith('image/') && !file.type.startsWith('video/') && <div className="grid h-40 place-items-center rounded-xl bg-slate-100 text-center"><div><strong>Document</strong><p className="text-xs text-slate-500">{file.type || 'Supporting file'}</p></div></div>}
                <div className="mt-3 flex items-center justify-between gap-2 text-xs"><span className="truncate">{file.name}</span><button type="button" className="font-bold text-red-700" onClick={() => removeEvidence(index)}>Remove</button></div>
              </article>;
            })}
          </div>}
        </section>

        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Exact location</h3><span className="status-pill">Step 3 of 4</span></div>
          <div className="form-grid">
            <label className="form-label">Latitude<input className="input" type="number" step="0.0000001" name="latitude" value={form.latitude} onChange={change} min="-90" max="90" required /></label>
            <label className="form-label">Longitude<input className="input" type="number" step="0.0000001" name="longitude" value={form.longitude} onChange={change} min="-180" max="180" required /></label>
          </div>
          <button type="button" onClick={useLocation} disabled={geo.loading} className="button outline" style={{ margin: '13px 0' }}>{geo.loading ? 'Capturing location…' : '⌖ Use my current location'}</button>
          <ComplaintLocationPicker latitude={form.latitude} longitude={form.longitude} onChange={setMapLocation} />
          <div style={{ marginTop: 15, paddingTop: 15, borderTop: '1px solid var(--civic-line)' }}>
            <label className="check-row"><input type="checkbox" name="possibleEmergency" checked={form.possibleEmergency} onChange={change} /><span><strong>Possible emergency requiring urgent official review</strong><small style={{ display: 'block', marginTop: 3, color: 'var(--civic-muted)' }}>Critical priority cannot be self-assigned; an authorized reviewer confirms it.</small></span></label>
            {form.possibleEmergency && <label className="form-label">Emergency reason<textarea className="input" style={{ minHeight: 90 }} name="emergencyReason" value={form.emergencyReason} onChange={change} minLength="10" maxLength="1000" required /></label>}
          </div>
        </section>

        <section className="surface form-section">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>AI pre-submission review</h3><span className="status-pill">Step 4 of 4</span></div>
          <p className="muted">Check the suggested category, confidence, likely priority and nearby duplicate complaints before creating a new issue.</p>
          <button type="button" onClick={runAiReview} disabled={analyzing} className="button outline" style={{ marginTop: 10 }}>{analyzing ? 'Running checks…' : 'Review AI suggestions'}</button>
          {analysis && <div style={{ marginTop: 16 }}>
            {analysis.warning && <div className="alert warning">{analysis.warning}</div>}
            <div className="stat-grid" style={{ gridTemplateColumns: 'repeat(3,minmax(0,1fr))' }}>
              <Info label="Suggested category" value={analysis.classification?.category || 'Unavailable'} detail={`Confidence ${formatPercent(analysis.classification?.confidence)}`} />
              <Info label="Possible duplicate" value={analysis.duplicate?.status || 'Unavailable'} detail={`Score ${formatPercent(analysis.duplicate?.score)}`} />
              <Info label="Predicted priority" value={analysis.priority?.priority || 'Unavailable'} detail={`Confidence ${formatPercent(analysis.priority?.score)}`} />
            </div>
            {lowConfidence && <div className="alert warning" style={{ marginTop: 12 }}>AI confidence is below 70%. Your complaint may require manual category review.</div>}
            {analysis.classification?.category && metadata.categories.includes(analysis.classification.category) && analysis.classification.category !== form.category && <button type="button" className="button soft" onClick={useSuggestedCategory} style={{ marginTop: 12 }}>Use suggested category: {analysis.classification.category}</button>}
            {duplicateMatches.length > 0 && <div className="duplicate-card" style={{ marginTop: 14 }}><strong>Nearby matching issues</strong>{duplicateMatches.slice(0, 3).map((match) => <div key={match.complaintId} style={{ marginTop: 10, paddingTop: 10, borderTop: '1px solid #e9d69f' }}><p><b>{match.title}</b> · {Math.round(Number(match.distanceMeters || 0))} metres away · match {formatPercent(match.combinedScore)}</p><div className="page-actions" style={{ marginTop: 8 }}><Link className="button ghost small" to={`/citizen/complaints/${match.complaintId}`}>View issue</Link><button type="button" className="button primary small" onClick={() => supportExisting(match.complaintId)}>Support existing issue instead</button></div></div>)}</div>}
            {Number(analysis.duplicate?.score || 0) >= 0.75 && strongestDuplicate && <label className="check-row" style={{ marginTop: 14 }}><input type="checkbox" checked={duplicateOverride} onChange={(event) => setDuplicateOverride(event.target.checked)} /><span><strong>This is a different issue</strong><small style={{ display: 'block', color: 'var(--civic-muted)' }}>Confirm only after reviewing the matching complaint.</small></span></label>}
          </div>}
        </section>

        <div style={{ display: 'flex', justifyContent: 'flex-end', flexWrap: 'wrap', gap: 10, marginTop: 15 }}>
          <button type="button" onClick={saveDraftNow} className="button outline large">Save and continue later</button>
          <Link to="/citizen" className="button outline large">Cancel</Link>
          <button type="submit" disabled={submitting || analyzing || metadataLoading || Boolean(metadataError)} className="button primary large">{submitLabel}</button>
        </div>
      </form>
    </section>
  );
}

function Info({ label, value, detail }) {
  return <article className="stat-card"><small>{label}</small><strong>{value}</strong><p className="muted" style={{ marginTop: 4, fontSize: 10 }}>{detail}</p></article>;
}
