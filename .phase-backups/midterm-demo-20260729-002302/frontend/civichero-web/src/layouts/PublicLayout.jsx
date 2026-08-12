import { Outlet } from 'react-router-dom';
import Footer from '../components/layout/Footer.jsx';
import Header from '../components/layout/Header.jsx';

export default function PublicLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-slate-950 text-slate-100">
      <Header />
      <main className="flex-1"><Outlet /></main>
      <Footer />
    </div>
  );
}
