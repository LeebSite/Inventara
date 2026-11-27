import { useState } from "react"
import {
  Archive,
  ChevronDown,
  ClipboardCheck,
  LayoutDashboard,
  LogOut,
  Menu,
  Package,
  ScrollText,
  Settings2,
  Wrench,
} from "lucide-react"
import { NavLink, Outlet, useLocation } from "react-router-dom"
import inventaraIcon from "@/assets/inventara-icon.svg"
import { useAuth } from "@/auth/auth-context"
import type { Role } from "@/types"
import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Button } from "@/components/ui/button"
import { Separator } from "@/components/ui/separator"
import { Sheet, SheetContent, SheetTitle, SheetTrigger } from "@/components/ui/sheet"

const navigation = [
  { to: "/dashboard", label: "Dashboard", icon: LayoutDashboard, roles: ["Admin", "Staff"] },
  { to: "/assets", label: "Aset", icon: Package, roles: ["Admin", "Staff", "User"] },
  { to: "/my-loans", label: "Pinjaman Saya", icon: Archive, roles: ["Admin", "Staff", "User"] },
  { to: "/approvals", label: "Persetujuan", icon: ClipboardCheck, roles: ["Admin", "Staff"] },
  { to: "/maintenance", label: "Maintenance", icon: Wrench, roles: ["Admin", "Staff"] },
  { to: "/master-data", label: "Master Data", icon: Settings2, roles: ["Admin"] },
  { to: "/audit-logs", label: "Audit Log", icon: ScrollText, roles: ["Admin"] },
] satisfies { to: string; label: string; icon: typeof Package; roles: Role[] }[]

const titles: Record<string, string> = {
  "/dashboard": "Dashboard",
  "/assets": "Aset",
  "/my-loans": "Pinjaman Saya",
  "/approvals": "Persetujuan",
  "/maintenance": "Maintenance",
  "/master-data": "Master Data",
  "/audit-logs": "Audit Log",
}

function Navigation({ onNavigate }: { onNavigate?: () => void }) {
  const { role } = useAuth()
  return (
    <nav className="space-y-1 px-3">
      {navigation
        .filter((item) => role && item.roles.includes(role))
        .map(({ to, label, icon: Icon }) => (
          <NavLink
            key={to}
            to={to}
            onClick={onNavigate}
            className={({ isActive }) =>
              `group flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium transition ${
                isActive
                  ? "bg-teal-50 text-teal-700"
                  : "text-slate-500 hover:bg-slate-100 hover:text-slate-900"
              }`
            }
          >
            <Icon className="size-[18px] opacity-80" />
            {label}
          </NavLink>
        ))}
    </nav>
  )
}

function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  return (
    <div className="flex h-full flex-col">
      <div className="flex h-[76px] items-center gap-3 px-6">
        <img src={inventaraIcon} alt="" className="size-9" />
        <div>
          <p className="text-[15px] font-semibold tracking-tight text-slate-900">Inventara</p>
          <p className="text-[11px] font-medium tracking-wide text-slate-400 uppercase">Asset workspace</p>
        </div>
      </div>
      <Separator />
      <div className="px-6 pt-6 pb-3 text-[10px] font-semibold tracking-[0.16em] text-slate-400 uppercase">
        Menu
      </div>
      <Navigation onNavigate={onNavigate} />
      <div className="mt-auto p-4">
        <div className="rounded-2xl border border-teal-100 bg-teal-50/60 p-4">
          <p className="text-xs font-semibold text-slate-900">Inventara workspace</p>
          <p className="mt-1 text-xs leading-relaxed text-teal-700/70">
            Kelola aset dan aktivitas peminjaman dalam satu tempat.
          </p>
        </div>
      </div>
    </div>
  )
}

export function AppShell() {
  const { user, role, logout } = useAuth()
  const location = useLocation()
  const [mobileOpen, setMobileOpen] = useState(false)
  const title = titles[location.pathname] ?? (location.pathname.startsWith("/assets/") ? "Detail Aset" : "Inventara")
  const initials = user?.fullName
    .split(" ")
    .map((part) => part[0])
    .slice(0, 2)
    .join("")
    .toUpperCase()

  return (
    <div className="min-h-screen bg-slate-50/80">
      <aside className="fixed inset-y-0 left-0 z-30 hidden w-[252px] border-r border-slate-200/80 bg-white lg:block">
        <SidebarContent />
      </aside>
      <div className="lg:pl-[252px]">
        <header className="sticky top-0 z-20 flex h-[76px] items-center justify-between border-b border-slate-200/80 bg-white/90 px-4 backdrop-blur-md sm:px-7">
          <div className="flex items-center gap-3">
            <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
              <SheetTrigger asChild>
                <Button variant="outline" size="icon" className="lg:hidden">
                  <Menu />
                  <span className="sr-only">Buka navigasi</span>
                </Button>
              </SheetTrigger>
              <SheetContent side="left" className="w-[270px] p-0">
                <SheetTitle className="sr-only">Navigasi Inventara</SheetTitle>
                <SidebarContent onNavigate={() => setMobileOpen(false)} />
              </SheetContent>
            </Sheet>
            <div>
              <p className="text-[11px] font-medium text-slate-400">Workspace / {title}</p>
              <h2 className="mt-0.5 text-sm font-semibold text-slate-800">{title}</h2>
            </div>
          </div>
          <div className="flex items-center gap-3">
            <div className="hidden text-right sm:block">
              <p className="text-sm font-semibold text-slate-800">{user?.fullName ?? "Pengguna"}</p>
              <p className="text-xs text-slate-400">{role}</p>
            </div>
            <Avatar className="size-9 border border-teal-100 bg-teal-50">
              <AvatarFallback className="bg-teal-50 text-xs font-semibold text-teal-700">
                {initials || "IN"}
              </AvatarFallback>
            </Avatar>
            <Button
              variant="ghost"
              size="icon"
              className="text-slate-500"
              title="Keluar"
              onClick={logout}
            >
              <LogOut className="size-[18px]" />
            </Button>
            <ChevronDown className="hidden size-4 text-slate-300 sm:block" />
          </div>
        </header>
        <main className="mx-auto w-full max-w-[1500px] px-4 py-7 sm:px-7 lg:px-9">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
