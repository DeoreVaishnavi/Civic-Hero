import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import SuccessPopup from '../../components/common/SuccessPopup.jsx';
import { useGeoLocation } from '../../hooks/useGeoLocation.js';
import { complaintApi } from '../../services/complaintApi.js';

const initialForm = { title: '', description: '', category: '', departmentId: '', wardId: '', latitude: '', longitude: '', address: '', images: [] };

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

  const change = (event) => setForm((current) => ({ ...current, [event.target.name]: event.target.value }));
  const chooseImages = (event) => {
    const selected = Array.from(event.target.files || []).slice(0, 5);
    const invalid = selected.find((file) => !['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024);
    if (invalid) { setError('Images must be JPEG, PNG or WebP and no larger than 5 MB each.'); return; }
    setError(''); setForm((current) => ({ ...current, images: selected }));
  };
  const useLocation = async () => {
    try {
      const coordinates = await geo.locate();
      setForm((current) => ({ ...current, latitude: coordinates.latitude, longitude: coordinates.longitude }));
    } catch { /* Hook already exposes a useful message. */ }
  };

  const submit = async (event) => {
    event.preventDefault(); setError(''); setSubmitting(true);
    try {
      const result = await complaintApi.create(form);
      setSuccess(`Complaint ${result.complaint.referenceNumber} was submitted.`);
      setTimeout(() => navigate(`/citizen/complaints/${result.complaint.id}`, { replace: true }), 700);
    } catch (reason) {
      setError(reason.errors?.length ? reason.errors.join(' ') : reason.message);
    } finally { setSubmitting(false); }
  };

  return (
    <section className="p-6 lg:p-10">
      <div className="mx-auto max-w-4xl">
        <h2 className="text-3xl font-black text-white">Report a civic issue</h2>
        <p className="mt-2 text-slate-400">Add a clear description, exact location and up to five supporting images.</p>
        <div className="mt-6"><SuccessPopup message={success} onClose={() => setSuccess('')} /></div>
        {error && <div className="mb-6 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}
        {geo.error && <div className="mb-6 rounded-xl border border-amber-400/30 bg-amber-400/10 p-4 text-amber-100">{geo.error}</div>}

        <form onSubmit={submit} className="space-y-6 rounded-3xl border border-white/10 bg-white/[0.04] p-6 lg:p-8">
          <div className="grid gap-5 md:grid-cols-2">
            <Field label="Title"><input className="input" name="title" value={form.title} onChange={change} minLength="5" maxLength="200" required /></Field>
            <Field label="Category"><select className="input" name="category" value={form.category} onChange={change} required><option value="">Select category</option>{metadata.categories.map((item) => <option key={item}>{item}</option>)}</select></Field>
          </div>
          <Field label="Description"><textarea className="input min-h-36" name="description" value={form.description} onChange={change} minLength="20" maxLength="5000" required /></Field>
          <div className="grid gap-5 md:grid-cols-2">
            <Field label="Department"><select className="input" name="departmentId" value={form.departmentId} onChange={(event) => setForm((current) => ({ ...current, departmentId: event.target.value, wardId: '' }))} required><option value="">Select department</option>{metadata.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
            <Field label="Ward"><select className="input" name="wardId" value={form.wardId} onChange={change} required disabled={!form.departmentId}><option value="">Select ward</option>{wards.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></Field>
          </div>
          <Field label="Address or landmark"><input className="input" name="address" value={form.address} onChange={change} maxLength="500" required /></Field>
          <div className="rounded-2xl border border-white/10 bg-slate-950/50 p-5">
            <div className="flex flex-wrap items-center justify-between gap-3"><div><h3 className="font-black text-white">GPS coordinates</h3><p className="text-sm text-slate-400">Location accuracy improves routing and duplicate checks.</p></div><button type="button" onClick={useLocation} disabled={geo.loading} className="rounded-lg border border-sky-400/30 px-4 py-2 text-sm font-bold text-sky-200">{geo.loading ? 'Locating…' : 'Use current location'}</button></div>
            <div className="mt-4 grid gap-4 md:grid-cols-2"><Field label="Latitude"><input className="input" type="number" step="0.0000001" name="latitude" value={form.latitude} onChange={change} min="-90" max="90" required /></Field><Field label="Longitude"><input className="input" type="number" step="0.0000001" name="longitude" value={form.longitude} onChange={change} min="-180" max="180" required /></Field></div>
          </div>
          <Field label="Issue images (optional, maximum five)"><input className="input" type="file" accept="image/jpeg,image/png,image/webp" multiple onChange={chooseImages} /></Field>
          {previews.length > 0 && <div className="grid grid-cols-2 gap-3 md:grid-cols-5">{previews.map(({ file, url }) => <div key={`${file.name}-${file.lastModified}`} className="overflow-hidden rounded-xl border border-white/10"><img src={url} alt="Selected complaint evidence" className="h-28 w-full object-cover" /><p className="truncate p-2 text-xs text-slate-400">{file.name}</p></div>)}</div>}
          <button disabled={submitting} className="w-full rounded-xl bg-sky-500 px-5 py-3 font-black text-white hover:bg-sky-400 disabled:opacity-50">{submitting ? 'Submitting securely…' : 'Submit complaint'}</button>
        </form>
      </div>
    </section>
  );
}

function Field({ label, children }) { return <label className="block text-sm font-semibold text-slate-300">{label}{children}</label>; }
