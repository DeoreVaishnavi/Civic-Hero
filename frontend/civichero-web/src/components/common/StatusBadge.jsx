const tone = {
  Created: 'neutral',
  AiTriage: 'violet',
  FraudReview: 'violet',
  PendingReview: 'violet',
  Unassigned: 'neutral',
  Assigned: 'blue',
  ReassignmentPending: 'amber',
  InProgress: 'orange',
  Resolved: 'green',
  VerificationPending: 'amber',
  Closed: 'green',
  ClosedAuto: 'green',
  Escalated: 'darkred',
  Disputed: 'red',
  Appealed: 'red',
  Rework: 'amber',
  Rejected: 'red',
  ClosedFraud: 'red',
  Withdrawn: 'neutral',
};

const readable = (value = '') => value.replace(/([a-z])([A-Z])/g, '$1 $2');

export default function StatusBadge({ status }) {
  const value = status || 'Created';
  return (
    <span className={`status-pill ${tone[value] || 'neutral'}`}>
      <span className="status-dot" aria-hidden="true" />
      {readable(value)}
    </span>
  );
}
