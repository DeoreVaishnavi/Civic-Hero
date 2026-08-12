export default function SuccessPopup({ message, onClose }) {
  if (!message) return null;
  return (
    <div className="mb-6 flex items-center justify-between gap-4 rounded-2xl border border-emerald-400/30 bg-emerald-400/10 p-4 text-emerald-100" role="status">
      <span>{message}</span>
      <button onClick={onClose} className="font-bold">×</button>
    </div>
  );
}
