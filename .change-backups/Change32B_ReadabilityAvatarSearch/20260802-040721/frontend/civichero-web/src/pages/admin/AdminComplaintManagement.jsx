import { useEffect, useMemo, useState } from 'react';
import { adminComplaintApi } from '../../services/adminComplaintApi.js';
import { complaintApi } from '../../services/complaintApi.js';

const priorities = ['Low', 'Medium', 'High', 'Critical'];
const statuses = ['Created', 'AiTriage', 'FraudReview', 'Merged', 'Assigned', 'ReassignmentPending', 'InProgress', 'Escalated', 'Resolved', 'VerificationPending', 'Disputed', 'Appealed', 'Closed', 'ClosedAuto', 'ClosedFraud', 'Withdrawn'];
const initialFilters = { page: 1, pageSize: 20, search: '', status: '', priority: '', category: '', departmentId: '', wardId: '', includeArchived: false, duplicateOnly: false };

export default function AdminComplaintManagement() {
  const [filters, setFilters] = useState(initialFilters);
  const [result, setResult] = useState({ items: [], totalCount: 0, totalPages: 0 });
  const [metadata, setMetadata] = useState({ categories: [], departments: [], wards: [] });
  const [selected, setSelected] = useState(null);
  const [officers, setOfficers] = useState([]);
  const [reason, setReason] = useState('');
  const [priority, setPriority] = useState('Medium');
  const [routing, setRouting] = useState({ departmentId: '', wardId: '' });
  const [officerId, setOfficerId] = useState('');
  const [canonicalId, setCanonicalId] = useState('');
  const [clusters, setClusters] = useState([]);
  const [tab, setTab] = useState('complaints');
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const load = async () => {
    setLoading(true); setError('');
    try {
      const data = await adminComplaintApi.list({ ...filters, departmentId: filters.departmentId || undefined, wardId: filters.wardId || undefined });
      setResult(data || { items: [] });
      if (selected) {
        const exists = data?.items?.some((item) => item.id === selected.complaint.id);
        if (!exists) setSelected(null);
      }
    } catch (e) { setError(e.errors?.join(' ') || e.message || 'Unable to load complaints.'); }
    finally { setLoading(false); }
  };

  const loadClusters = async () => {
    try { setClusters(await adminComplaintApi.duplicateClusters(filters.includeArchived) || []); }
    catch (e) { setError(e.message || 'Unable to load duplicate clusters.'); }
  };

  useEffect(() => { complaintApi.metadata().then(setMetadata).catch(() => {}); }, []);
  useEffect(() => { if (tab === 'complaints') load(); else loadClusters(); }, [filters.page, filters.status, filters.priority, filters.category, filters.departmentId, filters.wardId, filters.includeArchived, filters.duplicateOnly, tab]);

  const wards = useMemo(() => metadata.wards.filter((ward) => !filters.departmentId || String(ward.departmentId) === String(filters.departmentId)), [metadata.wards, filters.departmentId]);
  const routeWards = useMemo(() => metadata.wards.filter((ward) => !routing.departmentId || String(ward.departmentId) === String(routing.departmentId)), [metadata.wards, routing.departmentId]);

  const open = async (id) => {
    setBusy(`open-${id}`); setError('');
    try {
      const detail = await adminComplaintApi.get(id);
      setSelected(detail);
      setPriority(detail.complaint.priority);
      setRouting({ departmentId: String(detail.complaint.departmentId), wardId: String(detail.complaint.wardId) });
      setCanonicalId(detail.complaint.duplicateOfComplaintId ? String(detail.complaint.duplicateOfComplaintId) : '');
      setReason(''); setOfficerId('');
      if (detail.complaint.canAssign) {
        try { setOfficers(await adminComplaintApi.eligibleOfficers(id) || []); } catch { setOfficers([]); }
      } else setOfficers([]);
    } catch (e) { setError(e.errors?.join(' ') || e.message || 'Unable to open complaint.'); }
    finally { setBusy(''); }
  };

  const execute = async (name, operation) => {
    if (reason.trim().length < 10) { setError('Enter an audited reason of at least 10 characters.'); return; }
    setBusy(name); setError(''); setMessage('');
    try {
      const next = await operation();
      setSelected(next);
      setMessage(`${name} completed successfully.`);
      setReason('');
      await load();
      if (tab === 'duplicates') await loadClusters();
    } catch (e) { setError(e.errors?.join(' ') || e.message || `${name} failed.`); }
    finally { setBusy(''); }
  };

  const submitSearch = (event) => { event.preventDefault(); setFilters((value) => ({ ...value, page: 1 })); load(); };

  return <section className="space-y-6 p-6 lg:p-10">
    <div className="flex flex-wrap items-end justify-between gap-4"><div><p className="text-sm font-semibold uppercase tracking-[.18em] text-sky-400">Exceptional controls</p><h2 className="mt-2 text-3xl font-black text-white">Complaint administration</h2><p className="mt-2 text-slate-400">Priority, routing, assignment, lifecycle, duplicates and unsafe media.</p></div><div className="flex gap-2"><button onClick={() => setTab('complaints')} className={`rounded-xl px-4 py-2 font-bold ${tab === 'complaints' ? 'bg-sky-500 text-white' : 'border border-white/15 text-slate-300'}`}>Complaints</button><button onClick={() => setTab('duplicates')} className={`rounded-xl px-4 py-2 font-bold ${tab === 'duplicates' ? 'bg-violet-500 text-white' : 'border border-white/15 text-slate-300'}`}>Duplicate clusters</button></div></div>
    {message && <p className="rounded-xl bg-emerald-500/10 p-4 text-emerald-200">{message}</p>}
    {error && <p className="rounded-xl bg-rose-500/10 p-4 text-rose-200">{error}</p>}

    {tab === 'complaints' && <>
      <form onSubmit={submitSearch} className="grid gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 md:grid-cols-3 xl:grid-cols-6">
        <input value={filters.search} onChange={(e) => setFilters({ ...filters, search: e.target.value })} className="input mt-0 md:col-span-2" placeholder="Search complaint or citizen" />
        <select value={filters.status} onChange={(e) => setFilters({ ...filters, status: e.target.value, page: 1 })} className="input mt-0"><option value="">All statuses</option>{statuses.map((item) => <option key={item}>{item}</option>)}</select>
        <select value={filters.priority} onChange={(e) => setFilters({ ...filters, priority: e.target.value, page: 1 })} className="input mt-0"><option value="">All priorities</option>{priorities.map((item) => <option key={item}>{item}</option>)}</select>
        <select value={filters.category} onChange={(e) => setFilters({ ...filters, category: e.target.value, page: 1 })} className="input mt-0"><option value="">All categories</option>{metadata.categories.map((item) => <option key={item}>{item}</option>)}</select>
        <button className="rounded-xl bg-sky-500 px-4 py-3 font-bold text-white">Search</button>
        <select value={filters.departmentId} onChange={(e) => setFilters({ ...filters, departmentId: e.target.value, wardId: '', page: 1 })} className="input mt-0"><option value="">All departments</option>{metadata.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
        <select value={filters.wardId} onChange={(e) => setFilters({ ...filters, wardId: e.target.value, page: 1 })} className="input mt-0"><option value="">All wards</option>{wards.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select>
        <label className="flex items-center gap-2 rounded-xl border border-white/10 px-4 text-sm text-slate-300"><input type="checkbox" checked={filters.includeArchived} onChange={(e) => setFilters({ ...filters, includeArchived: e.target.checked, page: 1 })} /> Include archived</label>
        <label className="flex items-center gap-2 rounded-xl border border-white/10 px-4 text-sm text-slate-300"><input type="checkbox" checked={filters.duplicateOnly} onChange={(e) => setFilters({ ...filters, duplicateOnly: e.target.checked, page: 1 })} /> Duplicate records</label>
      </form>

      <div className="overflow-x-auto rounded-2xl border border-white/10"><table className="min-w-full divide-y divide-white/10 text-sm"><thead className="bg-white/5 text-left text-xs uppercase tracking-wider text-slate-400"><tr><th className="p-4">Complaint</th><th className="p-4">Workflow</th><th className="p-4">Routing</th><th className="p-4">Citizen / Officer</th><th className="p-4">Action</th></tr></thead><tbody className="divide-y divide-white/10 bg-slate-950/40">{loading ? <tr><td colSpan="5" className="p-8 text-center text-slate-400">Loading complaints…</td></tr> : result.items?.map((item) => <tr key={item.id} className={item.isArchived ? 'opacity-60' : ''}><td className="p-4"><p className="font-mono text-xs text-sky-300">{item.referenceNumber}</p><p className="mt-1 font-bold text-white">{item.title}</p><p className="mt-1 text-xs text-slate-500">{item.category} · {item.mediaCount} media</p></td><td className="p-4"><p className="text-slate-200">{item.status}</p><p className="mt-1 text-xs text-amber-300">{item.priority}</p>{item.isArchived && <p className="mt-1 text-xs text-rose-300">Archived</p>}{item.duplicateOfComplaintId && <p className="mt-1 text-xs text-violet-300">Duplicate of #{item.duplicateOfComplaintId}</p>}</td><td className="p-4 text-slate-300"><p>{item.departmentName}</p><p className="mt-1 text-xs text-slate-500">{item.wardName}</p></td><td className="p-4 text-slate-300"><p>{item.citizenName}</p><p className="mt-1 text-xs text-slate-500">{item.assignedOfficerName || 'Unassigned'}</p></td><td className="p-4"><button disabled={!!busy} onClick={() => open(item.id)} className="rounded-lg border border-sky-400/30 px-3 py-2 font-bold text-sky-200 disabled:opacity-40">Manage</button></td></tr>)}</tbody></table></div>
      <div className="flex items-center justify-between"><button disabled={filters.page <= 1} onClick={() => setFilters({ ...filters, page: filters.page - 1 })} className="rounded-lg border border-white/15 px-4 py-2 disabled:opacity-40">Previous</button><span className="text-sm text-slate-400">Page {filters.page} of {Math.max(1, result.totalPages || 1)} · {result.totalCount || 0} records</span><button disabled={filters.page >= (result.totalPages || 1)} onClick={() => setFilters({ ...filters, page: filters.page + 1 })} className="rounded-lg border border-white/15 px-4 py-2 disabled:opacity-40">Next</button></div>
    </>}

    {tab === 'duplicates' && <div className="space-y-4">{clusters.length === 0 ? <p className="rounded-2xl border border-white/10 bg-white/5 p-6 text-slate-400">No duplicate clusters found.</p> : clusters.map((cluster) => <article key={cluster.canonical.id} className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex flex-wrap justify-between gap-3"><div><p className="font-mono text-xs text-violet-300">Canonical {cluster.canonical.referenceNumber}</p><h3 className="mt-2 font-black text-white">{cluster.canonical.title}</h3><p className="mt-1 text-sm text-slate-400">{cluster.canonical.status} · {cluster.canonical.departmentName}</p></div><div className="text-right"><p className="text-2xl font-black text-white">{cluster.duplicates.length}</p><p className="text-xs text-slate-500">linked duplicates</p><p className="mt-1 text-sm text-emerald-300">{cluster.totalSupportCount} combined supports</p></div></div><div className="mt-4 grid gap-2 md:grid-cols-2">{cluster.duplicates.map((item) => <button key={item.id} onClick={() => { setTab('complaints'); open(item.id); }} className="rounded-xl border border-white/10 bg-slate-950/40 p-3 text-left"><p className="font-mono text-xs text-sky-300">{item.referenceNumber}</p><p className="mt-1 font-bold text-white">{item.title}</p><p className="mt-1 text-xs text-slate-500">{item.status}</p></button>)}</div></article>)}</div>}

    {selected && <div className="fixed inset-0 z-50 overflow-y-auto bg-black/75 p-4"><div className="mx-auto my-6 max-w-6xl rounded-3xl border border-white/10 bg-slate-900 p-6 lg:p-8"><div className="flex justify-between gap-4"><div><p className="font-mono text-xs text-sky-300">{selected.complaint.referenceNumber}</p><h3 className="mt-2 text-2xl font-black text-white">{selected.complaint.title}</h3><p className="mt-2 text-sm text-slate-400">{selected.complaint.status} · {selected.complaint.priority} · {selected.complaint.departmentName} / {selected.complaint.wardName}</p></div><button onClick={() => setSelected(null)} className="h-fit rounded-xl border border-white/15 px-4 py-2">Close</button></div>
      <label className="mt-6 block text-sm font-bold text-slate-300">Audited reason for the next operation<textarea value={reason} onChange={(e) => setReason(e.target.value)} className="input min-h-24" placeholder="Explain why this exceptional administrative action is necessary" /></label>
      <div className="mt-6 grid gap-4 lg:grid-cols-3">
        <div className="rounded-2xl border border-white/10 bg-white/5 p-4"><h4 className="font-black text-white">Priority override</h4><select value={priority} onChange={(e) => setPriority(e.target.value)} className="input">{priorities.map((item) => <option key={item}>{item}</option>)}</select><button disabled={!!busy || selected.complaint.isArchived} onClick={() => execute('Priority update', () => adminComplaintApi.changePriority(selected.complaint.id, priority, reason))} className="mt-3 w-full rounded-xl bg-amber-500/20 px-4 py-3 font-bold text-amber-200 disabled:opacity-40">Update priority</button></div>
        <div className="rounded-2xl border border-white/10 bg-white/5 p-4"><h4 className="font-black text-white">Routing correction</h4><select value={routing.departmentId} onChange={(e) => setRouting({ departmentId: e.target.value, wardId: '' })} className="input"><option value="">Department</option>{metadata.departments.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select><select value={routing.wardId} onChange={(e) => setRouting({ ...routing, wardId: e.target.value })} className="input"><option value="">Ward</option>{routeWards.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select><button disabled={!!busy || !routing.departmentId || !routing.wardId || selected.complaint.isArchived} onClick={() => execute('Routing correction', () => adminComplaintApi.correctRouting(selected.complaint.id, { departmentId: Number(routing.departmentId), wardId: Number(routing.wardId), reason }))} className="mt-3 w-full rounded-xl bg-sky-500/20 px-4 py-3 font-bold text-sky-200 disabled:opacity-40">Correct routing</button></div>
        <div className="rounded-2xl border border-white/10 bg-white/5 p-4"><h4 className="font-black text-white">Assignment override</h4><select value={officerId} onChange={(e) => setOfficerId(e.target.value)} className="input"><option value="">Select eligible Officer</option>{officers.map((item) => <option key={item.officerId} value={item.officerId}>{item.officerName} · {item.activeWorkload} active</option>)}</select><button disabled={!!busy || !officerId || !selected.complaint.canAssign} onClick={() => execute('Assignment override', () => adminComplaintApi.overrideAssignment(selected.complaint.id, Number(officerId), reason))} className="mt-3 w-full rounded-xl bg-violet-500/20 px-4 py-3 font-bold text-violet-200 disabled:opacity-40">Assign / reassign</button></div>
      </div>
      <div className="mt-4 grid gap-4 lg:grid-cols-2"><div className="rounded-2xl border border-white/10 bg-white/5 p-4"><h4 className="font-black text-white">Lifecycle control</h4><div className="mt-3 flex flex-wrap gap-2"><button disabled={!!busy || !selected.complaint.canClose} onClick={() => execute('Administrative close', () => adminComplaintApi.close(selected.complaint.id, reason))} className="rounded-lg bg-emerald-500/20 px-3 py-2 font-bold text-emerald-200 disabled:opacity-40">Close</button><button disabled={!!busy || !selected.complaint.canReopen} onClick={() => execute('Administrative reopen', () => adminComplaintApi.reopen(selected.complaint.id, reason))} className="rounded-lg bg-sky-500/20 px-3 py-2 font-bold text-sky-200 disabled:opacity-40">Reopen</button><button disabled={!!busy || !selected.complaint.canArchive} onClick={() => execute('Archive', () => adminComplaintApi.archive(selected.complaint.id, reason))} className="rounded-lg bg-rose-500/20 px-3 py-2 font-bold text-rose-200 disabled:opacity-40">Archive</button><button disabled={!!busy || !selected.complaint.canRestore} onClick={() => execute('Restore', () => adminComplaintApi.restore(selected.complaint.id, reason))} className="rounded-lg bg-emerald-500/20 px-3 py-2 font-bold text-emerald-200 disabled:opacity-40">Restore</button></div><p className="mt-3 text-xs text-slate-500">Archiving is limited to closed, withdrawn or merged records.</p></div><div className="rounded-2xl border border-white/10 bg-white/5 p-4"><h4 className="font-black text-white">Duplicate management</h4><input type="number" min="1" value={canonicalId} onChange={(e) => setCanonicalId(e.target.value)} className="input" placeholder="Canonical complaint ID" /><div className="mt-3 flex gap-2"><button disabled={!!busy || !canonicalId || !selected.complaint.canMerge} onClick={() => execute('Duplicate link', () => adminComplaintApi.linkDuplicate(selected.complaint.id, Number(canonicalId), reason))} className="rounded-lg bg-violet-500/20 px-3 py-2 font-bold text-violet-200 disabled:opacity-40">Link only</button><button disabled={!!busy || !canonicalId || !selected.complaint.canMerge} onClick={() => execute('Duplicate merge', () => adminComplaintApi.merge(selected.complaint.id, Number(canonicalId), reason))} className="rounded-lg bg-amber-500/20 px-3 py-2 font-bold text-amber-200 disabled:opacity-40">Merge into canonical</button></div></div></div>
      <div className="mt-4 grid gap-4 lg:grid-cols-2"><div className="rounded-2xl border border-white/10 bg-white/5 p-4"><h4 className="font-black text-white">Complaint media</h4><div className="mt-3 space-y-2">{selected.media.length === 0 ? <p className="text-sm text-slate-500">No media.</p> : selected.media.map((item) => <div key={item.id} className="flex items-center justify-between gap-3 rounded-xl bg-slate-950/50 p-3"><div><p className="text-sm font-bold text-white">{item.fileName}</p><p className="text-xs text-slate-500">{item.mimeType} · {Math.ceil(item.fileSize / 1024)} KB {item.isResolutionEvidence ? '· Resolution evidence' : ''}</p></div><button disabled={!!busy} onClick={() => execute('Unsafe media removal', () => adminComplaintApi.removeMedia(selected.complaint.id, item.id, reason))} className="rounded-lg border border-rose-400/30 px-3 py-2 text-sm font-bold text-rose-200 disabled:opacity-40">Remove</button></div>)}</div></div><div className="rounded-2xl border border-white/10 bg-white/5 p-4"><h4 className="font-black text-white">Recent audit timeline</h4><div className="mt-3 max-h-80 space-y-3 overflow-y-auto">{selected.timeline.slice(0, 20).map((item) => <div key={item.id} className="border-l border-white/10 pl-3"><p className="text-xs font-bold text-sky-300">{item.eventType}</p><p className="mt-1 text-sm text-slate-300">{item.description}</p><p className="mt-1 text-xs text-slate-600">{item.actorName} · {new Date(item.timestamp).toLocaleString()}</p></div>)}</div></div></div>
      <p className="mt-5 rounded-xl border border-amber-400/20 bg-amber-400/5 p-3 text-xs text-amber-200">Contractor assignment is intentionally unavailable because the current complaint schema has no ContractorId relationship. It requires explicit database-change permission.</p>
    </div></div>}
  </section>;
}
