export function mapApiError(error) {
  if (error?.status === 0) {
    return {
      title: 'Backend unavailable',
      message: 'Start the CivicHero backend at http://localhost:5180 and try again.',
      traceId: null,
    };
  }

  return {
    title: `Request failed${error?.status ? ` (${error.status})` : ''}`,
    message: error?.message || 'An unexpected frontend error occurred.',
    traceId: error?.traceId || null,
  };
}
