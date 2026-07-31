import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import SlaBadge from '../../components/common/SlaBadge.jsx';
import StatusBadge from '../../components/common/StatusBadge.jsx';
import { assignmentApi } from '../../services/assignmentApi.js';

const asArray = (value) => (Array.isArray(value) ? value : []);

export default function AssignmentDetails() {
  const { complaintId } = useParams();
  const numericComplaintId = Number(complaintId);
  const validComplaintId = Number.isSafeInteger(numericComplaintId) && numericComplaintId > 0;

  const [item, setItem] = useState(null);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState('');
  const [progress, setProgress] = useState(25);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    if (!validComplaintId) {
      setItem(null);
      setLoading(false);
      setError('This assignment link does not contain a valid complaint ID.');
      return null;
    }

    setLoading(true);
    setError('');
    try {
      const result = await assignmentApi.getByComplaintId(numericComplaintId);
      if (!result || typeof result !== 'object') {
        throw new Error('The assignment API returned an empty response.');
      }
      setItem(result);
      return result;
    } catch (reason) {
      setItem(null);
      setError(reason?.message || 'Unable to load this assignment.');
      return null;
    } finally {
      setLoading(false);
    }
  }, [numericComplaintId, validComplaintId]);

  useEffect(() => { load(); }, [load]);

  const progressUpdates = useMemo(() => asArray(item?.progressUpdates), [item]);

  const run = async (action) => {
    setBusy(true);
    setError('');
    try {
      await action();
      await load();
    } catch (reason) {
      setError(reason?.errors?.join?.(' ') || reason?.message || 'The assignment action failed.');
    } finally {
      setBusy(false);
    }
  };

  const reject = () => {
    const reason = window.prompt('Reason for rejecting this assignment:');
    if (reason?.trim()) run(() => assignmentApi.reject(numericComplaintId, reason.trim()));
  };

  const addProgress = () => run(async () => {
    await assignmentApi.progress(numericComplaintId, {
      message: message.trim(),
      progressPercent: Number(progress),
    });
    setMessage('');
  });

  if (loading) {
    return <section className="page-wrap"><div className="surface"><div className="surface-body">Loading assignment details…</div></div></section>;
  }

  if (!item) {
    return (
      <section className="page-wrap">
        <div className="surface">
          <div className="surface-header"><div><p className="section-kicker">Officer assignment</p><h2>Assignment could not be opened</h2></div></div>
          <div className="surface-body">
            <div className="alert error" role="alert">{error || 'The assignment was not found.'}</div>
            <p className="muted">It may have been reassigned, completed, or removed from your work queue.</p>
            <div className="page-actions">
              <Link to="/officer/assignments" className="button primary">Open work queue</Link>
              <Link to="/officer" className="button outline">Back to dashboard</Link>
              <button type="button" className="button ghost" onClick={load}>Retry</button>
            </div>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="page-wrap">
      <div className="page-actions" style={{ marginBottom: 16 }}>
        <Link to="/officer/assignments" className="button outline">← Back to work queue</Link>
        <Link to="/officer" className="button ghost">Officer dashboard</Link>
      </div>

      <div className="dashboard-grid main-aside">
        <div className="surface">
          <div className="surface-header">
            <div>
              <p className="section-kicker">{item.referenceNumber || `Complaint #${numericComplaintId}`}</p>
              <h2>{item.title || 'Untitled complaint'}</h2>
            </div>
            <SlaBadge state={item.slaState || 'Unknown'} remainingMinutes={item.remainingMinutes ?? 0} />
          </div>
          <div className="surface-body">
            <div className="page-actions" style={{ marginTop: 0 }}>
              <StatusBadge status={item.complaintStatus || 'Unknown'} />
              <span className="status-pill amber">{item.priority || '—'}</span>
            </div>
            <p style={{ lineHeight: 1.75 }}>{item.description || 'No complaint description is available.'}</p>
            <div className="form-grid">
              <Info label="Citizen" value={item.citizenName} />
              <Info label="Category" value={item.category} />
              <Info label="Department" value={item.departmentName} />
              <Info label="Ward" value={item.wardName} />
              <Info label="Address" value={item.address} />
              <Info label="Assignment state" value={item.assignmentStatus} />
            </div>

            <div className="page-actions">
              {item.canAccept && <button disabled={busy} onClick={() => run(() => assignmentApi.accept(numericComplaintId))} className="button success">Accept assignment</button>}
              {item.canReject && <button disabled={busy} onClick={reject} className="button danger">Reject</button>}
              {item.canComplete && <Link to={`/officer/assignments/${numericComplaintId}/resolve`} className="button primary">Submit resolution</Link>}
            </div>

            {error && <div className="alert error section-gap" role="alert">{error}</div>}
          </div>
        </div>

        <aside style={{ display: 'grid', gap: 18 }}>
          <section className="surface">
            <div className="surface-header"><h3>Add progress</h3></div>
            <div className="surface-body">
              <p className="muted">Available after accepting the assignment.</p>
              <textarea className="input" style={{ minHeight: 112 }} value={message} onChange={(event) => setMessage(event.target.value)} placeholder="Site inspection, crew status, materials…" disabled={!item.canAddProgress} />
              <label className="form-label" style={{ marginTop: 16 }}><span>Progress: {progress}%</span><input type="range" min="1" max="99" value={progress} onChange={(event) => setProgress(event.target.value)} disabled={!item.canAddProgress} /></label>
              <button type="button" disabled={!item.canAddProgress || !message.trim() || busy} onClick={addProgress} className="button primary" style={{ width: '100%' }}>Save progress</button>
            </div>
          </section>

          <section className="surface">
            <div className="surface-header"><h3>Activity</h3><span className="status-pill">{progressUpdates.length}</span></div>
            <div className="surface-body">
              {progressUpdates.length ? progressUpdates.map((update, index) => (
                <div key={update?.id || `${update?.createdAt || 'progress'}-${index}`} style={{ borderLeft: '2px solid var(--civic-blue)', paddingLeft: 14, marginBottom: 16 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
                    <strong>{update?.progressPercent ?? '—'}% · {update?.officerName || 'Officer'}</strong>
                    <time className="muted">{update?.createdAt ? new Date(update.createdAt).toLocaleString() : '—'}</time>
                  </div>
                  <p className="muted">{update?.message || 'No update message.'}</p>
                </div>
              )) : <p className="muted">No field updates yet.</p>}
            </div>
          </section>
        </aside>
      </div>
    </section>
  );
}

function Info({ label, value }) {
  return <div><p className="section-kicker">{label}</p><p>{value || '—'}</p></div>;
}
