import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react"
import api, { unwrap } from "@/lib/api"
import { clearStoredAuth, TOKEN_KEY, USER_KEY } from "@/lib/auth-storage"
import type { ApiEnvelope, LoginResponse, Role, UserProfile } from "@/types"

interface AuthContextValue {
  token: string | null
  user: UserProfile | null
  role: Role | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<UserProfile>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

function decodeRole(token: string): Role | null {
  try {
    const encodedPayload = token.split(".")[1]
    if (!encodedPayload) return null
    const normalized = encodedPayload.replace(/-/g, "+").replace(/_/g, "/")
    const payload = JSON.parse(atob(normalized)) as { role?: string; exp?: number }
    if (payload.exp && payload.exp <= Date.now() / 1000) return null
    return payload.role === "Admin" || payload.role === "Staff" || payload.role === "User"
      ? payload.role
      : null
  } catch {
    return null
  }
}

function readStoredUser(): UserProfile | null {
  try {
    const value = localStorage.getItem(USER_KEY)
    return value ? (JSON.parse(value) as UserProfile) : null
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(TOKEN_KEY))
  const [user, setUser] = useState<UserProfile | null>(readStoredUser)
  const role = token ? decodeRole(token) : null

  const logout = useCallback(async () => {
    try {
      await api.post("/auth/logout")
    } catch {
      // Local logout must still complete if the token is expired or the API is unavailable.
    } finally {
      clearStoredAuth()
      setToken(null)
      setUser(null)
    }
  }, [])

  const login = useCallback(async (email: string, password: string) => {
    const response = await api.post<ApiEnvelope<LoginResponse>>("/auth/login", { email, password })
    const result = unwrap(response)
    localStorage.setItem(TOKEN_KEY, result.accessToken)
    localStorage.setItem(USER_KEY, JSON.stringify(result.user))
    setToken(result.accessToken)
    setUser(result.user)
    return result.user
  }, [])

  useEffect(() => {
    const handleUnauthorized = () => {
      setToken(null)
      setUser(null)
    }
    window.addEventListener("inventara:unauthorized", handleUnauthorized)
    return () => window.removeEventListener("inventara:unauthorized", handleUnauthorized)
  }, [])

  useEffect(() => {
    if (token && !role) logout()
  }, [token, role, logout])

  const value = useMemo<AuthContextValue>(
    () => ({
      token,
      user,
      role,
      isAuthenticated: Boolean(token && role),
      login,
      logout,
    }),
    [token, user, role, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) throw new Error("useAuth must be used within AuthProvider.")
  return context
}
