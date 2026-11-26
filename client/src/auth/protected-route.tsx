import { Navigate, Outlet, useLocation } from "react-router-dom"
import { useAuth } from "@/auth/auth-context"
import type { Role } from "@/types"

export function roleHome(role: Role | null) {
  return role === "User" ? "/my-loans" : "/dashboard"
}

export function ProtectedRoute({ roles }: { roles?: Role[] }) {
  const { isAuthenticated, role } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) return <Navigate to="/login" replace state={{ from: location }} />
  if (roles && role && !roles.includes(role)) return <Navigate to={roleHome(role)} replace />
  return <Outlet />
}
