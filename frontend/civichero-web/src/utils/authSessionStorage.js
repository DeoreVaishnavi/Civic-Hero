const AUTH_SESSION_KEY = 'civichero.auth.session.v1';
const DEFAULT_EXPIRY_BUFFER_MS = 30_000;

function storageAvailable() {
  return typeof window !== 'undefined' && typeof window.sessionStorage !== 'undefined';
}

function normalizeSession(value) {
  if (!value || typeof value !== 'object') return null;

  const accessToken = typeof value.accessToken === 'string' ? value.accessToken.trim() : '';
  const expiresAtUtc = typeof value.expiresAtUtc === 'string' ? value.expiresAtUtc : null;
  const user = value.user && typeof value.user === 'object' ? value.user : null;

  if (!accessToken || !expiresAtUtc || !user) return null;

  const expiresAt = Date.parse(expiresAtUtc);
  if (!Number.isFinite(expiresAt)) return null;

  return { accessToken, expiresAtUtc, user };
}

export function readStoredAuthSession() {
  if (!storageAvailable()) return null;

  try {
    const raw = window.sessionStorage.getItem(AUTH_SESSION_KEY);
    if (!raw) return null;

    const session = normalizeSession(JSON.parse(raw));
    if (!session) {
      window.sessionStorage.removeItem(AUTH_SESSION_KEY);
      return null;
    }

    return session;
  } catch {
    window.sessionStorage.removeItem(AUTH_SESSION_KEY);
    return null;
  }
}

export function writeStoredAuthSession(session) {
  if (!storageAvailable()) return;

  const normalized = normalizeSession(session);
  if (!normalized) {
    window.sessionStorage.removeItem(AUTH_SESSION_KEY);
    return;
  }

  window.sessionStorage.setItem(AUTH_SESSION_KEY, JSON.stringify(normalized));
}

export function clearStoredAuthSession() {
  if (!storageAvailable()) return;
  window.sessionStorage.removeItem(AUTH_SESSION_KEY);
}

export function isAccessTokenUsable(session, bufferMs = DEFAULT_EXPIRY_BUFFER_MS) {
  const normalized = normalizeSession(session);
  if (!normalized) return false;

  const expiresAt = Date.parse(normalized.expiresAtUtc);
  return expiresAt - Math.max(0, bufferMs) > Date.now();
}

export { AUTH_SESSION_KEY };
