import { Outlet } from 'react-router-dom';
import Footer from '../components/layout/Footer.jsx';
import Header from '../components/layout/Header.jsx';

export default function PublicLayout() {
  return (
    <div className="civic-public public-shell">
      <Header />
      <main className="public-main"><Outlet /></main>
      <Footer />
    </div>
  );
}
