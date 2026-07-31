import { Outlet, Route, Routes } from "react-router";
import ErrorBoundary from "../components/common/ErrorBoundary";
import AppLayout from "../components/layout/AppLayout";
import PublicLayout from "../components/layout/PublicLayout";
import AdminDashboardPage from "../features/admin/pages/AdminDashboardPage";
import AuditLogsPage from "../features/admin/pages/AuditLogsPage";
import CategoryManagementPage from "../features/admin/pages/CategoryManagementPage";
import DepartmentManagementPage from "../features/admin/pages/DepartmentManagementPage";
import HeatMapPage from "../features/admin/pages/HeatMapPage";
import ReportsPage from "../features/admin/pages/ReportsPage";
import UserManagementPage from "../features/admin/pages/UserManagementPage";
import WardManagementPage from "../features/admin/pages/WardManagementPage";
import AnonymousComplaintPage from "../features/anonymous/AnonymousComplaintPage";
import AnonymousTrackingPage from "../features/anonymous/AnonymousTrackingPage";
import LoginPage from "../features/auth/pages/LoginPage";
import RegisterPage from "../features/auth/pages/RegisterPage";
import CitizenDashboardPage from "../features/citizen/pages/CitizenDashboardPage";
import CommunityPage from "../features/community/CommunityPage";
import ComplaintDetailsPage from "../features/complaints/pages/ComplaintDetailsPage";
import CreateComplaintPage from "../features/complaints/pages/CreateComplaintPage";
import MyComplaintsPage from "../features/complaints/pages/MyComplaintsPage";
import DisputesPage from "../features/disputes/DisputesPage";
import NotificationsPage from "../features/notifications/NotificationsPage";
import ProfilePage from "../features/profile/ProfilePage";
import LeaderboardPage from "../features/rewards/LeaderboardPage";
import RewardsPage from "../features/rewards/RewardsPage";
import StaffComplaintsPage from "../features/staff/pages/StaffComplaintsPage";
import StaffDashboardPage from "../features/staff/pages/StaffDashboardPage";
import StaffMapPage from "../features/staff/pages/StaffMapPage";
import VisualReviewPage from "../features/staff/pages/VisualReviewPage";
import VerificationPage from "../features/verification/VerificationPage";
import useSignalR from "../hooks/useSignalR";
import AboutPage from "../pages/AboutPage";
import HelpPage from "../pages/HelpPage";
import HomePage from "../pages/HomePage";
import NotFoundPage from "../pages/NotFoundPage";
import PrivacyPage from "../pages/PrivacyPage";
import UnauthorizedPage from "../pages/UnauthorizedPage";
import ProtectedRoute from "./ProtectedRoute";
import RoleRoute from "./RoleRoute";

function RealtimeLayout(){ useSignalR(); return <Outlet/>; }

export default function AppRoutes(){return <ErrorBoundary><Routes>
  <Route element={<PublicLayout/>}>
    <Route index element={<HomePage/>}/><Route path="login" element={<LoginPage/>}/><Route path="register" element={<RegisterPage/>}/>
    <Route path="report-anonymously" element={<AnonymousComplaintPage/>}/><Route path="track-anonymous" element={<AnonymousTrackingPage/>}/>
    <Route path="about" element={<AboutPage/>}/><Route path="help" element={<HelpPage/>}/><Route path="privacy" element={<PrivacyPage/>}/><Route path="unauthorized" element={<UnauthorizedPage/>}/>
  </Route>
  <Route element={<ProtectedRoute/>}><Route element={<RealtimeLayout/>}>
    <Route element={<RoleRoute allowedRoles={["Citizen"]}/>}> <Route element={<AppLayout/>}>
      <Route path="citizen/dashboard" element={<CitizenDashboardPage/>}/><Route path="community" element={<CommunityPage/>}/><Route path="report-complaint" element={<CreateComplaintPage/>}/><Route path="my-complaints" element={<MyComplaintsPage/>}/><Route path="complaints/:complaintId" element={<ComplaintDetailsPage/>}/><Route path="notifications" element={<NotificationsPage/>}/><Route path="verify-resolutions" element={<VerificationPage/>}/><Route path="disputes" element={<DisputesPage/>}/><Route path="rewards" element={<RewardsPage/>}/><Route path="leaderboard" element={<LeaderboardPage/>}/><Route path="profile" element={<ProfilePage/>}/>
    </Route></Route>
    <Route element={<RoleRoute allowedRoles={["Staff"]}/>}> <Route element={<AppLayout/>}>
      <Route path="staff/dashboard" element={<StaffDashboardPage/>}/><Route path="staff/complaints" element={<StaffComplaintsPage/>}/><Route path="staff/complaints/:complaintId" element={<ComplaintDetailsPage/>}/><Route path="staff/map" element={<StaffMapPage/>}/><Route path="staff/visual-review" element={<VisualReviewPage/>}/><Route path="staff/notifications" element={<NotificationsPage/>}/><Route path="staff/profile" element={<ProfilePage/>}/>
    </Route></Route>
    <Route element={<RoleRoute allowedRoles={["Admin"]}/>}> <Route element={<AppLayout/>}>
      <Route path="admin/dashboard" element={<AdminDashboardPage/>}/><Route path="admin/complaints" element={<StaffComplaintsPage/>}/><Route path="admin/complaints/:complaintId" element={<ComplaintDetailsPage/>}/><Route path="admin/users" element={<UserManagementPage/>}/><Route path="admin/staff" element={<UserManagementPage staffOnly/>}/><Route path="admin/departments" element={<DepartmentManagementPage/>}/><Route path="admin/categories" element={<CategoryManagementPage/>}/><Route path="admin/wards" element={<WardManagementPage/>}/><Route path="admin/reports" element={<ReportsPage/>}/><Route path="admin/heat-map" element={<HeatMapPage/>}/><Route path="admin/visual-review" element={<VisualReviewPage/>}/><Route path="admin/audit-logs" element={<AuditLogsPage/>}/><Route path="admin/profile" element={<ProfilePage/>}/>
    </Route></Route>
  </Route></Route>
  <Route path="*" element={<PublicLayout/>}><Route path="*" element={<NotFoundPage/>}/></Route>
</Routes></ErrorBoundary>}
