import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';

export default function Header() {
  const { isAuthenticated, user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate(ROUTE_PATHS.home);
  };

  return (
    <header className="border-b border-white/10 bg-slate-950/80 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-6 py-4">
        <Link to={ROUTE_PATHS.home} className="text-xl font-black tracking-tight text-white">
          Civic<span className="text-sky-400">Hero</span>
        </Link>

        <nav className="flex items-center gap-3 text-sm">
          {isAuthenticated ? (
            <>
              <Link to={ROUTE_PATHS.citizenDashboard} className="text-slate-300 hover:text-white">
                Dashboard
              </Link>
              <span className="hidden text-slate-400 sm:inline">{user?.fullName}</span>
              <button
                type="button"
                onClick={handleLogout}
                className="rounded-lg border border-white/15 px-3 py-2 font-semibold text-white hover:bg-white/10"
              >
                Logout
              </button>
            </>
          ) : (
            <>
              <Link to={ROUTE_PATHS.login} className="text-slate-300 hover:text-white">Login</Link>
              <Link
                to={ROUTE_PATHS.register}
                className="rounded-lg bg-sky-500 px-3 py-2 font-semibold text-white hover:bg-sky-400"
              >
                Register
              </Link>
            </>
          )}
          <span className="hidden rounded-full border border-sky-400/20 bg-sky-400/10 px-3 py-1 text-xs font-semibold text-sky-200 md:inline">
            Phase 3 · Authentication
          </span>
        </nav>
      </div>
    </header>
  );
}
