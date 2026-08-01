import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { superAdminGovernanceApi } from '../../services/superAdminGovernanceApi.js';

const roles = ['Citizen', 'Officer', 'Supervisor', 'Admin', 'SuperAdmin'];
const blankAdmin = { fullName: '', email: '', phone: '', temporaryPassword: '', markEmailVerified: true };
const blankRelease = { target: 'Launch', decision: 'Reject', reason: '', releaseVersion: '', evidenceReference: '' };
const fmt = (value) => value ? new Date(value).toLocaleString() : '—';
const reasonValid = (value) => value.trim().length >= 10 && value.trim().length <= 1000;

export default function SuperAdminGovernance() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [tab, setTab] = useState('admins');
  const [admins, setAdmins] = useState([]);
  const [adminSearch, setAdminSearch] = useState('');
  const [adminForm, setAdminForm] = useState(blankAdmin);
  const [policies, setPolicies] = useState(null);
  const [sessions, setSessions] = useState({ sessions: [], schemaNote: '' });
  const [sessionSearch, setSessionSearch] = useState('');
  const [sessionRole, setSessionRole] = useState('');
  const [releases, setReleases] = useState({ currentDecisions: [], history: [] });
  const [releaseForm, setReleaseForm] = useState(blankRelease);
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [secret, setSecret] = useState(null);

  const isSuperAdmin = String(user?.role || '').toLowerCase() === 'superadmin';

  const loadAdmins = useCallback(async () => {
    setAdmins(await superAdminGovernanceApi.getAdminAccounts({ search: adminSearch || undefined, includeInactive: true }));
  }, [adminSearch]);

  const loadPolicies = useCallback(async () => setPolicies(await superAdminGovernanceApi.getPolicies()), []);
  const loadSessions = useCallback(async () => setSessions(await superAdminGovernanceApi.getSessions({ search: sessionSearch || undefined, role: sessionRole || undefined })), [sessionSearch, sessionRole]);
  const loadReleases = useCallback(async () => setReleases(await superAdminGovernanceApi.getReleaseDecisions()), []);

  const loadTab = useCallback(async () => {
    if (!isSuperAdmin) return;
    setError('');
    try {
      if (tab === 'admins') await loadAdmins();
      if (tab === 'policies') await loadPolicies();
      if (tab === 'sessions') await loadSessions();
      if (tab === 'release') await loadReleases();
    } catch (e) { setError(e.message || 'Unable to load SuperAdmin governance data.'); }
  }, [isSuperAdmin, tab, loadAdmins, loadPolicies, loadSessions, loadReleases]);

  useEffect(() => { loadTab(); }, [loadTab]);

  const run = async (name, action, success) => {
    setBusy(name); setError(''); setMessage(''); setSecret(null);
    try { const result = await action(); setMessage(success); return result; }
    catch (e) { setError(e.errors?.length ? e.errors.join(' ') : e.message || 'Governance operation failed.'); return null; }
    finally { setBusy(''); }
  };

  const createAdmin = async (event) => {
    event.preventDefault();
    const result = await run('create-admin', () => superAdminGovernanceApi.createAdmin({
      ...adminForm,
      fullName: adminForm.fullName.trim(),
      email: adminForm.email.trim(),
      phone: adminForm.phone.trim() || null,
      temporaryPassword: adminForm.temporaryPassword || null,
    }), 'Admin account created. Copy the temporary credentials now.');
    if (!result) return;
    setSecret({ title: 'New Admin credentials', email: result.account.email, password: result.temporaryPassword, deadline: result.twoFactorEnrollmentDeadlineUtc });
    setAdminForm(blankAdmin);
    await loadAdmins();
  };

  const toggleAdmin = async (account) => {
    if (!reasonValid(reason)) { setError('Enter an audited reason containing at least 10 characters.'); return; }
    const result = await run(`active-${account.id}`, () => superAdminGovernanceApi.setAdminActive(account.id, { isActive: !account.isActive, reason }), `Admin account ${account.isActive ? 'deactivated' : 'activated'}.`);
    if (result) { setReason(''); await loadAdmins(); }
  };

  const resetPassword = async (account) => {
    if (!reasonValid(reason)) { setError('Enter an audited reason containing at least 10 characters.'); return; }
    const result = await run(`password-${account.id}`, () => superAdminGovernanceApi.resetAdminPassword(account.id, { temporaryPassword: null, reason }), 'Temporary password reset and sessions revoked.');
    if (result) { setSecret({ title: 'Reset Admin credentials', email: result.email, password: result.temporaryPassword }); setReason(''); await loadAdmins(); }
  };

  const updateRoleEntry = (index, field, value) => setPolicies((current) => ({
    ...current,
    rolePolicy: {
      ...current.rolePolicy,
      roles: current.rolePolicy.roles.map((entry, itemIndex) => itemIndex === index ? { ...entry, [field]: value } : entry),
    },
  }));

  const saveRolePolicy = async () => {
    const result = await run('role-policy', () => superAdminGovernanceApi.updateRolePolicy({
      ...policies.rolePolicy,
      roles: policies.rolePolicy.roles.map((entry) => ({
        ...entry,
        deniedApiPrefixes: Array.isArray(entry.deniedApiPrefixes)
          ? entry.deniedApiPrefixes
          : String(entry.deniedApiPrefixes || '').split(/[\n,]+/).map((item) => item.trim()).filter(Boolean),
      })),
    }), 'Role policy updated. All existing sessions were revoked; sign in again to continue safely.');
    if (result) await signOutAfterPolicyChange();
  };

  const toggleRequiredRole = (role) => setPolicies((current) => {
    const selected = current.authenticationPolicy.twoFactorRequiredRoles || [];
    const next = selected.includes(role) ? selected.filter((item) => item !== role) : [...selected, role];
    return { ...current, authenticationPolicy: { ...current.authenticationPolicy, twoFactorRequiredRoles: next } };
  });

  const saveAuthenticationPolicy = async () => {
    const result = await run('auth-policy', () => superAdminGovernanceApi.updateAuthenticationPolicy(policies.authenticationPolicy), 'Authentication policy updated. Sessions were revoked and affected users received a two-factor enrollment grace period.');
    if (result) await signOutAfterPolicyChange();
  };

  const signOutAfterPolicyChange = async () => {
    globalThis.setTimeout(async () => {
      try { await logout(); } finally { navigate('/login', { replace: true }); }
    }, 1400);
  };

  const revokeSession = async (session) => {
    if (!reasonValid(reason)) { setError('Enter an audited reason containing at least 10 characters.'); return; }
    const result = await run(`session-${session.sessionId}`, () => superAdminGovernanceApi.revokeSession(session.sessionId, { reason }), `Session for ${session.email} revoked.`);
    if (result) { setReason(''); await loadSessions(); }
  };

  const decideRelease = async (event) => {
    event.preventDefault();
    if (!reasonValid(releaseForm.reason)) { setError('Release decisions require a reason containing at least 10 characters.'); return; }
    const result = await run('release-decision', () => superAdminGovernanceApi.decideRelease(releaseForm.target, {
      decision: releaseForm.decision,
      reason: releaseForm.reason,
      releaseVersion: releaseForm.releaseVersion || null,
      evidenceReference: releaseForm.evidenceReference || null,
    }), `${releaseForm.target} decision recorded.`);
    if (result) { setReleaseForm(blankRelease); await loadReleases(); }
  };

  const policyRows = useMemo(() => policies?.rolePolicy?.roles || [], [policies]);

  if (!isSuperAdmin) return <section className="p-8"><div className="rounded-2xl border border-rose-400/30 bg-rose-400/10 p-6 text-rose-100"><h2 className="text-2xl font-black">SuperAdmin access required</h2><p className="mt-2">This workspace contains global authentication, session and release controls.</p></div></section>;

  return <section className="space-y-6 p-6 lg:p-10">
    <div><p className="text-xs font-black uppercase tracking-[0.2em] text-fuchsia-300">Global governance</p><h1 className="mt-2 text-3xl font-black text-white">SuperAdmin governance centre</h1><p className="mt-2 text-slate-400">Manage Admin accounts, deny-only role restrictions, authentication rules, global sessions and formal release decisions.</p></div>
    <div className="flex flex-wrap gap-2">{[['admins','Admin accounts'],['policies','Policies'],['sessions','Global sessions'],['release','Release decisions']].map(([key,label]) => <button key={key} onClick={() => { setTab(key); setError(''); setMessage(''); setSecret(null); }} className={`rounded-xl px-4 py-2 text-sm font-bold ${tab === key ? 'bg-fuchsia-500 text-white' : 'bg-white/5 text-slate-300'}`}>{label}</button>)}</div>
    {error && <div className="rounded-xl border border-rose-400/30 bg-rose-400/10 p-4 text-rose-100">{error}</div>}
    {message && <div className="rounded-xl border border-emerald-400/30 bg-emerald-400/10 p-4 text-emerald-100">{message}</div>}
    {secret && <div className="rounded-2xl border border-amber-300/30 bg-amber-300/10 p-5 text-amber-100"><h3 className="font-black">{secret.title}</h3><p className="mt-2">Email: <strong>{secret.email}</strong></p><p>Password: <code className="rounded bg-black/30 px-2 py-1">{secret.password}</code></p>{secret.deadline && <p className="mt-2 text-sm">2FA enrollment deadline: {fmt(secret.deadline)}</p>}<p className="mt-2 text-xs">This temporary password is displayed only in this response. Share it through a secure channel.</p></div>}

    {tab === 'admins' && <AdminsTab admins={admins} form={adminForm} setForm={setAdminForm} search={adminSearch} setSearch={setAdminSearch} load={loadAdmins} create={createAdmin} reason={reason} setReason={setReason} toggle={toggleAdmin} reset={resetPassword} busy={busy} />}
    {tab === 'policies' && <PoliciesTab policies={policies} rows={policyRows} setPolicies={setPolicies} updateRoleEntry={updateRoleEntry} saveRolePolicy={saveRolePolicy} toggleRequiredRole={toggleRequiredRole} saveAuthenticationPolicy={saveAuthenticationPolicy} busy={busy} />}
    {tab === 'sessions' && <SessionsTab data={sessions} search={sessionSearch} setSearch={setSessionSearch} role={sessionRole} setRole={setSessionRole} load={loadSessions} reason={reason} setReason={setReason} revoke={revokeSession} busy={busy} />}
    {tab === 'release' && <ReleaseTab data={releases} form={releaseForm} setForm={setReleaseForm} submit={decideRelease} busy={busy} />}
  </section>;
}

function AdminsTab({ admins, form, setForm, search, setSearch, load, create, reason, setReason, toggle, reset, busy }) {
  return <div className="grid gap-6 xl:grid-cols-[380px_1fr]">
    <form onSubmit={create} className="h-fit rounded-2xl border border-white/10 bg-white/5 p-5"><h2 className="text-xl font-black text-white">Create Admin</h2><p className="mt-2 text-sm text-slate-400">Creates an active Admin account directly under SuperAdmin authority.</p><div className="mt-4 space-y-3"><Input label="Full name" required value={form.fullName} onChange={(v) => setForm({ ...form, fullName: v })} /><Input label="Official email" type="email" required value={form.email} onChange={(v) => setForm({ ...form, email: v })} /><Input label="Phone (optional)" value={form.phone} onChange={(v) => setForm({ ...form, phone: v })} /><Input label="Temporary password (blank = generate)" type="password" value={form.temporaryPassword} onChange={(v) => setForm({ ...form, temporaryPassword: v })} /><label className="flex items-center gap-3 text-sm text-slate-300"><input type="checkbox" checked={form.markEmailVerified} onChange={(e) => setForm({ ...form, markEmailVerified: e.target.checked })} /> Mark email verified</label></div><button disabled={busy === 'create-admin'} className="mt-5 w-full rounded-xl bg-fuchsia-500 px-4 py-3 font-bold text-white disabled:opacity-50">{busy === 'create-admin' ? 'Creating…' : 'Create Admin account'}</button></form>
    <div><form onSubmit={(e) => { e.preventDefault(); load(); }} className="flex gap-3 rounded-2xl border border-white/10 bg-white/5 p-4"><input value={search} onChange={(e) => setSearch(e.target.value)} className="input mt-0 flex-1" placeholder="Search Admin accounts" /><button className="rounded-xl bg-sky-500 px-5 font-bold text-white">Search</button></form><label className="mt-4 block text-sm text-slate-300">Audited reason for activation, deactivation or password reset<textarea value={reason} onChange={(e) => setReason(e.target.value)} rows="3" className="input resize-y" placeholder="Explain why this privileged action is required" /></label><div className="mt-4 space-y-3">{admins.length === 0 ? <Empty text="No Admin accounts found." /> : admins.map((account) => <article key={account.id} className="rounded-2xl border border-white/10 bg-slate-950/45 p-5"><div className="flex flex-wrap justify-between gap-3"><div><h3 className="font-black text-white">{account.fullName}</h3><p className="text-sm text-slate-400">{account.email}</p><p className="mt-2 text-xs text-slate-500">2FA: {account.twoFactorEnabled ? 'Enabled' : 'Not enabled'} · Last login: {fmt(account.lastLoginAt)}</p></div><span className={`h-fit rounded-full px-3 py-1 text-xs font-bold ${account.isActive ? 'bg-emerald-400/10 text-emerald-200' : 'bg-rose-400/10 text-rose-200'}`}>{account.isActive ? 'Active' : 'Inactive'}</span></div><div className="mt-4 flex flex-wrap gap-2"><button type="button" disabled={busy === `active-${account.id}`} onClick={() => toggle(account)} className="rounded-xl border border-white/15 px-3 py-2 text-sm font-bold text-white">{account.isActive ? 'Deactivate' : 'Activate'}</button><button type="button" disabled={busy === `password-${account.id}`} onClick={() => reset(account)} className="rounded-xl bg-amber-400/15 px-3 py-2 text-sm font-bold text-amber-100">Reset password</button></div></article>)}</div></div>
  </div>;
}

function PoliciesTab({ policies, rows, setPolicies, updateRoleEntry, saveRolePolicy, toggleRequiredRole, saveAuthenticationPolicy, busy }) {
  if (!policies) return <Empty text="Loading governance policies…" />;
  const auth = policies.authenticationPolicy;
  return <div className="space-y-6">
    <div className="rounded-2xl border border-sky-400/20 bg-sky-400/10 p-4 text-sm text-sky-100">Role policy is deny-only: it can disable a role or deny API prefixes, but it cannot grant permissions beyond the backend controller authorization rules.</div>
    <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><h2 className="text-xl font-black text-white">Global role policy</h2><p className="mt-1 text-sm text-slate-400">Use one API prefix per line, such as <code>/api/v1/rewards</code>.</p></div><button onClick={saveRolePolicy} disabled={busy === 'role-policy'} className="rounded-xl bg-fuchsia-500 px-4 py-2 font-bold text-white disabled:opacity-50">Save role policy</button></div><div className="mt-5 grid gap-4 lg:grid-cols-2">{rows.map((entry,index) => <article key={entry.role} className="rounded-xl border border-white/10 bg-slate-950/45 p-4"><div className="flex items-center justify-between"><h3 className="font-black text-white">{entry.role}</h3><label className="flex items-center gap-2 text-sm text-slate-300"><input type="checkbox" checked={entry.enabled} disabled={entry.role === 'SuperAdmin'} onChange={(e) => updateRoleEntry(index,'enabled',e.target.checked)} /> Enabled</label></div><textarea value={entry.description} onChange={(e) => updateRoleEntry(index,'description',e.target.value)} rows="2" className="input resize-y" /><textarea value={(entry.deniedApiPrefixes || []).join('\n')} onChange={(e) => updateRoleEntry(index,'deniedApiPrefixes',e.target.value.split(/\n+/).map((item) => item.trim()).filter(Boolean))} rows="4" className="input resize-y" placeholder="Denied API prefixes" /></article>)}</div></div>
    <div className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><h2 className="text-xl font-black text-white">Authentication policy</h2><p className="mt-1 text-sm text-slate-400">Changes revoke active sessions. Non-compliant users receive the configured 2FA enrollment grace period.</p></div><button onClick={saveAuthenticationPolicy} disabled={busy === 'auth-policy'} className="rounded-xl bg-fuchsia-500 px-4 py-2 font-bold text-white disabled:opacity-50">Save authentication policy</button></div><div className="mt-5 grid gap-4 md:grid-cols-3"><NumberInput label="Failed attempts before lockout" value={auth.maximumFailedLoginAttempts} onChange={(v) => setPolicies({ ...policies, authenticationPolicy: { ...auth, maximumFailedLoginAttempts: v } })} /><NumberInput label="Lockout minutes" value={auth.lockoutMinutes} onChange={(v) => setPolicies({ ...policies, authenticationPolicy: { ...auth, lockoutMinutes: v } })} /><NumberInput label="2FA enrollment grace hours" value={auth.twoFactorEnrollmentGraceHours} onChange={(v) => setPolicies({ ...policies, authenticationPolicy: { ...auth, twoFactorEnrollmentGraceHours: v } })} /></div><label className="mt-4 flex items-center gap-3 text-sm text-slate-300"><input type="checkbox" checked={auth.requireVerifiedEmail} onChange={(e) => setPolicies({ ...policies, authenticationPolicy: { ...auth, requireVerifiedEmail: e.target.checked } })} /> Require verified email at login</label><p className="mt-5 text-sm font-bold text-white">Roles requiring 2FA</p><div className="mt-3 flex flex-wrap gap-3">{roles.map((role) => <label key={role} className="rounded-xl border border-white/10 bg-slate-950/45 px-3 py-2 text-sm text-slate-300"><input className="mr-2" type="checkbox" checked={(auth.twoFactorRequiredRoles || []).includes(role)} onChange={() => toggleRequiredRole(role)} />{role}</label>)}</div></div>
  </div>;
}

function SessionsTab({ data, search, setSearch, role, setRole, load, reason, setReason, revoke, busy }) {
  return <div className="space-y-5"><div className="rounded-xl border border-amber-300/20 bg-amber-300/10 p-4 text-sm text-amber-100">{data.schemaNote || 'The current schema stores at most one refresh session per user.'}</div><form onSubmit={(e) => { e.preventDefault(); load(); }} className="grid gap-3 rounded-2xl border border-white/10 bg-white/5 p-4 md:grid-cols-[1fr_220px_auto]"><input value={search} onChange={(e) => setSearch(e.target.value)} className="input mt-0" placeholder="Search user or email" /><select value={role} onChange={(e) => setRole(e.target.value)} className="input mt-0"><option value="">All roles</option>{roles.map((item) => <option key={item}>{item}</option>)}</select><button className="rounded-xl bg-sky-500 px-5 font-bold text-white">Search</button></form><label className="block text-sm text-slate-300">Audited revocation reason<textarea value={reason} onChange={(e) => setReason(e.target.value)} rows="3" className="input resize-y" /></label><div className="overflow-x-auto rounded-2xl border border-white/10 bg-white/5"><table className="min-w-full text-left text-sm"><thead className="text-xs uppercase tracking-wider text-slate-500"><tr><th className="p-3">User</th><th className="p-3">Role</th><th className="p-3">Created</th><th className="p-3">Expires</th><th className="p-3">2FA</th><th className="p-3">Action</th></tr></thead><tbody className="divide-y divide-white/10">{(data.sessions || []).length === 0 ? <tr><td colSpan="6" className="p-6 text-center text-slate-400">No active sessions found.</td></tr> : data.sessions.map((session) => <tr key={session.sessionId}><td className="p-3"><p className="font-bold text-white">{session.fullName}</p><p className="text-xs text-slate-500">{session.email}</p><p className="text-[10px] text-slate-600">{session.sessionId}</p></td><td className="p-3 text-slate-300">{session.role}</td><td className="p-3 text-slate-300">{fmt(session.createdAtUtc)}</td><td className="p-3 text-slate-300">{fmt(session.expiresAtUtc)}</td><td className="p-3 text-slate-300">{session.twoFactorEnabled ? 'Enabled' : 'No'}</td><td className="p-3"><button disabled={busy === `session-${session.sessionId}`} onClick={() => revoke(session)} className="rounded-lg bg-rose-500/15 px-3 py-2 text-xs font-bold text-rose-200">Revoke selected</button></td></tr>)}</tbody></table></div></div>;
}

function ReleaseTab({ data, form, setForm, submit, busy }) {
  return <div className="grid gap-6 xl:grid-cols-[380px_1fr]"><form onSubmit={submit} className="h-fit rounded-2xl border border-white/10 bg-white/5 p-5"><h2 className="text-xl font-black text-white">Record formal decision</h2><p className="mt-2 text-sm text-slate-400">Approval is blocked unless the required evidence file and readiness conditions are present. Rejection can always be recorded.</p><label className="mt-4 block text-sm text-slate-300">Target<select value={form.target} onChange={(e) => setForm({ ...form, target: e.target.value })} className="input"><option value="Launch">Launch</option><option value="FinalRelease">Final release</option></select></label><label className="mt-3 block text-sm text-slate-300">Decision<select value={form.decision} onChange={(e) => setForm({ ...form, decision: e.target.value })} className="input"><option value="Approve">Approve</option><option value="Reject">Reject</option></select></label><Input label="Release version (optional)" value={form.releaseVersion} onChange={(v) => setForm({ ...form, releaseVersion: v })} /><Input label="Evidence reference (optional)" value={form.evidenceReference} onChange={(v) => setForm({ ...form, evidenceReference: v })} /><label className="mt-3 block text-sm text-slate-300">Audited reason<textarea required value={form.reason} onChange={(e) => setForm({ ...form, reason: e.target.value })} rows="5" className="input resize-y" /></label><button disabled={busy === 'release-decision'} className="mt-5 w-full rounded-xl bg-fuchsia-500 px-4 py-3 font-bold text-white disabled:opacity-50">Record decision</button></form><div className="space-y-5"><div className="grid gap-4 md:grid-cols-2">{(data.currentDecisions || []).length === 0 ? <Empty text="No current launch or final-release decision has been recorded." /> : data.currentDecisions.map((item) => <article key={item.target} className="rounded-2xl border border-white/10 bg-white/5 p-5"><div className="flex justify-between gap-3"><h3 className="font-black text-white">{item.target}</h3><span className={`rounded-full px-3 py-1 text-xs font-bold ${item.decision === 'Approve' ? 'bg-emerald-400/10 text-emerald-200' : 'bg-rose-400/10 text-rose-200'}`}>{item.decision}</span></div><p className="mt-3 text-sm text-slate-300">{item.reason}</p><p className="mt-3 text-xs text-slate-500">{fmt(item.decidedAtUtc)} · {item.decidedByEmail || item.decidedByUserId}</p><p className="mt-1 break-all text-xs text-sky-300">{item.evidenceReference || 'No evidence reference'}</p></article>)}</div><div className="rounded-2xl border border-white/10 bg-white/5 p-5"><h3 className="text-xl font-black text-white">Decision history</h3><div className="mt-4 space-y-3">{(data.history || []).length === 0 ? <p className="text-slate-400">No decisions recorded.</p> : data.history.map((item) => <div key={item.id} className="rounded-xl border border-white/10 bg-slate-950/45 p-4"><div className="flex flex-wrap justify-between gap-2"><p className="font-bold text-white">{item.target} · {item.decision}</p><time className="text-xs text-slate-500">{fmt(item.decidedAtUtc)}</time></div><p className="mt-2 text-sm text-slate-300">{item.reason}</p><p className="mt-2 text-xs text-slate-500">By {item.decidedByEmail || item.decidedByUserId || 'Unknown'} · Evidence validated: {item.evidenceValidated ? 'Yes' : 'No'}</p></div>)}</div></div></div></div>;
}

function Input({ label, value, onChange, type = 'text', required = false }) { return <label className="mt-3 block text-sm text-slate-300">{label}<input required={required} type={type} value={value} onChange={(e) => onChange(e.target.value)} className="input" /></label>; }
function NumberInput({ label, value, onChange }) { return <label className="text-sm text-slate-300">{label}<input type="number" value={value} onChange={(e) => onChange(Number(e.target.value))} className="input" /></label>; }
function Empty({ text }) { return <div className="rounded-2xl border border-white/10 bg-white/5 p-6 text-center text-slate-400">{text}</div>; }
