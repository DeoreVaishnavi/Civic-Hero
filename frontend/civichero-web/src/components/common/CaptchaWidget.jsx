import { useEffect, useId, useRef, useState } from 'react';

const SITE_KEY = String(import.meta.env.VITE_RECAPTCHA_SITE_KEY || '').trim();
const BYPASS_SETTING = String(import.meta.env.VITE_CAPTCHA_DEVELOPMENT_BYPASS || '').trim().toLowerCase();
const DEVELOPMENT_BYPASS = BYPASS_SETTING === 'true' || (import.meta.env.DEV && BYPASS_SETTING !== 'false');
const SCRIPT_SELECTOR = 'script[data-civichero-recaptcha]';

export default function CaptchaWidget({ onToken, onStatus, resetKey = 0 }) {
  const id = useId().replace(/:/g, '');
  const containerRef = useRef(null);
  const widgetIdRef = useRef(null);
  const [status, setStatus] = useState({ kind: 'loading', message: 'Preparing CAPTCHA verification…' });

  useEffect(() => {
    const report = (kind, message, token = '') => {
      setStatus({ kind, message });
      onToken(token);
      onStatus?.({ kind, message });
    };

    if (!SITE_KEY && DEVELOPMENT_BYPASS) {
      report(
        'development',
        'Local development verification is active. Production still requires a configured CAPTCHA key.',
        'development-bypass',
      );
      return undefined;
    }

    if (!SITE_KEY) {
      report(
        'blocked',
        'Anonymous submission is unavailable because the CAPTCHA site key is not configured.',
      );
      return undefined;
    }

    let cancelled = false;
    let timeoutId;
    let script;

    const renderWidget = () => {
      if (cancelled || !containerRef.current || !globalThis.grecaptcha) return;

      globalThis.grecaptcha.ready(() => {
        if (cancelled || !containerRef.current || widgetIdRef.current !== null) return;

        try {
          widgetIdRef.current = globalThis.grecaptcha.render(containerRef.current, {
            sitekey: SITE_KEY,
            callback: (token) => report('verified', 'CAPTCHA verification completed.', token),
            'expired-callback': () => report('ready', 'CAPTCHA expired. Please verify again.'),
            'error-callback': () => report('error', 'CAPTCHA could not be completed. Check your network and retry.'),
            theme: 'dark',
          });
          report('ready', 'Complete the CAPTCHA to submit anonymously.');
        } catch {
          report('error', 'CAPTCHA could not be initialized. Refresh the page and try again.');
        }
      });
    };

    const handleScriptError = () => report('error', 'CAPTCHA script failed to load. Check your internet connection.');

    script = document.querySelector(SCRIPT_SELECTOR);
    if (script) {
      if (globalThis.grecaptcha) renderWidget();
      else {
        script.addEventListener('load', renderWidget, { once: true });
        script.addEventListener('error', handleScriptError, { once: true });
      }
    } else {
      script = document.createElement('script');
      script.src = 'https://www.google.com/recaptcha/api.js?render=explicit';
      script.async = true;
      script.defer = true;
      script.dataset.civicheroRecaptcha = 'true';
      script.addEventListener('load', renderWidget, { once: true });
      script.addEventListener('error', handleScriptError, { once: true });
      document.head.appendChild(script);
    }

    timeoutId = globalThis.setTimeout(() => {
      if (!cancelled && widgetIdRef.current === null) {
        report('error', 'CAPTCHA is taking too long to load. Check your connection and retry.');
      }
    }, 12000);

    return () => {
      cancelled = true;
      globalThis.clearTimeout(timeoutId);
      script?.removeEventListener('load', renderWidget);
      script?.removeEventListener('error', handleScriptError);
    };
  }, [onStatus, onToken]);

  useEffect(() => {
    if (!SITE_KEY || widgetIdRef.current === null || !globalThis.grecaptcha?.reset) return;
    globalThis.grecaptcha.reset(widgetIdRef.current);
    onToken('');
    setStatus({ kind: 'ready', message: 'Complete the CAPTCHA to submit anonymously.' });
  }, [onToken, resetKey]);

  const statusClass = status.kind === 'blocked' || status.kind === 'error'
    ? 'border-rose-400/30 bg-rose-400/10 text-rose-100'
    : status.kind === 'verified' || status.kind === 'development'
      ? 'border-emerald-400/30 bg-emerald-400/10 text-emerald-100'
      : 'border-amber-400/25 bg-amber-400/5 text-amber-100';

  return (
    <div className="space-y-3">
      <div id={id} ref={containerRef} />
      <p className={`rounded-xl border px-4 py-3 text-sm ${statusClass}`} role="status">
        {status.message}
      </p>
    </div>
  );
}
