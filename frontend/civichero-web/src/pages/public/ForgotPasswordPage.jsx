import { useState } from 'react';
import { Link } from 'react-router-dom';
import { authApi } from '../../services/authApi.js';
import { ROUTE_PATHS } from '../../routes/routePaths.js';

const initialPhoneForm = { identifier: '', code: '', newPassword: '', confirmPassword: '' };

export default function ForgotPasswordPage() {
  const [method, setMethod] = useState('email');
  const [email, setEmail] = useState('');
  const [emailResult, setEmailResult] = useState(null);
  const [phoneForm, setPhoneForm] = useState(initialPhoneForm);
  const [phoneStep, setPhoneStep] = useState('request');
  const [otpInfo, setOtpInfo] = useState(null);
  const [error, setError] = useState(null);
  const [submitting, setSubmitting] = useState(false);

  const switchMethod = (nextMethod) => {
    setMethod(nextMethod);
    setError(null);
    setEmailResult(null);
  };

  const requestEmailLink = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const result = await authApi.requestPasswordResetLink(email.trim());
      setEmailResult(result || {});
    } catch (apiError) {
      setError(apiError.errors?.length ? apiError.errors.join(' ') : apiError.message);
    } finally {
      setSubmitting(false);
    }
  };

  const updatePhone = (field) => (event) => setPhoneForm((current) => ({ ...current, [field]: event.target.value }));

  const requestPhoneCode = async (event) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const response = await authApi.forgotPassword(phoneForm.identifier.trim());
      setOtpInfo(response);
      setPhoneStep('reset');
    } catch (apiError) {
      setError(apiError.errors?.length ? apiError.errors.join(' ') : apiError.message);
    } finally {
      setSubmitting(false);
    }
  };

  const resetUsingPhone = async (event) => {
    event.preventDefault();
    setError(null);
    if (phoneForm.newPassword !== phoneForm.confirmPassword) {
      setError('Password and confirmation password must match.');
      return;
    }

    setSubmitting(true);
    try {
      await authApi.resetPassword({
        identifier: phoneForm.identifier.trim(),
        code: phoneForm.code,
        newPassword: phoneForm.newPassword,
        confirmPassword: phoneForm.confirmPassword,
      });
      setPhoneForm(initialPhoneForm);
      setPhoneStep('done');
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
          <p>Use a short-lived secure email link, or use the verified-phone OTP fallback.</p>
        </div>
        <div className="auth-benefits">
          <span>Time-limited and tamper-protected link</span>
          <span>One-time use after password change</span>
          <span>All existing sessions are revoked</span>
          <span>Generic responses prevent account discovery</span>
        </div>
      </aside>

      <div className="auth-panel">
        <div className="auth-card">
          <p className="section-kicker">Secure recovery</p>
          <h1>Forgot your password?</h1>
          <p>Choose the recovery method already verified on your CivicHero account.</p>

          <div className="mb-5 grid grid-cols-2 gap-2 rounded-2xl border border-white/10 bg-white/5 p-2">
            <button type="button" onClick={() => switchMethod('email')} className={method === 'email' ? 'button primary full' : 'button outline full'}>Email link</button>
            <button type="button" onClick={() => switchMethod('phone')} className={method === 'phone' ? 'button primary full' : 'button outline full'}>Phone OTP</button>
          </div>

          {error && <div className="alert error">{error}</div>}

          {method === 'email' && !emailResult && (
            <form onSubmit={requestEmailLink}>
              <Field label="Registered and verified email" icon="@">
                <input
                  required
                  className="input"
                  type="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  autoComplete="email"
                  placeholder="you@example.com"
                />
              </Field>
              <button disabled={submitting} className="button primary full large" style={{ marginTop: 22 }}>
                {submitting ? 'Sending secure link…' : 'Send password-reset link'}
              </button>
            </form>
          )}

          {method === 'email' && emailResult && (
            <>
              <div className="alert success">
                If an eligible account exists, CivicHero sent a secure password-reset link. Check your inbox and spam folder.
              </div>
              {emailResult.developmentResetUrl && (
                <a className="button primary full large" href={emailResult.developmentResetUrl}>Open development reset link</a>
              )}
              <button type="button" className="button outline full" style={{ marginTop: 10 }} onClick={() => { setEmailResult(null); setError(null); }}>
                Request another link
              </button>
            </>
          )}

          {method === 'phone' && phoneStep === 'request' && (
            <form onSubmit={requestPhoneCode}>
              <Field label="Registered email or verified phone" icon="○">
                <input
                  required
                  className="input"
                  value={phoneForm.identifier}
                  onChange={updatePhone('identifier')}
                  autoComplete="username"
                  placeholder="you@example.com or +919876543210"
                />
              </Field>
              <button disabled={submitting} className="button primary full large" style={{ marginTop: 22 }}>
                {submitting ? 'Sending code…' : 'Send reset code'}
              </button>
            </form>
          )}

          {method === 'phone' && phoneStep === 'reset' && (
            <form onSubmit={resetUsingPhone}>
              <div className="alert success">
                If an eligible account exists, a code was sent to {otpInfo?.maskedPhoneNumber || 'your verified phone'}.
                {otpInfo?.developmentCode && <><br /><strong>Development code: {otpInfo.developmentCode}</strong></>}
              </div>
              <Field label="Six-digit reset code" icon="#">
                <input required className="input" inputMode="numeric" pattern="[0-9]{6}" maxLength="6" autoComplete="one-time-code" value={phoneForm.code} onChange={(event) => setPhoneForm((current) => ({ ...current, code: event.target.value.replace(/\D/g, '') }))} placeholder="000000" />
              </Field>
              <PasswordFields form={phoneForm} update={updatePhone} />
              <button disabled={submitting || phoneForm.code.length !== 6} className="button primary full large">
                {submitting ? 'Resetting password…' : 'Reset password'}
              </button>
              <button type="button" className="button outline full" style={{ marginTop: 10 }} onClick={() => { setPhoneStep('request'); setOtpInfo(null); setError(null); setPhoneForm((current) => ({ ...initialPhoneForm, identifier: current.identifier })); }}>
                Request another code
              </button>
            </form>
          )}

          {method === 'phone' && phoneStep === 'done' && (
            <>
              <div className="alert success">Password reset successfully. You can now sign in with the new password.</div>
              <Link className="button primary full large" to={ROUTE_PATHS.login}>Return to login</Link>
            </>
          )}

          <p className="auth-note"><Link to={ROUTE_PATHS.login}>Back to login</Link></p>
        </div>
      </div>
    </section>
  );
}

function PasswordFields({ form, update }) {
  return (
    <>
      <Field label="New password" icon="◇">
        <input required className="input" type="password" minLength="8" maxLength="128" autoComplete="new-password" value={form.newPassword} onChange={update('newPassword')} placeholder="At least 8 characters" />
      </Field>
      <Field label="Confirm new password" icon="◇">
        <input required className="input" type="password" minLength="8" maxLength="128" autoComplete="new-password" value={form.confirmPassword} onChange={update('confirmPassword')} placeholder="Repeat the new password" />
      </Field>
      <div className="alert warning">Use uppercase, lowercase, number, and special character.</div>
    </>
  );
}

function Field({ label, icon, children }) {
  return <label className="form-field">{label}<div className="form-icon-field"><span>{icon}</span>{children}</div></label>;
}
