const tone = {
  Created: '', AiTriage: '', Assigned: '', InProgress: 'amber', Resolved: 'violet', VerificationPending: 'amber',
  Closed: 'green', ClosedAuto: 'green', Escalated: 'red', Disputed: 'red', Withdrawn: 'red',
};
const readable = (value = '') => value.replace(/([a-z])([A-Z])/g, '$1 $2');
export default function StatusBadge({ status }) { return <span className={`status-pill ${tone[status] || ''}`}>{readable(status || 'Created')}</span>; }
