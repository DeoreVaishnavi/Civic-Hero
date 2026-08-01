import { useEffect, useId, useRef, useState } from 'react';

const SITE_KEY = import.meta.env.VITE_RECAPTCHA_SITE_KEY || '';
const DEV_BYPASS = String(import.meta.env.VITE_CAPTCHA_DEVELOPMENT_BYPASS || '').toLowerCase() === 'true';

export default function CaptchaWidget({ onToken }) {
  const id = useId().replace(/:/g, '');
  const containerRef = useRef(null);
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!SITE_KEY && DEV_BYPASS) {
      onToken('development-bypass');
      setMessage('Development CAPTCHA bypass is active. Disable it before deployment.');
      return undefined;
    }
    if (!SITE_KEY) {
      onToken('');
      setMessage('CAPTCHA site key is not configured.');
      return undefined;
    }
    let cancelled = false;
    const render = () => {
      if (cancelled || !containerRef.current || !globalThis.grecaptcha) return;
      globalThis.grecaptcha.ready(() => {
        if (cancelled || containerRef.current.dataset.rendered) return;
        globalThis.grecaptcha.render(containerRef.current, {
          sitekey: SITE_KEY,
          callback: (token) => onToken(token),
          'expired-callback': () => onToken(''),
          'error-callback': () => { onToken(''); setMessage('CAPTCHA could not be loaded.'); },
          theme: 'dark',
        });
        containerRef.current.dataset.rendered = 'true';
      });
    };
    const existing = document.querySelector('script[data-civichero-recaptcha]');
    if (existing) render();
    else {
      const script = document.createElement('script');
      script.src = 'https://www.google.com/recaptcha/api.js?render=explicit';
      script.async = true; script.defer = true; script.dataset.civicheroRecaptcha = 'true';
      script.onload = render; script.onerror = () => setMessage('CAPTCHA script failed to load.');
      document.head.appendChild(script);
    }
    return () => { cancelled = true; };
  }, [onToken]);

  return <div><div id={id} ref={containerRef} />{message && <p className="mt-2 text-xs text-amber-300">{message}</p>}</div>;
}
