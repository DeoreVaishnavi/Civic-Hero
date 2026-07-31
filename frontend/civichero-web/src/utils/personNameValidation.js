const PERSON_NAME_PATTERN = /^[\p{L}\p{M}]+(?: [\p{L}\p{M}]+)*$/u;

export const PERSON_NAME_MIN_LENGTH = 2;
export const PERSON_NAME_MAX_LENGTH = 150;

export function normalizePersonName(value) {
  return String(value ?? '').trim().replace(/\s+/g, ' ');
}

export function validatePersonName(value) {
  const normalized = normalizePersonName(value);

  if (!normalized) return 'Full name is required.';
  if (normalized.length < PERSON_NAME_MIN_LENGTH) {
    return `Full name must contain at least ${PERSON_NAME_MIN_LENGTH} letters.`;
  }
  if (normalized.length > PERSON_NAME_MAX_LENGTH) {
    return `Full name cannot exceed ${PERSON_NAME_MAX_LENGTH} characters.`;
  }
  if (!PERSON_NAME_PATTERN.test(normalized)) {
    return 'Use letters and spaces only. Digits and special characters are not allowed.';
  }

  return '';
}
