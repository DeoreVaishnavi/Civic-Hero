import { useCallback, useEffect, useState } from 'react';
import ApiStatus from '../../components/common/ApiStatus.jsx';
import { getApiHealth } from '../../services/healthApi.js';

export default function HomePage() {
  const [status, setStatus] = useState('checking');
  const [health, setHealth] = useState(null);
  const [errorMessage, setErrorMessage] = useState('');

  const checkHealth = useCallback(async () => {
    setStatus('checking');
    setErrorMessage('');

    try {
      const response = await getApiHealth();
      setHealth(response.data);
      setStatus('connected');
    } catch (error) {
      setHealth(null);
      setErrorMessage(error.message);
      setStatus('unavailable');
    }
  }, []);

  useEffect(() => {
    checkHealth();
  }, [checkHealth]);

  const statusMessage = {
    checking: 'Checking CivicHero API…',
    connected: 'CivicHero API: Connected',
    unavailable: 'CivicHero API: Unavailable',
  }[status];

  return (
    <section className="relative overflow-hidden px-6 py-20 sm:py-28">
      <div className="absolute inset-0 -z-10 bg-[radial-gradient(circle_at_top_left,rgba(14,165,233,0.18),transparent_38%),radial-gradient(circle_at_bottom_right,rgba(16,185,129,0.12),transparent_35%)]" />

      <div className="mx-auto max-w-6xl">
        <div className="max-w-3xl">
          <ApiStatus status={status} message={statusMessage} />

          <h1 className="mt-8 text-5xl font-black tracking-tight text-white sm:text-7xl">
            Better civic issues.
            <span className="block text-sky-400">Faster public action.</span>
          </h1>

          <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-300">
            CivicHero connects citizens, officers, supervisors, and administrators
            through a transparent complaint-resolution workflow.
          </p>

          <div className="mt-10 flex flex-wrap gap-4">
            <button
              type="button"
              onClick={checkHealth}
              disabled={status === 'checking'}
              className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white transition hover:bg-sky-400 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {status === 'checking' ? 'Checking…' : 'Check API again'}
            </button>

            <a
              href="http://localhost:5180/swagger"
              target="_blank"
              rel="noreferrer"
              className="rounded-xl border border-white/15 px-5 py-3 font-bold text-slate-200 transition hover:border-white/30 hover:bg-white/5"
            >
              Open Swagger
            </a>
          </div>
        </div>

        <div className="mt-14 grid gap-4 sm:grid-cols-3">
          <InfoCard label="Frontend" value="React 19 + Vite 6" />
          <InfoCard label="Backend" value={health?.service || 'ASP.NET Core 8'} />
          <InfoCard label="Environment" value={health?.environment || 'Development'} />
        </div>

        {status === 'unavailable' && (
          <div className="mt-6 rounded-2xl border border-rose-400/20 bg-rose-400/10 p-4 text-sm text-rose-100">
            <strong>Connection error:</strong> {errorMessage}
            <div className="mt-2 text-rose-200/80">
              Start the backend at http://localhost:5180, then retry.
            </div>
          </div>
        )}
      </div>
    </section>
  );
}

function InfoCard({ label, value }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-5 shadow-2xl shadow-black/10">
      <div className="text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
        {label}
      </div>
      <div className="mt-2 text-lg font-bold text-white">{value}</div>
    </div>
  );
}
