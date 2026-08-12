import { useEffect, useState } from 'react';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { userApi } from '../../services/userApi.js';

export default function Profile() {
  const { user, updateCurrentUser } = useAuth();
  const [form, setForm] = useState({ fullName: user?.fullName || '', phone: user?.phone || '' });
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => setForm({ fullName: user?.fullName || '', phone: user?.phone || '' }), [user]);

  const submit = async (event) => {
    event.preventDefault(); setSaving(true); setMessage(''); setError('');
    try { const updated = await userApi.updateProfile(form); updateCurrentUser(updated); setMessage('Profile updated successfully.'); }
    catch (apiError) { setError(apiError.errors?.join(' ') || apiError.message); }
    finally { setSaving(false); }
  };

  return (
    <section className="p-6 lg:p-10">
      <div className="mx-auto max-w-3xl rounded-3xl border border-white/10 bg-white/5 p-7">
        <h2 className="text-3xl font-black text-white">My profile</h2>
        <p className="mt-2 text-slate-400">Identity and role fields are protected; you can update your display details.</p>
        {message && <p className="mt-4 rounded-xl bg-emerald-500/10 p-3 text-emerald-200">{message}</p>}
        {error && <p className="mt-4 rounded-xl bg-rose-500/10 p-3 text-rose-200">{error}</p>}
        <form onSubmit={submit} className="mt-6 grid gap-5 sm:grid-cols-2">
          <Field label="Full name"><input required value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} className="input" /></Field>
          <Field label="Phone"><input value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} className="input" /></Field>
          <ReadOnly label="Email" value={user?.email} /><ReadOnly label="Role" value={user?.role} />
          <ReadOnly label="Department" value={user?.departmentName || 'Not assigned'} /><ReadOnly label="Ward" value={user?.wardName || 'Not assigned'} />
          <button disabled={saving} className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white disabled:opacity-60 sm:col-span-2">{saving ? 'Saving…' : 'Save profile'}</button>
        </form>
      </div>
    </section>
  );
}
function Field({ label, children }) { return <label className="block text-sm font-semibold text-slate-300">{label}{children}</label>; }
function ReadOnly({ label, value }) { return <div><p className="text-sm font-semibold text-slate-400">{label}</p><p className="mt-1 rounded-xl border border-white/10 bg-slate-900/60 px-3 py-3 text-slate-300">{value || '—'}</p></div>; }
