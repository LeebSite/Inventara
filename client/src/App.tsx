import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom"
import { AuthProvider, useAuth } from "@/auth/auth-context"
import { ProtectedRoute, roleHome } from "@/auth/protected-route"
import { AppShell } from "@/components/app-shell"
import { AssetDetailPage } from "@/pages/asset-detail-page"
import { AssetsPage } from "@/pages/assets-page"
import { DashboardPage } from "@/pages/dashboard-page"
import { ApprovalsPage } from "@/pages/loans-pages"
import { MyLoansPage } from "@/pages/loans-pages"
import { LoginPage } from "@/pages/login-page"
import { MaintenancePage } from "@/pages/maintenance-page"
import { MasterDataPage } from "@/pages/master-data-page"
import { AuditLogsPage } from "@/pages/audit-logs-page"

function HomeRedirect() {
  const { role, isAuthenticated } = useAuth()
  return <Navigate to={isAuthenticated ? roleHome(role) : "/login"} replace />
}

function ApplicationRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<ProtectedRoute />}>
        <Route element={<AppShell />}>
          <Route path="/" element={<HomeRedirect />} />
          <Route path="/assets" element={<AssetsPage />} />
          <Route path="/assets/:id" element={<AssetDetailPage />} />
          <Route path="/my-loans" element={<MyLoansPage />} />
          <Route element={<ProtectedRoute roles={["Admin", "Staff"]} />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/approvals" element={<ApprovalsPage />} />
            <Route path="/maintenance" element={<MaintenancePage />} />
          </Route>
          <Route element={<ProtectedRoute roles={["Admin"]} />}>
            <Route path="/master-data" element={<MasterDataPage />} />
            <Route path="/audit-logs" element={<AuditLogsPage />} />
          </Route>
          <Route path="*" element={<HomeRedirect />} />
        </Route>
      </Route>
      <Route path="*" element={<HomeRedirect />} />
    </Routes>
  )
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <ApplicationRoutes />
      </AuthProvider>
    </BrowserRouter>
  )
}
