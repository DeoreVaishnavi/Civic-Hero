import { useEffect } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import Footer from '../components/layout/Footer.jsx';
import Header from '../components/layout/Header.jsx';
import GeneralChatbotWidget from '../components/public/GeneralChatbotWidget.jsx';

function ScrollToPublicSection() {
  const location = useLocation();

  useEffect(() => {
    if (!location.hash) return;

    let attempts = 0;
    const scrollToTarget = () => {
      const target = document.getElementById(location.hash.slice(1));
      if (target) {
        target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        return;
      }

      attempts += 1;
      if (attempts < 8) window.setTimeout(scrollToTarget, 75);
    };

    window.requestAnimationFrame(scrollToTarget);
  }, [location.pathname, location.hash]);

  return null;
}

export default function PublicLayout() {
  return (
    <div className="civic-public min-h-screen bg-white">
      <ScrollToPublicSection />
      <Header />
      <main><Outlet /></main>
      <Footer />
      <GeneralChatbotWidget />
    </div>
  );
}
