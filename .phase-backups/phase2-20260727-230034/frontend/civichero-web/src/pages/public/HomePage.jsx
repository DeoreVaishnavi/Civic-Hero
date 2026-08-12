import { useCallback, useEffect, useState } from 'react';
import ApiStatus from '../../components/common/ApiStatus.jsx';
import { getApiHealth, getCorrelationHeaderCheck } from '../../services/healthApi.js';
import { mapApiError } from '../../utils/errorMapper.js';

export default function HomePage() {
  const [status, setStatus] = useState('checking');
  const [health, setHealth] = useState(null);
  const [correlationCheck, setCorrelationCheck] = useState(null);
  const [error, setError] = useState(null);

  const checkHealth = useCallback(async () => {
    setStatus('checking');
    setError(null);

    try {
      const [healthResponse, headerResponse] = await Promise.all([
        getApiHealth(),
        getCorrelationHeaderCheck(),
      ]);

      setHealth(healthResponse.data);
      setCorrelationCheck(headerResponse.data);
      setStatus('connected');
    } catch (requestError) {
      setHealth(null);
      setCorrelationCheck(null);
      setError(mapApiError(requestError));
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
            Phase 1 verifies the complete browser-to-API request pipeline before
            database and authentication development begins.
          </p>

          <div className="mt-10 flex flex-wrap gap-4">
            <button
              type="button"
              onClick={checkHealth}
              disabled={status === 'checking'}
              className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white transition hover:bg-sky-400 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {status === 'checking' ? 'Checking…' : 'Check connection'}
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

        <div className="mt-14 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <InfoCard label="Frontend" value="React 19 + Vite" />
          <InfoCard label="Backend" value={health?.service || 'ASP.NET Core 8'} />
          <InfoCard label="API version" value={health?.apiVersion || 'v1'} />
          <InfoCard label="Environment" value={health?.environment || 'Development'} />
        </div>

        {status === 'connected' && (
          <div className="mt-6 rounded-2xl border border-emerald-400/20 bg-emerald-400/10 p-5 text-sm text-emerald-100">
            <div className="font-bold">Phase 1 connection checks passed</div>
            <div className="mt-2 break-all text-emerald-200/80">
              Correlation ID: {correlationCheck?.correlationId || health?.correlationId}
            </div>
            <div className="mt-1 text-emerald-200/80">
              Server time: {health?.serverTimeUtc ? new Date(health.serverTimeUtc).toLocaleString() : '—'}
            </div>
          </div>
        )}

        {status === 'unavailable' && error && (
          <div className="mt-6 rounded-2xl border border-rose-400/20 bg-rose-400/10 p-5 text-sm text-rose-100">
            <div className="font-bold">{error.title}</div>
            <div className="mt-2 text-rose-200/80">{error.message}</div>
            {error.traceId && (
              <div className="mt-2 break-all text-rose-200/70">Trace ID: {error.traceId}</div>
            )}
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
