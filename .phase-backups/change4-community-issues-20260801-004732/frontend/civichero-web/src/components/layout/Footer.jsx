import { Link } from 'react-router-dom';
import CivicLogo from '../ui/CivicLogo.jsx';
import { ROUTE_PATHS } from '../../routes/routePaths.js';

export default function Footer() {
  return (
    <footer className="bg-slate-950 text-slate-400">
      <div className="mx-auto grid w-full max-w-7xl gap-10 px-4 py-14 sm:px-6 md:grid-cols-[1.2fr_.8fr_.8fr] lg:px-8">
        <div><CivicLogo light /><p className="mt-5 max-w-lg text-sm leading-7">Transparent civic reporting, accountable resolution, real-time heatmaps and stronger communities through citizen participation.</p></div>
        <div><h3 className="font-black text-white">Explore</h3><div className="mt-4 space-y-3 text-sm"><Link className="block hover:text-white" to={ROUTE_PATHS.anonymousReport}>Report complaint</Link><Link className="block hover:text-white" to={ROUTE_PATHS.anonymousTrack}>Track complaint</Link><Link className="block hover:text-white" to={ROUTE_PATHS.publicIssues}>Community issues</Link><Link className="block hover:text-white" to={{ pathname: ROUTE_PATHS.home, hash: '#heatmap' }}>City heatmap</Link><Link className="block hover:text-white" to="/projects">Schemes and projects</Link></div></div>
        <div><h3 className="font-black text-white">Support</h3><div className="mt-4 space-y-3 text-sm"><a className="block hover:text-white" href="mailto:support@civichero.local">Contact support</a><Link className="block hover:text-white" to={{ pathname: ROUTE_PATHS.home, hash: '#how-it-works' }}>How it works</Link><Link className="block hover:text-white" to={ROUTE_PATHS.login}>Secure login</Link><span className="block">© 2026 CivicHero</span></div></div>
      </div>
      <div className="border-t border-white/10 px-4 py-5 text-center text-xs text-slate-500">Evidence-led civic service · Privacy-conscious public data · Role-based access</div>
    </footer>
  );
}
