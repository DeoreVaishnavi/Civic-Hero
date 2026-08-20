import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { authApi } from '../../services/authApi.js';
import { ROUTE_PATHS } from '../../routes/routePaths.js';

const initialForm = { newPassword: '', confirmPassword: '' };

export default function ResetPasswordPage() {
  const [searchParams] = useSearchParams();
  const [token] = useState(() => searchParams.get('token') || '');
  const [status, setStatus] = useState('checking');
  const [expiresAt, setExpiresAt] = useState(null);
  const [form, setForm] = useState(initialForm);
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    let active = true;
    const validate = async () => {
      if (token) window.history.replaceState({}, document.title, ROUTE_PATHS.resetPassword);
      if (!token) {
        setStatus('invalid');
        return;
      }
      try {
        const result = await authApi.validatePasswordResetLink(token);
        if (!active) return;
        setStatus(result?.isValid ? 'ready' : 'invalid');
        setExpiresAt(result?.expiresAtUtc || null);
      } catch {
        if (active) setStatus('invalid');
      }
    };
    validate();
    return () => { active = false; };
  }, [token]);

  const update = (field) => (event) => setForm((current) => ({ ...current, [field]: event.target.value }));

  const submit = async (event) => {
    event.preventDefault();
    setError(null);
    if (form.newPassword !== form.confirmPassword) {
      setError('Password and confirmation password must match.');
      return;
    }

    setSubmitting(true);
    try {
      await authApi.completePasswordResetLink({ token, ...form });
      setForm(initialForm);
      setStatus('done');
    } catch (apiError) {
      setError(apiError.errors?.length ? apiError.errors.join(' ') : apiError.message);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section className="auth-shell">
      <aside className="auth-visual">
        <div className="auth-visual-content">
          <p className="section-kicker" style={{ color: '#83baf8' }}>Secure email recovery</p>
          <h1>Create a new CivicHero password.</h1>
          <p>The link is integrity-protected, time-limited, and becomes unusable after a successful reset.</p>
        </div>
        <div className="auth-benefits">
          <span>No account identifier appears in the URL</span>
          <span>Existing sessions are revoked</span>
          <span>Pending reset OTPs are invalidated</span>
          <span>Security audit evidence is recorded</span>
        </div>
      </aside>

      <div className="auth-panel">
        <div className="auth-card">
          <p className="section-kicker">Password reset</p>
          <h1>{status === 'done' ? 'Password changed' : 'Choose a new password'}</h1>

          {status === 'checking' && <div className="alert warning">Checking the security and expiry of this link…</div>}

          {status === 'invalid' && (
            <>
              <div className="alert error">This password-reset link is invalid, expired, or was already used.</div>
              <Link className="button primary full large" to={ROUTE_PATHS.forgotPassword}>Request a new link</Link>
            </>
          )}

          {status === 'ready' && (
            <form onSubmit={submit}>
              {expiresAt && <div className="alert success">Secure link verified. It expires at {new Date(expiresAt).toLocaleString()}.</div>}
              {error && <div className="alert error">{error}</div>}
              <Field label="New password" icon="◇">
                <input required className="input" type="password" minLength="8" maxLength="128" autoComplete="new-password" value={form.newPassword} onChange={update('newPassword')} placeholder="At least 8 characters" />
              </Field>
              <Field label="Confirm new password" icon="◇">
                <input required className="input" type="password" minLength="8" maxLength="128" autoComplete="new-password" value={form.confirmPassword} onChange={update('confirmPassword')} placeholder="Repeat the new password" />
              </Field>
              <div className="alert warning">Use uppercase, lowercase, number, and special character.</div>
              <button disabled={submitting} className="button primary full large">{submitting ? 'Resetting password…' : 'Reset password'}</button>
            </form>
          )}

          {status === 'done' && (
            <>
              <div className="alert success">Your password was reset. All previous browser and device sessions were revoked.</div>
              <Link className="button primary full large" to={ROUTE_PATHS.login}>Return to login</Link>
            </>
          )}

          {status !== 'done' && <p className="auth-note"><Link to={ROUTE_PATHS.login}>Back to login</Link></p>}
        </div>
      </div>
    </section>
  );
}

function Field({ label, icon, children }) {
  return <label className="form-field">{label}<div className="form-icon-field"><span>{icon}</span>{children}</div></label>;
}
