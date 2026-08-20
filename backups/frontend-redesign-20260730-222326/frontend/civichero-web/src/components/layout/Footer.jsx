import CivicLogo from '../ui/CivicLogo.jsx';

export default function Footer() {
  return (
    <footer className="site-footer">
      <div className="site-footer-inner">
        <div>
          <CivicLogo light />
          <p>Transparent civic reporting, accountable resolution and stronger communities.</p>
        </div>
        <div className="footer-links">
          <a href="#how-it-works">How it works</a>
          <a href="#city-impact">City impact</a>
          <a href="mailto:support@civichero.local">Contact</a>
          <span>© 2026 CivicHero</span>
        </div>
      </div>
    </footer>
  );
}
