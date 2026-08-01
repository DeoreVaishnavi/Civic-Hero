import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import SlaBadge from '../../components/common/SlaBadge.jsx';
import StatusBadge from '../../components/common/StatusBadge.jsx';
import { assignmentApi } from '../../services/assignmentApi.js';

const asArray = (value) => (Array.isArray(value) ? value : []);
const transferReasons = [
  ['WrongDepartment', 'Wrong department'],
  ['OutsideScope', 'Outside my work scope'],
  ['WorkloadCapacity', 'Workload or capacity'],
  ['SafetyConcern', 'Safety concern'],
  ['SpecialistRequired', 'Specialist required'],
  ['Other', 'Other'],
];

export default function AssignmentDetails() {
  const { complaintId } = useParams();
  const numericComplaintId = Number(complaintId);
  const validComplaintId = Number.isSafeInteger(numericComplaintId) && numericComplaintId > 0;

  const [item, setItem] = useState(null);
  const [loading, setLoading] = useState(true);
  const [message, setMessage] = useState('');
  const [progress, setProgress] = useState(25);
  const [estimatedCompletionAt, setEstimatedCompletionAt] = useState('');
  const [progressEvidence, setProgressEvidence] = useState([]);
  const [progressLocation, setProgressLocation] = useState({ latitude: '', longitude: '' });
  const [transferReasonCode, setTransferReasonCode] = useState('WrongDepartment');
  const [transferDetails, setTransferDetails] = useState('');
  const [citizenRequest, setCitizenRequest] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
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
      if (!result || typeof result !== 'object') throw new Error('The assignment API returned an empty response.');
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
  const evidenceByUpdate = useMemo(() => {
    const grouped = new Map();
    asArray(item?.progressEvidence).forEach((file) => {
      const key = Number(file?.progressUpdateId || 0);
      if (!grouped.has(key)) grouped.set(key, []);
      grouped.get(key).push(file);
    });
    return grouped;
  }, [item]);

  const run = async (action, successMessage = '') => {
    setBusy(true);
    setError('');
    setSuccess('');
    try {
      await action();
      await load();
      if (successMessage) setSuccess(successMessage);
    } catch (reason) {
      setError(reason?.errors?.join?.(' ') || reason?.message || 'The assignment action failed.');
    } finally {
      setBusy(false);
    }
  };

  const reject = () => {
    const reason = window.prompt('Reason for rejecting this pending assignment:');
    if (reason?.trim()) run(() => assignmentApi.reject(numericComplaintId, reason.trim()), 'Assignment returned to the Supervisor queue.');
  };

  const captureProgressLocation = () => navigator.geolocation?.getCurrentPosition(
    (position) => setProgressLocation({ latitude: position.coords.latitude, longitude: position.coords.longitude }),
    () => setError('Location could not be captured. You may submit progress without GPS.'),
  );

  const addProgress = () => run(async () => {
    const request = {
      message: message.trim(),
      progressPercent: Number(progress),
      estimatedCompletionAt: estimatedCompletionAt ? new Date(estimatedCompletionAt).toISOString() : null,
      evidence: progressEvidence,
      ...progressLocation,
    };
    if (progressEvidence.length) await assignmentApi.progressWithEvidence(numericComplaintId, request);
    else await assignmentApi.progress(numericComplaintId, request);
    setMessage('');
    setEstimatedCompletionAt('');
    setProgressEvidence([]);
    setProgressLocation({ latitude: '', longitude: '' });
  }, 'Progress update saved.');

  const requestTransfer = () => run(async () => {
    await assignmentApi.requestTransfer(numericComplaintId, {
      reasonCode: transferReasonCode,
      details: transferDetails.trim(),
    });
    setTransferDetails('');
  }, 'Transfer request sent to the Supervisor queue.');

  const requestInformation = () => run(async () => {
    await assignmentApi.requestCitizenInformation(numericComplaintId, citizenRequest.trim());
    setCitizenRequest('');
  }, 'Private information request sent to the Citizen.');

  if (loading) return <section className="page-wrap"><div className="surface"><div className="surface-body">Loading assignment details…</div></div></section>;

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
        <Link to="/officer/map" className="button ghost">Open work map</Link>
        <Link to="/officer" className="button ghost">Officer dashboard</Link>
      </div>

      {error && <div className="alert error" role="alert">{error}</div>}
      {success && <div className="alert success" role="status">{success}</div>}

      <div className="dashboard-grid main-aside section-gap">
        <div style={{ display: 'grid', gap: 18 }}>
          <section className="surface">
            <div className="surface-header">
              <div><p className="section-kicker">{item.referenceNumber || `Complaint #${numericComplaintId}`}</p><h2>{item.title || 'Untitled complaint'}</h2></div>
              <SlaBadge state={item.slaState || 'Unknown'} remainingMinutes={item.remainingMinutes ?? 0} />
            </div>
            <div className="surface-body">
              <div className="page-actions" style={{ marginTop: 0 }}><StatusBadge status={item.complaintStatus || 'Unknown'} /><span className="status-pill amber">{item.priority || '—'}</span></div>
              <p style={{ lineHeight: 1.75 }}>{item.description || 'No complaint description is available.'}</p>
              <div className="form-grid">
                <Info label="Citizen" value={item.citizenName} />
                <Info label="Category" value={item.category} />
                <Info label="Department" value={item.departmentName} />
                <Info label="Ward" value={item.wardName} />
                <Info label="Address" value={item.address} />
                <Info label="Assignment state" value={item.assignmentStatus} />
                <Info label="Estimated completion" value={item.estimatedCompletionAt ? new Date(item.estimatedCompletionAt).toLocaleString() : 'Not set'} />
              </div>
              <div className="page-actions">
                {item.canAccept && <button disabled={busy} onClick={() => run(() => assignmentApi.accept(numericComplaintId), 'Assignment accepted.')} className="button success">Accept assignment</button>}
                {item.canReject && <button disabled={busy} onClick={reject} className="button danger">Reject</button>}
                {item.canComplete && <Link to={`/officer/assignments/${numericComplaintId}/resolve`} className="button primary">Submit resolution</Link>}
              </div>
            </div>
          </section>

          <section className="surface">
            <div className="surface-header"><h3>Progress history and evidence</h3><span className="status-pill">{progressUpdates.length}</span></div>
            <div className="surface-body">
              {progressUpdates.length ? progressUpdates.map((update, index) => {
                const attachments = evidenceByUpdate.get(Number(update?.id || 0)) || [];
                return (
                  <div key={update?.id || `${update?.createdAt || 'progress'}-${index}`} style={{ borderLeft: '2px solid var(--civic-blue)', paddingLeft: 14, marginBottom: 20 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, flexWrap: 'wrap' }}>
                      <strong>{update?.progressPercent ?? '—'}% · {update?.officerName || 'Officer'}</strong>
                      <time className="muted">{update?.createdAt ? new Date(update.createdAt).toLocaleString() : '—'}</time>
                    </div>
                    <p className="muted">{update?.message || 'No update message.'}</p>
                    {attachments.length > 0 && <div className="page-actions">{attachments.map((file) => <button key={file.id} type="button" className="button outline small" onClick={() => assignmentApi.downloadEvidence(file.downloadPath, file.fileName)}>{file.evidenceType}: {file.fileName}</button>)}</div>}
                  </div>
                );
              }) : <p className="muted">No field updates yet.</p>}
            </div>
          </section>
        </div>

        <aside style={{ display: 'grid', gap: 18 }}>
          <section className="surface">
            <div className="surface-header"><h3>Add progress</h3></div>
            <div className="surface-body">
              <p className="muted">Add field notes, ETA, GPS and up to five evidence files.</p>
              <textarea className="input" style={{ minHeight: 105 }} value={message} onChange={(event) => setMessage(event.target.value)} placeholder="Site inspection, crew status, materials…" disabled={!item.canAddProgress} />
              <label className="form-label" style={{ marginTop: 14 }}><span>Progress: {progress}%</span><input type="range" min="1" max="99" value={progress} onChange={(event) => setProgress(event.target.value)} disabled={!item.canAddProgress} /></label>
              <label className="form-label">Estimated completion<input className="input" type="datetime-local" value={estimatedCompletionAt} onChange={(event) => setEstimatedCompletionAt(event.target.value)} disabled={!item.canAddProgress} /></label>
              <label className="form-label">Progress evidence<input className="input" type="file" multiple accept="image/jpeg,image/png,image/webp,video/mp4,video/webm,video/quicktime,application/pdf,.doc,.docx" onChange={(event) => setProgressEvidence(Array.from(event.target.files || []).slice(0, 5))} disabled={!item.canAddProgress} /></label>
              {progressEvidence.length > 0 && <p className="muted">{progressEvidence.map((file) => file.name).join(', ')}</p>}
              <button type="button" className="button outline full" onClick={captureProgressLocation} disabled={!item.canAddProgress}>⌖ Capture progress location</button>
              {progressLocation.latitude && <div className="alert success">GPS captured: {Number(progressLocation.latitude).toFixed(6)}, {Number(progressLocation.longitude).toFixed(6)}</div>}
              <button type="button" disabled={!item.canAddProgress || message.trim().length < 5 || busy} onClick={addProgress} className="button primary full">Save progress</button>
            </div>
          </section>

          <section className="surface">
            <div className="surface-header"><h3>Request transfer</h3></div>
            <div className="surface-body">
              <p className="muted">Use this for wrong routing, scope, safety, capacity or specialist needs.</p>
              <label className="form-label">Transfer reason<select className="input" value={transferReasonCode} onChange={(event) => setTransferReasonCode(event.target.value)} disabled={!item.canRequestTransfer}>{transferReasons.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
              <label className="form-label">Details<textarea className="input" style={{ minHeight: 90 }} value={transferDetails} onChange={(event) => setTransferDetails(event.target.value)} disabled={!item.canRequestTransfer} placeholder="Explain why reassignment is required." /></label>
              <button type="button" className="button danger full" disabled={!item.canRequestTransfer || transferDetails.trim().length < 10 || busy} onClick={requestTransfer}>Submit transfer request</button>
            </div>
          </section>

          <section className="surface">
            <div className="surface-header"><h3>Request Citizen information</h3></div>
            <div className="surface-body">
              <p className="muted">This operational request is sent privately through notifications and the audit log.</p>
              <textarea className="input" style={{ minHeight: 90 }} value={citizenRequest} onChange={(event) => setCitizenRequest(event.target.value)} disabled={!item.canRequestCitizenInformation} placeholder="Ask for landmark details, access timing, documents or clarification." />
              <button type="button" className="button outline full" disabled={!item.canRequestCitizenInformation || citizenRequest.trim().length < 10 || busy} onClick={requestInformation}>Send private request</button>
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
