import axios, { type AxiosResponse } from "axios"
import { clearStoredAuth, TOKEN_KEY } from "@/lib/auth-storage"
import type { ApiEnvelope } from "@/types"

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || "http://localhost:5090/api/v1",
  headers: { Accept: "application/json" },
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (
      axios.isAxiosError(error)
      && error.response?.status === 401
      && error.config?.url !== "/auth/logout"
    ) {
      clearStoredAuth()
      window.dispatchEvent(new Event("inventara:unauthorized"))
      if (window.location.pathname !== "/login") window.location.assign("/login")
    }
    return Promise.reject(error)
  },
)

export function unwrap<T>(response: AxiosResponse<ApiEnvelope<T>>) {
  return response.data.data
}

export function getErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const body = error.response?.data as
      | { detail?: string; message?: string; errors?: Record<string, string[]> }
      | undefined
    const validationMessage = body?.errors
      ? Object.values(body.errors).flat().join(" ")
      : undefined
    return validationMessage || body?.detail || body?.message || error.message
  }
  return error instanceof Error ? error.message : "Terjadi kesalahan. Silakan coba lagi."
}

export default api
