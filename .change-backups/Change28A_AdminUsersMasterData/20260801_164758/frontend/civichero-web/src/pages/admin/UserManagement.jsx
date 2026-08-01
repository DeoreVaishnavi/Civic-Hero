import { useEffect, useMemo, useState } from 'react';
import RoleBadge from '../../components/common/RoleBadge.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { adminUserManagementApi } from '../../services/adminUserManagementApi.js';
import { userApi } from '../../services/userApi.js';

const createInitial = { fullName: '', email: '', phone: '', temporaryPassword: '', markEmailVerified: true };
const accessInitial = { id: null, role: 'Citizen', departmentId: '', wardId: '' };

export default function UserManagement() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState([]);
  const [metadata, setMetadata] = useState({ roles: [], departments: [], wards: [] });
  const [filters, setFilters] = useState({ page: 1, pageSize: 20, search: '', role: '', isActive: '', includeDeleted: false });
  const [pageInfo, setPageInfo] = useState({ totalCount: 0, totalPages: 0 });
  const [createForm, setCreateForm] = useState(createInitial);
  const [createdCredentials, setCreatedCredentials] = useState(null);
  const [access, setAccess] = useState(accessInitial);
  const [emailEdit, setEmailEdit] = useState(null);
  const [lifecycle, setLifecycle] = useState(null);
  const [history, setHistory] = useState(null);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  const load = async () => {
    setLoading(true); setError('');
    try {
      const params = { ...filters, isActive: filters.isActive === '' ? undefined : filters.isActive === 'true' };
      const [result, meta] = await Promise.all([adminUserManagementApi.users(params), userApi.getMetadata()]);
      setUsers(result.items || []); setPageInfo({ ...result, totalPages: Math.ceil((result.totalCount || 0) / filters.pageSize) }); setMetadata(meta);
    } catch (apiError) { setError(apiError.errors?.join(' ') || apiError.message); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [filters.page, filters.role, filters.isActive, filters.includeDeleted]);

  const filteredWards = useMemo(() => metadata.wards.filter((ward) => !access.departmentId || String(ward.departmentId) === String(access.departmentId)), [metadata.wards, access.departmentId]);
  const run = async (operation, success) => {
    setWorking(true); setError(''); setMessage('');
    try { const result = await operation(); if (success) setMessage(success); return result; }
    catch (apiError) { setError(apiError.errors?.join(' ') || apiError.message); return null; }
    finally { setWorking(false); }
  };

  const createCitizen = async (event) => {
    event.preventDefault();
    const result = await run(() => adminUserManagementApi.createCitizen({ ...createForm, temporaryPassword: createForm.temporaryPassword || null }), 'Citizen account created. Share the temporary password securely.');
    if (result) { setCreatedCredentials(result); setCreateForm(createInitial); await load(); }
  };

  const saveAccess = async () => {
    const result = await run(() => userApi.changeRole(access.id, { role: access.role, departmentId: access.departmentId ? Number(access.departmentId) : null, wardId: access.wardId ? Number(access.wardId) : null }), 'Role and scope updated; existing sessions were revoked.');
    if (result) { setAccess(accessInitial); await load(); }
  };

  const saveEmail = async () => {
    const result = await run(() => adminUserManagementApi.updateEmail(emailEdit.id, { newEmail: emailEdit.newEmail, markVerified: emailEdit.markVerified, reason: emailEdit.reason }), 'Email address updated; existing sessions were revoked.');
    if (result) { setEmailEdit(null); if (result.developmentVerificationToken) setCreatedCredentials({ temporaryPassword: null, developmentVerificationToken: result.developmentVerificationToken, user: result.user }); await load(); }
  };

  const lifecycleAction = async () => {
    const payload = { reason: lifecycle.reason, activateOnRestore: true };
    const result = lifecycle.person.isDeleted
      ? await run(() => adminUserManagementApi.restore(lifecycle.person.id, payload), 'User account restored.')
      : await run(() => adminUserManagementApi.softDelete(lifecycle.person.id, payload), 'User account soft-deleted.');
    if (result) { setLifecycle(null); await load(); }
  };

  const openHistory = async (person, roleOnly = false) => {
    const result = await run(() => roleOnly ? adminUserManagementApi.roleHistory(person.id) : adminUserManagementApi.history(person.id));
    if (result) setHistory({ ...result, roleOnly });
  };

  return <section className="p-6 lg:p-10">
    <div className="flex flex-wrap items-end justify-between gap-4"><div><h2 className="text-3xl font-black text-white">User management</h2><p className="mt-1 text-slate-400">Create Citizens, control access, change email, preserve account history and perform audited soft deletion.</p></div><span className="text-sm text-slate-400">{pageInfo.totalCount || 0} users</span></div>

    <form onSubmit={createCitizen} className="mt-6 rounded-2xl border border-emerald-400/20 bg-emerald-500/5 p-5">
      <h3 className="text-lg font-black text-white">Create Citizen account</h3>
      <div className="mt-4 grid gap-3 md:grid-cols-2 lg:grid-cols-4"><input required value={createForm.fullName} onChange={(e) => setCreateForm({ ...createForm, fullName: e.target.value })} placeholder="Full name" className="input mt-0"/><input required type="email" value={createForm.email} onChange={(e) => setCreateForm({ ...createForm, email: e.target.value })} placeholder="Email" className="input mt-0"/><input value={createForm.phone} onChange={(e) => setCreateForm({ ...createForm, phone: e.target.value })} placeholder="Phone (optional)" className="input mt-0"/><input value={createForm.temporaryPassword} onChange={(e) => setCreateForm({ ...createForm, temporaryPassword: e.target.value })} placeholder="Temporary password (auto if blank)" className="input mt-0"/></div>
      <div className="mt-4 flex flex-wrap items-center justify-between gap-3"><label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={createForm.markEmailVerified} onChange={(e) => setCreateForm({ ...createForm, markEmailVerified: e.target.checked })}/>Mark email verified after administrative identity check</label><button disabled={working} className="rounded-xl bg-emerald-500 px-5 py-3 font-bold text-white disabled:opacity-50">Create Citizen</button></div>
    </form>

    <form onSubmit={(e) => { e.preventDefault(); setFilters({ ...filters, page: 1 }); load(); }} className="mt-6 grid gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 md:grid-cols-[1fr_160px_160px_auto]">
      <input value={filters.search} onChange={(e) => setFilters({ ...filters, search: e.target.value })} placeholder="Search name, email or phone" className="input mt-0"/>
      <select value={filters.role} onChange={(e) => setFilters({ ...filters, role: e.target.value, page: 1 })} className="input mt-0"><option value="">All roles</option>{metadata.roles.map((role) => <option key={role}>{role}</option>)}</select>
      <select value={filters.isActive} onChange={(e) => setFilters({ ...filters, isActive: e.target.value, page: 1 })} className="input mt-0"><option value="">All activity</option><option value="true">Active</option><option value="false">Inactive</option></select>
      <button className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white">Search</button>
      <label className="flex items-center gap-2 text-sm text-slate-300 md:col-span-4"><input type="checkbox" checked={filters.includeDeleted} onChange={(e) => setFilters({ ...filters, includeDeleted: e.target.checked, page: 1 })}/>Include soft-deleted accounts</label>
    </form>
    {message && <p className="mt-4 rounded-xl bg-emerald-500/10 p-3 text-emerald-200">{message}</p>}
    {error && <p className="mt-4 rounded-xl bg-rose-500/10 p-3 text-rose-200">{error}</p>}
    {createdCredentials && <div className="mt-4 rounded-2xl border border-amber-400/25 bg-amber-500/10 p-4 text-amber-100"><p className="font-bold">One-time account information</p><p className="mt-2">User: {createdCredentials.user?.email}</p>{createdCredentials.temporaryPassword && <p>Temporary password: <code>{createdCredentials.temporaryPassword}</code></p>}{createdCredentials.developmentVerificationToken && <p className="break-all">Development verification token: <code>{createdCredentials.developmentVerificationToken}</code></p>}<button onClick={() => setCreatedCredentials(null)} className="mt-3 rounded-lg border border-amber-300/30 px-3 py-2">Dismiss</button></div>}

    <div className="mt-6 overflow-x-auto rounded-2xl border border-white/10"><table className="min-w-full divide-y divide-white/10 text-sm"><thead className="bg-white/5 text-left text-xs uppercase tracking-wider text-slate-400"><tr><th className="p-4">User</th><th className="p-4">Role / scope</th><th className="p-4">Status</th><th className="p-4">Actions</th></tr></thead><tbody className="divide-y divide-white/10 bg-slate-950/40">{loading ? <tr><td colSpan="4" className="p-8 text-center text-slate-400">Loading users…</td></tr> : users.map((person) => <tr key={person.id} className="align-top"><td className="p-4"><p className="font-bold text-white">{person.fullName}</p><p className="mt-1 text-slate-400">{person.email}</p><p className="mt-1 text-xs text-slate-600">ID {person.id}{person.phone ? ` • ${person.phone}` : ''}</p></td><td className="p-4"><RoleBadge role={person.role}/><p className="mt-2 text-slate-300">{person.departmentName || 'No department'}</p><p className="text-xs text-slate-500">{person.wardName || 'No ward'}</p></td><td className="p-4"><span className={person.isDeleted ? 'text-rose-300' : person.isActive ? 'text-emerald-300' : 'text-amber-300'}>{person.isDeleted ? 'Soft-deleted' : person.isActive ? 'Active' : 'Inactive'}</span><p className="mt-1 text-xs text-slate-500">{person.isEmailVerified ? 'Email verified' : 'Email pending'}</p></td><td className="p-4"><div className="flex min-w-72 flex-wrap gap-2"><button disabled={person.isDeleted} onClick={() => setAccess({ id: person.id, role: person.role, departmentId: person.departmentId || '', wardId: person.wardId || '' })} className="rounded-lg border border-sky-400/30 px-3 py-2 text-sky-200 disabled:opacity-40">Access</button><button disabled={person.isDeleted} onClick={() => setEmailEdit({ id: person.id, currentEmail: person.email, newEmail: '', markVerified: false, reason: '' })} className="rounded-lg border border-violet-400/30 px-3 py-2 text-violet-200 disabled:opacity-40">Email</button><button onClick={() => openHistory(person)} className="rounded-lg border border-white/15 px-3 py-2">History</button><button onClick={() => openHistory(person, true)} className="rounded-lg border border-white/15 px-3 py-2">Role history</button><button disabled={person.id === currentUser?.id} onClick={() => setLifecycle({ person, reason: '' })} className="rounded-lg border border-rose-400/30 px-3 py-2 text-rose-200 disabled:opacity-40">{person.isDeleted ? 'Restore' : 'Soft delete'}</button></div></td></tr>)}</tbody></table></div>
    <div className="mt-4 flex items-center justify-between"><button disabled={filters.page <= 1} onClick={() => setFilters({ ...filters, page: filters.page - 1 })} className="rounded-lg border border-white/15 px-4 py-2 disabled:opacity-40">Previous</button><span className="text-sm text-slate-400">Page {filters.page} of {Math.max(1, pageInfo.totalPages || 1)}</span><button disabled={filters.page >= (pageInfo.totalPages || 1)} onClick={() => setFilters({ ...filters, page: filters.page + 1 })} className="rounded-lg border border-white/15 px-4 py-2 disabled:opacity-40">Next</button></div>

    {access.id && <Modal title="Edit role and scope" close={() => setAccess(accessInitial)}><div className="grid gap-4 sm:grid-cols-2"><label className="text-sm text-slate-300">Role<select value={access.role} onChange={(e) => setAccess({ ...access, role: e.target.value, departmentId: '', wardId: '' })} className="input">{metadata.roles.filter((role) => currentUser?.role === 'SuperAdmin' || role !== 'SuperAdmin').map((role) => <option key={role}>{role}</option>)}</select></label><label className="text-sm text-slate-300">Department<select disabled={['Citizen','Admin','SuperAdmin'].includes(access.role)} value={access.departmentId} onChange={(e) => setAccess({ ...access, departmentId: e.target.value, wardId: '' })} className="input disabled:opacity-40"><option value="">Not assigned</option>{metadata.departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}</select></label><label className="text-sm text-slate-300 sm:col-span-2">Ward<select disabled={['Citizen','Admin','SuperAdmin'].includes(access.role) || !access.departmentId} value={access.wardId} onChange={(e) => setAccess({ ...access, wardId: e.target.value })} className="input disabled:opacity-40"><option value="">{access.role === 'Supervisor' ? 'All department wards' : 'Select ward'}</option>{filteredWards.map((ward) => <option key={ward.id} value={ward.id}>{ward.name}</option>)}</select></label></div><Actions cancel={() => setAccess(accessInitial)} save={saveAccess} working={working}/></Modal>}
    {emailEdit && <Modal title="Administratively change email" close={() => setEmailEdit(null)}><p className="text-sm text-slate-400">Current: {emailEdit.currentEmail}</p><input type="email" value={emailEdit.newEmail} onChange={(e) => setEmailEdit({ ...emailEdit, newEmail: e.target.value })} placeholder="New email" className="input"/><textarea value={emailEdit.reason} onChange={(e) => setEmailEdit({ ...emailEdit, reason: e.target.value })} placeholder="Audited reason (minimum 10 characters)" className="input min-h-28"/><label className="mt-3 flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={emailEdit.markVerified} onChange={(e) => setEmailEdit({ ...emailEdit, markVerified: e.target.checked })}/>Mark verified after identity confirmation</label><Actions cancel={() => setEmailEdit(null)} save={saveEmail} working={working}/></Modal>}
    {lifecycle && <Modal title={lifecycle.person.isDeleted ? 'Restore account' : 'Soft-delete account'} close={() => setLifecycle(null)}><p className="text-slate-300">{lifecycle.person.fullName} — {lifecycle.person.email}</p><textarea value={lifecycle.reason} onChange={(e) => setLifecycle({ ...lifecycle, reason: e.target.value })} placeholder="Audited reason (minimum 10 characters)" className="input min-h-28"/><Actions cancel={() => setLifecycle(null)} save={lifecycleAction} working={working} label={lifecycle.person.isDeleted ? 'Restore' : 'Soft delete'}/></Modal>}
    {history && <Modal title={history.roleOnly ? 'Role-change history' : 'Account history'} close={() => setHistory(null)} wide><p className="text-sm text-slate-400">{history.user.fullName} — {history.totalCount} records</p><div className="mt-4 max-h-[60vh] space-y-3 overflow-y-auto">{history.items.length === 0 ? <p className="text-slate-500">No matching history exists yet.</p> : history.items.map((item) => <article key={item.id} className="rounded-xl border border-white/10 bg-white/5 p-3"><div className="flex flex-wrap justify-between gap-2"><strong className="text-white">{item.action}</strong><span className="text-xs text-slate-500">{new Date(item.createdAt).toLocaleString()}</span></div><p className="mt-1 text-xs text-slate-400">{item.category} • Actor: {item.actorEmail || item.actorRole || 'System'}</p>{item.oldValuesJson && <pre className="mt-2 overflow-x-auto whitespace-pre-wrap text-xs text-slate-500">Before: {item.oldValuesJson}</pre>}{item.newValuesJson && <pre className="mt-2 overflow-x-auto whitespace-pre-wrap text-xs text-slate-400">After: {item.newValuesJson}</pre>}</article>)}</div></Modal>}
  </section>;
}

function Modal({ title, close, children, wide = false }) { return <div className="fixed inset-0 z-50 grid place-items-center bg-black/75 p-4"><div className={`w-full ${wide ? 'max-w-4xl' : 'max-w-xl'} rounded-3xl border border-white/10 bg-slate-900 p-7`}><div className="flex items-center justify-between"><h3 className="text-2xl font-black text-white">{title}</h3><button onClick={close} className="text-slate-400">✕</button></div><div className="mt-5">{children}</div></div></div>; }
function Actions({ cancel, save, working, label = 'Save' }) { return <div className="mt-6 flex justify-end gap-3"><button onClick={cancel} className="rounded-xl border border-white/15 px-4 py-3">Cancel</button><button disabled={working} onClick={save} className="rounded-xl bg-violet-500 px-5 py-3 font-bold text-white disabled:opacity-60">{working ? 'Working…' : label}</button></div>; }
