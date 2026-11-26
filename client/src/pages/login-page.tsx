import { useState, type FormEvent } from "react"
import { Activity, ArrowRight, Eye, EyeOff, LockKeyhole, Mail } from "lucide-react"
import { Navigate, useLocation, useNavigate } from "react-router-dom"
import { useAuth } from "@/auth/auth-context"
import { roleHome } from "@/auth/protected-route"
import { getErrorMessage } from "@/lib/api"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"

export function LoginPage() {
  const { login, isAuthenticated, role } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState("")
  const [password, setPassword] = useState("")
  const [showPassword, setShowPassword] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  if (isAuthenticated) return <Navigate to={roleHome(role)} replace />

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      const user = await login(email.trim(), password)
      const state = location.state as { from?: { pathname?: string } } | null
      navigate(state?.from?.pathname || roleHome(user.role), { replace: true })
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="relative flex min-h-screen items-center justify-center overflow-hidden bg-slate-50 px-4 py-10">
      <div className="absolute -top-36 -right-36 size-[30rem] rounded-full bg-indigo-100/70 blur-3xl" />
      <div className="absolute -bottom-48 -left-32 size-[32rem] rounded-full bg-sky-100/50 blur-3xl" />
      <div className="relative grid w-full max-w-5xl overflow-hidden rounded-[2rem] border border-slate-200/80 bg-white shadow-2xl shadow-slate-300/30 lg:grid-cols-[1fr_0.85fr]">
        <section className="hidden flex-col justify-between bg-gradient-to-br from-indigo-700 via-indigo-700 to-violet-800 p-12 text-white lg:flex">
          <div className="flex items-center gap-3">
            <div className="grid size-10 place-items-center rounded-xl bg-white/15"><Activity className="size-5" /></div>
            <span className="text-lg font-semibold tracking-tight">Inventara</span>
          </div>
          <div className="max-w-md">
            <p className="text-sm font-medium text-indigo-200">ASSET MANAGEMENT WORKSPACE</p>
            <h1 className="mt-4 text-4xl leading-tight font-semibold tracking-tight">Semua aset, dalam kendali.</h1>
            <p className="mt-4 text-sm leading-7 text-indigo-100/80">Kelola inventaris, pantau peminjaman, dan jaga setiap aset tetap siap digunakan.</p>
          </div>
          <p className="text-xs text-indigo-200/70">Ruang kerja inventaris untuk organisasi Anda.</p>
        </section>
        <section className="flex items-center px-7 py-10 sm:px-12 lg:px-11">
          <div className="mx-auto w-full max-w-sm">
            <div className="mb-9 lg:hidden">
              <div className="mb-5 grid size-11 place-items-center rounded-xl bg-indigo-600 text-white"><Activity className="size-5" /></div>
              <p className="font-semibold text-slate-900">Inventara</p>
            </div>
            <p className="text-xs font-semibold tracking-[0.16em] text-indigo-600 uppercase">Selamat datang</p>
            <h2 className="mt-2 text-3xl font-semibold tracking-tight text-slate-900">Masuk ke akun</h2>
            <p className="mt-2 text-sm text-slate-500">Masukkan kredensial untuk membuka workspace Anda.</p>
            <form className="mt-8 space-y-5" onSubmit={(event) => void submit(event)}>
              <div className="space-y-2">
                <Label htmlFor="login-email" className="text-xs font-medium text-slate-600">Email</Label>
                <div className="relative">
                  <Mail className="absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-slate-400" />
                  <Input id="login-email" type="email" autoComplete="username" required value={email} onChange={(event) => setEmail(event.target.value)} placeholder="nama@organisasi.com" className="h-11 rounded-xl border-slate-200 pl-10" />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="login-password" className="text-xs font-medium text-slate-600">Kata sandi</Label>
                <div className="relative">
                  <LockKeyhole className="absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-slate-400" />
                  <Input id="login-password" type={showPassword ? "text" : "password"} autoComplete="current-password" required value={password} onChange={(event) => setPassword(event.target.value)} placeholder="Masukkan kata sandi" className="h-11 rounded-xl border-slate-200 pr-11 pl-10" />
                  <button type="button" aria-label={showPassword ? "Sembunyikan kata sandi" : "Tampilkan kata sandi"} onClick={() => setShowPassword((value) => !value)} className="absolute top-1/2 right-3.5 -translate-y-1/2 text-slate-400 hover:text-slate-700">
                    {showPassword ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                  </button>
                </div>
              </div>
              {error && <p role="alert" className="rounded-xl border border-rose-100 bg-rose-50 px-3.5 py-3 text-sm text-rose-700">{error}</p>}
              <Button type="submit" className="h-11 w-full rounded-xl shadow-sm shadow-indigo-200" disabled={submitting}>
                {submitting ? "Memverifikasi..." : "Masuk"}
                {!submitting && <ArrowRight className="size-4" />}
              </Button>
            </form>
            <p className="mt-7 text-center text-xs text-slate-400">Akses diberikan oleh administrator organisasi Anda.</p>
          </div>
        </section>
      </div>
    </main>
  )
}
