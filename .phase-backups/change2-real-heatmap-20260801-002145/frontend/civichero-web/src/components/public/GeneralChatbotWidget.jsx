import { useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { dashboardForRole } from '../../utils/roleRouting.js';
import { ROUTE_PATHS } from '../../routes/routePaths.js';

const quickPrompts = ['How do I report an issue?', 'Show me the heatmap', 'What schemes are active?', 'How are complaints verified?'];

const answers = [
  { match: ['report', 'complaint', 'issue'], text: 'To report an issue, choose Report Complaint, add a clear title and category, attach up to three photos, confirm the ward and location, then submit. CivicHero checks for possible duplicates before creating the complaint.', actionLabel: 'Report a complaint', actionUrl: ROUTE_PATHS.anonymousReport },
  { match: ['heatmap', 'hotspot', 'map'], text: 'The city heatmap groups complaints by location. Larger red hotspots have more active or high-priority complaints. Select a hotspot to see total, active, solved, verification-pending and disputed counts.', actionLabel: 'View the heatmap section', actionUrl: '#heatmap' },
  { match: ['scheme', 'project', 'government'], text: 'CivicHero showcases active public schemes and projects with budget, department, ward coverage, milestones and completion progress. The initiatives tracker provides the full project view.', actionLabel: 'Explore initiatives', actionUrl: '/projects' },
  { match: ['verify', 'verification', 'resolved'], text: 'After an officer uploads resolution evidence, the citizen reviews the before-and-after proof. The citizen may accept the resolution or raise a dispute. Accepted work moves toward verified closure.', actionLabel: 'Learn how it works', actionUrl: '#how-it-works' },
  { match: ['reward', 'point', 'leaderboard'], text: 'Citizens earn points for valid reports, helpful verification and supporting existing issues. Points can unlock badges and eligible rewards after the required balance is reached.', actionLabel: 'Create an account', actionUrl: ROUTE_PATHS.register },
  { match: ['track', 'status', 'reference'], text: 'Use Track Complaint and enter the complete complaint reference. Account-specific details require login so that CivicHero can enforce privacy and role permissions.', actionLabel: 'Track a complaint', actionUrl: ROUTE_PATHS.anonymousTrack },
  { match: ['emergency', 'danger', 'fire', 'police', 'ambulance'], text: 'CivicHero is not an emergency service. For immediate danger, contact the appropriate local emergency authority. Use CivicHero for municipal issues that can follow the normal complaint workflow.' },
];

const welcome = { id: 'welcome', sender: 'assistant', text: 'Hello! I am CivicGuide, the general CivicHero assistant. Ask me about reporting, heatmaps, schemes, complaint tracking, verification or rewards.' };

export default function GeneralChatbotWidget() {
  const { isAuthenticated, user } = useAuth();
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([welcome]);
  const [input, setInput] = useState('');
  const [typing, setTyping] = useState(false);
  const bottomRef = useRef(null);

  useEffect(() => { if (open) bottomRef.current?.scrollIntoView({ behavior: 'smooth' }); }, [messages, typing, open]);

  const accountLink = useMemo(() => {
    if (!isAuthenticated) return ROUTE_PATHS.login;
    if (String(user?.role || '').toLowerCase() === 'citizen') return ROUTE_PATHS.citizenChatbot;
    return dashboardForRole(user?.role);
  }, [isAuthenticated, user]);

  const send = (raw = input) => {
    const text = raw.trim();
    if (!text || typing) return;
    setInput('');
    setMessages((current) => [...current, { id: `u-${Date.now()}`, sender: 'user', text }]);
    setTyping(true);
    window.setTimeout(() => {
      const lower = text.toLowerCase();
      const answer = answers.find((item) => item.match.some((word) => lower.includes(word))) || {
        text: 'I can help with complaint reporting, tracking, heatmaps, schemes and projects, verification, disputes, notifications, rewards and account access. For account-specific information, please sign in.',
        actionLabel: isAuthenticated ? 'Open your dashboard' : 'Login securely',
        actionUrl: accountLink,
      };
      setMessages((current) => [...current, { id: `a-${Date.now()}`, sender: 'assistant', ...answer }]);
      setTyping(false);
    }, 650);
  };

  const followAction = (url) => {
    if (url?.startsWith('#')) {
      document.querySelector(url)?.scrollIntoView({ behavior: 'smooth' });
      setOpen(false);
    }
  };

  return (
    <div className="fixed bottom-5 right-4 z-[80] sm:bottom-7 sm:right-7">
      <div className={`absolute bottom-20 right-0 w-[min(390px,calc(100vw-2rem))] origin-bottom-right overflow-hidden rounded-[1.75rem] border border-slate-200 bg-white shadow-2xl shadow-slate-950/25 transition duration-300 ${open ? 'pointer-events-auto translate-y-0 scale-100 opacity-100' : 'pointer-events-none translate-y-5 scale-95 opacity-0'}`}>
        <header className="relative overflow-hidden bg-gradient-to-br from-blue-700 via-blue-600 to-cyan-500 p-5 text-white">
          <div className="absolute -right-8 -top-8 h-28 w-28 rounded-full bg-white/10" />
          <div className="relative flex items-center justify-between gap-4">
            <div className="flex items-center gap-3"><div className="grid h-12 w-12 place-items-center rounded-2xl bg-white/15 text-2xl shadow-lg backdrop-blur">✦</div><div><p className="text-xs font-black uppercase tracking-[.18em] text-cyan-100">General assistant</p><h3 className="text-lg font-black">CivicGuide</h3><p className="text-xs text-blue-100">● Online · Public guidance</p></div></div>
            <button type="button" onClick={() => setOpen(false)} className="grid h-9 w-9 place-items-center rounded-full bg-white/10 text-lg transition hover:bg-white/20" aria-label="Close chatbot">×</button>
          </div>
        </header>

        <div className="h-[390px] space-y-4 overflow-y-auto bg-slate-50 p-4">
          {messages.map((message) => <ChatMessage key={message.id} message={message} onAction={followAction} />)}
          {typing && <div className="flex items-center gap-2"><div className="grid h-8 w-8 place-items-center rounded-xl bg-blue-100 text-blue-700">✦</div><div className="flex gap-1 rounded-2xl rounded-bl-md bg-white px-4 py-3 shadow-sm"><i className="h-2 w-2 animate-bounce rounded-full bg-blue-400 [animation-delay:-.2s]" /><i className="h-2 w-2 animate-bounce rounded-full bg-blue-400 [animation-delay:-.1s]" /><i className="h-2 w-2 animate-bounce rounded-full bg-blue-400" /></div></div>}
          <div ref={bottomRef} />
        </div>

        <div className="border-t border-slate-200 bg-white p-4">
          <div className="mb-3 flex gap-2 overflow-x-auto pb-1">{quickPrompts.map((prompt) => <button key={prompt} type="button" onClick={() => send(prompt)} className="shrink-0 rounded-full border border-slate-200 bg-slate-50 px-3 py-2 text-[11px] font-bold text-slate-600 transition hover:border-blue-300 hover:bg-blue-50 hover:text-blue-700">{prompt}</button>)}</div>
          <form onSubmit={(event) => { event.preventDefault(); send(); }} className="flex items-end gap-2">
            <textarea value={input} onChange={(event) => setInput(event.target.value.slice(0, 500))} onKeyDown={(event) => { if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); send(); } }} rows="2" placeholder="Ask CivicGuide…" className="min-h-[48px] flex-1 resize-none rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3 text-sm text-slate-800 outline-none transition placeholder:text-slate-400 focus:border-blue-400 focus:bg-white focus:ring-4 focus:ring-blue-100" />
            <button disabled={!input.trim() || typing} className="grid h-12 w-12 place-items-center rounded-2xl bg-blue-600 font-black text-white shadow-lg shadow-blue-600/20 transition hover:-translate-y-0.5 hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-40">➤</button>
          </form>
          <Link to={accountLink} className="mt-3 flex items-center justify-center rounded-xl bg-slate-100 px-4 py-2.5 text-xs font-black text-slate-700 transition hover:bg-slate-200">{isAuthenticated && String(user?.role || '').toLowerCase() === 'citizen' ? 'Open full account assistant' : isAuthenticated ? 'Open your dashboard' : 'Login for account-specific help'}</Link>
        </div>
      </div>

      <button type="button" onClick={() => setOpen((value) => !value)} className={`group relative grid h-16 w-16 place-items-center rounded-full bg-gradient-to-br from-blue-600 to-cyan-500 text-2xl text-white shadow-2xl shadow-blue-600/35 transition duration-300 hover:-translate-y-1 hover:scale-105 ${open ? 'rotate-6' : ''}`} aria-expanded={open} aria-label="Open general CivicHero chatbot">
        <span className="absolute inset-0 animate-ping rounded-full bg-blue-500/20 [animation-duration:2.8s]" />
        <span className="relative">{open ? '×' : '✦'}</span>
        {!open && <span className="absolute -left-32 top-1/2 hidden -translate-y-1/2 rounded-full bg-slate-950 px-4 py-2 text-xs font-black shadow-xl group-hover:block">Ask CivicGuide</span>}
      </button>
    </div>
  );
}

function ChatMessage({ message, onAction }) {
  const isUser = message.sender === 'user';
  const action = message.actionUrl;
  return <div className={`flex gap-2 ${isUser ? 'justify-end' : 'justify-start'}`}>
    {!isUser && <div className="grid h-8 w-8 shrink-0 place-items-center rounded-xl bg-blue-100 text-blue-700">✦</div>}
    <div className={`max-w-[82%] rounded-2xl px-4 py-3 text-sm leading-6 shadow-sm ${isUser ? 'rounded-br-md bg-blue-600 text-white' : 'rounded-bl-md bg-white text-slate-700'}`}>
      <p>{message.text}</p>
      {action && (action.startsWith('#') ? <button type="button" onClick={() => onAction(action)} className="mt-3 rounded-lg bg-blue-50 px-3 py-2 text-xs font-black text-blue-700">{message.actionLabel}</button> : <Link to={action} className="mt-3 inline-flex rounded-lg bg-blue-50 px-3 py-2 text-xs font-black text-blue-700">{message.actionLabel}</Link>)}
    </div>
  </div>;
}
