import { useEffect, useMemo, useState } from 'react';
import RoleBadge from '../../components/common/RoleBadge.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { staffAccountApi } from '../../services/staffAccountApi.js';

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

const emptyMetadata = { departments: [], wards: [] };

const statusClass = {
  PendingApproval: 'staff-status staff-status-pending',
  Approved: 'staff-status staff-status-approved',
  Rejected: 'staff-status staff-status-rejected',
  ExistingActive: 'staff-status staff-status-active',
  Inactive: 'staff-status staff-status-inactive',
};

const statusLabel = (value) => ({
  PendingApproval: 'Pending SuperAdmin approval',
  Approved: 'Approved',
  Rejected: 'Rejected',
  ExistingActive: 'Existing active account',
  Inactive: 'Inactive',
}[value] || value);

const errorMessage = (error, fallback) => {
  const validation = Array.isArray(error?.errors) ? error.errors.filter(Boolean) : [];
  return validation.length > 0 ? validation.join(' ') : error?.message || fallback;
};

const generateTemporaryPassword = () => {
  const random = globalThis.crypto?.getRandomValues
    ? Array.from(globalThis.crypto.getRandomValues(new Uint32Array(3)), (value) => value.toString(36)).join('')
    : Math.random().toString(36).slice(2) + Date.now().toString(36);
  return `Civic@${random.slice(0, 10)}A7`;
};

const validateForm = (form, metadata) => {
  if (!form.fullName.trim()) return 'Full name is required.';
  if (!/^\S+@\S+\.\S+$/.test(form.email.trim())) return 'Enter a valid official email address.';
  if (!form.departmentId) return 'Select an active department.';

  const departmentExists = metadata.departments.some(
    (department) => String(department.id) === String(form.departmentId),
  );
  if (!departmentExists) return 'The selected department is unavailable. Refresh department and ward data.';

  if (form.role === 'Officer' && !form.wardId) return 'Select a ward for the Officer account.';
  if (form.wardId) {
    const wardExists = metadata.wards.some(
      (ward) => String(ward.id) === String(form.wardId)
        && String(ward.departmentId) === String(form.departmentId),
    );
    if (!wardExists) return 'The selected ward does not belong to the selected department.';
  }

  const password = form.temporaryPassword;
  if (password.length < 8
    || !/[A-Z]/.test(password)
    || !/[a-z]/.test(password)
    || !/[0-9]/.test(password)
    || !/[^a-zA-Z0-9]/.test(password)) {
    return 'Temporary password must contain uppercase, lowercase, number and special character.';
  }
  if (password !== form.confirmPassword) return 'Password and confirmation password must match.';
  return '';
};

export default function StaffAccountManagement() {
  const { user } = useAuth();
  const isSuperAdmin = (user?.role || '').toLowerCase() === 'superadmin';
  const [form, setForm] = useState(emptyForm);
  const [metadata, setMetadata] = useState(emptyMetadata);
  const [metadataLoading, setMetadataLoading] = useState(true);
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

  const loadAccounts = async (overrides = {}) => {
    setLoading(true);
    setError('');
    try {
      const items = await staffAccountApi.getAccounts({
        search: (overrides.search ?? search) || undefined,
        status: (overrides.status ?? status) || undefined,
      });
      setAccounts(Array.isArray(items) ? items : []);
    } catch (apiError) {
      setError(errorMessage(apiError, 'Unable to load staff accounts.'));
    } finally {
      setLoading(false);
    }
  };

  const loadMetadata = async () => {
    setMetadataLoading(true);
    setError('');
    try {
      const result = await staffAccountApi.getMetadata();
      setMetadata({
        departments: Array.isArray(result?.departments) ? result.departments : [],
        wards: Array.isArray(result?.wards) ? result.wards : [],
      });
    } catch (apiError) {
      setMetadata(emptyMetadata);
      setError(errorMessage(apiError, 'Unable to load active departments and wards.'));
    } finally {
      setMetadataLoading(false);
    }
  };

  useEffect(() => {
    void loadMetadata();
  }, []);

  useEffect(() => {
    void loadAccounts({ status });
  }, [status]);

  const updateForm = (name, value) => {
    setForm((current) => {
      const next = { ...current, [name]: value };
      if (name === 'role' || name === 'departmentId') next.wardId = '';
      return next;
    });
  };

  const generatePassword = () => {
    const temporaryPassword = generateTemporaryPassword();
    setForm((current) => ({ ...current, temporaryPassword, confirmPassword: temporaryPassword }));
    setMessage('A strong temporary password was generated. Copy it securely before submitting.');
    setError('');
  };

  const submit = async (event) => {
    event.preventDefault();
    const validationError = validateForm(form, metadata);
    if (validationError) {
      setError(validationError);
      setMessage('');
      return;
    }

    setWorking(true);
    setError('');
    setMessage('');
    try {
      const created = await staffAccountApi.create({
        fullName: form.fullName.trim(),
        email: form.email.trim().toLowerCase(),
        phone: form.phone.trim() || null,
        role: form.role,
        departmentId: Number(form.departmentId),
        wardId: form.wardId ? Number(form.wardId) : null,
        temporaryPassword: form.temporaryPassword,
        confirmPassword: form.confirmPassword,
      });
      setMessage(created.approvalStatus === 'PendingApproval'
        ? `${created.fullName}'s ${created.role} account was created and is waiting for SuperAdmin approval.`
        : `${created.fullName}'s ${created.role} account was created and approved.`);
      setForm(emptyForm);
      await loadAccounts();
    } catch (apiError) {
      setError(errorMessage(apiError, 'Unable to create the staff account.'));
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
      await loadAccounts();
    } catch (apiError) {
      setError(errorMessage(apiError, 'Unable to review the staff account.'));
    } finally {
      setWorking(false);
    }
  };

  const creationDisabled = working || metadataLoading || metadata.departments.length === 0;

  return (
    <section className="p-6 lg:p-10">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h2 className="text-3xl font-black text-white">Staff accounts</h2>
          <p className="mt-1 max-w-3xl text-slate-400">
            Create Officer and Supervisor accounts using active department and ward data. Admin-created accounts stay inactive until a SuperAdmin verifies them.
          </p>
        </div>
        <span className="rounded-full border border-white/10 bg-white/5 px-4 py-2 text-sm text-slate-300">{accounts.length} staff accounts</span>
      </div>

      {message && <p className="cv-alert cv-alert-success mt-5">{message}</p>}
      {error && <p className="cv-alert cv-alert-error mt-5">{error}</p>}

      <div className="mt-7 grid gap-7 xl:grid-cols-[minmax(340px,0.82fr)_minmax(0,1.45fr)]">
        <form onSubmit={submit} className="cv-card h-fit rounded-3xl border p-6 shadow-sm">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h3 className="text-xl font-black text-slate-900">Create Officer or Supervisor</h3>
              <p className="mt-2 text-sm text-slate-400">
                {isSuperAdmin ? 'SuperAdmin-created accounts are approved immediately.' : 'The account cannot log in until SuperAdmin approval.'}
              </p>
            </div>
            <button type="button" disabled={metadataLoading} onClick={loadMetadata} className="rounded-lg border border-white/15 px-3 py-2 text-xs text-slate-300 disabled:opacity-50">
              {metadataLoading ? 'Loading scope…' : 'Refresh scope'}
            </button>
          </div>

          {!metadataLoading && metadata.departments.length === 0 && (
            <p className="mt-4 rounded-xl border border-amber-400/25 bg-amber-500/10 p-3 text-sm text-amber-100">
              No active department is available. Create or activate a department and ward before creating staff accounts.
            </p>
          )}

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
              <select required disabled={metadataLoading} value={form.departmentId} onChange={(event) => updateForm('departmentId', event.target.value)} className="input disabled:opacity-40">
                <option value="">{metadataLoading ? 'Loading departments…' : 'Select department'}</option>
                {metadata.departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}
              </select>
            </label>
            <label className="text-sm text-slate-300 sm:col-span-2">Ward
              <select required={form.role === 'Officer'} disabled={!form.departmentId || metadataLoading} value={form.wardId} onChange={(event) => updateForm('wardId', event.target.value)} className="input disabled:opacity-40">
                <option value="">{form.role === 'Supervisor' ? 'All department wards (optional)' : 'Select ward'}</option>
                {wards.map((ward) => <option key={ward.id} value={ward.id}>{ward.name}</option>)}
              </select>
            </label>
            <label className="text-sm text-slate-300">Temporary password
              <input required type="text" minLength="8" value={form.temporaryPassword} onChange={(event) => updateForm('temporaryPassword', event.target.value)} className="input" autoComplete="new-password" />
            </label>
            <label className="text-sm text-slate-300">Confirm password
              <input required type="text" minLength="8" value={form.confirmPassword} onChange={(event) => updateForm('confirmPassword', event.target.value)} className="input" autoComplete="new-password" />
            </label>
          </div>

          <button type="button" onClick={generatePassword} className="mt-3 rounded-lg border border-sky-400/30 px-3 py-2 text-xs font-semibold text-sky-200">
            Generate strong temporary password
          </button>
          <p className="mt-4 text-xs leading-5 text-slate-500">Password requires uppercase, lowercase, number and special character. Share it securely only after the account has been approved.</p>
          <button type="submit" disabled={creationDisabled} className="mt-5 w-full rounded-xl bg-violet-500 px-5 py-3 font-bold text-white disabled:cursor-not-allowed disabled:opacity-50">
            {working ? 'Creating account…' : metadataLoading ? 'Loading departments and wards…' : isSuperAdmin ? 'Create and approve account' : 'Create and send for approval'}
          </button>
        </form>

        <div>
          <form onSubmit={(event) => { event.preventDefault(); void loadAccounts(); }} className="grid gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 md:grid-cols-[1fr_220px_auto]">
            <input value={search} onChange={(event) => setSearch(event.target.value)} className="input mt-0" placeholder="Search staff name, email or scope" />
            <select value={status} onChange={(event) => setStatus(event.target.value)} className="input mt-0">
              <option value="">All approval statuses</option>
              <option value="PendingApproval">Pending approval</option>
              <option value="Approved">Approved</option>
              <option value="Rejected">Rejected</option>
              <option value="ExistingActive">Existing active</option>
              <option value="Inactive">Inactive</option>
            </select>
            <button type="submit" className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white">Search</button>
          </form>

          <div className="mt-4 space-y-4">
            {loading && <div className="cv-card rounded-2xl border p-8 text-center text-slate-600">Loading staff accounts…</div>}
            {!loading && accounts.length === 0 && <div className="cv-card rounded-2xl border p-8 text-center text-slate-600">No staff accounts match this filter.</div>}
            {!loading && accounts.map((account) => (
              <article key={account.id} className="cv-card rounded-2xl border p-5">
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
                    <button type="button" disabled={working} onClick={() => setReview({ account, decision: 'Approve', remarks: '' })} className="rounded-xl bg-emerald-500 px-4 py-2 font-bold text-white disabled:opacity-50">Verify and approve</button>
                    <button type="button" disabled={working} onClick={() => setReview({ account, decision: 'Reject', remarks: '' })} className="rounded-xl border border-rose-400/40 px-4 py-2 font-bold text-rose-200 disabled:opacity-50">Reject</button>
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
              <button type="button" disabled={working} onClick={() => setReview(null)} className="rounded-xl border border-white/15 px-4 py-3 text-slate-200">Cancel</button>
              <button type="button" disabled={working} onClick={submitReview} className={review.decision === 'Approve' ? 'rounded-xl bg-emerald-500 px-5 py-3 font-bold text-white disabled:opacity-50' : 'rounded-xl bg-rose-500 px-5 py-3 font-bold text-white disabled:opacity-50'}>
                {working ? 'Saving review…' : review.decision === 'Approve' ? 'Approve account' : 'Reject account'}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
