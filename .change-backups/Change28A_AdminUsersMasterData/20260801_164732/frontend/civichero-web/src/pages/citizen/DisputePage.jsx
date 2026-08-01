import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { disputeApi } from '../../services/disputeApi.js';

const activeStatuses = new Set(['Raised', 'UnderSupervisorReview', 'Appealed', 'UnderAdminReview']);
const accept = 'image/jpeg,image/png,image/webp,video/mp4,video/webm,video/quicktime,application/pdf,.doc,.docx';

export default function DisputePage() {
  const [items, setItems] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [detail, setDetail] = useState(null);
  const [history, setHistory] = useState([]);
  const [files, setFiles] = useState([]);
  const [requestId, setRequestId] = useState('');
  const [remarks, setRemarks] = useState('');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [busy, setBusy] = useState('');

  const loadList = async (preferredId) => {
    const next = await disputeApi.mine();
    setItems(next || []);
    const id = preferredId ?? selectedId ?? next?.[0]?.id ?? null;
    setSelectedId(id);
    return id;
  };

  const loadDetail = async (id) => {
    if (!id) { setDetail(null); setHistory([]); return; }
    const [record, records] = await Promise.all([disputeApi.get(id), disputeApi.history(id)]);
    setDetail(record);
    setHistory(records || []);
    const pendingRequest = record.evidenceRequests?.find((item) => item.targetRole === 'Citizen' && !item.isFulfilled);
    setRequestId(pendingRequest?.id ? String(pendingRequest.id) : '');
  };

  useEffect(() => {
    loadList().catch((reason) => setError(reason.message || 'Unable to load disputes.'));
  }, []);

  useEffect(() => {
    loadDetail(selectedId).catch((reason) => setError(reason.message || 'Unable to load dispute details.'));
  }, [selectedId]);

  const pending = useMemo(() => items.filter((item) => activeStatuses.has(item.status)).length, [items]);
  const selected = detail || items.find((item) => item.id === selectedId);

  const run = async (key, action, success) => {
    setBusy(key); setError(''); setMessage('');
    try {
      const updated = await action();
      const id = updated?.id || selectedId;
      await loadList(id);
      await loadDetail(id);
      setMessage(success);
      setRemarks('');
      setFiles([]);
    } catch (reason) {
      setError(reason.errors?.join(' ') || reason.message || 'Operation failed.');
    } finally { setBusy(''); }
  };

  const upload = () => {
    if (!files.length) { setError('Select at least one evidence file.'); return; }
    run('upload', () => disputeApi.uploadEvidence(selectedId, files, requestId || null), 'Private dispute evidence uploaded.');
  };

  const submitAppeal = () => {
    if (remarks.trim().length < 10) { setError('Enter at least 10 characters.'); return; }
    run('appeal', () => disputeApi.appeal(selectedId, remarks.trim()), 'Appeal submitted for administrative review.');
  };

  const submitReopen = () => {
    if (remarks.trim().length < 10) { setError('Enter at least 10 characters.'); return; }
    run('reopen', () => disputeApi.reopen(selectedId, remarks.trim()), 'Reopen request submitted to the Supervisor.');
  };

  return <section className="page-wrap space-y-6">
    <div className="page-title-row"><div><p className="section-kicker">Citizen review and escalation</p><h2>Disputes and appeals</h2><p>Upload private evidence, answer evidence requests, request reopening and track every decision cycle.</p></div><Link to="/citizen/complaints" className="button outline">View my complaints</Link></div>
    {message && <div className="alert success">{message}</div>}
    {error && <div className="alert error">{error}</div>}

    <div className="stat-grid" style={{ gridTemplateColumns: 'repeat(3,minmax(0,1fr))' }}>
      <Stat label="Total disputes" value={items.length} helper="All review cycles" />
      <Stat label="Pending review" value={pending} helper="Supervisor or final review" tone="amber" />
      <Stat label="Decided" value={items.length - pending} helper="Closed or resolved cycles" tone="green" />
    </div>

    <div className="grid gap-6 xl:grid-cols-[.72fr_1.28fr]">
      <section className="overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-sm">
        <div className="border-b border-slate-200 p-5"><h3 className="text-lg font-black text-slate-900">My dispute list</h3></div>
        <div className="divide-y divide-slate-100">{items.map((item) => <button type="button" key={item.id} onClick={() => setSelectedId(item.id)} className={`w-full p-5 text-left transition hover:bg-blue-50/50 ${selectedId === item.id ? 'bg-blue-50' : ''}`}><div className="flex items-start justify-between gap-3"><div><h4 className="font-black text-slate-900">{item.referenceNumber}</h4><p className="mt-1 text-xs font-bold uppercase tracking-wide text-slate-400">Cycle {item.cycleNumber}</p><p className="mt-2 line-clamp-2 text-sm leading-6 text-slate-500">{item.citizenRemarks}</p></div><span className="status-pill amber">{item.status}</span></div></button>)}{items.length === 0 && <div className="p-10 text-center text-sm text-slate-500">You have no dispute records.</div>}</div>
      </section>

      <section className="overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-sm">
        {!selected ? <Empty /> : <div>
          <div className="border-b border-slate-200 bg-gradient-to-r from-slate-50 to-blue-50 p-6"><p className="text-xs font-black uppercase tracking-[.18em] text-blue-600">Selected dispute</p><div className="mt-3 flex flex-wrap items-center justify-between gap-4"><div><h3 className="text-2xl font-black text-slate-950">{selected.referenceNumber} · Cycle {selected.cycleNumber}</h3><p className="mt-2 text-sm text-slate-500">{selected.title}</p></div><span className="status-pill red">{selected.status}</span></div></div>
          <div className="space-y-6 p-6">
            <Panel title="Citizen reason"><p className="text-sm leading-7 text-slate-600">{selected.citizenRemarks}</p></Panel>

            {selected.evidenceRequests?.length > 0 && <Panel title="Additional evidence requests"><div className="space-y-3">{selected.evidenceRequests.map((item) => <div key={item.id} className="rounded-xl border border-slate-200 bg-slate-50 p-4"><div className="flex flex-wrap justify-between gap-2"><b className="text-slate-900">Requested from {item.targetRole}</b><span className={item.isFulfilled ? 'text-emerald-600' : 'text-amber-600'}>{item.isFulfilled ? 'Fulfilled' : 'Pending'}</span></div><p className="mt-2 text-sm text-slate-600">{item.message}</p>{item.dueAt && <p className="mt-2 text-xs text-slate-400">Due {new Date(item.dueAt).toLocaleString()}</p>}</div>)}</div></Panel>}

            <Panel title="Private dispute evidence"><EvidenceList items={selected.evidence || []} onDownload={(e) => disputeApi.downloadEvidence(selected.id, e)} /></Panel>

            {selected.canUploadCitizenEvidence && <Panel title="Upload Citizen evidence"><p className="text-xs text-slate-500">JPEG, PNG, WebP, MP4, WebM, MOV, PDF, DOC or DOCX. Up to 5 files and 50 MB total.</p><input className="mt-3 block w-full text-sm" type="file" accept={accept} multiple onChange={(event) => setFiles(Array.from(event.target.files || []).slice(0, 5))} />{selected.evidenceRequests?.some((r) => r.targetRole === 'Citizen' && !r.isFulfilled) && <select className="input mt-3" value={requestId} onChange={(e) => setRequestId(e.target.value)}><option value="">General evidence</option>{selected.evidenceRequests.filter((r) => r.targetRole === 'Citizen' && !r.isFulfilled).map((r) => <option key={r.id} value={r.id}>Answer request #{r.id}</option>)}</select>}<button type="button" disabled={busy || !files.length} onClick={upload} className="button primary mt-4">{busy === 'upload' ? 'Uploading…' : 'Upload evidence'}</button></Panel>}

            {(selected.canAppeal || selected.canRequestReopen) && <Panel title="Grace-period actions"><textarea className="input min-h-28" value={remarks} onChange={(e) => setRemarks(e.target.value)} placeholder="Explain why the decision should be reviewed again"/><div className="mt-3 flex flex-wrap gap-3">{selected.canRequestReopen && <button type="button" disabled={!!busy} onClick={submitReopen} className="button outline">{busy === 'reopen' ? 'Submitting…' : 'Request reopen'}</button>}{selected.canAppeal && <button type="button" disabled={!!busy} onClick={submitAppeal} className="button primary">{busy === 'appeal' ? 'Submitting…' : 'Submit appeal'}</button>}</div>{selected.reopenDeadline && <p className="mt-3 text-xs text-amber-700">Available until {new Date(selected.reopenDeadline).toLocaleString()}</p>}</Panel>}

            <Panel title="Decision history"><div className="space-y-3">{history.map((record) => <article key={record.id} className="rounded-xl border border-slate-200 p-4"><div className="flex flex-wrap justify-between gap-2"><b className="text-slate-900">Cycle {record.cycleNumber} · {record.status}</b><span className="text-xs text-slate-400">{new Date(record.raisedAt).toLocaleString()}</span></div><p className="mt-2 text-sm text-slate-600">Citizen: {record.citizenRemarks}</p>{record.supervisorDecision && <p className="mt-2 text-sm text-blue-700">Supervisor: {record.supervisorDecision} — {record.supervisorRemarks}</p>}{record.adminDecision && <p className="mt-2 text-sm text-violet-700">Final review: {record.adminDecision} — {record.adminRemarks}</p>}</article>)}</div></Panel>
            <div className="flex flex-wrap gap-3"><Link to={`/citizen/complaints/${selected.complaintId}`} className="button primary">View complaint</Link><Link to="/citizen/notifications" className="button outline">View status alerts</Link></div>
          </div>
        </div>}
      </section>
    </div>
  </section>;
}

function Stat({ label, value, helper, tone = '' }) { return <div className={`stat-card ${tone}`}><small>{label}</small><strong>{value}</strong><p>{helper}</p></div>; }
function Panel({ title, children }) { return <div className="rounded-2xl border border-slate-200 p-5"><h4 className="mb-3 font-black text-slate-900">{title}</h4>{children}</div>; }
function Empty() { return <div className="grid min-h-[430px] place-items-center p-10 text-center"><div><div className="mx-auto grid h-20 w-20 place-items-center rounded-3xl bg-slate-100 text-3xl">!</div><h3 className="mt-5 text-xl font-black text-slate-900">No dispute selected</h3></div></div>; }
function EvidenceList({ items, onDownload }) { if (!items.length) return <p className="text-sm text-slate-500">No private dispute evidence has been uploaded.</p>; return <div className="space-y-2">{items.map((item) => <div key={item.id} className="flex flex-wrap items-center justify-between gap-3 rounded-xl bg-slate-50 p-3"><div><b className="text-sm text-slate-800">{item.fileName}</b><p className="text-xs text-slate-500">{item.sourceRole} · {(item.fileSize / 1024 / 1024).toFixed(2)} MB · {new Date(item.uploadedAt).toLocaleString()}</p></div><button type="button" onClick={() => onDownload(item)} className="text-sm font-bold text-blue-600">Download</button></div>)}</div>; }
