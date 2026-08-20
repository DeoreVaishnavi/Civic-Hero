import { useEffect, useState } from 'react';
import { verificationApi } from '../../services/verificationApi.js';

const label = (decision) => ({
  Pending: 'Pending',
  Approved: 'Approved',
  NotResolvedYet: 'Not resolved yet',
  RequestRevisit: 'Revisit requested',
  PartiallyResolved: 'Partially resolved',
  Rejected: 'Rejected',
  AutoClosed: 'Auto closed',
  AdminOverride: 'Admin override',
}[decision] || decision);

export default function VerificationHistory() {
  const [items, setItems] = useState([]);
  const [error, setError] = useState('');

  const load = async () => {
    setError('');
    try {
      setItems(await verificationApi.history() || []);
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message || 'Unable to load verification history.');
    }
  };

  useEffect(() => { load(); }, []);

  return (
    <section className="space-y-5 p-6 lg:p-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-sm font-semibold uppercase tracking-[.18em] text-sky-400">Audit</p>
          <h1 className="mt-2 text-3xl font-black text-white">Verification history</h1>
          <p className="mt-2 text-slate-400">Citizen outcomes, evidence totals, and the latest supervisor decision.</p>
        </div>
        <button type="button" onClick={load} className="rounded-xl border border-white/10 px-4 py-2 text-sm font-bold text-slate-200">Refresh</button>
      </div>

      {error && <p className="rounded-xl bg-rose-500/10 p-4 text-rose-200">{error}</p>}

      <div className="overflow-x-auto rounded-2xl border border-white/10 bg-white/5">
        <table className="min-w-full text-left text-sm">
          <thead className="text-xs uppercase text-slate-500">
            <tr>
              <th className="p-4">Complaint</th>
              <th className="p-4">Department / Ward</th>
              <th className="p-4">Citizen decision</th>
              <th className="p-4">Rating</th>
              <th className="p-4">Evidence</th>
              <th className="p-4">Supervisor decision</th>
              <th className="p-4">Completed</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/10">
            {items.length === 0 && <tr><td colSpan="7" className="p-6 text-center text-slate-400">No verification records found.</td></tr>}
            {items.map((item) => (
              <tr key={item.verificationId}>
                <td className="p-4"><p className="font-mono text-xs text-sky-300">{item.referenceNumber}</p><p className="mt-1 font-bold text-white">{item.title}</p></td>
                <td className="p-4 text-slate-400">{item.departmentName}<br />{item.wardName}</td>
                <td className="p-4"><p className="text-slate-200">{label(item.decision)}</p><p className="mt-1 text-xs text-slate-500">{item.complaintStatus}</p></td>
                <td className="p-4 text-slate-300">{item.rating ?? '—'}</td>
                <td className="p-4 text-slate-300">{item.citizenEvidenceCount}</td>
                <td className="p-4"><p className="text-slate-300">{item.supervisorDecision || '—'}</p>{item.supervisorRemarks && <p className="mt-1 max-w-xs text-xs text-slate-500">{item.supervisorRemarks}</p>}</td>
                <td className="p-4 text-slate-400">{item.completedAt ? new Date(item.completedAt).toLocaleString() : 'Pending'}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
