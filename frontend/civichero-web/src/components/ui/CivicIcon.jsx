const paths = {
  dashboard: <><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></>,
  plus: <><path d="M12 5v14"/><path d="M5 12h14"/></>,
  complaints: <><path d="M7 3h10a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2Z"/><path d="M9 8h6M9 12h6M9 16h4"/></>,
  map: <><path d="m3 6 5-3 8 3 5-3v15l-5 3-8-3-5 3Z"/><path d="M8 3v15M16 6v15"/></>,
  location: <><path d="M20 10c0 5-8 11-8 11S4 15 4 10a8 8 0 1 1 16 0Z"/><circle cx="12" cy="10" r="2.5"/></>,
  verify: <><path d="M20 6 9 17l-5-5"/></>,
  dispute: <><path d="M12 3v18M5 6h14M7 6l-3 6h6L7 6Zm10 0-3 6h6l-3-6Z"/></>,
  rewards: <><path d="m12 3 2.6 5.3 5.9.9-4.3 4.2 1 5.9-5.2-2.8-5.2 2.8 1-5.9-4.3-4.2 5.9-.9Z"/></>,
  chat: <><path d="M21 12a8 8 0 0 1-8 8H6l-3 2 1-5a8 8 0 1 1 17-5Z"/><path d="M8 11h.01M12 11h.01M16 11h.01"/></>,
  bell: <><path d="M18 8a6 6 0 0 0-12 0c0 7-3 7-3 7h18s-3 0-3-7"/><path d="M10 19a2 2 0 0 0 4 0"/></>,
  user: <><circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/></>,
  users: <><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75"/></>,
  shield: <><path d="M12 3 4 6v5c0 5 3.4 8.4 8 10 4.6-1.6 8-5 8-10V6Z"/></>,
  security: <><path d="M12 3 4 6v5c0 5 3.4 8.4 8 10 4.6-1.6 8-5 8-10V6Z"/><path d="m9 12 2 2 4-4"/></>,
  analytics: <><path d="M4 19V9M10 19V5M16 19v-7M22 19V3"/><path d="M2 21h22"/></>,
  health: <><path d="M3 12h4l2-5 4 10 2-5h6"/></>,
  assignment: <><path d="M9 4h10a2 2 0 0 1 2 2v14H9"/><path d="M3 8h10M3 12h10M3 16h7"/><path d="m16 16 2 2 4-4"/></>,
  queue: <><path d="M8 6h13M8 12h13M8 18h13"/><circle cx="3" cy="6" r="1"/><circle cx="3" cy="12" r="1"/><circle cx="3" cy="18" r="1"/></>,
  logout: <><path d="M10 17l5-5-5-5M15 12H3"/><path d="M14 3h5a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-5"/></>,
  help: <><circle cx="12" cy="12" r="9"/><path d="M9.5 9a2.8 2.8 0 1 1 4.8 2c-1.2 1.1-2.3 1.5-2.3 3"/><path d="M12 18h.01"/></>,
  search: <><circle cx="11" cy="11" r="7"/><path d="m20 20-4-4"/></>,
  menu: <><path d="M4 7h16M4 12h16M4 17h16"/></>,
  close: <><path d="m6 6 12 12M18 6 6 18"/></>,
  'chevron-down': <><path d="m7 10 5 5 5-5"/></>,
  arrow: <><path d="M5 12h14M13 6l6 6-6 6"/></>,
  login: <><path d="M10 17l5-5-5-5M15 12H3"/><path d="M14 3h5a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-5"/></>,
  track: <><circle cx="12" cy="12" r="8"/><circle cx="12" cy="12" r="3"/><path d="M12 2v3M12 19v3M2 12h3M19 12h3"/></>,
  clock: <><circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/></>,
  alert: <><path d="M12 3 2.5 20h19Z"/><path d="M12 9v4M12 17h.01"/></>,
  progress: <><path d="M20 11a8 8 0 1 0-2.3 5.7"/><path d="M20 4v7h-7"/></>,
  support: <><path d="M7 10v11H3V10h4Zm0 10h10.5a2 2 0 0 0 1.9-1.4l1.5-5A2 2 0 0 0 19 11h-5l1-4a3 3 0 0 0-3-4l-5 7"/></>,
  department: <><path d="M3 21h18M5 21V9h14v12M8 9V5h8v4M9 13h2M13 13h2M9 17h2M13 17h2"/></>,
  ward: <><path d="M3 6 8 3l8 3 5-3v15l-5 3-8-3-5 3Z"/><path d="M8 3v15M16 6v15"/></>,
  category: <><path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z"/></>,
  audit: <><path d="M6 3h12v18H6z"/><path d="M9 8h6M9 12h6M9 16h4"/><path d="m15 17 2 2 4-4"/></>,
  governance: <><path d="M3 10h18M5 10v9M9 10v9M15 10v9M19 10v9M3 21h18M12 3l9 5H3Z"/></>,
  report: <><path d="M5 3h14v18H5z"/><path d="M8 8h8M8 12h8M8 16h5"/></>,
  refresh: <><path d="M20 7v5h-5M4 17v-5h5"/><path d="M18.5 9A7 7 0 0 0 6 6.5L4 9M5.5 15A7 7 0 0 0 18 17.5l2-2.5"/></>,
  upload: <><path d="M12 16V4M7 9l5-5 5 5"/><path d="M4 20h16"/></>,
  filter: <><path d="M3 5h18l-7 8v6l-4 2v-8Z"/></>,
  info: <><circle cx="12" cy="12" r="9"/><path d="M12 11v5M12 8h.01"/></>,
  empty: <><path d="M4 7h16v12H4z"/><path d="M8 7V4h8v3M9 13h6"/></>,
  phone: <><path d="M7 3h10v18H7z"/><path d="M10 18h4"/></>,
  key: <><circle cx="8" cy="15" r="4"/><path d="m11 12 9-9M17 6l3 3M14 9l3 3"/></>,
  mail: <><path d="M3 5h18v14H3z"/><path d="m3 6 9 7 9-7"/></>,
  otp: <><rect x="3" y="6" width="18" height="12" rx="2"/><path d="M7 12h.01M11 12h.01M15 12h.01M19 12h.01"/></>,
  anonymous: <><circle cx="12" cy="8" r="4"/><path d="M5 21a7 7 0 0 1 14 0"/><path d="M4 4l16 16"/></>,
  road: <><path d="M9 3 6 21M15 3l3 18M12 4v4M12 11v4M12 18v2"/></>,
  light: <><path d="M9 18h6M10 22h4"/><path d="M8 14a6 6 0 1 1 8 0c-1 .8-1.5 1.7-1.5 3h-5c0-1.3-.5-2.2-1.5-3Z"/></>,
  recycle: <><path d="m7 7 2-4 2 4M9 3l3 5H6M17 8h4l-2 3M21 8l-3 5-3-5M7 17l-2 4h4M5 21l3-5 3 5"/></>,
  water: <><path d="M12 3s6 7 6 11a6 6 0 1 1-12 0c0-4 6-11 6-11Z"/></>,
};

export default function CivicIcon({ name = 'info', size = 20, className = '', title }) {
  const content = paths[name] || paths.info;
  return (
    <svg
      className={`civic-icon ${className}`.trim()}
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.9"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden={title ? undefined : true}
      role={title ? 'img' : undefined}
    >
      {title && <title>{title}</title>}
      {content}
    </svg>
  );
}
