import { Link } from 'react-router-dom';
import StatusBadge from './StatusBadge.jsx';

export default function ComplaintCard({
  complaint,
  basePath = '/citizen/complaints',
  onDelete,
  deleting = false,
}) {
  return (
    <article className="issue-card">
      <div className="issue-card-head">
        <div>
          <span style={{ color: 'var(--civic-blue)', fontFamily: 'ui-monospace, monospace', fontSize: 8, fontWeight: 850 }}>
            {complaint.referenceNumber}
          </span>
          <h3 style={{ marginTop: 6 }}>{complaint.title}</h3>
          <p>{complaint.category} · {complaint.wardName || 'Ward not assigned'}</p>
        </div>
        <StatusBadge status={complaint.status} />
      </div>

      <div style={{ padding: '0 14px' }}>
        <p style={{ minHeight: 42, margin: 0, color: 'var(--civic-muted)', fontSize: 9, lineHeight: 1.6 }}>
          {complaint.description}
        </p>
      </div>

      <div className="issue-card-images" style={{ marginTop: 12 }}>
        <div className="issue-photo">Issue photo</div>
        <div className="issue-photo">Location</div>
        <div className="issue-photo">Evidence</div>
      </div>

      <div className="issue-card-footer">
        <span className="support-count">{complaint.upvoteCount ?? 0} supports · {complaint.imageCount ?? 0} images</span>
        <div className="page-actions">
          {onDelete && complaint.canWithdraw && (
            <button
              type="button"
              className="button danger small"
              disabled={deleting}
              onClick={() => onDelete(complaint)}
            >
              {deleting ? 'Deleting…' : 'Delete'}
            </button>
          )}
          <Link to={`${basePath}/${complaint.id}`} className="button primary small">View details</Link>
        </div>
      </div>
    </article>
  );
}
