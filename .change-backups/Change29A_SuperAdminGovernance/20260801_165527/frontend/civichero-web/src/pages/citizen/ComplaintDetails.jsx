import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import StatusBadge from '../../components/common/StatusBadge.jsx';
import EvidenceMedia from '../../components/complaints/EvidenceMedia.jsx';
import { complaintApi } from '../../services/complaintApi.js';

export default function ComplaintDetails() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [detail, setDetail] = useState(null);
  const [metadata, setMetadata] = useState({ categories: [], departments: [], wards: [] });
  const [editing, setEditing] = useState(false);
  const [form, setForm] = useState(null);
  const [busyEvidenceId, setBusyEvidenceId] = useState(null);
  const [followStatus, setFollowStatus] = useState({ isFollowing: false, followerCount: 0 });
  const [followBusy, setFollowBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');

  const wards = useMemo(
    () => metadata.wards.filter((ward) => String(ward.departmentId) === String(form?.departmentId)),
    [metadata.wards, form?.departmentId],
  );

  const load = async () => {
    try {
      const response = await complaintApi.getById(id);
      setDetail(response);
      const item = response.complaint;
      setForm({
        title: item.title,
        description: item.description,
        category: item.category,
        departmentId: item.departmentId,
        wardId: item.wardId,
        latitude: item.latitude,
        longitude: item.longitude,
        address: item.address,
      });
      setError('');
    } catch (reason) {
      setError(reason.message || 'Complaint could not be loaded.');
    }
  };

  useEffect(() => {
    load();
    complaintApi.metadata().then(setMetadata).catch(() => {});
    complaintApi.followStatus(id).then(setFollowStatus).catch(() => {});
  }, [id]);

  const item = detail?.complaint;

  const save = async (event) => {
    event.preventDefault();
    setError('');
    try {
      const response = await complaintApi.update(id, form);
      setDetail(response);
      setEditing(false);
      setMessage('Complaint updated.');
    } catch (reason) {
      setError(reason.errors?.join(' ') || reason.message);
    }
  };

  const deleteComplaint = async () => {
    if (!window.confirm('Delete this complaint before assignment? The audit record will remain, but it will disappear from your active complaints.')) return;
    setError('');
    try {
      await complaintApi.deleteBeforeAssignment(id);
      navigate('/citizen/complaints', { replace: true });
    } catch (reason) {
      setError(reason.errors?.join(' ') || reason.message);
    }
  };

  const removeEvidence = async (evidence) => {
    if (!window.confirm(`Remove ${evidence.fileName}? This is allowed only before official review or assignment.`)) return;
    setBusyEvidenceId(evidence.id);
    setError('');
    try {
      const response = await complaintApi.removeEvidence(id, evidence.id);
      setDetail(response);
      setMessage(`${evidence.fileName} was removed from the complaint.`);
    } catch (reason) {
      setError(reason.errors?.join(' ') || reason.message || 'Evidence could not be removed.');
    } finally {
      setBusyEvidenceId(null);
    }
  };

  const toggleUpvote = async () => {
    try {
      if (item.hasUpvoted) await complaintApi.removeUpvote(id);
      else await complaintApi.upvote(id);
      await load();
    } catch (reason) {
      setError(reason.message);
    }
  };


  const toggleFollow = async () => {
    setFollowBusy(true);
    setError('');
    try {
      const response = followStatus.isFollowing
        ? await complaintApi.unfollow(id)
        : await complaintApi.follow(id);
      setFollowStatus(response);
      setMessage(response.isFollowing
        ? 'You are now following this complaint and will receive update notifications.'
        : 'This complaint was removed from your following feed.');
    } catch (reason) {
      setError(reason.message || 'Follow status could not be updated.');
    } finally {
      setFollowBusy(false);
    }
  };

  const download = async (evidence) => {
    try {
      const response = await complaintApi.downloadImage(id, evidence.id);
      const url = URL.createObjectURL(response.data);
      const link = document.createElement('a');
      link.href = url;
      link.download = evidence.fileName;
      link.click();
      URL.revokeObjectURL(url);
    } catch (reason) {
      setError(reason.message || 'Evidence could not be downloaded.');
    }
  };

  if (!detail && !error) return <section className="page-wrap"><div className="surface"><div className="surface-body">Loading complaint…</div></div></section>;
  if (!detail) return <section className="page-wrap"><div className="alert error">{error}</div></section>;

  return (
    <section className="page-wrap narrow">
      <div className="page-title-row">
        <div><p className="section-kicker">Complaint details</p><h2>{item.title}</h2><p>{item.referenceNumber} · {item.category} · {item.wardName}</p></div>
        <div className="page-actions"><StatusBadge status={item.status} /><button type="button" onClick={() => navigate(-1)} className="button outline">← Back</button></div>
      </div>
      {message && <div className="alert success">{message}</div>}
      {error && <div className="alert error">{error}</div>}

      <section className="surface">
        <div className="surface-header"><h3>Complaint evidence</h3><span className="muted" style={{ fontSize: 10 }}>Images, videos and supporting documents</span></div>
        <div className="surface-body">
          {detail.images.length ? (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {detail.images.map((evidence) => {
                const canRemove = item.isOwner && item.canEdit && !evidence.isResolutionEvidence;
                return (
                  <article key={evidence.id} className="rounded-2xl border border-slate-200 bg-white p-2 shadow-sm">
                    <EvidenceMedia image={evidence} />
                    <div className="px-2 py-2 text-xs text-slate-500"><span className="block truncate">{evidence.fileName}</span><span>{formatBytes(evidence.fileSize)} · {evidence.mimeType}</span></div>
                    <div className="flex items-center justify-end gap-3 px-2 pb-2 text-xs">
                      <button type="button" onClick={() => download(evidence)} className="font-semibold text-blue-700">Download</button>
                      {canRemove && <button type="button" disabled={busyEvidenceId === evidence.id} onClick={() => removeEvidence(evidence)} className="font-semibold text-red-700 disabled:opacity-50">{busyEvidenceId === evidence.id ? 'Removing…' : 'Remove'}</button>}
                    </div>
                  </article>
                );
              })}
            </div>
          ) : <div className="upload-zone"><strong>No evidence available</strong><p>Uploaded images, videos and documents will appear here.</p></div>}
        </div>
      </section>

      <section className="surface section-gap">
        <div className="surface-header"><h3>Complaint information</h3><div className="page-actions">{item.canEdit && <button type="button" onClick={() => setEditing((value) => !value)} className="button outline small">{editing ? 'Cancel editing' : 'Edit complaint'}</button>}{item.canWithdraw && <button type="button" onClick={deleteComplaint} className="button danger small">Delete complaint</button>}</div></div>
        <div className="surface-body">
          <div className="stat-grid" style={{ gridTemplateColumns: 'repeat(4,1fr)' }}><Info label="Category" value={item.category} /><Info label="Priority" value={item.priority} /><Info label="Department" value={item.departmentName} /><Info label="Ward" value={item.wardName} /></div>
          <p style={{ margin: '18px 0 0', color: '#3d4c61', fontSize: 12, lineHeight: 1.8, whiteSpace: 'pre-wrap' }}>{item.description}</p>
        </div>
      </section>

      {editing && (
        <form onSubmit={save} className="surface form-section section-gap">
          <div className="surface-header" style={{ margin: '-19px -19px 19px' }}><h3>Edit complaint</h3></div>
          <label className="form-label">Title<input className="input" value={form.title} onChange={(event) => setForm({ ...form, title: event.target.value })} required /></label>
          <label className="form-label" style={{ marginTop: 13 }}>Description<textarea className="input" style={{ minHeight: 120 }} value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} required /></label>
          <div className="form-grid three" style={{ marginTop: 13 }}>
            <label className="form-label">Category<select className="input" value={form.category} onChange={(event) => setForm({ ...form, category: event.target.value })}>{metadata.categories.map((value) => <option key={value}>{value}</option>)}</select></label>
            <label className="form-label">Department<select className="input" value={form.departmentId} onChange={(event) => setForm({ ...form, departmentId: event.target.value, wardId: '' })}>{metadata.departments.map((value) => <option key={value.id} value={value.id}>{value.name}</option>)}</select></label>
            <label className="form-label">Ward<select className="input" value={form.wardId} onChange={(event) => setForm({ ...form, wardId: event.target.value })}>{wards.map((value) => <option key={value.id} value={value.id}>{value.name}</option>)}</select></label>
          </div>
          <button className="button primary" style={{ marginTop: 15 }}>Save changes</button>
        </form>
      )}

      <div className="dashboard-grid main-aside section-gap">
        <section className="surface"><div className="surface-header"><h3>Location</h3></div><div className="surface-body"><div className="map-placeholder"><span className="map-pin-dot p2" /><div className="map-overlay-card"><strong>{item.address}</strong><p>{item.latitude}, {item.longitude}</p></div></div></div></section>
        <section className="surface"><div className="surface-header"><h3>Community engagement</h3></div><div className="surface-body"><div className="stat-grid" style={{ gridTemplateColumns: 'repeat(2,1fr)' }}><Info label="Supports" value={item.upvoteCount || 0} /><Info label="Followers" value={followStatus.followerCount || 0} /></div><p className="muted" style={{ marginTop: 10, fontSize: 10 }}>Supporting raises community priority. Following only subscribes you to future updates.</p><button type="button" onClick={toggleUpvote} className={`button ${item.hasUpvoted ? 'success' : 'primary'} full`}>{item.hasUpvoted ? '✓ You supported this issue' : '👍 Support this issue'}</button><button type="button" disabled={followBusy} onClick={toggleFollow} className={`button ${followStatus.isFollowing ? 'success' : 'outline'} full`} style={{ marginTop: 8 }}>{followBusy ? 'Updating…' : followStatus.isFollowing ? '✓ Following updates' : '☆ Follow for updates'}</button></div></section>
      </div>

      <section className="surface section-gap">
        <div className="surface-header"><h3>Complaint timeline</h3></div>
        <div className="surface-body timeline-list">{detail.timeline.map((event) => <div key={event.id} className="timeline-item complete"><span className="timeline-dot" /><h4>{readable(event.eventType)} <small style={{ color: 'var(--civic-muted)', fontWeight: 500 }}>· {new Date(event.timestamp).toLocaleString()}</small></h4><p>{event.description} · By {event.actorName}</p></div>)}</div>
      </section>
    </section>
  );
}

function Info({ label, value }) {
  return <article className="stat-card" style={{ minHeight: 82 }}><small style={{ marginTop: 0 }}>{label}</small><strong style={{ fontSize: 15, marginTop: 8 }}>{value || '—'}</strong></article>;
}

function readable(value = '') {
  return String(value).replace(/_/g, ' ').replace(/([a-z])([A-Z])/g, '$1 $2');
}

function formatBytes(value) {
  const bytes = Number(value || 0);
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
