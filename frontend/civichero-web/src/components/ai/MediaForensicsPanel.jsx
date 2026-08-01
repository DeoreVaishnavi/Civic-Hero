const severityClass = {
  High: 'border-rose-400/30 bg-rose-400/10 text-rose-100',
  Medium: 'border-amber-400/30 bg-amber-400/10 text-amber-100',
  Information: 'border-sky-400/20 bg-sky-400/[0.06] text-sky-100',
};

const verdictClass = {
  HighRisk: 'bg-rose-400/15 text-rose-100',
  NeedsHumanReview: 'bg-amber-400/15 text-amber-100',
  NoMaterialAnomalyDetected: 'bg-emerald-400/15 text-emerald-100',
};

export default function MediaForensicsPanel({ report, onClose }) {
  if (!report) return null;

  return <section className="mt-5 rounded-2xl border border-violet-400/25 bg-violet-400/[0.05] p-5">
    <div className="flex flex-wrap items-start justify-between gap-4">
      <div>
        <p className="text-xs font-black uppercase tracking-wider text-violet-200">Media metadata and reuse analysis</p>
        <h4 className="mt-1 text-lg font-black text-white">Evidence forensics</h4>
        <p className="mt-1 text-sm text-slate-400">These signals are advisory. Missing EXIF or editing metadata alone must never automatically reject a complaint.</p>
      </div>
      <div className="flex items-center gap-2">
        <span className={`rounded-full px-3 py-1.5 text-xs font-black ${verdictClass[report.verdict] ?? 'bg-white/10 text-white'}`}>
          {humanize(report.verdict)} · {Math.round((report.riskScore ?? 0) * 100)}%
        </span>
        {onClose && <button type="button" onClick={onClose} className="rounded-lg border border-white/10 px-3 py-1.5 text-xs font-bold text-slate-300 hover:bg-white/5">Close</button>}
      </div>
    </div>

    <div className="mt-4 grid gap-3 sm:grid-cols-3">
      <Metric label="Images analysed" value={report.imagesAnalyzed ?? 0} />
      <Metric label="High-risk signals" value={report.highRiskSignals ?? 0} />
      <Metric label="Medium-risk signals" value={report.mediumRiskSignals ?? 0} />
    </div>

    <div className="mt-4 space-y-2">
      {(report.summary ?? []).map((line) => <p key={line} className="rounded-xl bg-slate-950/50 px-4 py-3 text-sm text-slate-300">{line}</p>)}
    </div>

    <div className="mt-5 space-y-4">
      {(report.images ?? []).map((image) => <article key={image.imageId} className="rounded-2xl border border-white/10 bg-slate-950/70 p-4">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <p className="text-xs font-bold uppercase tracking-wider text-slate-500">{image.evidenceType} evidence · image #{image.imageId}</p>
            <h5 className="mt-1 break-all font-black text-white">{image.fileName}</h5>
            <p className="mt-1 text-xs text-slate-500">{image.mimeType} · {formatBytes(image.fileSize)} · SHA-256 {image.sha256 ? `${image.sha256.slice(0, 16)}…` : 'not available'}</p>
          </div>
          <span className="rounded-full bg-white/5 px-3 py-1 text-xs font-bold text-slate-300">{humanize(image.metadataStatus)}</span>
        </div>

        <div className="mt-4 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <Info label="Camera" value={[image.cameraMake, image.cameraModel].filter(Boolean).join(' ') || 'Not available'} />
          <Info label="Software" value={image.software || 'Not available'} />
          <Info label="Capture time" value={image.capturedAtRaw || image.capturedAt || 'Not available'} />
          <Info label="Photo GPS" value={image.latitude != null && image.longitude != null ? `${image.latitude}, ${image.longitude}` : 'Not available'} />
          <Info label="Distance from complaint" value={image.distanceFromComplaintMeters != null ? `${Math.round(image.distanceFromComplaintMeters)} m` : 'Not available'} />
          <Info label="Exact reuse" value={image.exactReuseDetected ? `Detected in ${image.reuseMatches?.length ?? 0} other image(s)` : 'Not detected'} />
        </div>

        {(image.reuseMatches?.length ?? 0) > 0 && <div className="mt-4 rounded-xl border border-rose-400/20 bg-rose-400/[0.06] p-3">
          <p className="text-xs font-black uppercase tracking-wider text-rose-200">Exact-content matches</p>
          <div className="mt-2 flex flex-wrap gap-2">
            {image.reuseMatches.map((match) => <span key={`${match.complaintId}-${match.imageId}`} className="rounded-lg bg-slate-950/70 px-3 py-2 text-xs text-slate-300">
              CH-{String(match.complaintId).padStart(6, '0')} · image #{match.imageId}
            </span>)}
          </div>
        </div>}

        <div className="mt-4 space-y-2">
          {(image.signals ?? []).length === 0 && <p className="rounded-xl border border-emerald-400/20 bg-emerald-400/[0.06] p-3 text-sm text-emerald-100">No metadata anomaly was detected for this file.</p>}
          {(image.signals ?? []).map((signal, index) => <div key={`${signal.code}-${index}`} className={`rounded-xl border p-3 text-sm ${severityClass[signal.severity] ?? severityClass.Information}`}>
            <div className="flex flex-wrap items-center gap-2"><span className="font-black">{signal.severity}</span><span className="font-mono text-[11px] opacity-70">{signal.code}</span></div>
            <p className="mt-1 leading-6">{signal.message}</p>
          </div>)}
        </div>
      </article>)}

      {(report.images?.length ?? 0) === 0 && <div className="rounded-xl border border-dashed border-white/10 p-6 text-center text-sm text-slate-500">No evidence images were available for metadata analysis.</div>}
    </div>
  </section>;
}

function Metric({ label, value }) {
  return <div className="rounded-xl border border-white/10 bg-white/5 p-3"><p className="text-xs font-bold uppercase text-slate-500">{label}</p><p className="mt-1 text-2xl font-black text-white">{value}</p></div>;
}

function Info({ label, value }) {
  return <div className="rounded-xl border border-white/10 bg-white/[0.03] p-3"><p className="text-[11px] font-bold uppercase text-slate-500">{label}</p><p className="mt-1 break-words text-sm font-bold text-slate-200">{value}</p></div>;
}

function humanize(value) {
  return String(value ?? 'Unknown').replace(/([a-z])([A-Z])/g, '$1 $2').replace(/_/g, ' ');
}

function formatBytes(value) {
  const bytes = Number(value ?? 0);
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
