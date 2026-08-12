const STATUS_STYLES = {
  checking: 'border-amber-400/30 bg-amber-400/10 text-amber-200',
  connected: 'border-emerald-400/30 bg-emerald-400/10 text-emerald-200',
  unavailable: 'border-rose-400/30 bg-rose-400/10 text-rose-200',
};

const STATUS_DOTS = {
  checking: 'animate-pulse bg-amber-300',
  connected: 'bg-emerald-300',
  unavailable: 'bg-rose-300',
};

export default function ApiStatus({ status, message }) {
  return (
    <div
      className={`inline-flex items-center gap-3 rounded-full border px-4 py-2 text-sm font-semibold ${STATUS_STYLES[status]}`}
      role="status"
      aria-live="polite"
    >
      <span className={`h-2.5 w-2.5 rounded-full ${STATUS_DOTS[status]}`} />
      <span>{message}</span>
    </div>
  );
}
