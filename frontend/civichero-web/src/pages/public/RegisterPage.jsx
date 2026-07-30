import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';

const initialForm = { fullName: '', email: '', phone: '', password: '', confirmPassword: '' };

export default function RegisterPage() {
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);
  const { register } = useAuth();
  const navigate = useNavigate();

  const update = (event) => setForm((current) => ({ ...current, [event.target.name]: event.target.value }));

  const submit = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const result = await register(form);
      navigate(ROUTE_PATHS.verifyEmail, {
        state: {
          email: result.email,
          token: result.developmentVerificationToken || '',
          message: result.message,
        },
      });
    } catch (apiError) {
      setError(apiError.errors?.length ? apiError.errors.join(' ') : apiError.message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section className="mx-auto max-w-lg px-6 py-16">
      <div className="rounded-2xl border border-white/10 bg-white/5 p-7 shadow-2xl">
        <p className="text-sm font-semibold uppercase tracking-wider text-sky-300">Citizen account</p>
        <h1 className="mt-2 text-3xl font-black text-white">Create your CivicHero account</h1>
        <p className="mt-2 text-slate-400">Registration is stored in AWS RDS MySQL.</p>

        {error && <div className="mt-5 rounded-lg border border-rose-400/30 bg-rose-500/10 p-3 text-sm text-rose-200">{error}</div>}

        <form onSubmit={submit} className="mt-6 space-y-4">
          <Field label="Full name" name="fullName" value={form.fullName} onChange={update} autoComplete="name" />
          <Field label="Email" name="email" type="email" value={form.email} onChange={update} autoComplete="email" />
          <Field label="Phone (optional)" name="phone" value={form.phone} onChange={update} autoComplete="tel" />
          <Field label="Password" name="password" type="password" value={form.password} onChange={update} autoComplete="new-password" />
          <Field label="Confirm password" name="confirmPassword" type="password" value={form.confirmPassword} onChange={update} autoComplete="new-password" />
          <button disabled={submitting} className="w-full rounded-lg bg-sky-500 px-4 py-3 font-bold text-white hover:bg-sky-400 disabled:opacity-60">
            {submitting ? 'Creating account…' : 'Register'}
          </button>
        </form>

        <p className="mt-5 text-center text-sm text-slate-400">
          Already registered? <Link className="font-semibold text-sky-300" to={ROUTE_PATHS.login}>Login</Link>
        </p>
      </div>
    </section>
  );
}

function Field({ label, ...props }) {
  return (
    <label className="block text-sm font-medium text-slate-300">
      {label}
      <input required={!label.includes('optional')} {...props} className="mt-1 w-full rounded-lg border border-white/10 bg-slate-900 px-3 py-3 text-white outline-none focus:border-sky-400" />
    </label>
  );
}
