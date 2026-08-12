export default function Header() {
  return (
    <header className="border-b border-white/10 bg-slate-950/80 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <a href="/" className="text-xl font-black tracking-tight text-white">
          Civic<span className="text-sky-400">Hero</span>
        </a>
        <span className="rounded-full border border-sky-400/20 bg-sky-400/10 px-3 py-1 text-xs font-semibold text-sky-200">
          Phase 1 · Connected
        </span>
      </div>
    </header>
  );
}
