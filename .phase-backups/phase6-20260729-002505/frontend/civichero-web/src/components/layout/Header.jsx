import { Link } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import { dashboardForRole } from '../../utils/roleRouting.js';

export default function Header() {
  const { isAuthenticated, user } = useAuth();
  return (
    <header className="border-b border-white/10 bg-slate-950/80 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-6 py-4">
        <Link to={ROUTE_PATHS.home} className="text-xl font-black tracking-tight text-white">Civic<span className="text-sky-400">Hero</span></Link>
        <nav className="flex items-center gap-3 text-sm">
          {isAuthenticated ? (
            <Link to={dashboardForRole(user?.role)} className="rounded-lg bg-sky-500 px-3 py-2 font-semibold text-white hover:bg-sky-400">Open portal</Link>
          ) : (
            <>
              <Link to={ROUTE_PATHS.login} className="text-slate-300 hover:text-white">Login</Link>
              <Link to={ROUTE_PATHS.register} className="rounded-lg bg-sky-500 px-3 py-2 font-semibold text-white hover:bg-sky-400">Register</Link>
            </>
          )}
          <span className="hidden rounded-full border border-violet-400/20 bg-violet-400/10 px-3 py-1 text-xs font-semibold text-violet-200 md:inline">Phase 4 · RBAC</span>
        </nav>
      </div>
    </header>
  );
}
