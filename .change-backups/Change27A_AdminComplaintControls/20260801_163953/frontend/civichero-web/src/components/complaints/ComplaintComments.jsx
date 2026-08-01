import { useEffect, useMemo, useState } from 'react';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { commentApi } from '../../services/commentApi.js';

const INTERNAL_COMMENT_ROLES = new Set(['officer', 'supervisor', 'admin', 'superadmin']);

export default function ComplaintComments({ complaintId }) {
  const { user } = useAuth();
  const canCreateInternalComment = useMemo(
    () => INTERNAL_COMMENT_ROLES.has((user?.role || '').toLowerCase()),
    [user?.role],
  );

  const [comments, setComments] = useState([]);
  const [body, setBody] = useState('');
  const [visibility, setVisibility] = useState('Public');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const load = async () => {
    try {
      setComments(await commentApi.list(complaintId));
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

    try {
      await commentApi.add(complaintId, {
        body,
        visibility: canCreateInternalComment ? visibility : 'Public',
      });
      setBody('');
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
      await commentApi.remove(complaintId, id);
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
      await load();
    } catch (requestError) {
      setError(requestError.message);
    }
  };

  return (
    <section className="mt-8 rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <h3 className="text-xl font-black text-white">Discussion and official updates</h3>
      <p className="mt-1 text-sm text-slate-400">
        Public comments are moderated. Internal comments are visible only to operational roles.
      </p>

      {error && (
        <p className="mt-4 rounded-lg bg-rose-500/10 p-3 text-sm text-rose-100">{error}</p>
      )}

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
            <select
              className="input max-w-xs"
              value={visibility}
              onChange={(event) => setVisibility(event.target.value)}
            >
              <option value="Public">Public</option>
              <option value="Internal">Internal (authorized roles only)</option>
            </select>
          )}

          <button
            disabled={busy}
            className="rounded-xl bg-sky-500 px-5 py-2.5 font-black text-white disabled:opacity-50"
          >
            {busy ? 'Posting…' : 'Post comment'}
          </button>
        </div>
      </form>

      <div className="mt-6 space-y-4">
        {comments.length === 0 && <p className="text-slate-500">No comments yet.</p>}

        {comments.map((comment) => (
          <article
            key={comment.id}
            className="rounded-2xl border border-white/10 bg-slate-950/40 p-4"
          >
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div>
                <strong className="text-white">{comment.authorName}</strong>
                <span className="ml-2 rounded-full bg-slate-800 px-2 py-1 text-xs text-slate-300">
                  {comment.authorRole}
                </span>
                <span className="ml-2 text-xs text-slate-500">{comment.visibility}</span>
              </div>
              <time className="text-xs text-slate-500">
                {new Date(comment.createdAt).toLocaleString()}
              </time>
            </div>

            <p className="mt-3 whitespace-pre-wrap text-slate-300">{comment.body}</p>

            <div className="mt-3 flex flex-wrap gap-2 text-xs">
              {comment.moderationStatus !== 'Visible' && (
                <span className="rounded bg-amber-400/10 px-2 py-1 text-amber-200">
                  {comment.moderationStatus}
                </span>
              )}
              {comment.canDelete && (
                <button type="button" onClick={() => remove(comment.id)} className="font-bold text-rose-300">
                  Delete
                </button>
              )}
              {comment.canModerate && (
                <>
                  <button type="button" onClick={() => moderate(comment.id, 'Visible')} className="font-bold text-emerald-300">
                    Approve
                  </button>
                  <button type="button" onClick={() => moderate(comment.id, 'Hidden')} className="font-bold text-amber-300">
                    Hide
                  </button>
                  <button type="button" onClick={() => moderate(comment.id, 'Removed')} className="font-bold text-rose-300">
                    Remove
                  </button>
                </>
              )}
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
