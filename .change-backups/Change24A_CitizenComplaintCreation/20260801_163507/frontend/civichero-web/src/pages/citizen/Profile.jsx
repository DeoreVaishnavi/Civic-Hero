import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { userApi } from '../../services/userApi.js';

const acceptedAvatarTypes = ['image/jpeg', 'image/png', 'image/webp'];
const maximumAvatarBytes = 5 * 1024 * 1024;

export default function Profile() {
  const { user, updateCurrentUser, logout } = useAuth();
  const navigate = useNavigate();
  const avatarObjectUrl = useRef(null);
  const [form, setForm] = useState({ fullName: user?.fullName || '', phone: user?.phone || '' });
  const [emailForm, setEmailForm] = useState({ newEmail: '', currentPassword: '' });
  const [avatarUrl, setAvatarUrl] = useState('');
  const [avatarFile, setAvatarFile] = useState(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState('');

  useEffect(() => setForm({ fullName: user?.fullName || '', phone: user?.phone || '' }), [user]);

  const replaceAvatarUrl = (nextUrl) => {
    if (avatarObjectUrl.current) URL.revokeObjectURL(avatarObjectUrl.current);
    avatarObjectUrl.current = nextUrl || null;
    setAvatarUrl(nextUrl || '');
  };

  const loadAvatar = async () => {
    try {
      const blob = await userApi.getAvatar();
      replaceAvatarUrl(URL.createObjectURL(blob));
    } catch (reason) {
      if (reason?.status !== 404) setError(reason.message || 'Unable to load the profile avatar.');
      replaceAvatarUrl('');
    }
  };

  useEffect(() => {
    loadAvatar();
    return () => {
      if (avatarObjectUrl.current) URL.revokeObjectURL(avatarObjectUrl.current);
    };
  }, []);

  const submit = async (event) => {
    event.preventDefault();
    setBusy('profile');
    setMessage('');
    setError('');
    try {
      const updated = await userApi.updateProfile(form);
      updateCurrentUser(updated);
      setMessage('Profile updated successfully.');
    } catch (apiError) {
      setError(apiError.errors?.join(' ') || apiError.message);
    } finally {
      setBusy('');
    }
  };

  const selectAvatar = (event) => {
    const file = event.target.files?.[0] || null;
    setError('');
    if (!file) {
      setAvatarFile(null);
      return;
    }
    if (!acceptedAvatarTypes.includes(file.type)) {
      setError('Only JPEG, PNG and WebP avatar images are allowed.');
      event.target.value = '';
      return;
    }
    if (file.size <= 0 || file.size > maximumAvatarBytes) {
      setError('Avatar image must be between 1 byte and 5 MB.');
      event.target.value = '';
      return;
    }
    setAvatarFile(file);
  };

  const uploadAvatar = async () => {
    if (!avatarFile) return;
    setBusy('avatar');
    setMessage('');
    setError('');
    try {
      await userApi.uploadAvatar(avatarFile);
      setAvatarFile(null);
      await loadAvatar();
      setMessage('Profile avatar updated successfully.');
    } catch (apiError) {
      setError(apiError.errors?.join(' ') || apiError.message);
    } finally {
      setBusy('');
    }
  };

  const removeAvatar = async () => {
    if (!window.confirm('Remove your current profile avatar?')) return;
    setBusy('avatar-remove');
    setMessage('');
    setError('');
    try {
      await userApi.deleteAvatar();
      replaceAvatarUrl('');
      setAvatarFile(null);
      setMessage('Profile avatar removed.');
    } catch (apiError) {
      setError(apiError.message || 'Unable to remove the avatar.');
    } finally {
      setBusy('');
    }
  };

  const changeEmail = async (event) => {
    event.preventDefault();
    if (!window.confirm('Changing your email signs you out until the new address is verified. Continue?')) return;

    setBusy('email');
    setMessage('');
    setError('');
    try {
      const result = await userApi.changeEmail(emailForm);
      try {
        await logout();
      } finally {
        navigate('/verify-email', {
          replace: true,
          state: {
            email: result.email,
            token: result.developmentVerificationToken || '',
            message: result.developmentVerificationToken
              ? 'Email changed. Use the development token below to verify the new address.'
              : 'Email changed. Enter the verification token delivered to the new address.',
          },
        });
      }
    } catch (apiError) {
      setError(apiError.errors?.join(' ') || apiError.message);
      setBusy('');
    }
  };

  const initials = (user?.fullName || 'C')
    .split(' ')
    .filter(Boolean)
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase();

  return (
    <section className="p-6 lg:p-10">
      <div className="mx-auto max-w-4xl space-y-6">
        <div>
          <p className="text-sm font-semibold uppercase tracking-[0.18em] text-sky-400">Account profile</p>
          <h2 className="mt-2 text-3xl font-black text-white">My profile</h2>
          <p className="mt-2 text-slate-400">Update your public identity, profile avatar and verified email address.</p>
        </div>

        {message && <p className="rounded-xl border border-emerald-400/20 bg-emerald-500/10 p-4 text-emerald-200">{message}</p>}
        {error && <p className="rounded-xl border border-rose-400/20 bg-rose-500/10 p-4 text-rose-200">{error}</p>}

        <div className="rounded-3xl border border-white/10 bg-white/5 p-7">
          <div className="flex flex-col gap-6 sm:flex-row sm:items-center">
            <div className="grid h-28 w-28 flex-none place-items-center overflow-hidden rounded-full border-4 border-sky-400/30 bg-slate-900 text-2xl font-black text-sky-200">
              {avatarUrl ? <img src={avatarUrl} alt="Profile avatar" className="h-full w-full object-cover" /> : initials}
            </div>
            <div className="flex-1">
              <h3 className="text-xl font-black text-white">Profile avatar</h3>
              <p className="mt-1 text-sm text-slate-400">Upload a JPEG, PNG or WebP image up to 5 MB.</p>
              <div className="mt-4 flex flex-wrap items-center gap-3">
                <input type="file" accept="image/jpeg,image/png,image/webp" onChange={selectAvatar} className="max-w-full text-sm text-slate-300" />
                <button type="button" disabled={!avatarFile || Boolean(busy)} onClick={uploadAvatar} className="rounded-xl bg-sky-500 px-4 py-2 font-bold text-white disabled:opacity-50">
                  {busy === 'avatar' ? 'Uploading…' : 'Upload avatar'}
                </button>
                <button type="button" disabled={!avatarUrl || Boolean(busy)} onClick={removeAvatar} className="rounded-xl border border-rose-400/30 px-4 py-2 font-bold text-rose-200 disabled:opacity-50">
                  Remove
                </button>
              </div>
              {avatarFile && <p className="mt-2 text-xs text-slate-500">Selected: {avatarFile.name}</p>}
            </div>
          </div>
        </div>

        <div className="rounded-3xl border border-white/10 bg-white/5 p-7">
          <h3 className="text-xl font-black text-white">Personal details</h3>
          <p className="mt-1 text-sm text-slate-400">Role and organizational scope are protected fields.</p>
          <form onSubmit={submit} className="mt-6 grid gap-5 sm:grid-cols-2">
            <Field label="Full name"><input required value={form.fullName} onChange={(event) => setForm({ ...form, fullName: event.target.value })} className="input" /></Field>
            <Field label="Phone"><input value={form.phone} onChange={(event) => setForm({ ...form, phone: event.target.value })} className="input" /></Field>
            <ReadOnly label="Email" value={user?.email} />
            <ReadOnly label="Role" value={user?.role} />
            <ReadOnly label="Department" value={user?.departmentName || 'Not assigned'} />
            <ReadOnly label="Ward" value={user?.wardName || 'Not assigned'} />
            <button disabled={Boolean(busy)} className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white disabled:opacity-60 sm:col-span-2">
              {busy === 'profile' ? 'Saving…' : 'Save profile'}
            </button>
          </form>
        </div>

        <div className="rounded-3xl border border-amber-400/20 bg-amber-400/5 p-7">
          <h3 className="text-xl font-black text-white">Change email address</h3>
          <p className="mt-2 text-sm text-slate-400">For security, confirm your current password. All sessions are revoked until the new email is verified.</p>
          <form onSubmit={changeEmail} className="mt-5 grid gap-4 sm:grid-cols-2">
            <Field label="New email"><input required type="email" value={emailForm.newEmail} onChange={(event) => setEmailForm({ ...emailForm, newEmail: event.target.value })} className="input" /></Field>
            <Field label="Current password"><input required type="password" autoComplete="current-password" value={emailForm.currentPassword} onChange={(event) => setEmailForm({ ...emailForm, currentPassword: event.target.value })} className="input" /></Field>
            <button disabled={Boolean(busy)} className="rounded-xl bg-amber-500 px-5 py-3 font-black text-slate-950 disabled:opacity-50 sm:col-span-2">
              {busy === 'email' ? 'Changing email…' : 'Change and verify email'}
            </button>
          </form>
        </div>

        <div className="rounded-2xl border border-dashed border-white/10 p-5 text-sm text-slate-500">
          Residential address is intentionally not included in this package because the current users table has no address field. It requires an approved database migration.
        </div>
      </div>
    </section>
  );
}

function Field({ label, children }) {
  return <label className="block text-sm font-semibold text-slate-300">{label}{children}</label>;
}

function ReadOnly({ label, value }) {
  return <div><p className="text-sm font-semibold text-slate-400">{label}</p><p className="mt-1 rounded-xl border border-white/10 bg-slate-900/60 px-3 py-3 text-slate-300">{value || '—'}</p></div>;
}
