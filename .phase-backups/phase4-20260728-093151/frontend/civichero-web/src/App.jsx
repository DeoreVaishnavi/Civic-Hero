import Footer from './components/layout/Footer.jsx';
import Header from './components/layout/Header.jsx';
import AppRoutes from './routes/AppRoutes.jsx';

export default function App() {
  return (
    <div className="flex min-h-screen flex-col bg-slate-950 text-slate-100">
      <Header />
      <main className="flex-1">
        <AppRoutes />
      </main>
      <Footer />
    </div>
  );
}
