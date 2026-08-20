import { useEffect, useMemo, useState } from 'react';
import ProtectedEvidenceImage from '../../components/complaints/ProtectedEvidenceImage.jsx';
import { verificationApi } from '../../services/verificationApi.js';

const decisions = [
  { value: 'Approved', label: 'Resolved completely' },
  { value: 'NotResolvedYet', label: 'Not resolved yet' },
  { value: 'RequestRevisit', label: 'Request revisit' },
  { value: 'PartiallyResolved', label: 'Partially resolved' },
];

const decisionLabel = (value) => decisions.find((item) => item.value === value)?.label || value;

export default function CitizenVerification() {
  const [pending, setPending] = useState([]);
  const [history, setHistory] = useState([]);
  const [selected, setSelected] = useState(null);
  const [decision, setDecision] = useState('Approved');
  const [rating, setRating] = useState(5);
  const [remarks, setRemarks] = useState('');
  const [location, setLocation] = useState(null);
  const [files, setFiles] = useState([]);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const isApproved = decision === 'Approved';
  const selectedCanSubmit = selected?.canVerify || selected?.canAmend;
  const remainingLabel = useMemo(() => {
    if (!selected?.dueAt) return '';
    const minutes = Math.floor((new Date(selected.dueAt).getTime() - Date.now()) / 60000);
    if (minutes < 0) return `Overdue by ${Math.abs(minutes)} minute(s)`;
    return `${minutes} minute(s) remaining`;
  }, [selected]);

  const load = async () => {
    setError('');
    try {
      const [pendingItems, historyItems] = await Promise.all([
        verificationApi.pending(),
        verificationApi.history(),
      ]);
      setPending(pendingItems || []);
      setHistory(historyItems || []);
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to load verification records.');
    }
  };

  useEffect(() => { load(); }, []);

  const openVerification = async (complaintId) => {
    setBusy(true);
    setError('');
    setMessage('');
    try {
      const item = await verificationApi.get(complaintId);
      setSelected(item);
      setDecision(item.decision === 'Pending' ? 'Approved' : item.decision);
      setRating(item.rating || 5);
      setRemarks(item.remarks || '');
      setLocation(null);
      setFiles([]);
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to open verification.');
    } finally {
      setBusy(false);
    }
  };

  const captureLocation = () => {
    setError('');
    if (!navigator.geolocation) {
      setError('Geolocation is not supported by this browser.');
      return;
    }

    navigator.geolocation.getCurrentPosition(
      async (position) => {
        const captured = {
          latitude: position.coords.latitude,
          longitude: position.coords.longitude,
        };
        setLocation(captured);
        try {
          const result = await verificationApi.geo(selected.complaintId, captured);
          if (!result.withinAllowedRange) {
            setError(`You are ${Math.round(result.distanceMetres)} metres away. Move within ${result.allowedDistanceMetres} metres.`);
          } else {
            setMessage(`Location verified: ${Math.round(result.distanceMetres)} metres from the complaint.`);
          }
        } catch (requestError) {
          setError(requestError.errors?.join(' ') || requestError.message || 'Unable to verify location.');
        }
      },
      () => setError('Location permission is required to submit a verification decision.'),
      { enableHighAccuracy: true, timeout: 15000 },
    );
  };

  const uploadEvidence = async () => {
    if (!selected || files.length === 0) {
      setError('Select at least one verification image.');
      return;
    }

    setBusy(true);
    setError('');
    try {
      const updated = await verificationApi.uploadEvidence(selected.complaintId, files);
      setSelected(updated);
      setFiles([]);
      setMessage('Verification evidence uploaded successfully.');
      await load();
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to upload evidence.');
    } finally {
      setBusy(false);
    }
  };

  const submitDecision = async () => {
    if (!selectedCanSubmit) {
      setError('This decision can no longer be changed.');
      return;
    }
    if (!location) {
      setError('Capture and verify your current GPS location first.');
      return;
    }
    if (!isApproved && remarks.trim().length < 10) {
      setError('Explain the unresolved issue in at least 10 characters.');
      return;
    }

    setBusy(true);
    setError('');
    try {
      const payload = {
        decision,
        rating: isApproved ? Number(rating) : (rating ? Number(rating) : null),
        remarks: remarks.trim(),
        ...location,
      };
      const updated = selected.canAmend
        ? await verificationApi.amend(selected.complaintId, payload)
        : await verificationApi.decide(selected.complaintId, payload);
      setSelected(updated);
      setMessage(isApproved
        ? 'Resolution approved and complaint closed.'
        : `${decisionLabel(decision)} submitted for supervisor review.`);
      await load();
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to save verification decision.');
    } finally {
      setBusy(false);
    }
  };

  const withdrawDecision = async () => {
    if (!selected?.canWithdraw) return;
    setBusy(true);
    setError('');
    try {
      const updated = await verificationApi.withdraw(selected.complaintId);
      setSelected(updated);
      setDecision('Approved');
      setRating(5);
      setRemarks('');
      setLocation(null);
      setMessage('Pending decision withdrawn. You can submit a new verification decision.');
      await load();
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to withdraw the decision.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="space-y-8 p-6 lg:p-10">
      <div>
        <p className="text-sm font-semibold uppercase tracking-[.18em] text-sky-400">Citizen action</p>
        <h1 className="mt-2 text-3xl font-black text-white">Citizen verification</h1>
        <p className="mt-2 max-w-3xl text-slate-400">
          Review completed work, upload fresh evidence, choose the correct resolution outcome, and track every previous decision.
        </p>
      </div>

      {message && <div className="rounded-xl bg-emerald-500/10 p-4 text-emerald-200">{message}</div>}
      {error && <div className="rounded-xl bg-rose-500/10 p-4 text-rose-200">{error}</div>}

      <div className="grid gap-6 xl:grid-cols-[.9fr_1.4fr]">
        <div className="space-y-4">
          <h2 className="text-xl font-black text-white">Awaiting your decision</h2>
          {pending.length === 0 && (
            <p className="rounded-2xl border border-white/10 bg-white/[.04] p-6 text-slate-400">
              No complaints currently need your verification.
            </p>
          )}
          {pending.map((item) => (
            <button
              key={item.complaintId}
              type="button"
              onClick={() => openVerification(item.complaintId)}
              className="w-full rounded-2xl border border-white/10 bg-white/[.04] p-5 text-left transition hover:border-sky-400/40 hover:bg-sky-400/5"
            >
              <span className="font-mono text-xs text-sky-300">{item.referenceNumber}</span>
              <h3 className="mt-2 text-xl font-black text-white">{item.title}</h3>
              <p className="mt-2 text-sm text-slate-400">{item.departmentName} · {item.wardName} · {item.priority}</p>
              <p className={`mt-3 text-xs ${item.isOverdue ? 'text-rose-300' : 'text-amber-300'}`}>
                {item.isOverdue ? 'Verification overdue' : `${Math.max(0, item.remainingMinutes)} minutes remaining`}
              </p>
            </button>
          ))}
        </div>

        <div className="min-h-80 rounded-3xl border border-sky-400/20 bg-sky-400/5 p-6">
          {!selected && (
            <div className="flex min-h-64 items-center justify-center text-center text-slate-400">
              Select a pending or historical verification to view its details.
            </div>
          )}

          {selected && (
            <div className="space-y-6">
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="font-mono text-sm text-sky-300">{selected.referenceNumber}</p>
                  <h2 className="mt-1 text-2xl font-black text-white">{selected.title}</h2>
                  <p className="mt-2 text-sm text-slate-400">Status: {selected.status} · Decision: {decisionLabel(selected.decision)}</p>
                  <p className="mt-1 text-xs text-amber-300">{remainingLabel}</p>
                </div>
                {selected.canWithdraw && (
                  <button
                    type="button"
                    disabled={busy}
                    onClick={withdrawDecision}
                    className="rounded-xl border border-amber-400/30 px-4 py-2 text-sm font-bold text-amber-200 disabled:opacity-50"
                  >
                    Withdraw pending decision
                  </button>
                )}
              </div>

              <div>
                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div>
                    <h3 className="font-black text-white">Citizen verification evidence</h3>
                    <p className="mt-1 text-xs text-slate-400">JPEG, PNG or WebP; maximum 5 MB each and 10 images per complaint.</p>
                  </div>
                  <span className="rounded-full bg-slate-950/60 px-3 py-1 text-xs text-slate-300">
                    {selected.citizenEvidence?.length || 0} uploaded
                  </span>
                </div>

                {selected.citizenEvidence?.length > 0 && (
                  <div className="mt-4 grid gap-3 sm:grid-cols-2">
                    {selected.citizenEvidence.map((image) => (
                      <div key={image.id} className="rounded-2xl border border-white/10 bg-slate-950/40 p-3">
                        <ProtectedEvidenceImage path={image.downloadPath} alt={image.fileName} />
                        <p className="mt-2 truncate text-xs text-slate-300">{image.fileName}</p>
                      </div>
                    ))}
                  </div>
                )}

                {(selected.canVerify || selected.canAmend) && (
                  <div className="mt-4 rounded-2xl border border-dashed border-white/15 p-4">
                    <input
                      type="file"
                      accept="image/jpeg,image/png,image/webp"
                      multiple
                      onChange={(event) => setFiles(Array.from(event.target.files || []).slice(0, 5))}
                      className="block w-full text-sm text-slate-300 file:mr-4 file:rounded-lg file:border-0 file:bg-sky-500 file:px-4 file:py-2 file:font-bold file:text-white"
                    />
                    {files.length > 0 && <p className="mt-2 text-xs text-slate-400">{files.length} image(s) selected.</p>}
                    <button
                      type="button"
                      disabled={busy || files.length === 0}
                      onClick={uploadEvidence}
                      className="mt-3 rounded-xl border border-sky-400/30 px-4 py-2 text-sm font-bold text-sky-200 disabled:opacity-50"
                    >
                      Upload evidence
                    </button>
                  </div>
                )}
              </div>

              {selectedCanSubmit ? (
                <div className="space-y-4 border-t border-white/10 pt-5">
                  <label className="block text-sm font-bold text-slate-200">
                    Verification outcome
                    <select className="input mt-2" value={decision} onChange={(event) => setDecision(event.target.value)}>
                      {decisions.map((item) => <option key={item.value} value={item.value}>{item.label}</option>)}
                    </select>
                  </label>

                  <label className="block text-sm font-bold text-slate-200">
                    Rating {isApproved ? '(required)' : '(optional)'}
                    <select className="input mt-2" value={rating} onChange={(event) => setRating(event.target.value)}>
                      {[5, 4, 3, 2, 1].map((value) => <option key={value} value={value}>{value} star{value > 1 ? 's' : ''}</option>)}
                    </select>
                  </label>

                  <label className="block text-sm font-bold text-slate-200">
                    Remarks {!isApproved && '(minimum 10 characters)'}
                    <textarea
                      className="input mt-2 min-h-28"
                      placeholder={isApproved ? 'Optional feedback about the completed work' : 'Explain what remains unresolved or why a revisit is required'}
                      value={remarks}
                      onChange={(event) => setRemarks(event.target.value)}
                    />
                  </label>

                  <div className="flex flex-wrap gap-3">
                    <button
                      type="button"
                      onClick={captureLocation}
                      className="rounded-xl border border-sky-400/30 px-4 py-2 font-bold text-sky-200"
                    >
                      {location ? 'Recapture GPS location' : 'Capture GPS location'}
                    </button>
                    <button
                      type="button"
                      disabled={busy || !location}
                      onClick={submitDecision}
                      className="rounded-xl bg-sky-500 px-5 py-2 font-black text-white disabled:opacity-50"
                    >
                      {selected.canAmend ? 'Update pending decision' : 'Submit verification'}
                    </button>
                  </div>
                </div>
              ) : (
                <p className="rounded-xl bg-slate-950/50 p-4 text-sm text-slate-400">
                  This verification has completed official review and can no longer be edited from this screen.
                </p>
              )}
            </div>
          )}
        </div>
      </div>

      <div>
        <div className="flex items-center justify-between gap-4">
          <div>
            <h2 className="text-2xl font-black text-white">Verification history</h2>
            <p className="mt-1 text-sm text-slate-400">All current and completed verification records for your complaints.</p>
          </div>
          <button type="button" onClick={load} className="rounded-xl border border-white/10 px-4 py-2 text-sm font-bold text-slate-200">
            Refresh
          </button>
        </div>

        <div className="mt-4 overflow-x-auto rounded-2xl border border-white/10 bg-white/[.04]">
          <table className="min-w-full text-left text-sm">
            <thead className="text-xs uppercase text-slate-500">
              <tr>
                <th className="p-4">Complaint</th>
                <th className="p-4">Decision</th>
                <th className="p-4">Status</th>
                <th className="p-4">Evidence</th>
                <th className="p-4">Completed</th>
                <th className="p-4">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-white/10">
              {history.length === 0 && (
                <tr><td colSpan="6" className="p-6 text-center text-slate-400">No verification history found.</td></tr>
              )}
              {history.map((item) => (
                <tr key={item.verificationId}>
                  <td className="p-4">
                    <p className="font-mono text-xs text-sky-300">{item.referenceNumber}</p>
                    <p className="mt-1 font-bold text-white">{item.title}</p>
                  </td>
                  <td className="p-4 text-slate-300">{decisionLabel(item.decision)}</td>
                  <td className="p-4 text-slate-400">{item.complaintStatus}</td>
                  <td className="p-4 text-slate-300">{item.citizenEvidenceCount}</td>
                  <td className="p-4 text-slate-400">{item.completedAt ? new Date(item.completedAt).toLocaleString() : 'Pending'}</td>
                  <td className="p-4">
                    <button type="button" onClick={() => openVerification(item.complaintId)} className="font-bold text-sky-300 hover:text-sky-200">
                      Open
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {busy && <p className="text-sm text-slate-500">Saving verification changes…</p>}
    </section>
  );
}
