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
    event.preventDefault(); setSubmitting(true); setError(null);
    try {
      const result = await register(form);
      navigate(ROUTE_PATHS.verifyEmail, { state: { email: result.email, token: result.developmentVerificationToken || '', message: result.message } });
    } catch (apiError) { setError(apiError.errors?.length ? apiError.errors.join(' ') : apiError.message); }
    finally { setSubmitting(false); }
  };

  return (
    <section className="auth-shell">
      <aside className="auth-visual">
        <div className="auth-visual-content"><p className="section-kicker" style={{ color: '#83baf8' }}>Citizen registration</p><h1>Join the community improving your city.</h1><p>Create one account to report issues, support nearby complaints, verify resolutions and see every important status update.</p></div>
        <div className="auth-benefits"><span>Quick complaint reporting</span><span>Ward-based issue discovery</span><span>Resolution verification</span><span>Civic points and badges</span></div>
      </aside>

      <div className="auth-panel">
        <div className="auth-card">
          <p className="section-kicker">Create account</p>
          <h1>Sign up for CivicHero</h1>
          <p>Your citizen account is protected with secure password hashing and email verification.</p>
          {error && <div className="alert error">{error}</div>}
          <form onSubmit={submit}>
            <Field label="Full name" icon="○"><input required name="fullName" value={form.fullName} onChange={update} autoComplete="name" className="input" placeholder="Your full name" /></Field>
            <Field label="Email address" icon="✉"><input required name="email" type="email" value={form.email} onChange={update} autoComplete="email" className="input" placeholder="you@example.com" /></Field>
            <Field label="Phone number (optional)" icon="▯"><input name="phone" value={form.phone} onChange={update} autoComplete="tel" className="input" placeholder="+919876543210" /></Field>
            <div className="form-grid">
              <Field label="Password" icon="◇"><input required name="password" type="password" value={form.password} onChange={update} autoComplete="new-password" className="input" placeholder="Strong password" /></Field>
              <Field label="Confirm password" icon="◇"><input required name="confirmPassword" type="password" value={form.confirmPassword} onChange={update} autoComplete="new-password" className="input" placeholder="Repeat password" /></Field>
            </div>
            <label className="check-row"><input required type="checkbox" /><span>I agree to the Terms of Service and Privacy Policy.</span></label>
            <button disabled={submitting} className="button primary full large">{submitting ? 'Creating account…' : 'Create citizen account'}</button>
          </form>
          <p className="auth-note">Already have an account? <Link to={ROUTE_PATHS.login}>Login</Link></p>
        </div>
      </div>
    </section>
  );
}

function Field({ label, icon, children }) { return <label className="form-field">{label}<div className="form-icon-field"><span>{icon}</span>{children}</div></label>; }
