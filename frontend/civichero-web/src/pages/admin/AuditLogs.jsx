import { useEffect, useState } from 'react';
import { exportAuditLogs, getAuditLogs } from '../../services/adminApi.js';
import { downloadBlob } from '../../utils/exportHelper.js';

export default function AuditLogs() {
  const [filters, setFilters] = useState({ search: '', ipAddress: '', success: '', page: 1, pageSize: 25 });
  const [data, setData] = useState({ items: [], totalCount: 0, page: 1, pageSize: 25 });
  const [format, setFormat] = useState('xlsx');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const toRequestFilters = (value) => ({
    ...value,
    search: value.search.trim() || undefined,
    ipAddress: value.ipAddress.trim() || undefined,
    success: value.success === '' ? undefined : value.success,
  });

  const load = async (nextFilters = filters) => {
    try {
      setError('');
      setData(await getAuditLogs(toRequestFilters(nextFilters)));
    } catch (reason) {
      setError(reason.message);
    }
  };

  useEffect(() => { load(filters); }, [filters.page, filters.success]);

  const search = () => {
    const next = { ...filters, page: 1 };
    setFilters(next);
    load(next);
  };

  const clear = () => {
    const next = { search: '', ipAddress: '', success: '', page: 1, pageSize: 25 };
    setFilters(next);
    load(next);
  };

  const download = async () => {
    setBusy(true); setError('');
    try {
      const result = await exportAuditLogs(format, { ...toRequestFilters(filters), page: undefined, pageSize: undefined });
      downloadBlob(result.blob, result.fileName);
    } catch (reason) {
      setError(reason.message);
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="p-6 lg:p-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div><p className="text-xs font-black uppercase tracking-[0.2em] text-violet-300">Immutable history</p><h1 className="mt-2 text-3xl font-black text-white">Audit logs</h1><p className="mt-2 text-slate-400">Trace every mutating API action with user, role, endpoint, result and correlation ID.</p></div>
        <div className="flex gap-2"><select value={format} onChange={(event) => setFormat(event.target.value)} className="rounded-xl border border-white/10 bg-slate-950 px-3 py-2 text-white"><option value="csv">CSV</option><option value="xlsx">Excel</option><option value="pdf">PDF</option></select><button disabled={busy} onClick={download} className="rounded-xl bg-violet-500 px-4 py-2 font-bold text-white disabled:opacity-50">{busy ? 'Preparing…' : `Export ${format.toUpperCase()}`}</button></div>
      </div>
      <div className="mt-6 grid gap-3 lg:grid-cols-[1fr_240px_180px_auto_auto]">
        <input value={filters.search} onChange={(event) => setFilters({ ...filters, search: event.target.value })} placeholder="Search user, action, entity, correlation ID or IP" className="rounded-xl border border-white/10 bg-white/5 px-4 py-3 text-white"/>
        <input maxLength={64} value={filters.ipAddress} onChange={(event) => setFilters({ ...filters, ipAddress: event.target.value })} placeholder="IP address, exact or partial" className="rounded-xl border border-white/10 bg-white/5 px-4 py-3 font-mono text-white"/>
        <select value={filters.success} onChange={(event) => setFilters({ ...filters, success: event.target.value, page: 1 })} className="rounded-xl border border-white/10 bg-slate-950 px-4 py-3 text-white"><option value="">All results</option><option value="true">Successful</option><option value="false">Failed</option></select>
        <button onClick={search} className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white">Search</button>
        <button onClick={clear} className="rounded-xl border border-white/10 bg-white/5 px-5 py-3 font-bold text-slate-200">Clear</button>
      </div>
      {error && <p className="mt-4 text-rose-300">{error}</p>}
      <div className="mt-6 overflow-x-auto rounded-2xl border border-white/10"><table className="min-w-[1000px] w-full text-left text-sm"><thead className="bg-white/5 text-slate-400"><tr><th className="p-4">Time</th><th>User</th><th>Action</th><th>Entity</th><th>IP address</th><th>Result</th><th>Correlation</th></tr></thead><tbody>{data.items.map((item) => <tr key={item.id} className="border-t border-white/10 text-slate-300"><td className="p-4">{new Date(item.createdAt).toLocaleString()}</td><td><b className="text-white">{item.userEmail || 'System'}</b><br/><span className="text-xs">{item.userRole || 'Background'}</span></td><td>{item.action}</td><td>{item.entityName}{item.entityId ? ` #${item.entityId}` : ''}</td><td className="font-mono text-xs">{item.ipAddress || '—'}</td><td><span className={item.success ? 'text-emerald-300' : 'text-rose-300'}>{item.success ? 'Success' : 'Failed'} · {item.httpStatusCode}</span></td><td className="font-mono text-xs">{item.correlationId || '—'}</td></tr>)}</tbody></table></div>
      <div className="mt-4 flex items-center justify-between text-sm text-slate-400"><span>{data.totalCount} events</span><div className="flex gap-2"><button disabled={filters.page <= 1} onClick={() => setFilters({ ...filters, page: filters.page - 1 })} className="rounded-lg bg-white/5 px-3 py-2 disabled:opacity-40">Previous</button><span className="px-2 py-2">Page {filters.page}</span><button disabled={filters.page * filters.pageSize >= data.totalCount} onClick={() => setFilters({ ...filters, page: filters.page + 1 })} className="rounded-lg bg-white/5 px-3 py-2 disabled:opacity-40">Next</button></div></div>
    </section>
  );
}
