import { useCallback, useEffect, useState } from "react"
import { ArrowLeft, CalendarDays, Camera, Pencil, Wrench } from "lucide-react"
import { Link, useParams } from "react-router-dom"
import api, { getErrorMessage, unwrap } from "@/lib/api"
import { formatCurrency, formatDate } from "@/lib/format"
import type { ApiEnvelope, AssetDetail, Loan, MaintenanceTicket, Page } from "@/types"
import { useAuth } from "@/auth/auth-context"
import { AssetFormDialog } from "@/components/asset-form-dialog"
import { ErrorState, LoadingBlock } from "@/components/feedback"
import { PageHeader } from "@/components/page-header"
import { StatusBadge } from "@/components/status-badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"

const pageSize = 100

async function loadAllPages<T>(endpoint: string, params: Record<string, string | number>) {
  const firstResponse = await api.get<ApiEnvelope<Page<T>>>(endpoint, { params: { ...params, page: 1, pageSize } })
  const firstPage = unwrap(firstResponse)
  const items = [...firstPage.items]
  for (let page = 2; page <= firstPage.totalPages; page += 1) {
    const response = await api.get<ApiEnvelope<Page<T>>>(endpoint, { params: { ...params, page, pageSize } })
    items.push(...unwrap(response).items)
  }
  return items
}

function photoUrl(path: string | null) {
  if (!path) return null
  if (/^https?:\/\//i.test(path)) return path
  const base = (import.meta.env.VITE_API_URL || "http://localhost:5090/api/v1").replace(/\/api\/v1\/?$/, "")
  return `${base}/${path.replace(/^\/+/, "")}`
}

export function AssetDetailPage() {
  const { id } = useParams()
  const { role } = useAuth()
  const canEdit = role === "Admin" || role === "Staff"
  const [detail, setDetail] = useState<AssetDetail | null>(null)
  const [loans, setLoans] = useState<Loan[]>([])
  const [maintenance, setMaintenance] = useState<MaintenanceTicket[]>([])
  const [loading, setLoading] = useState(true)
  const [photoUploading, setPhotoUploading] = useState(false)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const load = useCallback(async () => {
    if (!id) return
    setLoading(true)
    setError(null)
    try {
      const detailResponse = await api.get<ApiEnvelope<AssetDetail>>(`/assets/${id}`)
      setDetail(unwrap(detailResponse))
      if (canEdit) {
        const [loanResults, maintenanceResults] = await Promise.all([
          loadAllPages<Loan>("/loans", {}),
          loadAllPages<MaintenanceTicket>("/maintenance", { assetId: id }),
        ])
        setLoans(loanResults.filter((loan) => loan.assetId === id))
        setMaintenance(maintenanceResults)
      } else {
        const loanResults = await loadAllPages<Loan>("/loans/my", {})
        setLoans(loanResults.filter((loan) => loan.assetId === id))
        setMaintenance([])
      }
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }, [id, canEdit])

  useEffect(() => {
    void load()
  }, [load])

  async function uploadPhoto(file: File | undefined) {
    if (!id || !file) return
    setPhotoUploading(true)
    setError(null)
    const formData = new FormData()
    formData.append("file", file)
    try {
      await api.post(`/assets/${id}/photo`, formData)
      await load()
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setPhotoUploading(false)
    }
  }

  if (loading) return <LoadingBlock label="Memuat detail aset..." />
  if (error && !detail) return <ErrorState message={error} onRetry={() => void load()} />
  if (!detail) return null

  const asset = detail.asset
  const image = photoUrl(asset.photoUrl)

  return (
    <>
      <Button asChild variant="ghost" className="-ml-3 mb-4 text-slate-500">
        <Link to="/assets"><ArrowLeft className="size-4" /> Kembali ke aset</Link>
      </Button>
      <PageHeader
        eyebrow={asset.assetCode}
        title={asset.name}
        description={`${asset.categoryName} · ${asset.locationName}`}
        actions={
          canEdit ? (
            <>
              <label className="inline-flex h-9 cursor-pointer items-center justify-center gap-2 rounded-xl border border-slate-200 bg-white px-3 text-sm font-medium text-slate-600 shadow-sm transition hover:bg-slate-50">
                <Camera className="size-4" /> {photoUploading ? "Mengunggah..." : "Foto"}
                <input className="sr-only" type="file" accept="image/jpeg,image/png" disabled={photoUploading} onChange={(event) => void uploadPhoto(event.target.files?.[0])} />
              </label>
              <Button className="rounded-xl" onClick={() => setDialogOpen(true)}><Pencil className="size-4" /> Edit aset</Button>
            </>
          ) : undefined
        }
      />
      {error && <ErrorState message={error} onRetry={() => void load()} />}
      <div className="grid gap-5 xl:grid-cols-[0.8fr_1.2fr]">
        <div className="space-y-5">
          <Card className="overflow-hidden rounded-2xl border-slate-200/80 shadow-sm">
            <div className="flex aspect-[16/10] items-center justify-center bg-gradient-to-br from-slate-100 to-indigo-50/60">
              {image ? (
                <img src={image} alt={asset.name} className="h-full w-full object-cover" />
              ) : (
                <div className="flex flex-col items-center gap-3 text-slate-300">
                  <div className="grid size-16 place-items-center rounded-2xl bg-white/80 shadow-sm"><Camera className="size-7" /></div>
                  <span className="text-xs font-medium text-slate-400">Belum ada foto aset</span>
                </div>
              )}
            </div>
            <CardContent className="flex items-center justify-between p-5">
              <div>
                <p className="text-xs text-slate-400">Status aset</p>
                <div className="mt-2"><StatusBadge status={asset.status} /></div>
              </div>
              <div className="text-right">
                <p className="text-xs text-slate-400">Kondisi</p>
                <div className="mt-2"><StatusBadge status={asset.condition} /></div>
              </div>
            </CardContent>
          </Card>
          <Card className="rounded-2xl border-slate-200/80 shadow-sm">
            <CardHeader><CardTitle className="text-base">Spesifikasi</CardTitle></CardHeader>
            <CardContent>
              <dl className="grid grid-cols-2 gap-x-4 gap-y-5">
                <Spec label="Kode aset" value={asset.assetCode} />
                <Spec label="Nomor seri" value={asset.serialNumber || "—"} />
                <Spec label="Kategori" value={asset.categoryName} />
                <Spec label="Lokasi" value={asset.locationName} />
                <Spec label="Vendor" value={asset.vendorName || "—"} />
                <Spec label="Tanggal beli" value={formatDate(asset.purchaseDate)} />
                <Spec label="Harga beli" value={formatCurrency(asset.purchasePrice)} />
                <Spec label="Catatan" value={asset.notes || "—"} />
              </dl>
            </CardContent>
          </Card>
        </div>
        <div className="space-y-5">
          <Card className="rounded-2xl border-slate-200/80 shadow-sm">
            <CardHeader className="flex-row items-center justify-between">
              <div>
                <CardTitle className="text-base">Riwayat pinjaman</CardTitle>
                <p className="mt-1 text-sm text-slate-500">{canEdit ? "Riwayat semua peminjam." : "Pinjaman Anda untuk aset ini."}</p>
              </div>
              <CalendarDays className="size-4 text-slate-400" />
            </CardHeader>
            <CardContent className="pt-0">
              <div className="overflow-x-auto rounded-xl border border-slate-100">
                <Table>
                  <TableHeader><TableRow className="bg-slate-50/70"><TableHead>Status</TableHead><TableHead>Tanggal pinjam</TableHead><TableHead>Jatuh tempo</TableHead></TableRow></TableHeader>
                  <TableBody>
                    {loans.map((loan) => (
                      <TableRow key={loan.id}>
                        <TableCell><StatusBadge status={loan.status} /><p className="mt-1 text-xs text-slate-400">{loan.borrowerName}</p></TableCell>
                        <TableCell className="whitespace-nowrap text-xs text-slate-600">{formatDate(loan.loanDate)}</TableCell>
                        <TableCell className="whitespace-nowrap text-xs text-slate-600">{formatDate(loan.dueDate)}</TableCell>
                      </TableRow>
                    ))}
                    {!loans.length && <TableRow><TableCell colSpan={3} className="py-9 text-center text-sm text-slate-400">Belum ada riwayat pinjaman yang dapat ditampilkan.</TableCell></TableRow>}
                  </TableBody>
                </Table>
              </div>
              {!canEdit && detail.mostRecentLoan && !loans.some((loan) => loan.id === detail.mostRecentLoan?.id) && (
                <p className="mt-3 rounded-lg bg-slate-50 px-3 py-2 text-xs text-slate-500">
                  Pinjaman terakhir tercatat {formatDate(detail.mostRecentLoan.loanDate)}. Riwayat lengkap peminjam lain tidak tersedia untuk role Anda.
                </p>
              )}
            </CardContent>
          </Card>
          <Card className="rounded-2xl border-slate-200/80 shadow-sm">
            <CardHeader className="flex-row items-center justify-between">
              <div>
                <CardTitle className="text-base">Riwayat maintenance</CardTitle>
                <p className="mt-1 text-sm text-slate-500">{canEdit ? "Tiket perawatan untuk aset ini." : "Riwayat ini tersedia untuk Admin dan Staff."}</p>
              </div>
              <Wrench className="size-4 text-slate-400" />
            </CardHeader>
            <CardContent className="pt-0">
              {canEdit ? (
                <div className="space-y-3">
                  {maintenance.map((ticket) => (
                    <div key={ticket.id} className="flex items-start justify-between gap-4 rounded-xl border border-slate-100 p-3.5">
                      <div>
                        <p className="text-sm font-medium text-slate-800">{ticket.description}</p>
                        <p className="mt-1 text-xs text-slate-400">{formatDate(ticket.reportedAt)}</p>
                      </div>
                      <StatusBadge status={ticket.status} />
                    </div>
                  ))}
                  {!maintenance.length && <p className="py-7 text-center text-sm text-slate-400">Belum ada tiket maintenance.</p>}
                </div>
              ) : (
                <p className="rounded-xl bg-slate-50 px-4 py-4 text-sm text-slate-500">Akses histori maintenance dibatasi oleh backend.</p>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
      <AssetFormDialog open={dialogOpen} onOpenChange={setDialogOpen} asset={asset} onSaved={load} />
    </>
  )
}

function Spec({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs text-slate-400">{label}</dt>
      <dd className="mt-1 break-words text-sm font-medium text-slate-700">{value}</dd>
    </div>
  )
}
