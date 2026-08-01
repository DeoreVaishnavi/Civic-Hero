import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { commentApi } from '../../services/commentApi.js';

const INTERNAL_COMMENT_ROLES = new Set(['officer', 'supervisor', 'admin', 'superadmin']);
const REPORT_CATEGORIES = ['Unsafe', 'Abusive', 'Harassment', 'Spam', 'PersonalInformation', 'Misinformation', 'Other'];

export default function ComplaintComments({ complaintId }) {
  const { user } = useAuth();
  const canCreateInternalComment = useMemo(
    () => INTERNAL_COMMENT_ROLES.has((user?.role || '').toLowerCase()),
    [user?.role],
  );

  const [comments, setComments] = useState([]);
  const [body, setBody] = useState('');
  const [visibility, setVisibility] = useState('Public');
  const [editingId, setEditingId] = useState(null);
  const [editBody, setEditBody] = useState('');
  const [reportingId, setReportingId] = useState(null);
  const [reportCategory, setReportCategory] = useState('Unsafe');
  const [reportReason, setReportReason] = useState('');
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const load = async () => {
    try {
      setComments(await commentApi.list(complaintId));
      setError('');
    } catch (requestError) {
      setError(requestError.message);
    }
  };

  useEffect(() => {
    load();
  }, [complaintId]);

  useEffect(() => {
    if (!canCreateInternalComment && visibility === 'Internal') {
      setVisibility('Public');
    }
  }, [canCreateInternalComment, visibility]);

  const add = async (event) => {
    event.preventDefault();
    setBusy(true);
    setError('');
    setMessage('');

    try {
      await commentApi.add(complaintId, {
        body,
        visibility: canCreateInternalComment ? visibility : 'Public',
      });
      setBody('');
      setMessage('Comment posted successfully.');
      await load();
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message);
    } finally {
      setBusy(false);
    }
  };

  const beginEdit = (comment) => {
    setReportingId(null);
    setEditingId(comment.id);
    setEditBody(comment.body);
    setError('');
    setMessage('');
  };

  const saveEdit = async (commentId) => {
    setBusy(true);
    setError('');
    setMessage('');
    try {
      await commentApi.update(complaintId, commentId, { body: editBody });
      setEditingId(null);
      setEditBody('');
      setMessage('Comment updated successfully.');
      await load();
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message);
    } finally {
      setBusy(false);
    }
  };

  const beginReport = (commentId) => {
    setEditingId(null);
    setReportingId(commentId);
    setReportCategory('Unsafe');
    setReportReason('');
    setError('');
    setMessage('');
  };

  const submitReport = async (commentId) => {
    setBusy(true);
    setError('');
    setMessage('');
    try {
      const result = await commentApi.report(complaintId, commentId, {
        category: reportCategory,
        reason: reportReason,
      });
      setReportingId(null);
      setReportReason('');
      setMessage(result?.automaticallyHidden
        ? 'Report submitted. The comment was hidden automatically pending moderator review.'
        : 'Report submitted to the moderation team.');
      await load();
    } catch (requestError) {
      setError(requestError.errors?.join(' ') || requestError.message);
    } finally {
      setBusy(false);
    }
  };

  const remove = async (id) => {
    if (!window.confirm('Delete this comment?')) return;

    try {
      setError('');
      setMessage('');
      await commentApi.remove(complaintId, id);
      setMessage('Comment deleted.');
      await load();
    } catch (requestError) {
      setError(requestError.message);
    }
  };

  const moderate = async (id, decision) => {
    const reason = window.prompt(`Reason for ${decision.toLowerCase()} decision:`) || '';
    if (!reason.trim()) return;

    try {
      await commentApi.moderate(complaintId, id, { decision, reason: reason.trim() });
      setMessage(`Comment moderation changed to ${decision}.`);
      await load();
    } catch (requestError) {
      setError(requestError.message);
    }
  };

  return (
    <section className="mt-8 rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <h3 className="text-xl font-black text-white">Discussion and official updates</h3>
      <p className="mt-1 text-sm text-slate-400">
        Public comments can be edited by their authors and reported for safety review. Internal comments remain visible only to operational roles.
      </p>

      {message && <p className="mt-4 rounded-lg bg-emerald-500/10 p-3 text-sm text-emerald-100">{message}</p>}
      {error && <p className="mt-4 rounded-lg bg-rose-500/10 p-3 text-sm text-rose-100">{error}</p>}

      <form onSubmit={add} className="mt-5 space-y-3">
        <textarea
          className="input min-h-24"
          value={body}
          onChange={(event) => setBody(event.target.value)}
          minLength="2"
          maxLength="1500"
          placeholder="Add a constructive comment or official update"
          required
        />

        <div className="flex flex-wrap gap-3">
          {canCreateInternalComment && (
            <select className="input max-w-xs" value={visibility} onChange={(event) => setVisibility(event.target.value)}>
              <option value="Public">Public</option>
              <option value="Internal">Internal (authorized roles only)</option>
            </select>
          )}

          <button disabled={busy} className="rounded-xl bg-sky-500 px-5 py-2.5 font-black text-white disabled:opacity-50">
            {busy ? 'Posting…' : 'Post comment'}
          </button>
        </div>
      </form>

      <div className="mt-6 space-y-4">
        {comments.length === 0 && <p className="text-slate-500">No comments yet.</p>}

        {comments.map((comment) => (
          <article key={comment.id} className="rounded-2xl border border-white/10 bg-slate-950/40 p-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <strong className="text-white">{comment.authorName}</strong>
                <span className="ml-2 rounded-full bg-slate-800 px-2 py-1 text-xs text-slate-300">{comment.authorRole}</span>
                <span className="ml-2 text-xs text-slate-500">{comment.visibility}</span>
              </div>
              <time className="text-xs text-slate-500">
                {new Date(comment.createdAt).toLocaleString()}
                {comment.updatedAt && comment.updatedAt !== comment.createdAt ? ' · edited' : ''}
              </time>
            </div>

            {editingId === comment.id ? (
              <div className="mt-3 space-y-3">
                <textarea className="input min-h-24" minLength="2" maxLength="1500" value={editBody} onChange={(event) => setEditBody(event.target.value)} />
                <div className="flex gap-2">
                  <button type="button" disabled={busy} onClick={() => saveEdit(comment.id)} className="rounded-lg bg-sky-500 px-4 py-2 text-xs font-black text-white disabled:opacity-50">Save edit</button>
                  <button type="button" onClick={() => setEditingId(null)} className="rounded-lg border border-white/15 px-4 py-2 text-xs font-black text-slate-300">Cancel</button>
                </div>
              </div>
            ) : (
              <p className="mt-3 whitespace-pre-wrap text-slate-300">{comment.body}</p>
            )}

            {reportingId === comment.id && (
              <div className="mt-4 rounded-xl border border-amber-400/20 bg-amber-400/5 p-4">
                <p className="text-sm font-bold text-amber-100">Report unsafe public comment</p>
                <div className="mt-3 grid gap-3 sm:grid-cols-[220px_1fr]">
                  <select className="input" value={reportCategory} onChange={(event) => setReportCategory(event.target.value)}>
                    {REPORT_CATEGORIES.map((category) => <option key={category} value={category}>{category}</option>)}
                  </select>
                  <textarea className="input min-h-20" minLength="5" maxLength="500" value={reportReason} onChange={(event) => setReportReason(event.target.value)} placeholder="Explain why this comment may be unsafe or inappropriate" />
                </div>
                <div className="mt-3 flex gap-2">
                  <button type="button" disabled={busy || reportReason.trim().length < 5} onClick={() => submitReport(comment.id)} className="rounded-lg bg-amber-500 px-4 py-2 text-xs font-black text-slate-950 disabled:opacity-50">Submit report</button>
                  <button type="button" onClick={() => setReportingId(null)} className="rounded-lg border border-white/15 px-4 py-2 text-xs font-black text-slate-300">Cancel</button>
                </div>
              </div>
            )}

            <div className="mt-3 flex flex-wrap items-center gap-3 text-xs">
              {comment.moderationStatus !== 'Visible' && <span className="rounded bg-amber-400/10 px-2 py-1 text-amber-200">{comment.moderationStatus}</span>}
              {comment.reportCount > 0 && comment.canModerate && <span className="rounded bg-rose-400/10 px-2 py-1 text-rose-200">{comment.reportCount} safety report(s)</span>}
              {comment.isReportedByCurrentUser && <span className="rounded bg-amber-400/10 px-2 py-1 text-amber-200">Reported by you</span>}
              {comment.canEdit && editingId !== comment.id && <button type="button" onClick={() => beginEdit(comment)} className="font-bold text-sky-300">Edit</button>}
              {comment.canReport && reportingId !== comment.id && <button type="button" onClick={() => beginReport(comment.id)} className="font-bold text-amber-300">Report unsafe</button>}
              {comment.canDelete && <button type="button" onClick={() => remove(comment.id)} className="font-bold text-rose-300">Delete</button>}
              {comment.canModerate && (
                <>
                  <button type="button" onClick={() => moderate(comment.id, 'Visible')} className="font-bold text-emerald-300">Approve</button>
                  <button type="button" onClick={() => moderate(comment.id, 'Hidden')} className="font-bold text-amber-300">Hide</button>
                  <button type="button" onClick={() => moderate(comment.id, 'Removed')} className="font-bold text-rose-300">Remove</button>
                </>
              )}
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
