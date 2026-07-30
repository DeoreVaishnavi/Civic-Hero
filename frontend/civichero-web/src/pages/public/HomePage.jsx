import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ROUTE_PATHS } from '../../routes/routePaths.js';
import ApiStatus from '../../components/common/ApiStatus.jsx';
import {
  getApiHealth,
  getCorrelationHeaderCheck,
  getDependencyHealth,
} from '../../services/healthApi.js';
import { mapApiError } from '../../utils/errorMapper.js';

export default function HomePage() {
  const [status, setStatus] = useState('checking');
  const [health, setHealth] = useState(null);
  const [correlationCheck, setCorrelationCheck] = useState(null);
  const [dependencies, setDependencies] = useState([]);
  const [error, setError] = useState(null);

  const checkHealth = useCallback(async () => {
    setStatus('checking');
    setError(null);

    try {
      const [healthResponse, headerResponse, dependencyResponse] = await Promise.all([
        getApiHealth(),
        getCorrelationHeaderCheck(),
        getDependencyHealth(),
      ]);

      setHealth(healthResponse.data);
      setCorrelationCheck(headerResponse.data);
      setDependencies(dependencyResponse.data?.checks ?? []);
      setStatus('connected');
    } catch (requestError) {
      setHealth(null);
      setCorrelationCheck(null);
      setDependencies([]);
      setError(mapApiError(requestError));
      setStatus('unavailable');
    }
  }, []);

  useEffect(() => {
    checkHealth();
  }, [checkHealth]);

  const dependencyMap = useMemo(
    () => Object.fromEntries(dependencies.map((item) => [item.name, item])),
    [dependencies],
  );

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
            Report. Track. Verify.
            <span className="block text-sky-400">Improve your city transparently.</span>
          </h1>

          <p className="mt-6 max-w-2xl text-lg leading-8 text-slate-300">
            CivicHero connects citizens, officers and city administrators through secure complaint reporting, evidence-based resolution, transparent verification and accountable public-service analytics.
          </p>

          <div className="mt-10 flex flex-wrap gap-4">
            <Link to={ROUTE_PATHS.login} className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white transition hover:bg-sky-400">Login / report complaint</Link>
            <Link to={ROUTE_PATHS.anonymousReport} className="rounded-xl border border-emerald-400/30 px-5 py-3 font-bold text-emerald-200 transition hover:bg-emerald-400/10">Continue anonymously</Link>
            <Link to={ROUTE_PATHS.anonymousTrack} className="rounded-xl border border-white/15 px-5 py-3 font-bold text-slate-200 transition hover:border-white/30 hover:bg-white/5">Track anonymous complaint</Link>
            <button type="button" onClick={checkHealth} disabled={status === 'checking'} className="rounded-xl border border-white/15 px-5 py-3 font-bold text-slate-200 disabled:opacity-60">{status === 'checking' ? 'Checking…' : 'Check system health'}</button>
          </div>
        </div>

        <div className="mt-14 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <InfoCard label="Frontend" value="React 19 + Vite" />
          <InfoCard label="Backend" value={health?.service || 'ASP.NET Core 8'} />
          <DependencyCard
            label="Database"
            value="AWS RDS MySQL 8"
            dependency={dependencyMap.database}
          />
          <DependencyCard
            label="Storage"
            value="Amazon S3"
            dependency={dependencyMap.storage}
          />
        </div>

        {status === 'connected' && (
          <div className="mt-6 rounded-2xl border border-emerald-400/20 bg-emerald-400/10 p-5 text-sm text-emerald-100">
            <div className="font-bold">CivicHero application pipeline is running</div>
            <div className="mt-2 break-all text-emerald-200/80">
              Correlation ID: {correlationCheck?.correlationId || health?.correlationId}
            </div>
            <div className="mt-1 text-emerald-200/80">
              API version: {health?.version || '1.0.0'}
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

function DependencyCard({ label, value, dependency }) {
  const state = dependency?.status?.toLowerCase() ?? 'checking';
  const styles = {
    healthy: 'border-emerald-400/30 text-emerald-200',
    degraded: 'border-amber-400/30 text-amber-200',
    unhealthy: 'border-rose-400/30 text-rose-200',
    checking: 'border-white/10 text-slate-300',
  };

  return (
    <div className={`rounded-2xl border bg-white/[0.04] p-5 ${styles[state] || styles.checking}`}>
      <div className="text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
        {label}
      </div>
      <div className="mt-2 text-lg font-bold text-white">{value}</div>
      <div className="mt-3 text-sm font-semibold">
        {dependency?.status ?? 'Checking'}
      </div>
      <div className="mt-1 min-h-10 text-xs text-slate-400">
        {dependency?.description ?? 'Waiting for backend dependency check.'}
      </div>
    </div>
  );
}
