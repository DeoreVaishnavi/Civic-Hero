import { useEffect, useMemo, useState } from 'react';
import RoleBadge from '../../components/common/RoleBadge.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { userApi } from '../../services/userApi.js';

const emptyEdit = { id: null, role: 'Citizen', departmentId: '', wardId: '' };

export default function UserManagement() {
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState([]);
  const [metadata, setMetadata] = useState({ roles: [], departments: [], wards: [] });
  const [filters, setFilters] = useState({ page: 1, pageSize: 20, search: '', role: '', isActive: '' });
  const [pageInfo, setPageInfo] = useState({ totalCount: 0, totalPages: 0 });
  const [edit, setEdit] = useState(emptyEdit);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  const load = async () => {
    setLoading(true); setError('');
    try {
      const params = { ...filters, isActive: filters.isActive === '' ? undefined : filters.isActive === 'true' };
      const [result, meta] = await Promise.all([userApi.getUsers(params), userApi.getMetadata()]);
      setUsers(result.items || []); setPageInfo(result); setMetadata(meta);
    } catch (apiError) { setError(apiError.message); }
    finally { setLoading(false); }
  };

  useEffect(() => { load(); }, [filters.page, filters.role, filters.isActive]);

  const filteredWards = useMemo(() => metadata.wards.filter((ward) => !edit.departmentId || String(ward.departmentId) === String(edit.departmentId)), [metadata.wards, edit.departmentId]);

  const search = (event) => { event.preventDefault(); setFilters((value) => ({ ...value, page: 1 })); load(); };
  const beginEdit = (person) => setEdit({ id: person.id, role: person.role, departmentId: person.departmentId || '', wardId: person.wardId || '' });

  const saveRole = async () => {
    setWorking(true); setError(''); setMessage('');
    try {
      await userApi.changeRole(edit.id, { role: edit.role, departmentId: edit.departmentId ? Number(edit.departmentId) : null, wardId: edit.wardId ? Number(edit.wardId) : null });
      setMessage('Role and access scope updated. Existing sessions were revoked.'); setEdit(emptyEdit); await load();
    } catch (apiError) { setError(apiError.errors?.join(' ') || apiError.message); }
    finally { setWorking(false); }
  };

  const setActive = async (person) => {
    setWorking(true); setError(''); setMessage('');
    try { person.isActive ? await userApi.deactivate(person.id) : await userApi.activate(person.id); setMessage(person.isActive ? 'User deactivated.' : 'User activated.'); await load(); }
    catch (apiError) { setError(apiError.message); }
    finally { setWorking(false); }
  };

  const forceLogout = async (person) => {
    setWorking(true); setError(''); setMessage('');
    try { await userApi.forceLogout(person.id); setMessage(`Sessions revoked for ${person.fullName}.`); }
    catch (apiError) { setError(apiError.message); }
    finally { setWorking(false); }
  };

  return (
    <section className="p-6 lg:p-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div><h2 className="text-3xl font-black text-white">User management</h2><p className="mt-1 text-slate-400">Role, department, ward, activation, and session controls.</p></div>
        <span className="text-sm text-slate-400">{pageInfo.totalCount || 0} users</span>
      </div>
      <form onSubmit={search} className="mt-6 grid gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 md:grid-cols-[1fr_180px_160px_auto]">
        <input value={filters.search} onChange={(e) => setFilters({ ...filters, search: e.target.value })} placeholder="Search name or email" className="input mt-0" />
        <select value={filters.role} onChange={(e) => setFilters({ ...filters, role: e.target.value, page: 1 })} className="input mt-0"><option value="">All roles</option>{metadata.roles.map((role) => <option key={role}>{role}</option>)}</select>
        <select value={filters.isActive} onChange={(e) => setFilters({ ...filters, isActive: e.target.value, page: 1 })} className="input mt-0"><option value="">All statuses</option><option value="true">Active</option><option value="false">Inactive</option></select>
        <button className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white">Search</button>
      </form>
      {message && <p className="mt-4 rounded-xl bg-emerald-500/10 p-3 text-emerald-200">{message}</p>}
      {error && <p className="mt-4 rounded-xl bg-rose-500/10 p-3 text-rose-200">{error}</p>}

      <div className="mt-6 overflow-x-auto rounded-2xl border border-white/10">
        <table className="min-w-full divide-y divide-white/10 text-sm">
          <thead className="bg-white/5 text-left text-xs uppercase tracking-wider text-slate-400"><tr><th className="p-4">User</th><th className="p-4">Role</th><th className="p-4">Scope</th><th className="p-4">Status</th><th className="p-4">Actions</th></tr></thead>
          <tbody className="divide-y divide-white/10 bg-slate-950/40">
            {loading ? <tr><td colSpan="5" className="p-8 text-center text-slate-400">Loading users…</td></tr> : users.map((person) => (
              <tr key={person.id} className="align-top">
                <td className="p-4"><p className="font-bold text-white">{person.fullName}</p><p className="mt-1 text-slate-400">{person.email}</p><p className="mt-1 text-xs text-slate-600">ID {person.id}</p></td>
                <td className="p-4"><RoleBadge role={person.role} /></td>
                <td className="p-4 text-slate-300"><p>{person.departmentName || 'No department'}</p><p className="mt-1 text-xs text-slate-500">{person.wardName || 'No ward'}</p></td>
                <td className="p-4"><span className={person.isActive ? 'text-emerald-300' : 'text-rose-300'}>{person.isActive ? 'Active' : 'Inactive'}</span><p className="mt-1 text-xs text-slate-500">{person.isEmailVerified ? 'Email verified' : 'Email pending'}</p></td>
                <td className="p-4"><div className="flex min-w-56 flex-wrap gap-2"><button disabled={working} onClick={() => beginEdit(person)} className="rounded-lg border border-sky-400/30 px-3 py-2 text-sky-200">Edit access</button><button disabled={working || person.id === currentUser?.id} onClick={() => setActive(person)} className="rounded-lg border border-white/15 px-3 py-2 text-slate-200 disabled:opacity-40">{person.isActive ? 'Deactivate' : 'Activate'}</button><button disabled={working} onClick={() => forceLogout(person)} className="rounded-lg border border-amber-400/30 px-3 py-2 text-amber-200">Force logout</button></div></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="mt-4 flex items-center justify-between"><button disabled={filters.page <= 1} onClick={() => setFilters({ ...filters, page: filters.page - 1 })} className="rounded-lg border border-white/15 px-4 py-2 disabled:opacity-40">Previous</button><span className="text-sm text-slate-400">Page {filters.page} of {Math.max(1, pageInfo.totalPages || 1)}</span><button disabled={filters.page >= (pageInfo.totalPages || 1)} onClick={() => setFilters({ ...filters, page: filters.page + 1 })} className="rounded-lg border border-white/15 px-4 py-2 disabled:opacity-40">Next</button></div>

      {edit.id && <div className="fixed inset-0 z-50 grid place-items-center bg-black/70 p-4"><div className="w-full max-w-xl rounded-3xl border border-white/10 bg-slate-900 p-7"><h3 className="text-2xl font-black text-white">Edit role and scope</h3><div className="mt-5 grid gap-4 sm:grid-cols-2"><label className="text-sm text-slate-300">Role<select value={edit.role} onChange={(e) => setEdit({ ...edit, role: e.target.value, departmentId: '', wardId: '' })} className="input">{metadata.roles.filter((role) => currentUser?.role === 'SuperAdmin' || role !== 'SuperAdmin').map((role) => <option key={role}>{role}</option>)}</select></label><label className="text-sm text-slate-300">Department<select disabled={['Citizen','Admin','SuperAdmin'].includes(edit.role)} value={edit.departmentId} onChange={(e) => setEdit({ ...edit, departmentId: e.target.value, wardId: '' })} className="input disabled:opacity-40"><option value="">Not assigned</option>{metadata.departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}</select></label><label className="text-sm text-slate-300 sm:col-span-2">Ward<select disabled={['Citizen','Admin','SuperAdmin'].includes(edit.role) || !edit.departmentId} value={edit.wardId} onChange={(e) => setEdit({ ...edit, wardId: e.target.value })} className="input disabled:opacity-40"><option value="">{edit.role === 'Supervisor' ? 'All department wards' : 'Select ward'}</option>{filteredWards.map((ward) => <option key={ward.id} value={ward.id}>{ward.name}</option>)}</select></label></div><p className="mt-4 text-xs text-slate-400">Officer requires department + ward. Supervisor requires department; ward is optional.</p><div className="mt-6 flex justify-end gap-3"><button onClick={() => setEdit(emptyEdit)} className="rounded-xl border border-white/15 px-4 py-3">Cancel</button><button disabled={working} onClick={saveRole} className="rounded-xl bg-violet-500 px-5 py-3 font-bold text-white disabled:opacity-60">{working ? 'Saving…' : 'Save access'}</button></div></div></div>}
    </section>
  );
}
