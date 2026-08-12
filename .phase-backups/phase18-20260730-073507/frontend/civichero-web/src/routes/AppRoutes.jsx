import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext.jsx';
import AdminLayout from '../layouts/AdminLayout.jsx';
import CitizenLayout from '../layouts/CitizenLayout.jsx';
import OfficerLayout from '../layouts/OfficerLayout.jsx';
import PublicLayout from '../layouts/PublicLayout.jsx';
import SupervisorLayout from '../layouts/SupervisorLayout.jsx';
import AccessDeniedPage from '../pages/common/AccessDeniedPage.jsx';
import NotificationsPage from '../pages/common/NotificationsPage.jsx';
import AccountSecurity from '../pages/common/AccountSecurity.jsx';
import AdminDashboard from '../pages/admin/AdminDashboard.jsx';
import BroadcastNotifications from '../pages/admin/BroadcastNotifications.jsx';
import UserManagement from '../pages/admin/UserManagement.jsx';
import AppealManagement from '../pages/admin/AppealManagement.jsx';
import VerificationHistory from '../pages/admin/VerificationHistory.jsx';
import FraudReviewQueue from '../pages/admin/FraudReviewQueue.jsx';
import AnalyticsDashboard from '../pages/admin/AnalyticsDashboard.jsx';
import Reports from '../pages/admin/Reports.jsx';
import Governance from '../pages/admin/Governance.jsx';
import AuditLogs from '../pages/admin/AuditLogs.jsx';
import SystemHealth from '../pages/admin/SystemHealth.jsx';
import SecurityCenter from '../pages/admin/SecurityCenter.jsx';
import QualityCenter from '../pages/admin/QualityCenter.jsx';
import ReleaseCenter from '../pages/admin/ReleaseCenter.jsx';
import LaunchCenter from '../pages/admin/LaunchCenter.jsx';
import IntegrationCenter from '../pages/admin/IntegrationCenter.jsx';
import CitizenDashboard from '../pages/citizen/CitizenDashboard.jsx';
import RewardsOverview from '../pages/citizen/RewardsOverview.jsx';
import Chatbot from '../pages/citizen/Chatbot.jsx';
import Leaderboard from '../pages/citizen/Leaderboard.jsx';
import CitizenVerification from '../pages/citizen/CitizenVerification.jsx';
import DisputePage from '../pages/citizen/DisputePage.jsx';
import CommonIssues from '../pages/citizen/CommonIssues.jsx';
import ComplaintDetails from '../pages/citizen/ComplaintDetails.jsx';
import MyComplaints from '../pages/citizen/MyComplaints.jsx';
import Profile from '../pages/citizen/Profile.jsx';
import ReportComplaint from '../pages/citizen/ReportComplaint.jsx';
import AssignmentDetails from '../pages/officer/AssignmentDetails.jsx';
import OfficerDashboard from '../pages/officer/OfficerDashboard.jsx';
import UploadResolution from '../pages/officer/UploadResolution.jsx';
import WorkQueue from '../pages/officer/WorkQueue.jsx';
import HomePage from '../pages/public/HomePage.jsx';
import LoginPage from '../pages/public/LoginPage.jsx';
import RegisterPage from '../pages/public/RegisterPage.jsx';
import VerifyEmailPage from '../pages/public/VerifyEmailPage.jsx';
import AssignmentQueue from '../pages/supervisor/AssignmentQueue.jsx';
import ManageAssignment from '../pages/supervisor/ManageAssignment.jsx';
import OverdueAssignments from '../pages/supervisor/OverdueAssignments.jsx';
import SupervisorDashboard from '../pages/supervisor/SupervisorDashboard.jsx';
import VerificationQueue from '../pages/supervisor/VerificationQueue.jsx';
import DisputeQueue from '../pages/supervisor/DisputeQueue.jsx';
import DisputeReview from '../pages/supervisor/DisputeReview.jsx';
import TeamAnalytics from '../pages/supervisor/TeamAnalytics.jsx';
import { dashboardForRole } from '../utils/roleRouting.js';
import { ROUTE_PATHS } from './routePaths.js';

function ProtectedRoute() { const { isAuthenticated, isInitializing } = useAuth(); const location = useLocation(); if (isInitializing) return <div className="min-h-screen bg-slate-950 px-6 py-20 text-slate-300">Restoring your secure session…</div>; return isAuthenticated ? <Outlet /> : <Navigate to={ROUTE_PATHS.login} replace state={{ from: location.pathname }} />; }
function RoleGuard({ roles }) { const { user } = useAuth(); return roles.some((role) => role.toLowerCase() === (user?.role || '').toLowerCase()) ? <Outlet /> : <Navigate to={ROUTE_PATHS.accessDenied} replace />; }
function DashboardRedirect() { const { user } = useAuth(); return <Navigate to={dashboardForRole(user?.role)} replace />; }

export default function AppRoutes() {
  return <Routes>
    <Route element={<PublicLayout />}><Route path={ROUTE_PATHS.home} element={<HomePage />} /><Route path={ROUTE_PATHS.login} element={<LoginPage />} /><Route path={ROUTE_PATHS.register} element={<RegisterPage />} /><Route path={ROUTE_PATHS.verifyEmail} element={<VerifyEmailPage />} /></Route>
    <Route element={<ProtectedRoute />}><Route path={ROUTE_PATHS.dashboard} element={<DashboardRedirect />} /><Route path={ROUTE_PATHS.accessDenied} element={<AccessDeniedPage />} />
      <Route element={<RoleGuard roles={['Citizen']} />}><Route path="/citizen" element={<CitizenLayout />}><Route index element={<CitizenDashboard />} /><Route path="report" element={<ReportComplaint />} /><Route path="complaints" element={<MyComplaints />} /><Route path="complaints/:id" element={<ComplaintDetails />} /><Route path="nearby" element={<CommonIssues />} /><Route path="verifications" element={<CitizenVerification />} /><Route path="disputes" element={<DisputePage />} /><Route path="rewards" element={<RewardsOverview />} /><Route path="leaderboard" element={<Leaderboard />} /><Route path="chatbot" element={<Chatbot />} /><Route path="notifications" element={<NotificationsPage />} /><Route path="profile" element={<Profile />} /><Route path="security" element={<AccountSecurity />} /></Route></Route>
      <Route element={<RoleGuard roles={['Officer']} />}><Route path="/officer" element={<OfficerLayout />}><Route index element={<OfficerDashboard />} /><Route path="assignments" element={<WorkQueue />} /><Route path="assignments/:complaintId" element={<AssignmentDetails />} /><Route path="assignments/:complaintId/resolve" element={<UploadResolution />} /><Route path="notifications" element={<NotificationsPage />} /><Route path="profile" element={<Profile />} /><Route path="security" element={<AccountSecurity />} /></Route></Route>
      <Route element={<RoleGuard roles={['Supervisor']} />}><Route path="/supervisor" element={<SupervisorLayout />}><Route index element={<SupervisorDashboard />} /><Route path="assignments" element={<AssignmentQueue />} /><Route path="assignments/:complaintId" element={<ManageAssignment />} /><Route path="overdue" element={<OverdueAssignments />} /><Route path="verifications" element={<VerificationQueue />} /><Route path="disputes" element={<DisputeQueue />} /><Route path="disputes/:id" element={<DisputeReview />} /><Route path="analytics" element={<TeamAnalytics />} /><Route path="notifications" element={<NotificationsPage />} /><Route path="profile" element={<Profile />} /><Route path="security" element={<AccountSecurity />} /></Route></Route>
      <Route element={<RoleGuard roles={['Admin', 'SuperAdmin']} />}><Route path="/admin" element={<AdminLayout />}><Route index element={<AdminDashboard />} /><Route path="users" element={<UserManagement />} /><Route path="appeals" element={<AppealManagement />} /><Route path="verifications" element={<VerificationHistory />} /><Route path="broadcast" element={<BroadcastNotifications />} /><Route path="ai-review" element={<FraudReviewQueue />} /><Route path="analytics" element={<AnalyticsDashboard />} /><Route path="reports" element={<Reports />} /><Route path="governance" element={<Governance />} /><Route path="audit-logs" element={<AuditLogs />} /><Route path="system-health" element={<SystemHealth />} /><Route path="security-center" element={<SecurityCenter />} /><Route path="quality-center" element={<QualityCenter />} /><Route path="release-center" element={<ReleaseCenter />} /><Route path="launch-center" element={<LaunchCenter />} /><Route path="integration-center" element={<IntegrationCenter />} /><Route path="notifications" element={<NotificationsPage />} /><Route path="profile" element={<Profile />} /><Route path="security" element={<AccountSecurity />} /></Route></Route>
    </Route>
    <Route path="*" element={<Navigate to={ROUTE_PATHS.home} replace />} />
  </Routes>;
}
