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
  }, []);

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
      setMessages((current) => [...current.filter((item) => item.id !== optimistic.id), result.userMessage, result.assistantMessage]);
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
    <section className="page-wrap">
      <div className="page-title-row"><div><p className="section-kicker">Citizen self-service</p><h2>CivicHero Assistant</h2><p>Ask about complaint reporting, status, verification, disputes, rewards and platform guidance.</p></div><Link to="/citizen" className="button outline">← Back to dashboard</Link></div>

      <div className="mx-auto grid max-w-6xl overflow-hidden rounded-[2rem] border border-slate-200 bg-white shadow-2xl shadow-slate-900/10 xl:grid-cols-[1fr_320px]">
        <div className="min-w-0">
          <header className="relative overflow-hidden border-b border-slate-200 bg-gradient-to-r from-blue-700 via-blue-600 to-cyan-500 px-5 py-6 text-white sm:px-7">
            <div className="absolute -right-12 -top-16 h-48 w-48 rounded-full bg-white/10" />
            <div className="relative flex flex-wrap items-center justify-between gap-4">
              <div className="flex items-center gap-4"><div className="grid h-14 w-14 place-items-center rounded-2xl bg-white/15 text-3xl shadow-lg backdrop-blur">✦</div><div><p className="text-xs font-black uppercase tracking-[.18em] text-cyan-100">Secure account assistant</p><h3 className="text-2xl font-black">Ask CivicHero</h3><p className="mt-1 text-sm text-blue-100"><span className={status === 'Active' ? 'text-emerald-200' : 'text-amber-200'}>● {status}</span> · Saved conversation</p></div></div>
              <div className="flex gap-2"><button type="button" onClick={startNew} disabled={isSending} className="rounded-xl border border-white/20 bg-white/10 px-4 py-2.5 text-sm font-black transition hover:bg-white/20 disabled:opacity-50">New chat</button><button type="button" onClick={end} disabled={!activeSession} className="rounded-xl border border-rose-200/20 bg-rose-400/10 px-4 py-2.5 text-sm font-black text-rose-100 transition hover:bg-rose-400/20 disabled:opacity-40">End</button></div>
            </div>
          </header>

          <div className="h-[56vh] min-h-[470px] space-y-5 overflow-y-auto bg-slate-50 px-4 py-6 sm:px-7">
            {messages.map((item) => <Message key={item.id || `${item.sender}-${item.sentAt}`} item={item} />)}
            {isSending && <div className="flex items-center gap-3 text-sm text-slate-500"><div className="grid h-9 w-9 place-items-center rounded-xl bg-blue-100 text-blue-700">✦</div><div className="flex gap-1 rounded-2xl bg-white px-4 py-3 shadow-sm"><i className="h-2 w-2 animate-bounce rounded-full bg-blue-400 [animation-delay:-.2s]" /><i className="h-2 w-2 animate-bounce rounded-full bg-blue-400 [animation-delay:-.1s]" /><i className="h-2 w-2 animate-bounce rounded-full bg-blue-400" /></div></div>}
            {messages.length === 0 && !isSending && <div className="grid min-h-[360px] place-items-center text-center"><div><div className="mx-auto grid h-20 w-20 place-items-center rounded-3xl bg-blue-100 text-4xl text-blue-700">✦</div><h3 className="mt-5 text-xl font-black text-slate-900">Starting your assistant session</h3><p className="mt-2 text-sm text-slate-500">Your secure conversation will appear here.</p></div></div>}
            <div ref={bottomRef} />
          </div>

          <footer className="border-t border-slate-200 bg-white p-4 sm:p-6">
            {error && <div className="mb-4 rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm font-bold text-rose-700">{error}</div>}
            <form onSubmit={(event) => { event.preventDefault(); send(); }} className="flex items-end gap-3">
              <label className="min-w-0 flex-1"><span className="sr-only">Message</span><textarea value={message} onChange={(event) => setMessage(event.target.value.slice(0, 1000))} onKeyDown={(event) => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); send(); } }} disabled={!activeSession} rows="2" placeholder={activeSession ? 'Ask about a complaint, verification, disputes or rewards…' : 'Start a new chat to continue'} className="w-full resize-none rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-blue-400 focus:bg-white focus:ring-4 focus:ring-blue-100 disabled:opacity-50" /><span className="mt-1 block text-right text-xs text-slate-400">{message.length}/1000</span></label>
              <button disabled={!canSend || !activeSession} className="mb-5 rounded-2xl bg-blue-600 px-6 py-3 font-black text-white shadow-lg shadow-blue-600/20 transition hover:-translate-y-0.5 hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-40">Send</button>
            </form>
          </footer>
        </div>

        <aside className="border-t border-slate-200 bg-white p-6 xl:border-l xl:border-t-0">
          <h3 className="text-lg font-black text-slate-900">Quick questions</h3><p className="mt-1 text-sm text-slate-500">Choose a prompt or type your own question.</p>
          <div className="mt-5 space-y-3">{QUICK_PROMPTS.map((prompt) => <button key={prompt} type="button" onClick={() => send(prompt)} disabled={!activeSession || isSending} className="w-full rounded-xl border border-slate-200 bg-slate-50 p-3 text-left text-sm font-bold text-slate-700 transition hover:-translate-y-0.5 hover:border-blue-300 hover:bg-blue-50 hover:text-blue-700 disabled:opacity-40">{prompt}</button>)}</div>
          <div className="mt-7 rounded-2xl border border-amber-200 bg-amber-50 p-4"><h4 className="font-black text-amber-800">Complaint lookup</h4><p className="mt-2 text-sm leading-6 text-amber-700">Enter the complete reference, such as <strong>CH-2026-000123</strong>. Results are limited by your role and permissions.</p></div>
          <div className="mt-5 rounded-2xl border border-slate-200 p-4"><h4 className="font-black text-slate-900">Important</h4><p className="mt-2 text-sm leading-6 text-slate-500">The assistant provides guidance and status information. It does not replace emergency services or formal dispute decisions.</p></div>
        </aside>
      </div>
    </section>
  );
}

function Message({ item }) {
  const isCitizen = item.sender?.toLowerCase() === 'citizen';
  return <article className={`flex gap-3 ${isCitizen ? 'justify-end' : 'justify-start'}`}>
    {!isCitizen && <div className="grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-blue-100 text-blue-700">✦</div>}
    <div className={`max-w-[82%] rounded-2xl px-4 py-3 shadow-sm ${isCitizen ? 'rounded-br-md bg-blue-600 text-white' : 'rounded-bl-md border border-slate-200 bg-white text-slate-700'}`}>
      <p className="whitespace-pre-wrap text-sm leading-6">{item.content}</p>
      {item.actionUrl && <Link to={item.actionUrl} className={`mt-3 inline-flex rounded-lg px-3 py-2 text-xs font-black ${isCitizen ? 'bg-white/15 text-white' : 'bg-blue-50 text-blue-700 hover:bg-blue-100'}`}>{item.actionLabel || 'Open'}</Link>}
      <time className={`mt-2 block text-[11px] ${isCitizen ? 'text-blue-100' : 'text-slate-400'}`}>{item.sentAt ? new Date(item.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : ''}</time>
    </div>
  </article>;
}
