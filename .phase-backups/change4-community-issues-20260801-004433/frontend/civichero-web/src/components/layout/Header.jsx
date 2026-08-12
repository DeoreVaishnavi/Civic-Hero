import { useState } from 'react';
import { Link } from 'react-router-dom';
import CivicLogo from '../ui/CivicLogo.jsx';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { dashboardForRole } from '../../utils/roleRouting.js';

const nav = [
  { label: 'Home', to: ROUTE_PATHS.home },
  { label: 'Report complaint', to: ROUTE_PATHS.anonymousReport },
  { label: 'Track complaint', to: ROUTE_PATHS.anonymousTrack },
  { label: 'Heatmap', to: { pathname: ROUTE_PATHS.home, hash: '#heatmap' } },
  { label: 'Schemes & projects', to: '/projects' },
];

export default function Header() {
  const { isAuthenticated, user } = useAuth();
  const [open, setOpen] = useState(false);
  return (
    <header className="sticky top-0 z-50 border-b border-slate-200/80 bg-white/90 backdrop-blur-xl">
      <div className="mx-auto flex min-h-[76px] w-full max-w-7xl items-center justify-between gap-6 px-4 sm:px-6 lg:px-8">
        <CivicLogo />
        <nav className="hidden items-center gap-6 text-sm font-bold text-slate-600 lg:flex" aria-label="Public navigation">
          {nav.map((item) => item.to ? <Link key={item.label} to={item.to} className="relative py-7 transition after:absolute after:bottom-5 after:left-1/2 after:h-0.5 after:w-0 after:-translate-x-1/2 after:rounded-full after:bg-blue-600 after:transition-all hover:text-blue-700 hover:after:w-full">{item.label}</Link> : <a key={item.label} href={item.href} className="relative py-7 transition after:absolute after:bottom-5 after:left-1/2 after:h-0.5 after:w-0 after:-translate-x-1/2 after:rounded-full after:bg-blue-600 after:transition-all hover:text-blue-700 hover:after:w-full">{item.label}</a>)}
        </nav>
        <div className="hidden items-center gap-2 sm:flex">
          {isAuthenticated ? <Link to={dashboardForRole(user?.role)} className="rounded-xl bg-blue-600 px-5 py-3 text-sm font-black text-white shadow-lg shadow-blue-600/20 transition hover:-translate-y-0.5 hover:bg-blue-700">Open dashboard</Link> : <><Link to={ROUTE_PATHS.login} className="rounded-xl px-4 py-3 text-sm font-black text-slate-700 transition hover:bg-slate-100">Login</Link><Link to={ROUTE_PATHS.register} className="rounded-xl bg-blue-600 px-5 py-3 text-sm font-black text-white shadow-lg shadow-blue-600/20 transition hover:-translate-y-0.5 hover:bg-blue-700">Create account</Link></>}
        </div>
        <button type="button" onClick={() => setOpen((value) => !value)} className="grid h-11 w-11 place-items-center rounded-xl border border-slate-200 text-xl text-slate-700 sm:hidden" aria-label="Toggle navigation">{open ? '×' : '☰'}</button>
      </div>
      <div className={`overflow-hidden border-t border-slate-200 bg-white transition-all duration-300 sm:hidden ${open ? 'max-h-[520px] opacity-100' : 'max-h-0 opacity-0'}`}>
        <nav className="space-y-1 p-4">{nav.map((item) => item.to ? <Link key={item.label} to={item.to} onClick={() => setOpen(false)} className="block rounded-xl px-4 py-3 text-sm font-black text-slate-700 hover:bg-blue-50 hover:text-blue-700">{item.label}</Link> : <a key={item.label} href={item.href} onClick={() => setOpen(false)} className="block rounded-xl px-4 py-3 text-sm font-black text-slate-700 hover:bg-blue-50 hover:text-blue-700">{item.label}</a>)}<div className="grid grid-cols-2 gap-2 pt-3">{isAuthenticated ? <Link to={dashboardForRole(user?.role)} className="col-span-2 rounded-xl bg-blue-600 px-4 py-3 text-center text-sm font-black text-white">Open dashboard</Link> : <><Link to={ROUTE_PATHS.login} className="rounded-xl border border-slate-200 px-4 py-3 text-center text-sm font-black text-slate-700">Login</Link><Link to={ROUTE_PATHS.register} className="rounded-xl bg-blue-600 px-4 py-3 text-center text-sm font-black text-white">Create account</Link></>}</div></nav>
      </div>
    </header>
  );
}
