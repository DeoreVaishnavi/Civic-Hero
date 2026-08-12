import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext.jsx';
import AdminLayout from '../layouts/AdminLayout.jsx';
import CitizenLayout from '../layouts/CitizenLayout.jsx';
import OfficerLayout from '../layouts/OfficerLayout.jsx';
import PublicLayout from '../layouts/PublicLayout.jsx';
import SupervisorLayout from '../layouts/SupervisorLayout.jsx';
import AccessDeniedPage from '../pages/common/AccessDeniedPage.jsx';
import AdminDashboard from '../pages/admin/AdminDashboard.jsx';
import UserManagement from '../pages/admin/UserManagement.jsx';
import CitizenDashboard from '../pages/citizen/CitizenDashboard.jsx';
import CommonIssues from '../pages/citizen/CommonIssues.jsx';
import ComplaintDetails from '../pages/citizen/ComplaintDetails.jsx';
import MyComplaints from '../pages/citizen/MyComplaints.jsx';
import Profile from '../pages/citizen/Profile.jsx';
import ReportComplaint from '../pages/citizen/ReportComplaint.jsx';
import OfficerDashboard from '../pages/officer/OfficerDashboard.jsx';
import HomePage from '../pages/public/HomePage.jsx';
import LoginPage from '../pages/public/LoginPage.jsx';
import RegisterPage from '../pages/public/RegisterPage.jsx';
import VerifyEmailPage from '../pages/public/VerifyEmailPage.jsx';
import SupervisorDashboard from '../pages/supervisor/SupervisorDashboard.jsx';
import { dashboardForRole } from '../utils/roleRouting.js';
import { ROUTE_PATHS } from './routePaths.js';

function ProtectedRoute() {
  const { isAuthenticated, isInitializing } = useAuth();
  const location = useLocation();
  if (isInitializing) return <div className="min-h-screen bg-slate-950 px-6 py-20 text-slate-300">Restoring your secure session…</div>;
  return isAuthenticated ? <Outlet /> : <Navigate to={ROUTE_PATHS.login} replace state={{ from: location.pathname }} />;
}
function RoleGuard({ roles }) {
  const { user } = useAuth();
  return roles.some((role) => role.toLowerCase() === (user?.role || '').toLowerCase()) ? <Outlet /> : <Navigate to={ROUTE_PATHS.accessDenied} replace />;
}
function DashboardRedirect() {
  const { user } = useAuth();
  return <Navigate to={dashboardForRole(user?.role)} replace />;
}

export default function AppRoutes() {
  return (
    <Routes>
      <Route element={<PublicLayout />}><Route path={ROUTE_PATHS.home} element={<HomePage />} /><Route path={ROUTE_PATHS.login} element={<LoginPage />} /><Route path={ROUTE_PATHS.register} element={<RegisterPage />} /><Route path={ROUTE_PATHS.verifyEmail} element={<VerifyEmailPage />} /></Route>
      <Route element={<ProtectedRoute />}>
        <Route path={ROUTE_PATHS.dashboard} element={<DashboardRedirect />} /><Route path={ROUTE_PATHS.accessDenied} element={<AccessDeniedPage />} />
        <Route element={<RoleGuard roles={['Citizen']} />}><Route path="/citizen" element={<CitizenLayout />}><Route index element={<CitizenDashboard />} /><Route path="report" element={<ReportComplaint />} /><Route path="complaints" element={<MyComplaints />} /><Route path="complaints/:id" element={<ComplaintDetails />} /><Route path="nearby" element={<CommonIssues />} /><Route path="profile" element={<Profile />} /></Route></Route>
        <Route element={<RoleGuard roles={['Officer']} />}><Route path="/officer" element={<OfficerLayout />}><Route index element={<OfficerDashboard />} /><Route path="profile" element={<Profile />} /></Route></Route>
        <Route element={<RoleGuard roles={['Supervisor']} />}><Route path="/supervisor" element={<SupervisorLayout />}><Route index element={<SupervisorDashboard />} /><Route path="profile" element={<Profile />} /></Route></Route>
        <Route element={<RoleGuard roles={['Admin', 'SuperAdmin']} />}><Route path="/admin" element={<AdminLayout />}><Route index element={<AdminDashboard />} /><Route path="users" element={<UserManagement />} /><Route path="profile" element={<Profile />} /></Route></Route>
      </Route>
      <Route path="*" element={<Navigate to={ROUTE_PATHS.home} replace />} />
    </Routes>
  );
}
