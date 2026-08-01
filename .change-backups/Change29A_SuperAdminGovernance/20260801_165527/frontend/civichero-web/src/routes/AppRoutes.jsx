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
import MasterDataManagement from '../pages/admin/MasterDataManagement.jsx';
import AdminComplaintManagement from '../pages/admin/AdminComplaintManagement.jsx';
import StaffAccountManagement from '../pages/admin/StaffAccountManagement.jsx';
import AppealManagement from '../pages/admin/AppealManagement.jsx';
import VerificationHistory from '../pages/admin/VerificationHistory.jsx';
import FraudReviewQueue from '../pages/admin/FraudReviewQueue.jsx';
import AnalyticsDashboard from '../pages/admin/AnalyticsDashboard.jsx';
import Reports from '../pages/admin/Reports.jsx';
import RewardManagement from '../pages/admin/RewardManagement.jsx';
import Governance from '../pages/admin/Governance.jsx';
import AuditLogs from '../pages/admin/AuditLogs.jsx';
import SystemHealth from '../pages/admin/SystemHealth.jsx';
import SecurityCenter from '../pages/admin/SecurityCenter.jsx';
import QualityCenter from '../pages/admin/QualityCenter.jsx';
import ReleaseCenter from '../pages/admin/ReleaseCenter.jsx';
import LaunchCenter from '../pages/admin/LaunchCenter.jsx';
import IntegrationCenter from '../pages/admin/IntegrationCenter.jsx';
import FinalReleaseCenter from '../pages/admin/FinalReleaseCenter.jsx';
import CitizenDashboard from '../pages/citizen/CitizenDashboard.jsx';
import RewardsOverview from '../pages/citizen/RewardsOverview.jsx';
import Redemption from '../pages/citizen/Redemption.jsx';
import Chatbot from '../pages/citizen/Chatbot.jsx';
import Leaderboard from '../pages/citizen/Leaderboard.jsx';
import Heatmap from '../pages/citizen/Heatmap.jsx';
import UpvotePage from '../pages/citizen/UpvotePage.jsx';
import ComplaintTimeline from '../pages/citizen/ComplaintTimeline.jsx';
import CitizenVerification from '../pages/citizen/CitizenVerification.jsx';
import DisputePage from '../pages/citizen/DisputePage.jsx';
import CommonIssues from '../pages/citizen/CommonIssues.jsx';
import FollowingComplaints from '../pages/citizen/FollowingComplaints.jsx';
import ComplaintDetailsAddon from '../pages/citizen/ComplaintDetailsAddon.jsx';
import MyComplaints from '../pages/citizen/MyComplaints.jsx';
import Profile from '../pages/citizen/Profile.jsx';
import ReportComplaint from '../pages/citizen/ReportComplaint.jsx';
import AssignmentDetailsAddon from '../pages/officer/AssignmentDetailsAddon.jsx';
import OfficerDashboard from '../pages/officer/OfficerDashboard.jsx';
import OfficerWorkMap from '../pages/officer/OfficerWorkMap.jsx';
import OfficerDisputes from '../pages/officer/OfficerDisputes.jsx';
import UploadResolution from '../pages/officer/UploadResolution.jsx';
import WorkQueue from '../pages/officer/WorkQueue.jsx';
import HomePage from '../pages/public/HomePage.jsx';
import PublicIssuesPage from '../pages/public/PublicIssuesPage.jsx';
import ProjectsPage from '../pages/public/ProjectsPage.jsx';
import LoginPage from '../pages/public/LoginPage.jsx';
import ForgotPasswordPage from '../pages/public/ForgotPasswordPage.jsx';
import AnonymousReportPage from '../pages/public/AnonymousReportPage.jsx';
import AnonymousTrackPage from '../pages/public/AnonymousTrackPage.jsx';
import RegisterPage from '../pages/public/RegisterPage.jsx';
import VerifyEmailPage from '../pages/public/VerifyEmailPage.jsx';
import AssignmentQueue from '../pages/supervisor/AssignmentQueue.jsx';
import ManageAssignmentAddon from '../pages/supervisor/ManageAssignmentAddon.jsx';
import OverdueAssignments from '../pages/supervisor/OverdueAssignments.jsx';
import EscalationHistory from '../pages/supervisor/EscalationHistory.jsx';
import SupervisorDashboard from '../pages/supervisor/SupervisorDashboard.jsx';
import VerificationQueue from '../pages/supervisor/VerificationQueue.jsx';
import DisputeQueue from '../pages/supervisor/DisputeQueue.jsx';
import DisputeReview from '../pages/supervisor/DisputeReview.jsx';
import TeamAnalytics from '../pages/supervisor/TeamAnalytics.jsx';
import EmergencyReviewQueue from '../pages/supervisor/EmergencyReviewQueue.jsx';
import VisualVerificationQueue from '../pages/supervisor/VisualVerificationQueue.jsx';
import CommentModerationQueue from '../pages/supervisor/CommentModerationQueue.jsx';
import InitiativeEngagement from '../pages/supervisor/InitiativeEngagement.jsx';
import { dashboardForRole } from '../utils/roleRouting.js';
import { ROUTE_PATHS } from './routePaths.js';

function ProtectedRoute() { const { isAuthenticated, isInitializing } = useAuth(); const location = useLocation(); if (isInitializing) return <div className="min-h-screen bg-slate-950 px-6 py-20 text-slate-300">Restoring your secure session…</div>; return isAuthenticated ? <Outlet /> : <Navigate to={ROUTE_PATHS.login} replace state={{ from: location.pathname }} />; }
function RoleGuard({ roles }) { const { user } = useAuth(); return roles.some((role) => role.toLowerCase() === (user?.role || '').toLowerCase()) ? <Outlet /> : <Navigate to={ROUTE_PATHS.accessDenied} replace />; }
function DashboardRedirect() { const { user } = useAuth(); return <Navigate to={dashboardForRole(user?.role)} replace />; }

export default function AppRoutes() {
  return <Routes>
    <Route element={<PublicLayout />}><Route path={ROUTE_PATHS.home} element={<HomePage />} /><Route path={ROUTE_PATHS.publicIssues} element={<PublicIssuesPage />} /><Route path={ROUTE_PATHS.projects} element={<ProjectsPage />} /><Route path={ROUTE_PATHS.login} element={<LoginPage />} /><Route path="/forgot-password" element={<ForgotPasswordPage />} /><Route path={ROUTE_PATHS.register} element={<RegisterPage />} /><Route path={ROUTE_PATHS.verifyEmail} element={<VerifyEmailPage />} /><Route path={ROUTE_PATHS.anonymousReport} element={<AnonymousReportPage />} /><Route path={ROUTE_PATHS.anonymousTrack} element={<AnonymousTrackPage />} /></Route>
    <Route element={<ProtectedRoute />}><Route path={ROUTE_PATHS.dashboard} element={<DashboardRedirect />} /><Route path={ROUTE_PATHS.accessDenied} element={<AccessDeniedPage />} />
      <Route element={<RoleGuard roles={['Citizen']} />}><Route path="/citizen" element={<CitizenLayout />}><Route index element={<CitizenDashboard />} /><Route path="report" element={<ReportComplaint />} /><Route path="complaints" element={<MyComplaints />} /><Route path="complaints/:id" element={<ComplaintDetailsAddon />} /><Route path="nearby" element={<CommonIssues />} /><Route path="following" element={<FollowingComplaints />} /><Route path="verifications" element={<CitizenVerification />} /><Route path="disputes" element={<DisputePage />} /><Route path="rewards" element={<RewardsOverview />} /><Route path="rewards/:rewardId/redeem" element={<Redemption />} /><Route path="leaderboard" element={<Leaderboard />} /><Route path="heatmap" element={<Heatmap />} /><Route path="issues/:id" element={<UpvotePage />} /><Route path="complaints/:id/timeline" element={<ComplaintTimeline />} /><Route path="chatbot" element={<Chatbot />} /><Route path="notifications" element={<NotificationsPage />} /><Route path="profile" element={<Profile />} /><Route path="security" element={<AccountSecurity />} /></Route></Route>
      <Route element={<RoleGuard roles={['Officer']} />}><Route path="/officer" element={<OfficerLayout />}><Route index element={<OfficerDashboard />} /><Route path="assignments" element={<WorkQueue />} /><Route path="map" element={<OfficerWorkMap />} /><Route path="disputes" element={<OfficerDisputes />} /><Route path="assignments/:complaintId" element={<AssignmentDetailsAddon />} /><Route path="assignments/:complaintId/resolve" element={<UploadResolution />} /><Route path="notifications" element={<NotificationsPage />} /><Route path="profile" element={<Profile />} /><Route path="security" element={<AccountSecurity />} /></Route></Route>
      <Route element={<RoleGuard roles={['Supervisor']} />}><Route path="/supervisor" element={<SupervisorLayout />}><Route index element={<SupervisorDashboard />} /><Route path="assignments" element={<AssignmentQueue />} /><Route path="assignments/:complaintId" element={<ManageAssignmentAddon />} /><Route path="overdue" element={<OverdueAssignments />} /><Route path="escalations" element={<EscalationHistory />} /><Route path="emergency-reviews" element={<EmergencyReviewQueue />} /><Route path="comment-moderation" element={<CommentModerationQueue />} /><Route path="visual-verification" element={<VisualVerificationQueue />} /><Route path="verifications" element={<VerificationQueue />} /><Route path="disputes" element={<DisputeQueue />} /><Route path="disputes/:id" element={<DisputeReview />} /><Route path="analytics" element={<TeamAnalytics />} /><Route path="initiative-engagement" element={<InitiativeEngagement />} /><Route path="notifications" element={<NotificationsPage />} /><Route path="profile" element={<Profile />} /><Route path="security" element={<AccountSecurity />} /></Route></Route>
      <Route element={<RoleGuard roles={['Admin', 'SuperAdmin']} />}><Route path="/admin" element={<AdminLayout />}><Route index element={<AdminDashboard />} /><Route path="users" element={<UserManagement />} /><Route path="master-data" element={<MasterDataManagement />} /><Route path="complaints" element={<AdminComplaintManagement />} /><Route path="staff-accounts" element={<StaffAccountManagement />} /><Route path="emergency-reviews" element={<EmergencyReviewQueue />} /><Route path="comment-moderation" element={<CommentModerationQueue />} /><Route path="visual-verification" element={<VisualVerificationQueue />} /><Route path="appeals" element={<AppealManagement />} /><Route path="verifications" element={<VerificationHistory />} /><Route path="broadcast" element={<BroadcastNotifications />} /><Route path="ai-review" element={<FraudReviewQueue />} /><Route path="analytics" element={<AnalyticsDashboard />} /><Route path="reports" element={<Reports />} /><Route path="rewards" element={<RewardManagement />} /><Route path="governance" element={<Governance />} /><Route path="audit-logs" element={<AuditLogs />} /><Route path="system-health" element={<SystemHealth />} /><Route path="security-center" element={<SecurityCenter />} /><Route path="quality-center" element={<QualityCenter />} /><Route path="release-center" element={<ReleaseCenter />} /><Route path="launch-center" element={<LaunchCenter />} /><Route path="integration-center" element={<IntegrationCenter />} /><Route path="final-release" element={<FinalReleaseCenter />} /><Route path="notifications" element={<NotificationsPage />} /><Route path="profile" element={<Profile />} /><Route path="security" element={<AccountSecurity />} /></Route></Route>
    </Route>
    <Route path="*" element={<Navigate to={ROUTE_PATHS.home} replace />} />
  </Routes>;
}
