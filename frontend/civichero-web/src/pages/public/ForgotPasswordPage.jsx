import { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../../services/authApi.js';
import { ROUTE_PATHS } from '../../routes/routePaths.js';

const initialForm = { identifier: '', code: '', newPassword: '', confirmPassword: '' };

export default function ForgotPasswordPage() {
  const [form, setForm] = useState(initialForm);
  const [step, setStep] = useState('request');
  const [otpInfo, setOtpInfo] = useState(null);
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const update = (field) => (event) => setForm((current) => ({ ...current, [field]: event.target.value }));

  const requestCode = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const response = await authApi.forgotPassword(form.identifier.trim());
      setOtpInfo(response);
      setStep('reset');
    } catch (apiError) {
      setError(apiError.errors?.length ? apiError.errors.join(' ') : apiError.message);
    } finally {
      setSubmitting(false);
    }
  };

  const resetPassword = async (event) => {
    event.preventDefault();
    setError(null);

    if (form.newPassword !== form.confirmPassword) {
      setError('Password and confirmation password must match.');
      return;
    }

    setSubmitting(true);
    try {
      await authApi.resetPassword({
        identifier: form.identifier.trim(),
        code: form.code,
        newPassword: form.newPassword,
        confirmPassword: form.confirmPassword,
      });
      setForm(initialForm);
      setStep('done');
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
          <p className="section-kicker" style={{ color: '#83baf8' }}>Account recovery</p>
          <h1>Recover access without exposing your account.</h1>
          <p>CivicHero sends a short-lived verification code only to the verified phone number already linked to your account.</p>
        </div>
        <div className="auth-benefits">
          <span>Six-digit one-time verification code</span>
          <span>Old sessions are revoked after reset</span>
          <span>Strong-password validation</span>
          <span>No account information is exposed</span>
        </div>
      </aside>

      <div className="auth-panel">
        <div className="auth-card">
          <p className="section-kicker">Secure recovery</p>
          <h1>{step === 'done' ? 'Password changed' : 'Forgot your password?'}</h1>
          <p>
            {step === 'request' && 'Enter your registered email or verified phone number.'}
            {step === 'reset' && 'Enter the code sent to your verified phone and choose a new password.'}
            {step === 'done' && 'Your password was reset and previous refresh sessions were revoked.'}
          </p>

          {error && <div className="alert error">{error}</div>}

          {step === 'request' && (
            <form onSubmit={requestCode}>
              <Field label="Registered email or verified phone" icon="○">
                <input
                  required
                  className="input"
                  value={form.identifier}
                  onChange={update('identifier')}
                  autoComplete="username"
                  placeholder="you@example.com or +919876543210"
                />
              </Field>
              <button disabled={submitting} className="button primary full large" style={{ marginTop: 22 }}>
                {submitting ? 'Sending code…' : 'Send reset code'}
              </button>
            </form>
          )}

          {step === 'reset' && (
            <form onSubmit={resetPassword}>
              <div className="alert success">
                If an eligible account exists, a code was sent to {otpInfo?.maskedPhoneNumber || 'your verified phone'}.
                {otpInfo?.developmentCode && <><br /><strong>Development code: {otpInfo.developmentCode}</strong></>}
              </div>

              <Field label="Six-digit reset code" icon="#">
                <input
                  required
                  className="input"
                  inputMode="numeric"
                  pattern="[0-9]{6}"
                  maxLength="6"
                  autoComplete="one-time-code"
                  value={form.code}
                  onChange={(event) => setForm((current) => ({ ...current, code: event.target.value.replace(/\D/g, '') }))}
                  placeholder="000000"
                />
              </Field>

              <Field label="New password" icon="◇">
                <input
                  required
                  className="input"
                  type="password"
                  minLength="8"
                  maxLength="128"
                  autoComplete="new-password"
                  value={form.newPassword}
                  onChange={update('newPassword')}
                  placeholder="At least 8 characters"
                />
              </Field>

              <Field label="Confirm new password" icon="◇">
                <input
                  required
                  className="input"
                  type="password"
                  minLength="8"
                  maxLength="128"
                  autoComplete="new-password"
                  value={form.confirmPassword}
                  onChange={update('confirmPassword')}
                  placeholder="Repeat the new password"
                />
              </Field>

              <div className="alert warning">Use uppercase, lowercase, number, and special character.</div>

              <button disabled={submitting || form.code.length !== 6} className="button primary full large">
                {submitting ? 'Resetting password…' : 'Reset password'}
              </button>
              <button
                type="button"
                className="button outline full"
                style={{ marginTop: 10 }}
                onClick={() => { setStep('request'); setOtpInfo(null); setError(null); setForm((current) => ({ ...initialForm, identifier: current.identifier })); }}
              >
                Request another code
              </button>
            </form>
          )}

          {step === 'done' && (
            <>
              <div className="alert success">Password reset successfully. You can now sign in with the new password.</div>
              <Link className="button primary full large" to={ROUTE_PATHS.login}>Return to login</Link>
            </>
          )}

          {step !== 'done' && <p className="auth-note"><Link to={ROUTE_PATHS.login}>Back to login</Link></p>}
        </div>
      </div>
    </section>
  );
}

function Field({ label, icon, children }) {
  return <label className="form-field">{label}<div className="form-icon-field"><span>{icon}</span>{children}</div></label>;
}
