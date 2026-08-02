export default function ApiStatus({ status, message }) {
  const safeStatus = ['checking', 'connected', 'unavailable'].includes(status) ? status : 'checking';
  return (
    <div className={`api-status ${safeStatus}`} role="status" aria-live="polite">
      <span className="api-dot" aria-hidden="true" />
      <span>{message}</span>
    </div>
  );
}
