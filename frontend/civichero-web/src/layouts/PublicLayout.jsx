import { Outlet } from 'react-router-dom';
import Footer from '../components/layout/Footer.jsx';
import Header from '../components/layout/Header.jsx';
import GeneralChatbotWidget from '../components/public/GeneralChatbotWidget.jsx';

export default function PublicLayout() {
  return (
    <div className="civic-public min-h-screen bg-white">
      <Header />
      <main><Outlet /></main>
      <Footer />
      <GeneralChatbotWidget />
    </div>
  );
}
