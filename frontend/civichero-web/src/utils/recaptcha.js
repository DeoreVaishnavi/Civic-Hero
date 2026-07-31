let scriptPromise;

const loadRecaptcha = (siteKey) => {
  if (window.grecaptcha) return Promise.resolve(window.grecaptcha);
  if (!scriptPromise) {
    scriptPromise = new Promise((resolve, reject) => {
      const script = document.createElement("script");
      script.src = `https://www.google.com/recaptcha/api.js?render=${encodeURIComponent(siteKey)}`;
      script.async = true;
      script.onerror = () => reject(new Error("CAPTCHA could not be loaded."));
      script.onload = () => resolve(window.grecaptcha);
      document.head.appendChild(script);
    });
  }
  return scriptPromise;
};

export async function getAnonymousComplaintCaptchaToken() {
  if (import.meta.env.DEV && import.meta.env.VITE_CAPTCHA_DEVELOPMENT_BYPASS !== "false") return "development-bypass";
  const siteKey = import.meta.env.VITE_RECAPTCHA_SITE_KEY;
  if (!siteKey) throw new Error("Anonymous reporting requires VITE_RECAPTCHA_SITE_KEY in this environment.");
  const grecaptcha = await loadRecaptcha(siteKey);
  return new Promise((resolve, reject) => grecaptcha.ready(() => grecaptcha.execute(siteKey, { action: "anonymous_complaint" }).then(resolve).catch(reject)));
}
