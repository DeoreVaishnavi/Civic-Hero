import { Navigate, Route, Routes } from 'react-router-dom';
import HomePage from '../pages/public/HomePage.jsx';
import { ROUTE_PATHS } from './routePaths.js';

export default function AppRoutes() {
  return (
    <Routes>
      <Route path={ROUTE_PATHS.home} element={<HomePage />} />
      <Route path="*" element={<Navigate to={ROUTE_PATHS.home} replace />} />
    </Routes>
  );
}
