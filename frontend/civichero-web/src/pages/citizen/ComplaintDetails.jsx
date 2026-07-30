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
    if (!confirm('Withdraw this complaint? This action changes its status to Withdrawn.')) return;
    try { setDetail(await complaintApi.withdraw(id)); setMessage('Complaint withdrawn.'); }
    catch (reason) { setError(reason.message); }
  };
  const toggleUpvote = async () => {
    try {
      if (item.hasUpvoted) await complaintApi.removeUpvote(id); else await complaintApi.upvote(id);
      await load();
    } catch (reason) { setError(reason.message); }
  };
  const download = async (image) => {
    try {
      const response = await complaintApi.downloadImage(id, image.id);
      const url = URL.createObjectURL(response.data);
      const link = document.createElement('a'); link.href = url; link.download = image.fileName; link.click();
      URL.revokeObjectURL(url);
    } catch (reason) { setError(reason.message); }
  };

  if (!detail && !error) return <section className="p-10 text-slate-400">Loading complaint…</section>;
  if (!detail) return <section className="p-10"><div className="rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div></section>;

  return (
    <section className="p-6 lg:p-10">
      <button onClick={() => navigate(-1)} className="text-sm font-bold text-sky-300">← Back</button>
      {message && <div className="mt-5 rounded-xl border border-emerald-400/30 bg-emerald-400/10 p-4 text-emerald-100">{message}</div>}
      {error && <div className="mt-5 rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}
      <div className="mt-5 rounded-3xl border border-white/10 bg-white/[0.04] p-6 lg:p-8">
        <div className="flex flex-wrap items-start justify-between gap-5"><div><p className="font-mono text-sm text-sky-300">{item.referenceNumber}</p><h2 className="mt-2 text-3xl font-black text-white">{item.title}</h2></div><StatusBadge status={item.status} /></div>
        <div className="mt-6 grid gap-4 text-sm md:grid-cols-4"><Info label="Category" value={item.category} /><Info label="Priority" value={item.priority} /><Info label="Department" value={item.departmentName} /><Info label="Ward" value={item.wardName} /></div>
        <p className="mt-6 whitespace-pre-wrap leading-7 text-slate-300">{item.description}</p>
        <div className="mt-6 rounded-2xl border border-white/10 bg-slate-950/50 p-5"><p className="font-bold text-white">Location</p><p className="mt-1 text-slate-400">{item.address}</p><p className="mt-2 font-mono text-xs text-slate-500">{item.latitude}, {item.longitude}</p></div>
        <div className="mt-6 flex flex-wrap gap-3"><button onClick={toggleUpvote} className={`rounded-xl px-4 py-2 font-bold ${item.hasUpvoted ? 'bg-emerald-500 text-white' : 'border border-white/15 text-slate-200'}`}>▲ {item.upvoteCount} {item.hasUpvoted ? 'Upvoted' : 'Upvote'}</button>{item.canEdit && <button onClick={() => setEditing((value) => !value)} className="rounded-xl border border-sky-400/30 px-4 py-2 font-bold text-sky-200">{editing ? 'Cancel editing' : 'Edit complaint'}</button>}{item.canWithdraw && <button onClick={withdraw} className="rounded-xl border border-rose-400/30 px-4 py-2 font-bold text-rose-200">Withdraw</button>}</div>
      </div>

      {editing && <form onSubmit={save} className="mt-6 space-y-5 rounded-3xl border border-sky-400/20 bg-sky-400/5 p-6"><h3 className="text-xl font-black text-white">Edit complaint</h3><input className="input" value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} required /><textarea className="input min-h-32" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} required /><div className="grid gap-4 md:grid-cols-3"><select className="input" value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value })}>{metadata.categories.map((value) => <option key={value}>{value}</option>)}</select><select className="input" value={form.departmentId} onChange={(e) => setForm({ ...form, departmentId: e.target.value, wardId: '' })}>{metadata.departments.map((value) => <option key={value.id} value={value.id}>{value.name}</option>)}</select><select className="input" value={form.wardId} onChange={(e) => setForm({ ...form, wardId: e.target.value })}>{wards.map((value) => <option key={value.id} value={value.id}>{value.name}</option>)}</select></div><input className="input" value={form.address} onChange={(e) => setForm({ ...form, address: e.target.value })} required /><div className="grid gap-4 md:grid-cols-2"><input className="input" type="number" step="0.0000001" value={form.latitude} onChange={(e) => setForm({ ...form, latitude: e.target.value })} required /><input className="input" type="number" step="0.0000001" value={form.longitude} onChange={(e) => setForm({ ...form, longitude: e.target.value })} required /></div><button className="rounded-xl bg-sky-500 px-5 py-3 font-black text-white">Save changes</button></form>}

      <div className="mt-8 grid gap-6 xl:grid-cols-2">
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6"><h3 className="text-xl font-black text-white">Evidence files</h3><div className="mt-4 space-y-3">{detail.images.length ? detail.images.map((image) => <button key={image.id} onClick={() => download(image)} className="flex w-full items-center justify-between rounded-xl border border-white/10 p-4 text-left hover:bg-white/5"><span><span className="block font-bold text-white">{image.fileName}</span><span className="text-xs text-slate-500">{Math.ceil(image.fileSize / 1024)} KB · {image.mimeType}</span></span><span className="text-sky-300">Download</span></button>) : <p className="text-slate-500">No images were uploaded.</p>}</div></section>
        <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6"><h3 className="text-xl font-black text-white">Timeline</h3><ol className="mt-5 space-y-5">{detail.timeline.map((event) => <li key={event.id} className="border-l-2 border-sky-400/30 pl-4"><div className="flex flex-wrap justify-between gap-2"><span className="font-bold text-white">{event.eventType}</span><time className="text-xs text-slate-500">{new Date(event.timestamp).toLocaleString()}</time></div><p className="mt-1 text-sm text-slate-400">{event.description}</p><p className="mt-1 text-xs text-slate-600">By {event.actorName}</p></li>)}</ol></section>
      </div>
    </section>
  );
}
function Info({ label, value }) { return <div><p className="text-xs font-bold uppercase tracking-wider text-slate-500">{label}</p><p className="mt-1 font-semibold text-white">{value}</p></div>; }
