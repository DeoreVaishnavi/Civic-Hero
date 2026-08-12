import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import RoleBadge from '../components/common/RoleBadge.jsx';
import { useAuth } from '../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../routes/routePaths.js';

export default function PortalLayout({ title, subtitle, navItems }) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const handleLogout = async () => { await logout(); navigate(ROUTE_PATHS.home, { replace: true }); };

  return (
    <div className="min-h-screen bg-slate-950 text-slate-100 lg:grid lg:grid-cols-[260px_1fr]">
      <aside className="border-b border-white/10 bg-slate-900/80 p-5 lg:min-h-screen lg:border-b-0 lg:border-r">
        <div className="flex items-center justify-between lg:block">
          <div>
            <div className="text-2xl font-black">Civic<span className="text-sky-400">Hero</span></div>
            <p className="mt-1 text-xs uppercase tracking-[0.16em] text-slate-500">{title}</p>
          </div>
          <RoleBadge role={user?.role} />
        </div>
        <nav className="mt-6 flex gap-2 overflow-x-auto lg:block lg:space-y-2">
          {navItems.map((item) => (
            <NavLink key={item.to} to={item.to} end={item.end} className={({ isActive }) => `block whitespace-nowrap rounded-xl px-4 py-3 text-sm font-semibold transition ${isActive ? 'bg-sky-500 text-white' : 'text-slate-300 hover:bg-white/10 hover:text-white'}`}>
              {item.label}
            </NavLink>
          ))}
        </nav>
        <div className="mt-6 hidden rounded-2xl border border-white/10 bg-white/5 p-4 lg:block">
          <p className="font-semibold text-white">{user?.fullName}</p>
          <p className="mt-1 break-all text-xs text-slate-400">{user?.email}</p>
          <button onClick={handleLogout} className="mt-4 w-full rounded-lg border border-white/15 px-3 py-2 text-sm font-semibold hover:bg-white/10">Logout</button>
        </div>
      </aside>
      <main className="min-w-0">
        <header className="border-b border-white/10 bg-slate-950/80 px-6 py-5 backdrop-blur">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div><h1 className="text-xl font-black text-white">{title}</h1><p className="text-sm text-slate-400">{subtitle}</p></div>
            <button onClick={handleLogout} className="rounded-lg border border-white/15 px-3 py-2 text-sm font-semibold hover:bg-white/10 lg:hidden">Logout</button>
          </div>
        </header>
        <Outlet />
      </main>
    </div>
  );
}
