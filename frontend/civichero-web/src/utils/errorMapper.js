export function mapApiError(error, fallback = 'Something went wrong.') {
  if (!error) return fallback;
  if (Array.isArray(error.errors) && error.errors.length) return error.errors.join(' ');
  if (typeof error.message === 'string' && error.message.trim()) return error.message;
  if (typeof error === 'string' && error.trim()) return error;
  return fallback;
}

export default mapApiError;
