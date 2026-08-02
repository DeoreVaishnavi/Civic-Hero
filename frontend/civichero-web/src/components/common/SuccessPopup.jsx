import CivicIcon from '../ui/CivicIcon.jsx';

export default function SuccessPopup({ message, onClose }) {
  if (!message) return null;
  return (
    <div className="alert success" role="status" aria-live="polite" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 15 }}>
      <span style={{ display: 'inline-flex', alignItems: 'center', gap: 9 }}><CivicIcon name="verify" size={19} /><span><strong>Completed:</strong> {message}</span></span>
      <button type="button" onClick={onClose} className="icon-button" style={{ width: 32, height: 32 }} aria-label="Dismiss message"><CivicIcon name="close" size={17} /></button>
    </div>
  );
}
