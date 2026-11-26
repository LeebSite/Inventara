import { useEffect, useState } from "react"
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts"
import {
  AlertTriangle,
  ArrowUpRight,
  Box,
  CircleCheck,
  Clock3,
  Wrench,
} from "lucide-react"
import api, { getErrorMessage, unwrap } from "@/lib/api"
import { formatDate } from "@/lib/format"
import type { ApiEnvelope, CategoryCount, DashboardSummary, Loan, Page } from "@/types"
import { PageHeader } from "@/components/page-header"
import { ErrorState, LoadingBlock } from "@/components/feedback"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Link } from "react-router-dom"

const chartColors = ["#4f46e5", "#0ea5e9", "#f59e0b", "#e879f9", "#10b981", "#64748b"]

export function DashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)
  const [categories, setCategories] = useState<CategoryCount[]>([])
  const [overdue, setOverdue] = useState<Loan[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  async function loadDashboard() {
    setLoading(true)
    setError(null)
    try {
      const [summaryResponse, categoryResponse, overdueResponse] = await Promise.all([
        api.get<ApiEnvelope<DashboardSummary>>("/dashboard/summary"),
        api.get<ApiEnvelope<CategoryCount[]>>("/dashboard/assets-by-category"),
        api.get<ApiEnvelope<Page<Loan>>>("/loans/overdue", { params: { page: 1, pageSize: 5 } }),
      ])
      setSummary(unwrap(summaryResponse))
      setCategories(unwrap(categoryResponse))
      setOverdue(unwrap(overdueResponse).items)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadDashboard()
  }, [])

  return (
    <>
      <PageHeader
        eyebrow="Ringkasan inventaris"
        title="Selamat datang kembali"
        description="Pantau kondisi aset dan aktivitas peminjaman organisasi."
        actions={
          <Button asChild variant="outline" className="rounded-xl">
            <Link to="/assets">Lihat aset <ArrowUpRight className="ml-1 size-4" /></Link>
          </Button>
        }
      />
      {loading ? (
        <LoadingBlock />
      ) : error ? (
        <ErrorState message={error} onRetry={() => void loadDashboard()} />
      ) : summary ? (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <MetricCard title="Total aset" value={summary.totalAssets} note="Seluruh aset terdaftar" icon={Box} iconClass="bg-indigo-50 text-indigo-600" />
            <MetricCard title="Pinjaman aktif" value={summary.activeLoans} note={`${summary.onLoanAssets} aset sedang dipinjam`} icon={CircleCheck} iconClass="bg-sky-50 text-sky-600" />
            <MetricCard title="Sudah terlambat" value={summary.overdueLoans} note="Perlu ditindaklanjuti" icon={AlertTriangle} iconClass="bg-rose-50 text-rose-600" />
            <MetricCard title="Maintenance berjalan" value={summary.activeMaintenances} note={`${summary.maintenanceAssets} aset dalam maintenance`} icon={Wrench} iconClass="bg-orange-50 text-orange-600" />
          </div>
          <div className="mt-6 grid gap-5 xl:grid-cols-[1.05fr_0.95fr]">
            <Card className="rounded-2xl border-slate-200/80 shadow-sm shadow-slate-200/40">
              <CardHeader className="flex-row items-start justify-between space-y-0 pb-1">
                <div>
                  <CardTitle className="text-base font-semibold">Aset per kategori</CardTitle>
                  <p className="mt-1 text-sm text-slate-500">Distribusi inventaris menurut kategori.</p>
                </div>
                <span className="rounded-lg bg-indigo-50 px-2.5 py-1 text-xs font-medium text-indigo-700">
                  {categories.length} kategori
                </span>
              </CardHeader>
              <CardContent className="pt-4">
                {categories.length === 0 ? (
                  <div className="grid h-[280px] place-items-center text-sm text-slate-400">Belum ada data kategori.</div>
                ) : (
                  <ResponsiveContainer width="100%" height={280}>
                    <BarChart data={categories} margin={{ top: 8, right: 12, left: -18, bottom: 4 }}>
                      <CartesianGrid vertical={false} stroke="#eef0f4" />
                      <XAxis dataKey="categoryName" tickLine={false} axisLine={false} tick={{ fill: "#64748b", fontSize: 12 }} />
                      <YAxis allowDecimals={false} tickLine={false} axisLine={false} tick={{ fill: "#94a3b8", fontSize: 12 }} />
                      <Tooltip cursor={{ fill: "#f8fafc" }} contentStyle={{ borderRadius: 12, border: "1px solid #e2e8f0" }} />
                      <Bar dataKey="assetCount" name="Jumlah aset" radius={[7, 7, 0, 0]} maxBarSize={46}>
                        {categories.map((category, index) => (
                          <Cell key={category.categoryId} fill={chartColors[index % chartColors.length]} />
                        ))}
                      </Bar>
                    </BarChart>
                  </ResponsiveContainer>
                )}
              </CardContent>
            </Card>
            <Card className="rounded-2xl border-slate-200/80 shadow-sm shadow-slate-200/40">
              <CardHeader className="flex-row items-start justify-between space-y-0 pb-1">
                <div>
                  <CardTitle className="text-base font-semibold">Pinjaman terlambat</CardTitle>
                  <p className="mt-1 text-sm text-slate-500">Aset yang belum dikembalikan sesuai jadwal.</p>
                </div>
                <Clock3 className="mt-1 size-4 text-slate-400" />
              </CardHeader>
              <CardContent className="pt-4">
                <div className="overflow-hidden rounded-xl border border-slate-100">
                  <Table>
                    <TableHeader>
                      <TableRow className="bg-slate-50/80 hover:bg-slate-50/80">
                        <TableHead>Aset</TableHead>
                        <TableHead>Peminjam</TableHead>
                        <TableHead>Jatuh tempo</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {overdue.map((loan) => (
                        <TableRow key={loan.id}>
                          <TableCell>
                            <Link to={`/assets/${loan.assetId}`} className="font-medium text-slate-800 hover:text-indigo-700">
                              {loan.assetCode}
                            </Link>
                            <p className="mt-0.5 text-xs text-slate-400">{loan.assetName}</p>
                          </TableCell>
                          <TableCell className="text-sm text-slate-600">{loan.borrowerName}</TableCell>
                          <TableCell className="whitespace-nowrap text-xs text-rose-600">{formatDate(loan.dueDate)}</TableCell>
                        </TableRow>
                      ))}
                      {overdue.length === 0 && (
                        <TableRow><TableCell colSpan={3} className="py-12 text-center text-sm text-slate-400">Tidak ada pinjaman terlambat.</TableCell></TableRow>
                      )}
                    </TableBody>
                  </Table>
                </div>
                <Button asChild variant="ghost" size="sm" className="mt-3 px-2 text-indigo-700">
                  <Link to="/approvals">Lihat seluruh pinjaman <ArrowUpRight className="ml-1 size-3.5" /></Link>
                </Button>
              </CardContent>
            </Card>
          </div>
          <div className="mt-5 grid gap-4 sm:grid-cols-4">
            <StatusCount label="Tersedia" value={summary.availableAssets} color="bg-emerald-500" />
            <StatusCount label="Dipinjam" value={summary.onLoanAssets} color="bg-sky-500" />
            <StatusCount label="Maintenance" value={summary.maintenanceAssets} color="bg-orange-500" />
            <StatusCount label="Pensiun" value={summary.retiredAssets} color="bg-slate-400" />
          </div>
        </>
      ) : null}
    </>
  )
}

function MetricCard({
  title,
  value,
  note,
  icon: Icon,
  iconClass,
}: {
  title: string
  value: number
  note: string
  icon: typeof Box
  iconClass: string
}) {
  return (
    <Card className="rounded-2xl border-slate-200/80 shadow-sm shadow-slate-200/40">
      <CardContent className="p-5">
        <div className="flex items-start justify-between">
          <p className="text-sm font-medium text-slate-500">{title}</p>
          <div className={`grid size-9 place-items-center rounded-xl ${iconClass}`}><Icon className="size-[18px]" /></div>
        </div>
        <p className="mt-5 text-3xl font-semibold tracking-tight text-slate-900">{value.toLocaleString("id-ID")}</p>
        <p className="mt-1 text-xs text-slate-400">{note}</p>
      </CardContent>
    </Card>
  )
}

function StatusCount({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="flex items-center gap-3 rounded-xl border border-slate-200/70 bg-white px-4 py-3 shadow-sm shadow-slate-100">
      <span className={`size-2.5 rounded-full ${color}`} />
      <span className="flex-1 text-sm text-slate-500">{label}</span>
      <span className="text-sm font-semibold text-slate-800">{value}</span>
    </div>
  )
}
