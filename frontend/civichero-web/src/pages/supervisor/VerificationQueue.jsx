import { useEffect, useState } from 'react';
import { verificationApi } from '../../services/verificationApi.js';

const formatDate = (value) => (value ? new Date(value).toLocaleString() : 'Not sent');

const label = (decision) => ({
  Pending: 'Awaiting citizen',
  NotResolvedYet: 'Not resolved yet',
  RequestRevisit: 'Revisit requested',
  PartiallyResolved: 'Partially resolved',
  Rejected: 'Resolution rejected',
}[decision] || decision);

export default function VerificationQueue() {
  const [items, setItems] = useState([]);
  const [selected, setSelected] = useState(null);
  const [remarks, setRemarks] = useState('');
  const [overdueOnly, setOverdueOnly] = useState(false);
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const load = async () => {
    setError('');
    try {
      setItems(await verificationApi.queue(overdueOnly) || []);
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to load verification queue.');
    }
  };

  useEffect(() => { load(); }, [overdueOnly]);

  const open = async (complaintId) => {
    setBusy(true);
    setError('');
    setMessage('');
    try {
      setSelected(await verificationApi.get(complaintId));
      setRemarks('');
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to open verification.');
    } finally {
      setBusy(false);
    }
  };

  const remind = async (complaintId) => {
    setBusy(true);
    setError('');
    try {
      const result = await verificationApi.remind(complaintId);
      setSelected((current) => current?.complaintId === complaintId ? result : current);
      setMessage('Citizen verification reminder sent and audited.');
      await load();
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to send reminder.');
    } finally {
      setBusy(false);
    }
  };

  const decide = async (approveCitizen) => {
    if (!selected || remarks.trim().length < 10) {
      setError('Enter decision remarks of at least 10 characters.');
      return;
    }

    setBusy(true);
    setError('');
    try {
      await verificationApi.supervisorDecision(selected.complaintId, {
        approveCitizen,
        remarks: remarks.trim(),
      });
      setMessage(approveCitizen
        ? 'Citizen decision upheld. Complaint returned for rework.'
        : 'Officer evidence accepted. Complaint closed with an appeal window.');
      setSelected(null);
      setRemarks('');
      await load();
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to save supervisor decision.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="space-y-6 p-6 lg:p-10">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-sm font-semibold uppercase tracking-[.18em] text-sky-400">Verification SLA</p>
          <h1 className="mt-2 text-3xl font-black text-white">Verification queue</h1>
          <p className="mt-2 text-slate-400">Send reminders and decide citizen verification challenges within your department and ward scope.</p>
        </div>
        <label className="flex items-center gap-2 rounded-xl border border-white/10 px-4 py-2 text-sm text-slate-300">
          <input type="checkbox" checked={overdueOnly} onChange={(event) => setOverdueOnly(event.target.checked)} />
          Overdue citizen decisions only
        </label>
      </div>

      {message && <p className="rounded-xl bg-emerald-500/10 p-4 text-emerald-200">{message}</p>}
      {error && <p className="rounded-xl bg-rose-500/10 p-4 text-rose-200">{error}</p>}

      <div className="grid gap-5 xl:grid-cols-[1fr_1.2fr]">
        <div className="space-y-4">
          {items.length === 0 && <p className="rounded-2xl border border-white/10 bg-white/5 p-6 text-slate-400">No verification records match this queue.</p>}
          {items.map((item) => (
            <article key={item.complaintId} className="rounded-2xl border border-white/10 bg-white/5 p-5">
              <div className="flex items-start justify-between gap-3">
                <div>
                  <p className="font-mono text-xs text-sky-300">{item.referenceNumber}</p>
                  <h2 className="mt-2 font-black text-white">{item.title}</h2>
                  <p className="mt-2 text-sm text-slate-400">{item.departmentName} · {item.wardName} · {item.priority}</p>
                </div>
                <span className={`rounded-full px-3 py-1 text-xs font-bold ${item.requiresSupervisorDecision ? 'bg-rose-400/10 text-rose-200' : 'bg-amber-400/10 text-amber-200'}`}>
                  {label(item.decision)}
                </span>
              </div>
              <p className={`mt-3 text-xs ${item.isOverdue ? 'text-rose-300' : 'text-slate-500'}`}>
                {item.isOverdue ? 'Citizen verification is overdue.' : `Due ${new Date(item.dueAt).toLocaleString()}`}
              </p>
              <p className="mt-2 text-xs text-slate-500">
                Reminders: {item.reminderCount || 0}/{item.reminderLimit || 0}
                {item.lastReminderSentAt ? ` · Last sent ${formatDate(item.lastReminderSentAt)}` : ''}
              </p>
              {!item.canRemind && item.reminderUnavailableReason && !item.requiresSupervisorDecision && (
                <p className="mt-2 text-xs text-amber-200">{item.reminderUnavailableReason}</p>
              )}
              <div className="mt-4 flex flex-wrap gap-3">
                <button type="button" onClick={() => open(item.complaintId)} className="rounded-lg bg-sky-500 px-4 py-2 text-sm font-bold text-white">
                  Open
                </button>
                {!item.requiresSupervisorDecision && (
                  <button type="button" disabled={busy || !item.canRemind} onClick={() => remind(item.complaintId)} className="rounded-lg border border-amber-400/30 px-4 py-2 text-sm font-bold text-amber-200 disabled:opacity-50">
                    Send reminder
                  </button>
                )}
              </div>
            </article>
          ))}
        </div>

        <div className="rounded-3xl border border-white/10 bg-white/[.04] p-6">
          {!selected && <div className="flex min-h-64 items-center justify-center text-center text-slate-400">Open a queue item to review its citizen decision.</div>}
          {selected && (
            <div className="space-y-5">
              <div>
                <p className="font-mono text-sm text-sky-300">{selected.referenceNumber}</p>
                <h2 className="mt-1 text-2xl font-black text-white">{selected.title}</h2>
                <p className="mt-2 text-sm text-slate-400">Citizen decision: {label(selected.decision)}</p>
              </div>

              <div className="grid gap-3 sm:grid-cols-3">
                <Metric label="Rating" value={selected.rating ? `${selected.rating}/5` : 'Not given'} />
                <Metric label="Distance" value={selected.distanceMetres == null ? 'Not captured' : `${Math.round(selected.distanceMetres)} m`} />
                <Metric label="Citizen evidence" value={selected.citizenEvidence?.length || 0} />
              </div>

              {selected.decision === 'Pending' && (
                <div className="rounded-xl border border-amber-400/20 bg-amber-400/5 p-4 text-sm text-slate-300">
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div>
                      <p className="font-bold text-amber-100">Controlled verification reminders</p>
                      <p className="mt-1 text-xs text-slate-400">
                        {selected.reminderCount || 0}/{selected.reminderLimit || 0} sent · Last: {formatDate(selected.lastReminderSentAt)}
                      </p>
                    </div>
                    <button type="button" disabled={busy || !selected.canRemind} onClick={() => remind(selected.complaintId)} className="rounded-lg border border-amber-400/30 px-4 py-2 text-sm font-bold text-amber-200 disabled:opacity-50">
                      Send reminder
                    </button>
                  </div>
                  {!selected.canRemind && selected.reminderUnavailableReason && (
                    <p className="mt-3 text-xs text-amber-200">{selected.reminderUnavailableReason}</p>
                  )}
                  {selected.nextReminderAllowedAt && (
                    <p className="mt-2 text-xs text-slate-500">Next eligible time: {formatDate(selected.nextReminderAllowedAt)}</p>
                  )}
                </div>
              )}

              {selected.remarks && <p className="rounded-xl bg-slate-950/50 p-4 text-slate-300">{selected.remarks}</p>}

              {selected.citizenEvidence?.length > 0 && (
                <ul className="space-y-2 rounded-xl border border-white/10 p-4 text-sm text-slate-300">
                  {selected.citizenEvidence.map((image) => (
                    <li key={image.id} className="flex justify-between gap-3"><span>{image.fileName}</span><span className="text-slate-500">{Math.ceil(image.fileSize / 1024)} KB</span></li>
                  ))}
                </ul>
              )}

              {selected.canAmend ? (
                <div className="space-y-4 border-t border-white/10 pt-5">
                  <label className="block text-sm font-bold text-slate-200">
                    Supervisor remarks
                    <textarea
                      className="input mt-2 min-h-28"
                      placeholder="Explain why the citizen is correct or why the officer evidence is sufficient"
                      value={remarks}
                      onChange={(event) => setRemarks(event.target.value)}
                    />
                  </label>
                  <div className="flex flex-wrap gap-3">
                    <button type="button" disabled={busy} onClick={() => decide(true)} className="rounded-xl bg-emerald-500 px-4 py-2 font-black text-white disabled:opacity-50">
                      Citizen correct — request rework
                    </button>
                    <button type="button" disabled={busy} onClick={() => decide(false)} className="rounded-xl bg-rose-500 px-4 py-2 font-black text-white disabled:opacity-50">
                      Officer evidence sufficient
                    </button>
                  </div>
                </div>
              ) : (
                <p className="rounded-xl bg-slate-950/50 p-4 text-sm text-slate-400">This item is awaiting the citizen’s initial decision; use the reminder action when necessary.</p>
              )}
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

function Metric({ label: metricLabel, value }) {
  return <div className="rounded-xl bg-slate-950/50 p-4"><p className="text-xs uppercase text-slate-500">{metricLabel}</p><p className="mt-1 font-black text-white">{value}</p></div>;
}
