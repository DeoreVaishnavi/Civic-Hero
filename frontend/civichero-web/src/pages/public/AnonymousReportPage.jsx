import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import CaptchaWidget from '../../components/common/CaptchaWidget.jsx';
import { useGeoLocation } from '../../hooks/useGeoLocation.js';
import { anonymousComplaintApi } from '../../services/anonymousComplaintApi.js';
import { complaintApi } from '../../services/complaintApi.js';
import { ROUTE_PATHS } from '../../routes/routePaths.js';

const MAX_IMAGES = 5;
const MAX_IMAGE_BYTES = 5 * 1024 * 1024;
const ALLOWED_IMAGE_TYPES = new Set(['image/jpeg', 'image/png', 'image/webp']);

const initial = {
  title: '',
  description: '',
  category: '',
  departmentId: '',
  wardId: '',
  latitude: '',
  longitude: '',
  address: '',
  contactEmail: '',
  contactPhone: '',
  consentToLimitedContactStorage: false,
  possibleEmergency: false,
  emergencyReason: '',
  captchaToken: '',
  images: [],
};

function validate(form) {
  const errors = [];
  const latitude = Number(form.latitude);
  const longitude = Number(form.longitude);

  if (form.title.trim().length < 5 || form.title.trim().length > 200) errors.push('Title must contain 5 to 200 characters.');
  if (form.description.trim().length < 20 || form.description.trim().length > 5000) errors.push('Description must contain 20 to 5000 characters.');
  if (!form.category) errors.push('Select a complaint category.');
  if (!form.departmentId) errors.push('Select a department.');
  if (!form.wardId) errors.push('Select a ward.');
  if (form.address.trim().length < 5 || form.address.trim().length > 500) errors.push('Address or landmark must contain 5 to 500 characters.');
  if (!Number.isFinite(latitude) || latitude < -90 || latitude > 90) errors.push('Enter a valid latitude between -90 and 90.');
  if (!Number.isFinite(longitude) || longitude < -180 || longitude > 180) errors.push('Enter a valid longitude between -180 and 180.');
  if (form.images.length > MAX_IMAGES) errors.push(`Upload at most ${MAX_IMAGES} evidence images.`);

  form.images.forEach((file) => {
    if (!ALLOWED_IMAGE_TYPES.has(file.type)) errors.push(`${file.name}: only JPEG, PNG and WebP images are accepted.`);
    if (file.size <= 0 || file.size > MAX_IMAGE_BYTES) errors.push(`${file.name}: image size must be between 1 byte and 5 MB.`);
  });

  if ((form.contactEmail || form.contactPhone) && !form.consentToLimitedContactStorage) {
    errors.push('Consent is required before optional contact details can be stored.');
  }
  if (form.possibleEmergency && form.emergencyReason.trim().length < 10) {
    errors.push('Explain the immediate safety risk using at least 10 characters.');
  }
  if (!form.captchaToken) errors.push('Complete CAPTCHA verification before submitting.');

  return [...new Set(errors)];
}

export default function AnonymousReportPage() {
  const geo = useGeoLocation();
  const errorRef = useRef(null);
  const fileInputRef = useRef(null);
  const [metadata, setMetadata] = useState({ categories: [], departments: [], wards: [] });
  const [metadataLoading, setMetadataLoading] = useState(true);
  const [form, setForm] = useState(initial);
  const [result, setResult] = useState(null);
  const [errors, setErrors] = useState([]);
  const [submitting, setSubmitting] = useState(false);
  const [captchaStatus, setCaptchaStatus] = useState({ kind: 'loading', message: '' });
  const [captchaResetKey, setCaptchaResetKey] = useState(0);

  useEffect(() => {
    let active = true;
    complaintApi.metadata()
      .then((data) => {
        if (active) setMetadata(data || { categories: [], departments: [], wards: [] });
      })
      .catch((error) => {
        if (active) setErrors([error.message || 'Complaint form options could not be loaded.']);
      })
      .finally(() => {
        if (active) setMetadataLoading(false);
      });
    return () => { active = false; };
  }, []);

  useEffect(() => {
    if (errors.length > 0) errorRef.current?.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }, [errors]);

  const wards = useMemo(
    () => metadata.wards.filter((ward) => String(ward.departmentId) === String(form.departmentId)),
    [metadata.wards, form.departmentId],
  );

  const captcha = useCallback((captchaToken) => {
    setForm((current) => ({ ...current, captchaToken }));
  }, []);

  const captchaState = useCallback((status) => setCaptchaStatus(status), []);

  const change = (event) => {
    const { name, value, type, checked } = event.target;
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }));
  };

  const selectImages = (event) => {
    const files = Array.from(event.target.files || []);
    const selected = files.slice(0, MAX_IMAGES);
    const fileErrors = [];

    if (files.length > MAX_IMAGES) fileErrors.push(`Only the first ${MAX_IMAGES} images were selected.`);
    selected.forEach((file) => {
      if (!ALLOWED_IMAGE_TYPES.has(file.type)) fileErrors.push(`${file.name}: unsupported image format.`);
      if (file.size <= 0 || file.size > MAX_IMAGE_BYTES) fileErrors.push(`${file.name}: image exceeds the 5 MB limit.`);
    });

    setErrors(fileErrors);
    setForm((current) => ({ ...current, images: selected }));
  };

  const removeImage = (index) => {
    setForm((current) => ({ ...current, images: current.images.filter((_, itemIndex) => itemIndex !== index) }));
    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const locate = async () => {
    setErrors([]);
    try {
      const position = await geo.locate();
      setForm((current) => ({
        ...current,
        latitude: String(position.latitude),
        longitude: String(position.longitude),
      }));
    } catch {
      // The geolocation hook exposes the user-facing error.
    }
  };

  const submit = async (event) => {
    event.preventDefault();
    if (submitting) return;

    const validationErrors = validate(form);
    if (validationErrors.length > 0) {
      setErrors(validationErrors);
      return;
    }

    setSubmitting(true);
    setErrors([]);
    try {
      const created = await anonymousComplaintApi.create({
        ...form,
        title: form.title.trim(),
        description: form.description.trim(),
        address: form.address.trim(),
        contactEmail: form.contactEmail.trim(),
        contactPhone: form.contactPhone.trim(),
        emergencyReason: form.emergencyReason.trim(),
      });

      if (!created?.referenceNumber || !created?.trackingToken) {
        throw new Error('The backend accepted the complaint but did not return tracking details.');
      }
      setResult(created);
    } catch (error) {
      const apiErrors = Array.isArray(error.errors) && error.errors.length > 0
        ? error.errors
        : [error.message || 'Anonymous complaint submission failed.'];
      setErrors(apiErrors);
      setCaptchaResetKey((value) => value + 1);
    } finally {
      setSubmitting(false);
    }
  };

  if (result) {
    return (
      <section className="mx-auto max-w-2xl px-6 py-14">
        <div className="rounded-3xl border border-emerald-400/30 bg-emerald-400/10 p-8">
          <p className="text-sm font-bold uppercase tracking-widest text-emerald-200">Submitted securely</p>
          <h1 className="mt-2 text-3xl font-black text-white">Save your tracking details</h1>
          <p className="mt-3 text-slate-200">Anonymous complaints cannot earn points, verify closure or raise an account-based appeal. The token is shown only once.</p>
          <dl className="mt-6 space-y-4 rounded-2xl bg-slate-950/60 p-5">
            <Row label="Reference" value={result.referenceNumber} />
            <Row label="Tracking token" value={result.trackingToken} mono />
            <Row label="Expires" value={new Date(result.trackingExpiresAtUtc).toLocaleString()} />
          </dl>
          <div className="mt-6 flex flex-wrap gap-3">
            <button type="button" onClick={() => navigator.clipboard?.writeText(result.trackingToken)} className="rounded-xl border border-emerald-400/30 px-5 py-3 font-bold text-emerald-100">Copy tracking token</button>
            <Link to={`${ROUTE_PATHS.anonymousTrack}?reference=${encodeURIComponent(result.referenceNumber)}`} state={{ trackingToken: result.trackingToken }} className="rounded-xl bg-emerald-500 px-5 py-3 font-black text-white">Track now</Link>
            <Link to={ROUTE_PATHS.home} className="rounded-xl border border-white/15 px-5 py-3 font-bold text-slate-200">Back home</Link>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="mx-auto max-w-4xl px-6 py-12">
      <div className="mb-6">
        <p className="text-sm font-bold uppercase tracking-widest text-emerald-300">Limited anonymous access</p>
        <h1 className="mt-2 text-3xl font-black text-white">Report without creating an account</h1>
        <p className="mt-2 text-slate-400">CAPTCHA and stricter rate limits protect the service. Contact details are optional and encrypted only with your consent.</p>
      </div>

      {errors.length > 0 && (
        <div ref={errorRef} className="mb-5 rounded-xl border border-rose-400/30 bg-rose-500/10 p-4 text-rose-100" role="alert">
          <p className="font-bold">The complaint was not submitted:</p>
          <ul className="mt-2 list-disc space-y-1 pl-5 text-sm">
            {errors.map((error) => <li key={error}>{error}</li>)}
          </ul>
        </div>
      )}
      {geo.error && <p className="mb-5 rounded-xl bg-amber-500/10 p-4 text-amber-100">{geo.error}</p>}

      <form noValidate onSubmit={submit} className="space-y-6 rounded-3xl border border-white/10 bg-white/[0.04] p-6 lg:p-8">
        <div className="grid gap-5 md:grid-cols-2">
          <Field label="Title"><input className="input" name="title" value={form.title} onChange={change} maxLength="200" /></Field>
          <Field label="Category"><select className="input" name="category" value={form.category} onChange={change} disabled={metadataLoading}><option value="">Select category</option>{metadata.categories.map((item) => <option key={item} value={item}>{item}</option>)}</select></Field>
        </div>

        <Field label="Description"><textarea className="input min-h-36" name="description" value={form.description} onChange={change} maxLength="5000" /></Field>

        <div className="grid gap-5 md:grid-cols-2">
          <Field label="Department"><select className="input" name="departmentId" value={form.departmentId} onChange={(event) => setForm((current) => ({ ...current, departmentId: event.target.value, wardId: '' }))} disabled={metadataLoading}><option value="">Select department</option>{metadata.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
          <Field label="Ward"><select className="input" name="wardId" value={form.wardId} onChange={change} disabled={!form.departmentId || metadataLoading}><option value="">Select ward</option>{wards.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
        </div>

        <Field label="Address or landmark"><input className="input" name="address" value={form.address} onChange={change} maxLength="500" /></Field>

        <div className="rounded-2xl border border-white/10 bg-slate-950/50 p-5">
          <div className="flex flex-wrap justify-between gap-3">
            <div><h2 className="font-black text-white">Location</h2><p className="text-sm text-slate-400">Required for routing and duplicate detection.</p></div>
            <button type="button" onClick={locate} className="rounded-lg border border-sky-400/30 px-4 py-2 font-bold text-sky-200">Use current location</button>
          </div>
          <div className="mt-4 grid gap-4 md:grid-cols-2">
            <Field label="Latitude"><input className="input" type="number" step="0.0000001" name="latitude" value={form.latitude} onChange={change} /></Field>
            <Field label="Longitude"><input className="input" type="number" step="0.0000001" name="longitude" value={form.longitude} onChange={change} /></Field>
          </div>
        </div>

        <Field label="Evidence images (maximum five)">
          <input ref={fileInputRef} className="input" type="file" multiple accept="image/jpeg,image/png,image/webp" onChange={selectImages} />
        </Field>
        {form.images.length > 0 && (
          <ul className="space-y-2 rounded-2xl border border-white/10 bg-slate-950/40 p-4">
            {form.images.map((file, index) => (
              <li key={`${file.name}-${file.lastModified}`} className="flex items-center justify-between gap-4 text-sm text-slate-200">
                <span className="min-w-0 truncate">{file.name} · {(file.size / 1024 / 1024).toFixed(2)} MB</span>
                <button type="button" onClick={() => removeImage(index)} className="font-bold text-rose-300">Remove</button>
              </li>
            ))}
          </ul>
        )}

        <div className="rounded-2xl border border-amber-400/25 bg-amber-400/5 p-5">
          <label className="flex items-start gap-3">
            <input type="checkbox" name="possibleEmergency" checked={form.possibleEmergency} onChange={change} className="mt-1" />
            <span><strong className="text-amber-100">Possible emergency requiring urgent official review</strong><span className="mt-1 block text-sm text-amber-200/70">This does not automatically make the complaint Critical.</span></span>
          </label>
          {form.possibleEmergency && <textarea className="input mt-4 min-h-24" name="emergencyReason" value={form.emergencyReason} onChange={change} placeholder="Explain the immediate safety risk" maxLength="1000" />}
        </div>

        <div className="grid gap-5 md:grid-cols-2">
          <Field label="Contact email (optional)"><input className="input" type="email" name="contactEmail" value={form.contactEmail} onChange={change} /></Field>
          <Field label="Contact phone (optional)"><input className="input" name="contactPhone" value={form.contactPhone} onChange={change} placeholder="+919876543210" /></Field>
        </div>

        {(form.contactEmail || form.contactPhone) && (
          <label className="flex items-start gap-3 text-sm text-slate-300">
            <input type="checkbox" name="consentToLimitedContactStorage" checked={form.consentToLimitedContactStorage} onChange={change} className="mt-1" />
            <span>I consent to encrypted, limited contact storage for official updates. It is not shown publicly.</span>
          </label>
        )}

        <CaptchaWidget onToken={captcha} onStatus={captchaState} resetKey={captchaResetKey} />

        <button
          type="submit"
          disabled={submitting || metadataLoading || captchaStatus.kind === 'blocked'}
          className="w-full rounded-xl bg-emerald-500 px-5 py-3 font-black text-white disabled:cursor-not-allowed disabled:opacity-50"
        >
          {metadataLoading ? 'Loading complaint form…' : submitting ? 'Submitting securely…' : 'Submit anonymous complaint'}
        </button>
        {!form.captchaToken && captchaStatus.kind !== 'blocked' && (
          <p className="text-center text-sm text-amber-200">Complete CAPTCHA verification before submission.</p>
        )}
      </form>
    </section>
  );
}

function Field({ label, children }) {
  return <label className="block text-sm font-semibold text-slate-300">{label}{children}</label>;
}

function Row({ label, value, mono }) {
  return <div><dt className="text-xs font-bold uppercase tracking-wider text-slate-500">{label}</dt><dd className={`mt-1 break-all text-white ${mono ? 'font-mono text-sm' : 'font-semibold'}`}>{value}</dd></div>;
}
