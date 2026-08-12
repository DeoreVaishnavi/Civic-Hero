import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext.jsx';
import CitizenDashboard from '../pages/citizen/CitizenDashboard.jsx';
import HomePage from '../pages/public/HomePage.jsx';
import LoginPage from '../pages/public/LoginPage.jsx';
import RegisterPage from '../pages/public/RegisterPage.jsx';
import VerifyEmailPage from '../pages/public/VerifyEmailPage.jsx';
import { ROUTE_PATHS } from './routePaths.js';

function ProtectedRoute({ children }) {
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();

  if (isInitializing) {
    return <div className="mx-auto max-w-6xl px-6 py-20 text-slate-300">Restoring your session…</div>;
  }

  return isAuthenticated
    ? children
    : <Navigate to={ROUTE_PATHS.login} replace state={{ from: location.pathname }} />;
}

export default function AppRoutes() {
  return (
    <Routes>
      <Route path={ROUTE_PATHS.home} element={<HomePage />} />
      <Route path={ROUTE_PATHS.login} element={<LoginPage />} />
      <Route path={ROUTE_PATHS.register} element={<RegisterPage />} />
      <Route path={ROUTE_PATHS.verifyEmail} element={<VerifyEmailPage />} />
      <Route
        path={ROUTE_PATHS.citizenDashboard}
        element={<ProtectedRoute><CitizenDashboard /></ProtectedRoute>}
      />
      <Route path="*" element={<Navigate to={ROUTE_PATHS.home} replace />} />
    </Routes>
  );
}
