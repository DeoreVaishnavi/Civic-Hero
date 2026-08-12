export default function Pagination({ page, totalPages, onPageChange }) {
  if (!totalPages || totalPages <= 1) return null;
  return (
    <div className="mt-8 flex items-center justify-center gap-3">
      <button disabled={page <= 1} onClick={() => onPageChange(page - 1)} className="rounded-lg border border-white/10 px-4 py-2 text-sm font-semibold disabled:opacity-40">Previous</button>
      <span className="text-sm text-slate-400">Page {page} of {totalPages}</span>
      <button disabled={page >= totalPages} onClick={() => onPageChange(page + 1)} className="rounded-lg border border-white/10 px-4 py-2 text-sm font-semibold disabled:opacity-40">Next</button>
    </div>
  );
}
