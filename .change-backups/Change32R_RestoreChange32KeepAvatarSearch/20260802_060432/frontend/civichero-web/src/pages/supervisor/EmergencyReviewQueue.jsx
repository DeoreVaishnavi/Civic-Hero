import { useEffect, useState } from 'react';
import { emergencyReviewApi } from '../../services/addonReviewApi.js';

export default function EmergencyReviewQueue() {
  const [data, setData] = useState({ items: [] });
  const [status, setStatus] = useState('Pending');
  const [error, setError] = useState('');

  const load = async () => {
    try {
      setError('');
      setData(await emergencyReviewApi.list({ status, page: 1, pageSize: 50 }));
    } catch (reason) {
      setError(reason.message || 'Unable to load emergency reviews.');
    }
  };

  useEffect(() => { void load(); }, [status]);

  const decide = async (item, decision) => {
    const reason = prompt(`Official reason for ${decision.toLowerCase()}:`) || '';
    if (!reason) return;
    let confirmedPriority = null;
    if (decision === 'Confirmed') confirmedPriority = prompt('Confirmed priority: High or Critical', 'High') || 'High';
    try {
      await emergencyReviewApi.decide(item.id, { decision, confirmedPriority, reason });
      await load();
    } catch (apiError) {
      setError(apiError.errors?.join(' ') || apiError.message);
    }
  };

  return (
    <section className="space-y-6 p-6 lg:p-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p className="text-sm font-bold uppercase tracking-[.18em] text-red-700">Safety review</p>
          <h2 className="mt-2 text-3xl font-black text-slate-900">Possible emergency review</h2>
          <p className="mt-2 text-slate-600">Citizens can flag risk, but only an authorized reviewer confirms High or Critical priority.</p>
        </div>
        <select className="input mt-0 max-w-xs" value={status} onChange={(event) => setStatus(event.target.value)}>
          <option>Pending</option><option>Confirmed</option><option>Rejected</option><option value="">All</option>
        </select>
      </div>

      {error && <p className="cv-alert cv-alert-error">{error}</p>}

      <div className="space-y-4">
        {data.items?.map((item) => (
          <article key={item.id} className="cv-card rounded-2xl border p-5 shadow-sm">
            <div className="flex flex-wrap justify-between gap-4">
              <div>
                <p className="font-mono text-sm font-semibold text-blue-700">{item.referenceNumber}</p>
                <h3 className="mt-1 text-xl font-black text-slate-900">{item.complaintTitle}</h3>
                <p className="mt-1 text-sm text-slate-600">{item.departmentName} · {item.wardName} · {item.isAnonymous ? 'Anonymous reporter' : 'Registered citizen'}</p>
              </div>
              <span className={item.status === 'Pending' ? 'cv-status-amber h-fit' : item.status === 'Confirmed' ? 'cv-status-red h-fit' : 'cv-status-blue h-fit'}>{item.status}</span>
            </div>
            <p className="mt-4 rounded-xl border border-slate-200 bg-slate-50 p-4 text-slate-800">{item.reporterReason}</p>
            {item.status === 'Pending' && (
              <div className="mt-4 flex flex-wrap gap-3">
                <button onClick={() => decide(item, 'Confirmed')} className="cv-btn-danger">Confirm emergency</button>
                <button onClick={() => decide(item, 'Rejected')} className="cv-btn-secondary">Reject flag</button>
              </div>
            )}
          </article>
        ))}
        {!data.items?.length && <p className="cv-card rounded-2xl border p-8 text-center text-slate-600">No reviews match this filter.</p>}
      </div>
    </section>
  );
}

