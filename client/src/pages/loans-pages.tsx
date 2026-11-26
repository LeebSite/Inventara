import { useEffect, useState, type FormEvent, type ReactNode } from "react"
import { Check, ClipboardCheck, Plus, RotateCcw, X } from "lucide-react"
import api, { getErrorMessage, unwrap } from "@/lib/api"
import { formatDate } from "@/lib/format"
import type { ApiEnvelope, Asset, AssetCondition, Loan, LoanStatus, Page } from "@/types"
import { useAuth } from "@/auth/auth-context"
import { ErrorState, LoadingBlock } from "@/components/feedback"
import { PageHeader } from "@/components/page-header"
import { StatusBadge } from "@/components/status-badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Textarea } from "@/components/ui/textarea"

export function MyLoansPage() {
  const [loans, setLoans] = useState<Loan[]>([])
  const [assets, setAssets] = useState<Asset[]>([])
  const [assetId, setAssetId] = useState("")
  const [dueDate, setDueDate] = useState("")
  const [purpose, setPurpose] = useState("")
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const [loanResponse, assetResponse] = await Promise.all([
        api.get<ApiEnvelope<Page<Loan>>>("/loans/my", { params: { page: 1, pageSize: 100, sortBy: "requestedAt", sortDir: "desc" } }),
        api.get<ApiEnvelope<Page<Asset>>>("/assets", { params: { status: "Available", page: 1, pageSize: 100, sortBy: "assetCode", sortDir: "asc" } }),
      ])
      setLoans(unwrap(loanResponse).items)
      setAssets(unwrap(assetResponse).items.filter((asset) => asset.status === "Available"))
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [])

  async function requestLoan(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await api.post("/loans", { assetId, dueDate: new Date(`${dueDate}T23:59:59`).toISOString(), purpose: purpose.trim() })
      setAssetId("")
      setDueDate("")
      setPurpose("")
      await load()
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      <PageHeader eyebrow="Peminjaman" title="Pinjaman saya" description="Ajukan pinjaman aset dan pantau status permintaan Anda." />
      {error && <ErrorState message={error} onRetry={() => void load()} />}
      <Card className="mb-5 rounded-2xl border-slate-200/80 shadow-sm">
        <CardContent className="p-5">
          <div className="mb-4 flex items-center gap-3">
            <div className="grid size-9 place-items-center rounded-xl bg-indigo-50 text-indigo-600"><Plus className="size-4" /></div>
            <div><h2 className="text-sm font-semibold text-slate-800">Ajukan pinjaman</h2><p className="mt-0.5 text-xs text-slate-400">Pilih aset yang tersedia dan tanggal pengembalian.</p></div>
          </div>
          <form onSubmit={(event) => void requestLoan(event)} className="grid gap-3 md:grid-cols-[1.2fr_0.8fr_1.4fr_auto] md:items-end">
            <div className="space-y-2">
              <Label htmlFor="loan-asset" className="text-xs text-slate-600">Aset</Label>
              <select id="loan-asset" required className="form-select" value={assetId} onChange={(event) => setAssetId(event.target.value)}>
                <option value="">Pilih aset tersedia</option>
                {assets.map((asset) => <option key={asset.id} value={asset.id}>{asset.assetCode} — {asset.name}</option>)}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="loan-due-date" className="text-xs text-slate-600">Tanggal jatuh tempo</Label>
              <Input id="loan-due-date" type="date" required min={new Date().toISOString().slice(0, 10)} value={dueDate} onChange={(event) => setDueDate(event.target.value)} className="h-10 rounded-xl" />
            </div>
            <div className="space-y-2">
              <Label htmlFor="loan-purpose" className="text-xs text-slate-600">Tujuan</Label>
              <Input id="loan-purpose" required maxLength={500} value={purpose} onChange={(event) => setPurpose(event.target.value)} placeholder="Contoh: keperluan presentasi" className="h-10 rounded-xl" />
            </div>
            <Button type="submit" className="h-10 rounded-xl" disabled={submitting || !assets.length}>{submitting ? "Mengirim..." : "Ajukan"}</Button>
          </form>
          {!assets.length && <p className="mt-3 text-xs text-slate-400">Tidak ada aset tersedia untuk dipinjam saat ini.</p>}
        </CardContent>
      </Card>
      <Card className="rounded-2xl border-slate-200/80 shadow-sm">
        <div className="border-b border-slate-100 px-5 py-4"><h2 className="text-sm font-semibold text-slate-800">Riwayat pengajuan</h2><p className="mt-1 text-xs text-slate-400">Semua permintaan yang diajukan menggunakan akun Anda.</p></div>
        {loading ? <LoadingBlock /> : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader><TableRow className="bg-slate-50/70"><TableHead className="pl-5">Aset</TableHead><TableHead>Status</TableHead><TableHead>Diminta</TableHead><TableHead>Jatuh tempo</TableHead><TableHead className="pr-5">Tujuan / catatan</TableHead></TableRow></TableHeader>
              <TableBody>
                {loans.map((loan) => <TableRow key={loan.id}>
                  <TableCell className="py-3.5 pl-5"><p className="font-medium text-slate-800">{loan.assetCode}</p><p className="mt-0.5 text-xs text-slate-400">{loan.assetName}</p></TableCell>
                  <TableCell><StatusBadge status={loan.status} /></TableCell>
                  <TableCell className="whitespace-nowrap text-xs text-slate-500">{formatDate(loan.requestedAt)}</TableCell>
                  <TableCell className="whitespace-nowrap text-xs text-slate-500">{formatDate(loan.dueDate)}</TableCell>
                  <TableCell className="max-w-xs pr-5 text-xs text-slate-500"><p>{loan.purpose}</p>{loan.rejectionReason && <p className="mt-1 text-rose-600">Alasan: {loan.rejectionReason}</p>}</TableCell>
                </TableRow>)}
                {!loans.length && <TableRow><TableCell colSpan={5} className="py-16 text-center text-sm text-slate-400">Belum ada pinjaman yang diajukan.</TableCell></TableRow>}
              </TableBody>
            </Table>
          </div>
        )}
      </Card>
    </>
  )
}

type ModalAction = { kind: "reject" | "return"; loan: Loan } | null

export function ApprovalsPage() {
  const { role } = useAuth()
  const isAdmin = role === "Admin"
  const [pending, setPending] = useState<Loan[]>([])
  const [active, setActive] = useState<Loan[]>([])
  const [overdue, setOverdue] = useState<Loan[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [action, setAction] = useState<ModalAction>(null)
  const [reason, setReason] = useState("")
  const [returnCondition, setReturnCondition] = useState<AssetCondition>("Good")
  const [submitting, setSubmitting] = useState(false)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const [pendingResponse, activeResponse, overdueResponse] = await Promise.all([
        api.get<ApiEnvelope<Page<Loan>>>("/loans", { params: { status: "Pending", page: 1, pageSize: 100 } }),
        api.get<ApiEnvelope<Page<Loan>>>("/loans", { params: { status: "OnLoan", page: 1, pageSize: 100 } }),
        api.get<ApiEnvelope<Page<Loan>>>("/loans/overdue", { params: { page: 1, pageSize: 100 } }),
      ])
      setPending(unwrap(pendingResponse).items)
      setActive(unwrap(activeResponse).items)
      setOverdue(unwrap(overdueResponse).items)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [])

  async function transition(loan: Loan, endpoint: string, body?: object) {
    setSubmitting(true)
    setError(null)
    try {
      await api.post(`/loans/${loan.id}/${endpoint}`, body ?? {})
      setAction(null)
      setReason("")
      await load()
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  async function submitAction(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!action) return
    if (action.kind === "reject") await transition(action.loan, "reject", { reason: reason.trim() })
    else await transition(action.loan, "return", { returnCondition })
  }

  return (
    <>
      <PageHeader eyebrow="Alur peminjaman" title="Persetujuan & pengembalian" description="Tinjau permintaan baru dan kelola aset yang sedang dipinjam." />
      {error && <ErrorState message={error} onRetry={() => void load()} />}
      {loading ? <LoadingBlock /> : (
        <div className="space-y-5">
          <LoanTable
            title="Antrean persetujuan"
            subtitle="Permintaan yang menunggu keputusan."
            loans={pending}
            empty="Tidak ada permintaan yang menunggu persetujuan."
            actions={(loan) => isAdmin ? (
              <div className="flex justify-end gap-1">
                <Button size="sm" className="h-8 rounded-lg bg-emerald-600 hover:bg-emerald-700" onClick={() => void transition(loan, "approve")}><Check className="size-3.5" /> Setujui</Button>
                <Button size="sm" variant="outline" className="h-8 rounded-lg text-rose-600" onClick={() => { setReason(""); setAction({ kind: "reject", loan }) }}><X className="size-3.5" /> Tolak</Button>
              </div>
            ) : <span className="text-xs text-slate-400">Keputusan hanya oleh Admin</span>}
          />
          <LoanTable
            title="Pinjaman aktif"
            subtitle="Pinjaman yang telah disetujui dan belum dikembalikan."
            loans={mergeUnique(active, overdue)}
            empty="Tidak ada pinjaman aktif."
            actions={(loan) => (
              <Button size="sm" variant="outline" className="h-8 rounded-lg" onClick={() => { setReturnCondition("Good"); setAction({ kind: "return", loan }) }}><RotateCcw className="size-3.5" /> Catat kembali</Button>
            )}
          />
        </div>
      )}
      <Dialog open={Boolean(action)} onOpenChange={(open) => !open && setAction(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{action?.kind === "reject" ? "Tolak pengajuan pinjaman" : "Catat pengembalian aset"}</DialogTitle>
            <DialogDescription>{action?.loan.assetCode} · {action?.loan.assetName}</DialogDescription>
          </DialogHeader>
          <form onSubmit={(event) => void submitAction(event)} className="space-y-4">
            {action?.kind === "reject" ? (
              <div className="space-y-2"><Label htmlFor="reject-reason">Alasan penolakan</Label><Textarea id="reject-reason" required minLength={1} value={reason} onChange={(event) => setReason(event.target.value)} rows={3} /></div>
            ) : action?.kind === "return" ? (
              <div className="space-y-2"><Label htmlFor="return-condition">Kondisi aset saat dikembalikan</Label><select id="return-condition" className="form-select" value={returnCondition} onChange={(event) => setReturnCondition(event.target.value as AssetCondition)}><option value="Good">Baik</option><option value="MinorDamage">Kerusakan ringan</option><option value="MajorDamage">Kerusakan berat</option></select><p className="text-xs text-slate-500">Aset rusak otomatis dibuatkan tiket maintenance.</p></div>
            ) : null}
            <DialogFooter><Button type="button" variant="outline" onClick={() => setAction(null)}>Batal</Button><Button type="submit" disabled={submitting}>{submitting ? "Menyimpan..." : action?.kind === "reject" ? "Tolak pengajuan" : "Konfirmasi kembali"}</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  )
}

function mergeUnique(first: Loan[], second: Loan[]) {
  return [...new Map([...first, ...second].map((loan) => [loan.id, loan])).values()]
}

function LoanTable({
  title,
  subtitle,
  loans,
  empty,
  actions,
}: {
  title: string
  subtitle: string
  loans: Loan[]
  empty: string
  actions: (loan: Loan) => ReactNode
}) {
  return (
    <Card className="rounded-2xl border-slate-200/80 shadow-sm">
      <div className="flex items-center gap-3 border-b border-slate-100 px-5 py-4">
        <div className="grid size-9 place-items-center rounded-xl bg-indigo-50 text-indigo-600"><ClipboardCheck className="size-4" /></div>
        <div><h2 className="text-sm font-semibold text-slate-800">{title}</h2><p className="mt-0.5 text-xs text-slate-400">{subtitle}</p></div>
        <span className="ml-auto rounded-lg bg-slate-100 px-2.5 py-1 text-xs font-semibold text-slate-600">{loans.length}</span>
      </div>
      <CardContent className="p-0">
        <div className="overflow-x-auto">
          <Table>
            <TableHeader><TableRow className="bg-slate-50/70"><TableHead className="pl-5">Aset / peminjam</TableHead><TableHead>Status</TableHead><TableHead>Diminta</TableHead><TableHead>Jatuh tempo</TableHead><TableHead>Tujuan</TableHead><TableHead className="pr-5 text-right">Aksi</TableHead></TableRow></TableHeader>
            <TableBody>
              {loans.map((loan) => <TableRow key={loan.id}>
                <TableCell className="py-3.5 pl-5"><p className="font-medium text-slate-800">{loan.assetCode}</p><p className="mt-0.5 text-xs text-slate-400">{loan.borrowerName}</p></TableCell>
                <TableCell><StatusBadge status={loan.status as LoanStatus} /></TableCell>
                <TableCell className="whitespace-nowrap text-xs text-slate-500">{formatDate(loan.requestedAt)}</TableCell>
                <TableCell className={`whitespace-nowrap text-xs ${loan.status === "Overdue" ? "font-medium text-rose-600" : "text-slate-500"}`}>{formatDate(loan.dueDate)}</TableCell>
                <TableCell className="max-w-[200px] truncate text-xs text-slate-500">{loan.purpose}</TableCell>
                <TableCell className="pr-5 text-right">{actions(loan)}</TableCell>
              </TableRow>)}
              {!loans.length && <TableRow><TableCell colSpan={6} className="py-12 text-center text-sm text-slate-400">{empty}</TableCell></TableRow>}
            </TableBody>
          </Table>
        </div>
      </CardContent>
    </Card>
  )
}
