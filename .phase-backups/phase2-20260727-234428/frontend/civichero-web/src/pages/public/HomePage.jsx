import { useCallback, useEffect, useState } from 'react';
import ApiStatus from '../../components/common/ApiStatus.jsx';
import {
  getApiHealth,
  getCorrelationHeaderCheck,
  getInfrastructureHealth,
} from '../../services/healthApi.js';
import { mapApiError } from '../../utils/errorMapper.js';

export default function HomePage() {
  const [status, setStatus] = useState('checking');
  const [health, setHealth] = useState(null);
  const [infrastructure, setInfrastructure] = useState(null);
  const [correlationCheck, setCorrelationCheck] = useState(null);
  const [error, setError] = useState(null);

  const checkHealth = useCallback(async () => {
    setStatus('checking');
    setError(null);

    try {
      const [healthResponse, headerResponse, infrastructureResponse] = await Promise.all([
        getApiHealth(),
        getCorrelationHeaderCheck(),
        getInfrastructureHealth(),
      ]);

      setHealth(healthResponse.data);
      setCorrelationCheck(headerResponse.data);
      setInfrastructure(infrastructureResponse.data);
      setStatus('connected');
    } catch (requestError) {
      setHealth(null);
      setInfrastructure(null);
      setCorrelationCheck(null);
      setError(mapApiError(requestError));
      setStatus('unavailable');
    }
  }, []);

  useEffect(() => {
    checkHealth();
  }, [checkHealth]);

  const statusMessage = {
    checking: 'Checking CivicHero services…',
    connected: 'CivicHero API: Connected',
    unavailable: 'CivicHero API: Unavailable',
  }[status];

  const database = infrastructure?.services?.['aws-rds-mysql'];
  const storage = infrastructure?.services?.['amazon-s3'];

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
            Phase 2 connects the ASP.NET Core backend to AWS RDS MySQL and
            prepares secure complaint-image storage in Amazon S3.
          </p>

          <div className="mt-10 flex flex-wrap gap-4">
            <button
              type="button"
              onClick={checkHealth}
              disabled={status === 'checking'}
              className="rounded-xl bg-sky-500 px-5 py-3 font-bold text-white transition hover:bg-sky-400 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {status === 'checking' ? 'Checking…' : 'Check infrastructure'}
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
          <InfrastructureCard
            label="AWS RDS MySQL"
            service={database}
            fallback="Not configured"
          />
          <InfrastructureCard
            label="Amazon S3"
            service={storage}
            fallback="Not configured"
          />
        </div>

        {status === 'connected' && (
          <div className="mt-6 rounded-2xl border border-sky-400/20 bg-sky-400/10 p-5 text-sm text-sky-100">
            <div className="font-bold">Phase 2 infrastructure checks completed</div>
            <div className="mt-2 break-all text-sky-200/80">
              Correlation ID: {correlationCheck?.correlationId || health?.correlationId}
            </div>
            <div className="mt-1 text-sky-200/80">
              Overall infrastructure: {infrastructure?.status || 'Unknown'}
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

function InfrastructureCard({ label, service, fallback }) {
  const value = service?.status || fallback;
  const statusClasses = {
    Healthy: 'text-emerald-300',
    Degraded: 'text-amber-300',
    Unhealthy: 'text-rose-300',
  };

  return (
    <div className="rounded-2xl border border-white/10 bg-white/[0.04] p-5 shadow-2xl shadow-black/10">
      <div className="text-xs font-bold uppercase tracking-[0.18em] text-slate-500">
        {label}
      </div>
      <div className={`mt-2 text-lg font-bold ${statusClasses[value] || 'text-slate-300'}`}>
        {value}
      </div>
      <div className="mt-2 line-clamp-2 text-xs leading-5 text-slate-500">
        {service?.description || 'Add AWS configuration to activate this service.'}
      </div>
    </div>
  );
}
