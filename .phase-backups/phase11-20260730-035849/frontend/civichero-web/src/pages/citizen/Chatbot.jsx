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
  const [sessionId, setSessionId] = useState(() => localStorage.getItem(STORAGE_KEY) || '');
  const [messages, setMessages] = useState([]);
  const [message, setMessage] = useState('');
  const [status, setStatus] = useState('Connecting');
  const [error, setError] = useState('');
  const [isSending, setIsSending] = useState(false);
  const bottomRef = useRef(null);

  const canSend = message.trim().length > 0 && !isSending;
  const activeSession = useMemo(() => Boolean(sessionId) && status !== 'Ended', [sessionId, status]);

  useEffect(() => {
    let disposed = false;
    const initialize = async () => {
      setError('');
      try {
        let session;
        if (sessionId) {
          try { session = await chatbotApi.getSession(sessionId); }
          catch { session = await chatbotApi.startSession(); }
        } else {
          session = await chatbotApi.startSession();
        }
        if (disposed) return;
        localStorage.setItem(STORAGE_KEY, session.sessionId);
        setSessionId(session.sessionId);
        setMessages(session.messages || []);
        setStatus(session.status || 'Active');
      } catch (reason) {
        if (!disposed) { setError(reason.message); setStatus('Unavailable'); }
      }
    };
    initialize();
    return () => { disposed = true; };
  }, []); // Restore exactly once.

  useEffect(() => { bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages, isSending]);

  const send = async (text = message) => {
    const clean = text.trim();
    if (!clean || isSending || !sessionId) return;
    setMessage('');
    setError('');
    setIsSending(true);
    const optimistic = { id: `local-${Date.now()}`, sender: 'Citizen', content: clean, sentAt: new Date().toISOString() };
    setMessages((current) => [...current, optimistic]);
    try {
      const result = await chatbotApi.sendMessage(sessionId, clean);
      setMessages((current) => [
        ...current.filter((item) => item.id !== optimistic.id),
        result.userMessage,
        result.assistantMessage,
      ]);
    } catch (reason) {
      setMessages((current) => current.filter((item) => item.id !== optimistic.id));
      setError(reason.message);
      setMessage(clean);
    } finally { setIsSending(false); }
  };

  const startNew = async () => {
    setError('');
    setIsSending(true);
    try {
      if (sessionId && status === 'Active') await chatbotApi.endSession(sessionId).catch(() => undefined);
      const session = await chatbotApi.startSession();
      localStorage.setItem(STORAGE_KEY, session.sessionId);
      setSessionId(session.sessionId);
      setMessages(session.messages || []);
      setStatus(session.status || 'Active');
    } catch (reason) { setError(reason.message); }
    finally { setIsSending(false); }
  };

  const end = async () => {
    if (!sessionId) return;
    setError('');
    try { await chatbotApi.endSession(sessionId); setStatus('Ended'); }
    catch (reason) { setError(reason.message); }
  };

  return (
    <section className="p-4 sm:p-6 lg:p-10">
      <div className="mx-auto max-w-6xl overflow-hidden rounded-3xl border border-white/10 bg-slate-900 shadow-2xl shadow-black/30 xl:grid xl:grid-cols-[1fr_300px]">
        <div className="min-w-0">
          <header className="flex flex-wrap items-center justify-between gap-4 border-b border-white/10 bg-gradient-to-r from-sky-500/10 to-emerald-500/5 px-5 py-5 sm:px-7">
            <div className="flex items-center gap-4">
              <div className="grid h-12 w-12 place-items-center rounded-2xl bg-sky-500 text-2xl shadow-lg shadow-sky-500/20">✦</div>
              <div><p className="text-xs font-black uppercase tracking-[0.18em] text-sky-300">Citizen self-service</p><h2 className="text-xl font-black text-white">CivicHero Assistant</h2><p className="text-sm text-slate-400"><span className={status === 'Active' ? 'text-emerald-300' : 'text-amber-300'}>● {status}</span> · Saved conversation</p></div>
            </div>
            <div className="flex gap-2"><button onClick={startNew} disabled={isSending} className="rounded-xl border border-white/15 px-4 py-2 text-sm font-bold text-slate-200 hover:bg-white/10 disabled:opacity-50">New chat</button><button onClick={end} disabled={!activeSession} className="rounded-xl border border-rose-400/20 px-4 py-2 text-sm font-bold text-rose-200 hover:bg-rose-400/10 disabled:opacity-40">End</button></div>
          </header>

          <div className="h-[52vh] min-h-[430px] space-y-5 overflow-y-auto bg-slate-950/60 px-4 py-6 sm:px-7">
            {messages.map((item) => <Message key={item.id || `${item.sender}-${item.sentAt}`} item={item} />)}
            {isSending && <div className="flex items-center gap-3 text-sm text-slate-400"><div className="grid h-9 w-9 place-items-center rounded-xl bg-sky-500/15 text-sky-300">✦</div><span className="animate-pulse">CivicHero is preparing a response…</span></div>}
            {messages.length === 0 && !isSending && <p className="py-20 text-center text-slate-500">Starting your secure assistant session…</p>}
            <div ref={bottomRef} />
          </div>

          <footer className="border-t border-white/10 p-4 sm:p-6">
            {error && <div className="mb-4 rounded-xl border border-rose-400/30 bg-rose-400/10 p-3 text-sm text-rose-100">{error}</div>}
            <form onSubmit={(event) => { event.preventDefault(); send(); }} className="flex items-end gap-3">
              <label className="min-w-0 flex-1"><span className="sr-only">Message</span><textarea value={message} onChange={(event) => setMessage(event.target.value.slice(0, 1000))} onKeyDown={(event) => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); send(); } }} disabled={!activeSession} rows="2" placeholder={activeSession ? 'Ask about a complaint, verification, disputes or rewards…' : 'Start a new chat to continue'} className="w-full resize-none rounded-2xl border border-white/10 bg-slate-950 px-4 py-3 text-slate-100 outline-none transition placeholder:text-slate-600 focus:border-sky-400 disabled:opacity-50" /><span className="mt-1 block text-right text-xs text-slate-600">{message.length}/1000</span></label>
              <button disabled={!canSend || !activeSession} className="mb-5 rounded-2xl bg-sky-500 px-5 py-3 font-black text-white shadow-lg shadow-sky-500/20 hover:bg-sky-400 disabled:cursor-not-allowed disabled:opacity-40">Send</button>
            </form>
          </footer>
        </div>

        <aside className="border-t border-white/10 bg-white/[0.03] p-6 xl:border-l xl:border-t-0">
          <h3 className="font-black text-white">Quick questions</h3><p className="mt-1 text-sm text-slate-500">Use a prompt or type your own.</p>
          <div className="mt-5 space-y-3">{QUICK_PROMPTS.map((prompt) => <button key={prompt} onClick={() => send(prompt)} disabled={!activeSession || isSending} className="w-full rounded-xl border border-white/10 bg-slate-950/50 p-3 text-left text-sm font-semibold text-slate-300 transition hover:border-sky-400/40 hover:text-white disabled:opacity-40">{prompt}</button>)}</div>
          <div className="mt-7 rounded-2xl border border-amber-400/20 bg-amber-400/10 p-4"><h4 className="font-black text-amber-200">Complaint lookup</h4><p className="mt-2 text-sm leading-6 text-amber-100/70">Enter the complete reference, such as <strong>CH-2026-000123</strong>. Results are limited by your role and permissions.</p></div>
          <div className="mt-5 rounded-2xl border border-white/10 p-4"><h4 className="font-black text-white">Important</h4><p className="mt-2 text-sm leading-6 text-slate-400">The assistant provides guidance and status information. It does not replace emergency services or formal dispute decisions.</p></div>
        </aside>
      </div>
    </section>
  );
}

function Message({ item }) {
  const isCitizen = item.sender?.toLowerCase() === 'citizen';
  return <article className={`flex gap-3 ${isCitizen ? 'justify-end' : 'justify-start'}`}>
    {!isCitizen && <div className="grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-sky-500/15 text-sky-300">✦</div>}
    <div className={`max-w-[82%] rounded-2xl px-4 py-3 ${isCitizen ? 'rounded-br-md bg-sky-500 text-white' : 'rounded-bl-md border border-white/10 bg-slate-900 text-slate-200'}`}>
      <p className="whitespace-pre-wrap text-sm leading-6">{item.content}</p>
      {item.actionUrl && <Link to={item.actionUrl} className={`mt-3 inline-flex rounded-lg px-3 py-2 text-xs font-black ${isCitizen ? 'bg-white/15 text-white' : 'bg-sky-500/15 text-sky-300 hover:bg-sky-500/25'}`}>{item.actionLabel || 'Open'}</Link>}
      <time className={`mt-2 block text-[11px] ${isCitizen ? 'text-sky-100' : 'text-slate-600'}`}>{item.sentAt ? new Date(item.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : ''}</time>
    </div>
  </article>;
}
