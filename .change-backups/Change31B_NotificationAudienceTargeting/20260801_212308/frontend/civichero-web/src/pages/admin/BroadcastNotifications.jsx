import { useCallback, useEffect, useMemo, useState } from 'react';
import { notificationApi } from '../../services/notificationApi.js';
import { downloadBlob } from '../../utils/exportHelper.js';

const blankBroadcast = {
  templateKey: '', title: '', message: '', role: '', actionUrl: '', type: 'General',
  sendInApp: true, sendSignalR: true, sendSms: false, sendEmail: false, scheduledFor: '',
};

const blankTemplate = {
  key: '', name: '', titleTemplate: '', messageTemplate: '', type: 'General', actionUrlTemplate: '', isActive: true,
};

const notificationTypes = [
  'General', 'ComplaintCreated', 'ComplaintAssigned', 'ComplaintProgress', 'ResolutionReady',
  'VerificationRequired', 'DisputeUpdate', 'RewardEarned', 'SlaEscalation', 'SecurityAlert',
];

export default function BroadcastNotifications() {
  const [tab, setTab] = useState('broadcast');
  const [templates, setTemplates] = useState([]);
  const [schedules, setSchedules] = useState([]);
  const [deliveries, setDeliveries] = useState({ items: [], totalCount: 0, page: 1, pageSize: 25 });
  const [summary, setSummary] = useState({ total: 0, sent: 0, failed: 0, skipped: 0, notConfigured: 0, byChannel: {} });
  const [broadcast, setBroadcast] = useState(blankBroadcast);
  const [templateForm, setTemplateForm] = useState(blankTemplate);
  const [editingTemplateKey, setEditingTemplateKey] = useState('');
  const [filters, setFilters] = useState({ channel: '', status: '', search: '', page: 1, pageSize: 25 });
  const [status, setStatus] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [deliveryFormat, setDeliveryFormat] = useState('xlsx');

  const loadTemplates = useCallback(async () => setTemplates(await notificationApi.adminTemplates()), []);
  const loadSchedules = useCallback(async () => setSchedules(await notificationApi.adminSchedules()), []);
  const loadDeliveries = useCallback(async () => {
    const [list, totals] = await Promise.all([
      notificationApi.adminDeliveries({ ...filters, channel: filters.channel || undefined, status: filters.status || undefined, search: filters.search || undefined }),
      notificationApi.adminDeliverySummary(),
    ]);
    setDeliveries(list);
    setSummary(totals);
  }, [filters]);

  const refreshCurrentTab = useCallback(async () => {
    setError('');
    try {
      if (tab === 'templates') await loadTemplates();
      if (tab === 'deliveries') await loadDeliveries();
      if (tab === 'schedules') await loadSchedules();
    } catch (reason) {
      setError(reason.message);
    }
  }, [tab, loadTemplates, loadDeliveries, loadSchedules]);

  useEffect(() => { loadTemplates().catch((reason) => setError(reason.message)); }, [loadTemplates]);
  useEffect(() => { refreshCurrentTab(); }, [refreshCurrentTab]);

  const selectedTemplate = useMemo(
    () => templates.find((item) => item.key === broadcast.templateKey),
    [templates, broadcast.templateKey],
  );

  const chooseTemplate = (key) => {
    const item = templates.find((candidate) => candidate.key === key);
    setBroadcast((current) => item ? {
      ...current,
      templateKey: key,
      title: item.titleTemplate,
      message: item.messageTemplate,
      type: item.type,
      actionUrl: item.actionUrlTemplate || '',
    } : { ...current, templateKey: '' });
  };

  const submitBroadcast = async (event) => {
    event.preventDefault();
    setLoading(true); setError(''); setStatus('');
    try {
      const result = await notificationApi.adminBroadcast({
        ...broadcast,
        templateKey: broadcast.templateKey || null,
        role: broadcast.role || null,
        actionUrl: broadcast.actionUrl || null,
        scheduledFor: broadcast.scheduledFor ? new Date(broadcast.scheduledFor).toISOString() : null,
      });
      if (result.status === 'Pending') {
        setStatus(`Broadcast ${result.broadcastId} scheduled for ${formatDate(result.scheduledFor)}.`);
      } else {
        setStatus(`Broadcast processed for ${result.targetUsers} users: ${result.successfulDeliveries} sent, ${result.failedDeliveries} failed, ${result.skippedDeliveries} skipped.`);
      }
      setBroadcast(blankBroadcast);
      await Promise.all([loadSchedules(), loadDeliveries()]);
    } catch (reason) {
      setError(reason.message);
    } finally {
      setLoading(false);
    }
  };

  const saveTemplate = async (event) => {
    event.preventDefault();
    setLoading(true); setError(''); setStatus('');
    try {
      const payload = { ...templateForm, actionUrlTemplate: templateForm.actionUrlTemplate || null };
      if (editingTemplateKey) {
        await notificationApi.updateAdminTemplate(editingTemplateKey, payload);
        setStatus('Notification template updated.');
      } else {
        await notificationApi.createAdminTemplate(payload);
        setStatus('Notification template created.');
      }
      setTemplateForm(blankTemplate);
      setEditingTemplateKey('');
      await loadTemplates();
    } catch (reason) {
      setError(reason.message);
    } finally {
      setLoading(false);
    }
  };

  const editTemplate = (item) => {
    setEditingTemplateKey(item.key);
    setTemplateForm({
      key: item.key,
      name: item.name,
      titleTemplate: item.titleTemplate,
      messageTemplate: item.messageTemplate,
      type: item.type,
      actionUrlTemplate: item.actionUrlTemplate || '',
      isActive: item.isActive,
    });
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const removeTemplate = async (key) => {
    if (!window.confirm(`Delete notification template "${key}"?`)) return;
    setError(''); setStatus('');
    try {
      await notificationApi.deleteAdminTemplate(key);
      setStatus('Notification template deleted.');
      await loadTemplates();
    } catch (reason) {
      setError(reason.message);
    }
  };

  const retryDelivery = async (item) => {
    const reason = window.prompt('Reason for retry (minimum 10 characters):');
    if (!reason) return;
    setError(''); setStatus('');
    try {
      const result = await notificationApi.retryAdminDelivery(item.id, { reason });
      setStatus(`${result.channel} retry finished with status ${result.status}.`);
      await loadDeliveries();
    } catch (reasonValue) {
      setError(reasonValue.message);
    }
  };

  const exportDeliveries = async () => {
    setLoading(true); setError(''); setStatus('');
    try {
      const result = await notificationApi.exportAdminDeliveries(deliveryFormat, {
        channel: filters.channel || undefined,
        status: filters.status || undefined,
        search: filters.search || undefined,
      });
      downloadBlob(result.blob, result.fileName);
      setStatus(`Delivery report downloaded as ${deliveryFormat.toUpperCase()}.`);
    } catch (reason) {
      setError(reason.message);
    } finally {
      setLoading(false);
    }
  };

  const cancelSchedule = async (item) => {
    if (!window.confirm(`Cancel scheduled broadcast ${item.id}?`)) return;
    setError(''); setStatus('');
    try {
      await notificationApi.cancelAdminSchedule(item.id);
      setStatus('Scheduled broadcast cancelled.');
      await loadSchedules();
    } catch (reason) {
      setError(reason.message);
    }
  };

  return (
    <section className="p-6 lg:p-10">
      <p className="text-sm font-bold uppercase tracking-wider text-sky-300">Administrative communication</p>
      <h2 className="mt-2 text-3xl font-black text-white">Notification administration</h2>
      <p className="mt-2 max-w-4xl text-slate-400">Create reusable templates, send or schedule broadcasts, inspect channel delivery outcomes, and retry unsuccessful attempts.</p>

      <div className="mt-6 flex flex-wrap gap-2">
        {[
          ['broadcast', 'Broadcast'], ['templates', 'Templates'], ['deliveries', 'Delivery history'], ['schedules', 'Schedules'],
        ].map(([value, label]) => (
          <button key={value} type="button" onClick={() => setTab(value)} className={tab === value ? activeTab : inactiveTab}>{label}</button>
        ))}
        <button type="button" onClick={refreshCurrentTab} className="ml-auto rounded-xl border border-white/10 px-4 py-2 text-sm font-bold text-slate-200 hover:bg-white/10">Refresh</button>
      </div>

      {error && <Alert tone="error">{error}</Alert>}
      {status && <Alert>{status}</Alert>}

      {tab === 'broadcast' && (
        <form onSubmit={submitBroadcast} className="mt-8 max-w-4xl space-y-5 rounded-2xl border border-white/10 bg-white/5 p-7">
          <div className="grid gap-5 md:grid-cols-2">
            <Field label="Template">
              <select value={broadcast.templateKey} onChange={(event) => chooseTemplate(event.target.value)} className="input">
                <option value="">Custom message</option>
                {templates.filter((item) => item.isActive).map((item) => <option key={item.key} value={item.key}>{item.name}</option>)}
              </select>
            </Field>
            <Field label="Audience">
              <select value={broadcast.role} onChange={(event) => setBroadcast({ ...broadcast, role: event.target.value })} className="input">
                <option value="">All active users</option><option>Citizen</option><option>Officer</option><option>Supervisor</option><option>Admin</option><option>SuperAdmin</option>
              </select>
            </Field>
          </div>
          {selectedTemplate && <p className="rounded-xl border border-sky-400/20 bg-sky-400/10 p-3 text-sm text-sky-100">Template placeholders supported: {'{{FullName}}'}, {'{{Email}}'}, {'{{Role}}'}, {'{{UserId}}'}.</p>}
          <Field label="Title"><input required={!broadcast.templateKey} maxLength={200} value={broadcast.title} onChange={(event) => setBroadcast({ ...broadcast, title: event.target.value })} className="input" /></Field>
          <Field label="Message"><textarea required={!broadcast.templateKey} rows={6} maxLength={2000} value={broadcast.message} onChange={(event) => setBroadcast({ ...broadcast, message: event.target.value })} className="input" /></Field>
          <div className="grid gap-5 md:grid-cols-2">
            <Field label="Notification type"><select value={broadcast.type} onChange={(event) => setBroadcast({ ...broadcast, type: event.target.value })} className="input">{notificationTypes.map((item) => <option key={item}>{item}</option>)}</select></Field>
            <Field label="Optional action URL"><input placeholder="/citizen/complaints" maxLength={500} value={broadcast.actionUrl} onChange={(event) => setBroadcast({ ...broadcast, actionUrl: event.target.value })} className="input" /></Field>
          </div>
          <div>
            <p className="text-sm font-bold text-slate-300">Delivery channels</p>
            <div className="mt-3 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
              <Check label="In-app" checked={broadcast.sendInApp} onChange={(value) => setBroadcast({ ...broadcast, sendInApp: value })} />
              <Check label="SignalR" checked={broadcast.sendSignalR} onChange={(value) => setBroadcast({ ...broadcast, sendSignalR: value })} />
              <Check label="SMS" checked={broadcast.sendSms} onChange={(value) => setBroadcast({ ...broadcast, sendSms: value })} />
              <Check label="Email" checked={broadcast.sendEmail} onChange={(value) => setBroadcast({ ...broadcast, sendEmail: value })} />
            </div>
            {broadcast.sendEmail && <p className="mt-2 text-xs text-amber-300">Email attempts are recorded as NotConfigured until a production email sender is added.</p>}
          </div>
          <Field label="Schedule for later (optional)"><input type="datetime-local" value={broadcast.scheduledFor} onChange={(event) => setBroadcast({ ...broadcast, scheduledFor: event.target.value })} className="input" /></Field>
          <button disabled={loading} className="rounded-xl bg-sky-500 px-6 py-3 font-black text-white hover:bg-sky-400 disabled:opacity-50">{broadcast.scheduledFor ? 'Schedule broadcast' : 'Send broadcast'}</button>
        </form>
      )}

      {tab === 'templates' && (
        <div className="mt-8 grid gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(0,1.2fr)]">
          <form onSubmit={saveTemplate} className="space-y-4 rounded-2xl border border-white/10 bg-white/5 p-6">
            <h3 className="text-xl font-black text-white">{editingTemplateKey ? 'Edit template' : 'Create template'}</h3>
            <Field label="Template key"><input required disabled={Boolean(editingTemplateKey)} pattern="[A-Za-z0-9][A-Za-z0-9_-]{1,59}" value={templateForm.key} onChange={(event) => setTemplateForm({ ...templateForm, key: event.target.value })} className="input disabled:opacity-60" /></Field>
            <Field label="Display name"><input required maxLength={120} value={templateForm.name} onChange={(event) => setTemplateForm({ ...templateForm, name: event.target.value })} className="input" /></Field>
            <Field label="Title template"><input required maxLength={200} value={templateForm.titleTemplate} onChange={(event) => setTemplateForm({ ...templateForm, titleTemplate: event.target.value })} className="input" /></Field>
            <Field label="Message template"><textarea required rows={5} maxLength={2000} value={templateForm.messageTemplate} onChange={(event) => setTemplateForm({ ...templateForm, messageTemplate: event.target.value })} className="input" /></Field>
            <Field label="Type"><select value={templateForm.type} onChange={(event) => setTemplateForm({ ...templateForm, type: event.target.value })} className="input">{notificationTypes.map((item) => <option key={item}>{item}</option>)}</select></Field>
            <Field label="Action URL template"><input maxLength={500} value={templateForm.actionUrlTemplate} onChange={(event) => setTemplateForm({ ...templateForm, actionUrlTemplate: event.target.value })} className="input" /></Field>
            <Check label="Template is active" checked={templateForm.isActive} onChange={(value) => setTemplateForm({ ...templateForm, isActive: value })} />
            <div className="flex gap-3">
              <button disabled={loading} className="rounded-xl bg-sky-500 px-5 py-2.5 font-black text-white disabled:opacity-50">{editingTemplateKey ? 'Save changes' : 'Create template'}</button>
              {editingTemplateKey && <button type="button" onClick={() => { setEditingTemplateKey(''); setTemplateForm(blankTemplate); }} className="rounded-xl border border-white/10 px-5 py-2.5 font-bold text-slate-200">Cancel</button>}
            </div>
          </form>
          <div className="space-y-4">
            {templates.length === 0 && <Empty>No notification templates have been created.</Empty>}
            {templates.map((item) => (
              <article key={item.key} className="rounded-2xl border border-white/10 bg-white/5 p-5">
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div><p className="text-lg font-black text-white">{item.name}</p><p className="text-xs text-slate-500">{item.key} · {item.type}</p></div>
                  <Status value={item.isActive ? 'Active' : 'Inactive'} />
                </div>
                <p className="mt-4 font-bold text-slate-200">{item.titleTemplate}</p>
                <p className="mt-2 whitespace-pre-wrap text-sm text-slate-400">{item.messageTemplate}</p>
                <div className="mt-4 flex gap-2"><button type="button" onClick={() => editTemplate(item)} className="smallButton">Edit</button><button type="button" onClick={() => removeTemplate(item.key)} className="smallDanger">Delete</button></div>
              </article>
            ))}
          </div>
        </div>
      )}

      {tab === 'deliveries' && (
        <div className="mt-8 space-y-5">
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
            <Metric label="Recorded" value={summary.total} /><Metric label="Sent" value={summary.sent} /><Metric label="Failed" value={summary.failed} /><Metric label="Skipped" value={summary.skipped} /><Metric label="Not configured" value={summary.notConfigured} />
          </div>
          <div className="grid gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 md:grid-cols-5">
            <input placeholder="Recipient, email or title" value={filters.search} onChange={(event) => setFilters({ ...filters, search: event.target.value, page: 1 })} className="input !mt-0" />
            <select value={filters.channel} onChange={(event) => setFilters({ ...filters, channel: event.target.value, page: 1 })} className="input !mt-0"><option value="">All channels</option><option>InApp</option><option>SignalR</option><option>SMS</option><option>Email</option></select>
            <select value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value, page: 1 })} className="input !mt-0"><option value="">All statuses</option><option>Sent</option><option>Failed</option><option>Skipped</option><option>NotConfigured</option></select>
            <select value={deliveryFormat} onChange={(event) => setDeliveryFormat(event.target.value)} className="input !mt-0"><option value="csv">CSV</option><option value="xlsx">Excel</option><option value="pdf">PDF</option></select>
            <div className="grid grid-cols-2 gap-2"><button type="button" onClick={loadDeliveries} className="rounded-xl border border-white/10 px-3 py-2 font-black text-white hover:bg-white/10">Filter</button><button type="button" disabled={loading} onClick={exportDeliveries} className="rounded-xl bg-sky-500 px-3 py-2 font-black text-white disabled:opacity-50">Export</button></div>
          </div>
          <div className="overflow-x-auto rounded-2xl border border-white/10">
            <table className="min-w-full divide-y divide-white/10 text-left text-sm">
              <thead className="bg-white/5 text-xs uppercase text-slate-400"><tr><Th>Time</Th><Th>Recipient</Th><Th>Channel</Th><Th>Status</Th><Th>Message</Th><Th>Attempt</Th><Th>Action</Th></tr></thead>
              <tbody className="divide-y divide-white/10">
                {deliveries.items.map((item) => <tr key={item.id} className="align-top"><Td>{formatDate(item.createdAt)}</Td><Td><p className="font-bold text-white">{item.recipientName}</p><p className="text-xs text-slate-500">{item.recipientEmail}</p></Td><Td>{item.channel}<p className="text-xs text-slate-500">{item.provider || '—'}</p></Td><Td><Status value={item.status} />{item.error && <p className="mt-1 max-w-xs text-xs text-rose-300">{item.error}</p>}</Td><Td><p className="max-w-xs font-bold text-slate-200">{item.title}</p><p className="mt-1 max-w-xs line-clamp-2 text-xs text-slate-500">{item.message}</p></Td><Td>#{item.attemptNumber}</Td><Td>{item.status !== 'Sent' && <button type="button" onClick={() => retryDelivery(item)} className="smallButton">Retry</button>}</Td></tr>)}
                {deliveries.items.length === 0 && <tr><Td colSpan={7}>No delivery records match the filters.</Td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={deliveries.page} pageSize={deliveries.pageSize} total={deliveries.totalCount} onPage={(page) => setFilters({ ...filters, page })} />
        </div>
      )}

      {tab === 'schedules' && (
        <div className="mt-8 space-y-4">
          {schedules.length === 0 && <Empty>No scheduled broadcasts have been created.</Empty>}
          {schedules.map((item) => (
            <article key={item.id} className="rounded-2xl border border-white/10 bg-white/5 p-5">
              <div className="flex flex-wrap items-start justify-between gap-3"><div><p className="font-black text-white">{item.title || item.templateKey || 'Scheduled notification'}</p><p className="mt-1 text-xs text-slate-500">{item.id}</p></div><Status value={item.status} /></div>
              <div className="mt-4 grid gap-3 text-sm text-slate-300 sm:grid-cols-2 lg:grid-cols-4"><p><span className="text-slate-500">Audience:</span> {item.role || 'All users'}</p><p><span className="text-slate-500">Scheduled:</span> {formatDate(item.scheduledFor)}</p><p><span className="text-slate-500">Recipients:</span> {item.recipientCount}</p><p><span className="text-slate-500">Created:</span> {formatDate(item.createdAt)}</p></div>
              {item.error && <p className="mt-3 rounded-xl bg-rose-500/10 p-3 text-sm text-rose-200">{item.error}</p>}
              {item.status === 'Pending' && <button type="button" onClick={() => cancelSchedule(item)} className="smallDanger mt-4">Cancel schedule</button>}
            </article>
          ))}
        </div>
      )}

      <style>{`.input{margin-top:.5rem;width:100%;border-radius:.75rem;border:1px solid rgba(255,255,255,.12);background:#0f172a;padding:.8rem 1rem;color:white}.input:focus{outline:2px solid #38bdf8}.smallButton{border-radius:.65rem;border:1px solid rgba(56,189,248,.3);background:rgba(56,189,248,.1);padding:.45rem .8rem;font-weight:800;color:#bae6fd}.smallDanger{border-radius:.65rem;border:1px solid rgba(251,113,133,.3);background:rgba(251,113,133,.1);padding:.45rem .8rem;font-weight:800;color:#fecdd3}`}</style>
    </section>
  );
}

const activeTab = 'rounded-xl bg-sky-500 px-4 py-2 text-sm font-black text-white';
const inactiveTab = 'rounded-xl border border-white/10 px-4 py-2 text-sm font-bold text-slate-300 hover:bg-white/10';
function Field({ label, children }) { return <label className="block text-sm font-bold text-slate-300">{label}{children}</label>; }
function Check({ label, checked, onChange }) { return <label className="flex items-center gap-3 rounded-xl border border-white/10 bg-slate-950/40 p-3 text-sm font-bold text-slate-200"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="h-4 w-4" />{label}</label>; }
function Alert({ children, tone = 'success' }) { return <div className={`mt-5 rounded-xl border p-4 ${tone === 'error' ? 'border-rose-400/30 bg-rose-400/10 text-rose-100' : 'border-emerald-400/30 bg-emerald-400/10 text-emerald-100'}`}>{children}</div>; }
function Metric({ label, value }) { return <div className="rounded-2xl border border-white/10 bg-white/5 p-4"><p className="text-xs font-bold uppercase text-slate-500">{label}</p><p className="mt-1 text-2xl font-black text-white">{value ?? 0}</p></div>; }
function Empty({ children }) { return <div className="rounded-2xl border border-dashed border-white/15 p-8 text-center text-slate-500">{children}</div>; }
function Status({ value }) { const tone = value === 'Sent' || value === 'Completed' || value === 'Active' ? 'border-emerald-400/20 bg-emerald-400/10 text-emerald-200' : value === 'Failed' || value === 'NotConfigured' || value === 'Inactive' ? 'border-rose-400/20 bg-rose-400/10 text-rose-200' : value === 'Pending' || value === 'Processing' ? 'border-amber-400/20 bg-amber-400/10 text-amber-200' : 'border-slate-400/20 bg-slate-400/10 text-slate-200'; return <span className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-black ${tone}`}>{value}</span>; }
function Th({ children }) { return <th className="px-4 py-3">{children}</th>; }
function Td({ children, colSpan }) { return <td colSpan={colSpan} className="px-4 py-4 text-slate-300">{children}</td>; }
function Pagination({ page, pageSize, total, onPage }) { const pages = Math.max(1, Math.ceil(total / pageSize)); return <div className="flex items-center justify-between text-sm text-slate-400"><p>Page {page} of {pages} · {total} records</p><div className="flex gap-2"><button type="button" disabled={page <= 1} onClick={() => onPage(page - 1)} className="smallButton disabled:opacity-40">Previous</button><button type="button" disabled={page >= pages} onClick={() => onPage(page + 1)} className="smallButton disabled:opacity-40">Next</button></div></div>; }
function formatDate(value) { return value ? new Date(value).toLocaleString() : '—'; }
