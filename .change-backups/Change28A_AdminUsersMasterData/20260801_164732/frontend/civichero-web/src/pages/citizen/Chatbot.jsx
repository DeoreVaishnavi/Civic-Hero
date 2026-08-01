import { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { chatbotApi } from '../../services/chatbotApi.js';

const STORAGE_KEY = 'civichero.chatbot.session';
const QUICK_PROMPTS = [
  'How do I report a pothole?',
  'Show my recent complaints',
  'How does verification work?',
  'How do rewards and points work?',
];

export default function Chatbot() {
  const [sessionId, setSessionId] = useState(() => sessionStorage.getItem(STORAGE_KEY) || '');
  const [sessionTitle, setSessionTitle] = useState('New CivicHero conversation');
  const [messages, setMessages] = useState([]);
  const [sessions, setSessions] = useState([]);
  const [historySearch, setHistorySearch] = useState('');
  const [message, setMessage] = useState('');
  const [status, setStatus] = useState('Connecting');
  const [error, setError] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [historyLoading, setHistoryLoading] = useState(false);
  const bottomRef = useRef(null);

  const canSend = message.trim().length > 0 && !isSending;
  const activeSession = useMemo(() => Boolean(sessionId) && String(status).toLowerCase() === 'active', [sessionId, status]);

  const applySession = (session) => {
    sessionStorage.setItem(STORAGE_KEY, session.sessionId);
    setSessionId(session.sessionId);
    setSessionTitle(session.title || 'New CivicHero conversation');
    setMessages(session.messages || []);
    setStatus(session.status || 'Active');
  };

  const loadHistory = async (search = historySearch) => {
    setHistoryLoading(true);
    try {
      const result = await chatbotApi.listSessions({ search, take: 40 });
      setSessions(result || []);
      return result || [];
    } catch (reason) {
      setError(reason.message);
      return [];
    } finally {
      setHistoryLoading(false);
    }
  };

  useEffect(() => {
    let disposed = false;
    const initialize = async () => {
      setError('');
      try {
        const recent = await chatbotApi.listSessions({ take: 40 });
        if (disposed) return;
        setSessions(recent || []);

        let session = null;
        const storedSessionId = sessionStorage.getItem(STORAGE_KEY);
        if (storedSessionId) {
          try { session = await chatbotApi.getSession(storedSessionId); } catch { sessionStorage.removeItem(STORAGE_KEY); }
        }
        if (!session && recent?.length) {
          try { session = await chatbotApi.getSession(recent[0].sessionId); } catch { /* start below */ }
        }
        if (!session) session = await chatbotApi.startSession();
        if (!disposed) applySession(session);
      } catch (reason) {
        if (!disposed) { setError(reason.message); setStatus('Unavailable'); }
      }
    };
    initialize();
    return () => { disposed = true; };
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => loadHistory(historySearch), 300);
    return () => window.clearTimeout(timer);
  }, [historySearch]);

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages, isSending]);

  const selectSession = async (selectedId) => {
    if (!selectedId || selectedId === sessionId || isSending) return;
    setError('');
    try { applySession(await chatbotApi.getSession(selectedId)); }
    catch (reason) { setError(reason.message); }
  };

  const send = async (text = message) => {
    const clean = text.trim();
    if (!clean || isSending || !activeSession) return;

    setMessage('');
    setError('');
    setIsSending(true);
    const stamp = Date.now();
    const optimisticId = `local-user-${stamp}`;
    const streamId = `stream-assistant-${stamp}`;
    const optimistic = { id: optimisticId, sender: 'Citizen', content: clean, sentAt: new Date().toISOString() };
    const streaming = { id: streamId, sender: 'Assistant', content: '', sentAt: new Date().toISOString(), streaming: true };
    setMessages((current) => [...current, optimistic, streaming]);

    try {
      await chatbotApi.streamMessage(sessionId, clean, {
        onUser: (serverMessage) => setMessages((current) => current.map((item) => item.id === optimisticId ? serverMessage : item)),
        onMeta: (meta) => setMessages((current) => current.map((item) => item.id === streamId ? {
          ...item,
          id: meta.messageId || item.id,
          sentAt: meta.sentAt || item.sentAt,
          actionLabel: meta.actionLabel,
          actionUrl: meta.actionUrl,
        } : item)),
        onToken: (token) => setMessages((current) => current.map((item) =>
          item.id === streamId || item.streaming ? { ...item, content: `${item.content || ''}${token}` } : item)),
        onDone: (serverMessage) => setMessages((current) => current.map((item) =>
          item.id === streamId || item.streaming ? { ...serverMessage, streaming: false } : item)),
      });
      await loadHistory();
    } catch (reason) {
      setMessages((current) => current.filter((item) => item.id !== optimisticId && item.id !== streamId && !item.streaming));
      setError(reason.message);
      setMessage(clean);
    } finally {
      setIsSending(false);
    }
  };

  const startNew = async () => {
    setError('');
    setIsSending(true);
    try {
      if (sessionId && activeSession) await chatbotApi.endSession(sessionId).catch(() => undefined);
      const session = await chatbotApi.startSession();
      applySession(session);
      await loadHistory();
    } catch (reason) { setError(reason.message); }
    finally { setIsSending(false); }
  };

  const end = async () => {
    if (!sessionId || !activeSession) return;
    setError('');
    try {
      await chatbotApi.endSession(sessionId);
      setStatus('Ended');
      await loadHistory();
    } catch (reason) { setError(reason.message); }
  };

  const continueSelected = async () => {
    if (!sessionId || activeSession) return;
    setError('');
    try {
      applySession(await chatbotApi.continueSession(sessionId));
      await loadHistory();
    } catch (reason) { setError(reason.message); }
  };

  const renameSelected = async () => {
    if (!sessionId) return;
    const title = window.prompt('Enter a title for this conversation (3–80 characters):', sessionTitle);
    if (title === null) return;
    setError('');
    try {
      const session = await chatbotApi.renameSession(sessionId, title);
      setSessionTitle(session.title);
      await loadHistory();
    } catch (reason) { setError(reason.message); }
  };

  const deleteSelected = async () => {
    if (!sessionId || !window.confirm(`Permanently delete “${sessionTitle}” and all of its messages?`)) return;
    setError('');
    try {
      await chatbotApi.deleteSession(sessionId);
      sessionStorage.removeItem(STORAGE_KEY);
      const recent = await chatbotApi.listSessions({ search: historySearch, take: 40 });
      setSessions(recent || []);
      if (recent?.length) applySession(await chatbotApi.getSession(recent[0].sessionId));
      else applySession(await chatbotApi.startSession());
    } catch (reason) { setError(reason.message); }
  };

  const submitFeedback = async (item, helpful) => {
    if (!sessionId || !Number.isFinite(Number(item.id))) return;
    const comment = helpful ? null : window.prompt('Optional: tell us what was missing or incorrect.', '') ?? null;
    setError('');
    try {
      const feedback = await chatbotApi.submitFeedback(sessionId, item.id, helpful, comment);
      setMessages((current) => current.map((messageItem) => Number(messageItem.id) === Number(item.id) ? {
        ...messageItem,
        feedback: feedback.feedback,
        feedbackComment: feedback.comment,
      } : messageItem));
    } catch (reason) { setError(reason.message); }
  };

  return (
    <section className="page-wrap">
      <div className="page-title-row"><div><p className="section-kicker">Citizen self-service</p><h2>CivicHero Assistant</h2><p>Search, continue and manage your saved CivicHero conversations.</p></div><Link to="/citizen" className="button outline">← Back to dashboard</Link></div>

      <div className="mx-auto grid max-w-7xl overflow-hidden rounded-[2rem] border border-slate-200 bg-white shadow-2xl shadow-slate-900/10 xl:grid-cols-[320px_1fr]">
        <aside className="border-b border-slate-200 bg-slate-950 p-5 text-white xl:border-b-0 xl:border-r">
          <div className="flex items-center justify-between gap-3"><div><p className="text-xs font-black uppercase tracking-[.18em] text-cyan-300">History</p><h3 className="mt-1 text-xl font-black">Conversations</h3></div><button type="button" onClick={startNew} disabled={isSending} className="rounded-xl bg-blue-500 px-3 py-2 text-sm font-black hover:bg-blue-400 disabled:opacity-50">＋ New</button></div>
          <label className="mt-5 block"><span className="sr-only">Search chat history</span><input value={historySearch} onChange={(event) => setHistorySearch(event.target.value.slice(0, 100))} placeholder="Search messages or titles…" className="w-full rounded-xl border border-white/10 bg-white/10 px-4 py-3 text-sm text-white outline-none placeholder:text-slate-500 focus:border-cyan-400" /></label>
          <p className="mt-2 text-xs text-slate-500">{historyLoading ? 'Searching…' : `${sessions.length} recent session${sessions.length === 1 ? '' : 's'}`}</p>
          <div className="mt-4 max-h-[66vh] space-y-2 overflow-y-auto pr-1">
            {sessions.map((session) => <button key={session.sessionId} type="button" onClick={() => selectSession(session.sessionId)} className={`w-full rounded-xl border p-3 text-left transition ${session.sessionId === sessionId ? 'border-cyan-400 bg-cyan-400/10' : 'border-white/10 bg-white/[0.04] hover:bg-white/[0.08]'}`}>
              <div className="flex items-start justify-between gap-2"><strong className="line-clamp-1 text-sm text-white">{session.title}</strong><span className={`shrink-0 rounded-full px-2 py-1 text-[10px] font-black ${String(session.status).toLowerCase() === 'active' ? 'bg-emerald-400/15 text-emerald-300' : 'bg-slate-700 text-slate-300'}`}>{session.status}</span></div>
              <p className="mt-2 line-clamp-2 text-xs leading-5 text-slate-400">{session.preview}</p>
              <div className="mt-2 flex justify-between text-[10px] text-slate-500"><span>{session.messageCount} messages</span><time>{new Date(session.lastMessageAt).toLocaleDateString()}</time></div>
            </button>)}
            {!historyLoading && sessions.length === 0 && <p className="rounded-xl border border-dashed border-white/10 p-5 text-center text-sm text-slate-500">No matching conversations.</p>}
          </div>
        </aside>

        <div className="min-w-0">
          <header className="relative overflow-hidden border-b border-slate-200 bg-gradient-to-r from-blue-700 via-blue-600 to-cyan-500 px-5 py-6 text-white sm:px-7">
            <div className="absolute -right-12 -top-16 h-48 w-48 rounded-full bg-white/10" />
            <div className="relative flex flex-wrap items-center justify-between gap-4">
              <div className="flex min-w-0 items-center gap-4"><div className="grid h-14 w-14 shrink-0 place-items-center rounded-2xl bg-white/15 text-3xl shadow-lg backdrop-blur">✦</div><div className="min-w-0"><p className="text-xs font-black uppercase tracking-[.18em] text-cyan-100">Secure account assistant</p><h3 className="truncate text-2xl font-black">{sessionTitle}</h3><p className="mt-1 text-sm text-blue-100"><span className={activeSession ? 'text-emerald-200' : 'text-amber-200'}>● {status}</span> · Saved to your account</p></div></div>
              <div className="flex flex-wrap gap-2"><button type="button" onClick={renameSelected} disabled={!sessionId || isSending} className="rounded-xl border border-white/20 bg-white/10 px-3 py-2 text-sm font-black hover:bg-white/20 disabled:opacity-40">Rename</button>{activeSession ? <button type="button" onClick={end} disabled={isSending} className="rounded-xl border border-amber-200/20 bg-amber-400/10 px-3 py-2 text-sm font-black text-amber-100 hover:bg-amber-400/20">End</button> : <button type="button" onClick={continueSelected} disabled={!sessionId || isSending} className="rounded-xl border border-emerald-200/20 bg-emerald-400/10 px-3 py-2 text-sm font-black text-emerald-100 hover:bg-emerald-400/20">Continue</button>}<button type="button" onClick={deleteSelected} disabled={!sessionId || isSending} className="rounded-xl border border-rose-200/20 bg-rose-400/10 px-3 py-2 text-sm font-black text-rose-100 hover:bg-rose-400/20 disabled:opacity-40">Delete</button></div>
            </div>
          </header>

          <div className="border-b border-slate-200 bg-white px-4 py-3 sm:px-7"><div className="flex gap-2 overflow-x-auto">{QUICK_PROMPTS.map((prompt) => <button key={prompt} type="button" onClick={() => send(prompt)} disabled={!activeSession || isSending} className="shrink-0 rounded-full border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-bold text-slate-600 hover:border-blue-300 hover:bg-blue-50 hover:text-blue-700 disabled:opacity-40">{prompt}</button>)}</div></div>

          <div className="h-[58vh] min-h-[470px] space-y-5 overflow-y-auto bg-slate-50 px-4 py-6 sm:px-7">
            {messages.map((item) => <Message key={item.id || `${item.sender}-${item.sentAt}`} item={item} onFeedback={submitFeedback} />)}
            {isSending && <div className="flex items-center gap-3 text-sm text-slate-500"><div className="grid h-9 w-9 place-items-center rounded-xl bg-blue-100 text-blue-700">✦</div><span>Streaming response…</span></div>}
            {messages.length === 0 && !isSending && <div className="grid min-h-[360px] place-items-center text-center"><div><div className="mx-auto grid h-20 w-20 place-items-center rounded-3xl bg-blue-100 text-4xl text-blue-700">✦</div><h3 className="mt-5 text-xl font-black text-slate-900">No messages in this session</h3><p className="mt-2 text-sm text-slate-500">Continue the session or create a new conversation.</p></div></div>}
            <div ref={bottomRef} />
          </div>

          <footer className="border-t border-slate-200 bg-white p-4 sm:p-6">
            {error && <div className="mb-4 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm font-bold text-rose-700">{error}</div>}
            <form onSubmit={(event) => { event.preventDefault(); send(); }} className="flex items-end gap-3">
              <label className="min-w-0 flex-1"><span className="sr-only">Message</span><textarea value={message} onChange={(event) => setMessage(event.target.value.slice(0, 1000))} onKeyDown={(event) => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); send(); } }} disabled={!activeSession} rows="2" placeholder={activeSession ? 'Ask about a complaint, verification, disputes or rewards…' : 'Continue this session to send another message'} className="w-full resize-none rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-blue-400 focus:bg-white focus:ring-4 focus:ring-blue-100 disabled:opacity-50" /><span className="mt-1 block text-right text-xs text-slate-400">{message.length}/1000</span></label>
              <button disabled={!canSend || !activeSession} className="mb-5 rounded-2xl bg-blue-600 px-6 py-3 font-black text-white shadow-lg shadow-blue-600/20 transition hover:-translate-y-0.5 hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-40">Send</button>
            </form>
          </footer>
        </div>
      </div>
    </section>
  );
}

function Message({ item, onFeedback }) {
  const isCitizen = item.sender?.toLowerCase() === 'citizen';
  const canRate = !isCitizen && !item.streaming && Number.isFinite(Number(item.id));
  return <article className={`flex gap-3 ${isCitizen ? 'justify-end' : 'justify-start'}`}>
    {!isCitizen && <div className="grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-blue-100 text-blue-700">✦</div>}
    <div className={`max-w-[86%] rounded-2xl px-4 py-3 shadow-sm ${isCitizen ? 'rounded-br-md bg-blue-600 text-white' : 'rounded-bl-md border border-slate-200 bg-white text-slate-700'}`}>
      <p className="whitespace-pre-wrap text-sm leading-6">{item.content}{item.streaming && <span className="ml-1 inline-block h-4 w-1 animate-pulse bg-blue-500 align-middle" />}</p>
      {item.actionUrl && <Link to={item.actionUrl} className={`mt-3 inline-flex rounded-lg px-3 py-2 text-xs font-black ${isCitizen ? 'bg-white/15 text-white' : 'bg-blue-50 text-blue-700 hover:bg-blue-100'}`}>{item.actionLabel || 'Open'}</Link>}
      <div className="mt-2 flex flex-wrap items-center justify-between gap-3"><time className={`text-[11px] ${isCitizen ? 'text-blue-100' : 'text-slate-400'}`}>{item.sentAt ? new Date(item.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : ''}</time>{canRate && <div className="flex items-center gap-1"><button type="button" onClick={() => onFeedback(item, true)} className={`rounded-lg px-2 py-1 text-[11px] font-bold ${item.feedback === 'Helpful' ? 'bg-emerald-100 text-emerald-700' : 'bg-slate-100 text-slate-500 hover:bg-emerald-50 hover:text-emerald-700'}`}>👍 Helpful</button><button type="button" onClick={() => onFeedback(item, false)} className={`rounded-lg px-2 py-1 text-[11px] font-bold ${item.feedback === 'NotHelpful' ? 'bg-rose-100 text-rose-700' : 'bg-slate-100 text-slate-500 hover:bg-rose-50 hover:text-rose-700'}`}>👎 Not helpful</button></div>}</div>
      {item.feedbackComment && <p className="mt-2 rounded-lg bg-slate-50 px-3 py-2 text-xs text-slate-500">Your feedback: {item.feedbackComment}</p>}
    </div>
  </article>;
}
