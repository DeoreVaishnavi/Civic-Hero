export default function SuccessPopup({ message, onClose }) {
  if (!message) return null;
  return <div className="alert success" role="status" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 15 }}><span><strong>Success:</strong> {message}</span><button type="button" onClick={onClose} className="icon-button" style={{ width: 30, height: 30 }}>×</button></div>;
}
