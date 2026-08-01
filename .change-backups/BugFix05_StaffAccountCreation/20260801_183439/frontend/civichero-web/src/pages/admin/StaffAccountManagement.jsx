import { useEffect, useMemo, useState } from 'react';
import RoleBadge from '../../components/common/RoleBadge.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { staffAccountApi } from '../../services/staffAccountApi.js';
import { userApi } from '../../services/userApi.js';

const emptyForm = {
  fullName: '',
  email: '',
  phone: '',
  role: 'Officer',
  departmentId: '',
  wardId: '',
  temporaryPassword: '',
  confirmPassword: '',
};

const statusClass = {
  PendingApproval: 'border-amber-400/30 bg-amber-500/10 text-amber-200',
  Approved: 'border-emerald-400/30 bg-emerald-500/10 text-emerald-200',
  Rejected: 'border-rose-400/30 bg-rose-500/10 text-rose-200',
  ExistingActive: 'border-sky-400/30 bg-sky-500/10 text-sky-200',
  Inactive: 'border-slate-400/30 bg-slate-500/10 text-slate-200',
};

const statusLabel = (value) => ({
  PendingApproval: 'Pending SuperAdmin approval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  ExistingActive: 'Existing active account',
  Inactive: 'Inactive',
}[value] || value);

export default function StaffAccountManagement() {
  const { user } = useAuth();
  const isSuperAdmin = (user?.role || '').toLowerCase() === 'superadmin';
  const [form, setForm] = useState(emptyForm);
  const [metadata, setMetadata] = useState({ departments: [], wards: [] });
  const [accounts, setAccounts] = useState([]);
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [review, setReview] = useState(null);

  const wards = useMemo(
    () => metadata.wards.filter((ward) => String(ward.departmentId) === String(form.departmentId)),
    [metadata.wards, form.departmentId],
  );

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const [items, meta] = await Promise.all([
        staffAccountApi.getAccounts({ search: search || undefined, status: status || undefined }),
        userApi.getMetadata(),
      ]);
      setAccounts(Array.isArray(items) ? items : []);
      setMetadata(meta || { departments: [], wards: [] });
    } catch (apiError) {
      setError(apiError.errors?.join(' ') || apiError.message || 'Unable to load staff accounts.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [status]);

  const updateForm = (name, value) => {
    setForm((current) => {
      const next = { ...current, [name]: value };
      if (name === 'role' || name === 'departmentId') next.wardId = '';
      return next;
    });
  };

  const submit = async (event) => {
    event.preventDefault();
    setWorking(true);
    setError('');
    setMessage('');
    try {
      const created = await staffAccountApi.create({
        ...form,
        departmentId: Number(form.departmentId),
        wardId: form.wardId ? Number(form.wardId) : null,
        phone: form.phone.trim() || null,
      });
      setMessage(created.approvalStatus === 'PendingApproval'
        ? 'Staff account created. SuperAdmin approval is now required before login.'
        : 'Staff account created and approved by SuperAdmin.');
      setForm(emptyForm);
      await load();
    } catch (apiError) {
      setError(apiError.errors?.join(' ') || apiError.message || 'Unable to create staff account.');
    } finally {
      setWorking(false);
    }
  };

  const submitReview = async () => {
    if (!review) return;
    setWorking(true);
    setError('');
    setMessage('');
    try {
      const updated = await staffAccountApi.review(review.account.id, {
        decision: review.decision,
        remarks: review.remarks.trim() || null,
      });
      setMessage(`${updated.fullName}'s ${updated.role} account was ${updated.approvalStatus.toLowerCase()}.`);
      setReview(null);
      await load();
    } catch (apiError) {
      setError(apiError.errors?.join(' ') || apiError.message || 'Unable to review staff account.');
    } finally {
      setWorking(false);
    }
  };

  return (
    <section className="p-6 lg:p-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 className="text-3xl font-black text-white">Staff accounts</h2>
          <p className="mt-1 max-w-3xl text-slate-400">
            Create Officer and Supervisor accounts with department scope. Accounts created by an Admin remain inactive until a SuperAdmin verifies them.
          </p>
        </div>
        <span className="rounded-full border border-white/10 bg-white/5 px-4 py-2 text-sm text-slate-300">{accounts.length} staff accounts</span>
      </div>

      {message && <p className="mt-5 rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-4 text-emerald-100">{message}</p>}
      {error && <p className="mt-5 rounded-xl border border-rose-400/20 bg-rose-500/10 p-4 text-rose-100">{error}</p>}

      <div className="mt-7 grid gap-7 xl:grid-cols-[minmax(340px,0.82fr)_minmax(0,1.45fr)]">
        <form onSubmit={submit} className="h-fit rounded-3xl border border-white/10 bg-white/5 p-6 shadow-2xl shadow-black/20">
          <h3 className="text-xl font-black text-white">Create Officer or Supervisor</h3>
          <p className="mt-2 text-sm text-slate-400">
            {isSuperAdmin ? 'SuperAdmin-created accounts are approved immediately.' : 'The new account cannot log in until SuperAdmin approval.'}
          </p>

          <div className="mt-5 grid gap-4 sm:grid-cols-2">
            <label className="text-sm text-slate-300 sm:col-span-2">Full name
              <input required value={form.fullName} onChange={(event) => updateForm('fullName', event.target.value)} className="input" placeholder="Officer or supervisor name" />
            </label>
            <label className="text-sm text-slate-300 sm:col-span-2">Official email
              <input required type="email" value={form.email} onChange={(event) => updateForm('email', event.target.value)} className="input" placeholder="name@department.gov.in" />
            </label>
            <label className="text-sm text-slate-300 sm:col-span-2">Phone (optional)
              <input value={form.phone} onChange={(event) => updateForm('phone', event.target.value)} className="input" placeholder="+919876543210" />
            </label>
            <label className="text-sm text-slate-300">Role
              <select value={form.role} onChange={(event) => updateForm('role', event.target.value)} className="input">
                <option value="Officer">Officer</option>
                <option value="Supervisor">Supervisor</option>
              </select>
            </label>
            <label className="text-sm text-slate-300">Department
              <select required value={form.departmentId} onChange={(event) => updateForm('departmentId', event.target.value)} className="input">
                <option value="">Select department</option>
                {metadata.departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
              </select>
            </label>
            <label className="text-sm text-slate-300 sm:col-span-2">Ward
              <select required={form.role === 'Officer'} disabled={!form.departmentId} value={form.wardId} onChange={(event) => updateForm('wardId', event.target.value)} className="input disabled:opacity-40">
                <option value="">{form.role === 'Supervisor' ? 'All department wards (optional)' : 'Select ward'}</option>
                {wards.map((ward) => <option key={ward.id} value={ward.id}>{ward.name}</option>)}
              </select>
            </label>
            <label className="text-sm text-slate-300">Temporary password
              <input required type="password" minLength="8" value={form.temporaryPassword} onChange={(event) => updateForm('temporaryPassword', event.target.value)} className="input" autoComplete="new-password" />
            </label>
            <label className="text-sm text-slate-300">Confirm password
              <input required type="password" minLength="8" value={form.confirmPassword} onChange={(event) => updateForm('confirmPassword', event.target.value)} className="input" autoComplete="new-password" />
            </label>
          </div>
          <p className="mt-4 text-xs leading-5 text-slate-500">Password requires uppercase, lowercase, number and special character. Share it securely with the staff member only after approval.</p>
          <button disabled={working} className="mt-5 w-full rounded-xl bg-violet-500 px-5 py-3 font-bold text-white disabled:opacity-60">
            {working ? 'Creating account…' : isSuperAdmin ? 'Create and approve account' : 'Create and send for approval'}
          </button>
        </form>

        <div>
          <form onSubmit={(event) => { event.preventDefault(); load(); }} className="grid gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 md:grid-cols-[1fr_220px_auto]">
            <input value={search} onChange={(event) => setSearch(event.target.value)} className="input mt-0" placeholder="Search staff name, email or scope" />
            <select value={status} onChange={(event) => setStatus(event.target.value)} className="input mt-0">
              <option value="">All approval statuses</option>
              <option value="PendingApproval">Pending approval</option>
              <option value="Approved">Approved</option>
              <option value="Rejected">Rejected</option>
              <option value="ExistingActive">Existing active</option>
              <option value="Inactive">Inactive</option>
            </select>
            <button className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white">Search</button>
          </form>

          <div className="mt-4 space-y-4">
            {loading && <div className="rounded-2xl border border-white/10 bg-white/5 p-8 text-center text-slate-400">Loading staff accounts…</div>}
            {!loading && accounts.length === 0 && <div className="rounded-2xl border border-white/10 bg-white/5 p-8 text-center text-slate-400">No staff accounts match this filter.</div>}
            {!loading && accounts.map((account) => (
              <article key={account.id} className="rounded-2xl border border-white/10 bg-slate-950/45 p-5">
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="text-lg font-black text-white">{account.fullName}</h3>
                      <RoleBadge role={account.role} />
                    </div>
                    <p className="mt-1 text-sm text-slate-400">{account.email}</p>
                    <p className="mt-2 text-sm text-slate-300">{account.departmentName || 'No department'} · {account.wardName || (account.role === 'Supervisor' ? 'All wards' : 'No ward')}</p>
                  </div>
                  <span className={`rounded-full border px-3 py-1 text-xs font-bold ${statusClass[account.approvalStatus] || statusClass.Inactive}`}>{statusLabel(account.approvalStatus)}</span>
                </div>
                <div className="mt-4 grid gap-2 text-xs text-slate-500 sm:grid-cols-2">
                  <p>Requested by: <span className="text-slate-300">{account.requestedBy || 'Existing account'}</span></p>
                  <p>Requested: <span className="text-slate-300">{account.requestedAt ? new Date(account.requestedAt).toLocaleString() : new Date(account.createdAt).toLocaleString()}</span></p>
                  {account.reviewedBy && <p>Reviewed by: <span className="text-slate-300">{account.reviewedBy}</span></p>}
                  {account.reviewedAt && <p>Reviewed: <span className="text-slate-300">{new Date(account.reviewedAt).toLocaleString()}</span></p>}
                </div>
                {account.reviewRemarks && <p className="mt-3 rounded-xl bg-white/5 p-3 text-sm text-slate-300">Review note: {account.reviewRemarks}</p>}
                {isSuperAdmin && account.approvalStatus === 'PendingApproval' && (
                  <div className="mt-4 flex flex-wrap gap-3">
                    <button disabled={working} onClick={() => setReview({ account, decision: 'Approve', remarks: '' })} className="rounded-xl bg-emerald-500 px-4 py-2 font-bold text-white disabled:opacity-50">Verify and approve</button>
                    <button disabled={working} onClick={() => setReview({ account, decision: 'Reject', remarks: '' })} className="rounded-xl border border-rose-400/40 px-4 py-2 font-bold text-rose-200 disabled:opacity-50">Reject</button>
                  </div>
                )}
              </article>
            ))}
          </div>
        </div>
      </div>

      {review && (
        <div className="fixed inset-0 z-50 grid place-items-center bg-black/75 p-4" onMouseDown={(event) => { if (event.target === event.currentTarget) setReview(null); }}>
          <div className="w-full max-w-lg rounded-3xl border border-white/10 bg-slate-900 p-7 shadow-2xl">
            <h3 className="text-2xl font-black text-white">{review.decision === 'Approve' ? 'Verify staff account' : 'Reject staff account'}</h3>
            <p className="mt-2 text-slate-400">{review.account.fullName} · {review.account.role}</p>
            <label className="mt-5 block text-sm text-slate-300">Review remarks
              <textarea value={review.remarks} onChange={(event) => setReview({ ...review, remarks: event.target.value })} maxLength="500" rows="4" className="input resize-y" placeholder="Add an approval note or explain the rejection" />
            </label>
            <div className="mt-6 flex justify-end gap-3">
              <button disabled={working} onClick={() => setReview(null)} className="rounded-xl border border-white/15 px-4 py-3 text-slate-200">Cancel</button>
              <button disabled={working} onClick={submitReview} className={review.decision === 'Approve' ? 'rounded-xl bg-emerald-500 px-5 py-3 font-bold text-white disabled:opacity-50' : 'rounded-xl bg-rose-500 px-5 py-3 font-bold text-white disabled:opacity-50'}>
                {working ? 'Saving review…' : review.decision === 'Approve' ? 'Approve account' : 'Reject account'}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
