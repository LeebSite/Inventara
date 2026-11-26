import { useEffect, useState, type FormEvent } from "react"
import { CheckCircle2, Plus, Wrench } from "lucide-react"
import api, { getErrorMessage, unwrap } from "@/lib/api"
import { formatCurrency, formatDate, toDateInput } from "@/lib/format"
import type { ApiEnvelope, Asset, MaintenanceStatus, MaintenanceTicket, Page, Vendor } from "@/types"
import { ErrorState, LoadingBlock } from "@/components/feedback"
import { PageHeader } from "@/components/page-header"
import { StatusBadge } from "@/components/status-badge"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Textarea } from "@/components/ui/textarea"

export function MaintenancePage() {
  const [tickets, setTickets] = useState<MaintenanceTicket[]>([])
  const [assets, setAssets] = useState<Asset[]>([])
  const [vendors, setVendors] = useState<Vendor[]>([])
  const [statusFilter, setStatusFilter] = useState("")
  const [assetFilter, setAssetFilter] = useState("")
  const [createOpen, setCreateOpen] = useState(false)
  const [editing, setEditing] = useState<MaintenanceTicket | null>(null)
  const [assetId, setAssetId] = useState("")
  const [vendorId, setVendorId] = useState("")
  const [description, setDescription] = useState("")
  const [ticketStatus, setTicketStatus] = useState<MaintenanceStatus>("InProgress")
  const [cost, setCost] = useState("")
  const [finishedAt, setFinishedAt] = useState("")
  const [submitting, setSubmitting] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const response = await api.get<ApiEnvelope<Page<MaintenanceTicket>>>("/maintenance", {
        params: {
          status: statusFilter || undefined,
          assetId: assetFilter || undefined,
          page: 1,
          pageSize: 100,
          sortBy: "reportedAt",
          sortDir: "desc",
        },
      })
      setTickets(unwrap(response).items)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void load()
  }, [statusFilter, assetFilter])

  useEffect(() => {
    Promise.all([
      api.get<ApiEnvelope<Page<Asset>>>("/assets", { params: { page: 1, pageSize: 100 } }),
      api.get<ApiEnvelope<Page<Vendor>>>("/vendors", { params: { page: 1, pageSize: 100 } }),
    ])
      .then(([assetResponse, vendorResponse]) => {
        setAssets(unwrap(assetResponse).items)
        setVendors(unwrap(vendorResponse).items)
      })
      .catch((requestError: unknown) => setError(getErrorMessage(requestError)))
  }, [])

  function startCreate() {
    setAssetId("")
    setVendorId("")
    setDescription("")
    setCreateOpen(true)
  }

  function startEdit(ticket: MaintenanceTicket) {
    setEditing(ticket)
    setTicketStatus(ticket.status === "Reported" ? "InProgress" : ticket.status)
    setCost(ticket.cost === null ? "" : String(ticket.cost))
    setFinishedAt(ticket.finishedAt ? toDateInput(ticket.finishedAt) : new Date().toISOString().slice(0, 10))
  }

  async function createTicket(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await api.post("/maintenance", { assetId, vendorId: vendorId || null, description: description.trim() })
      setCreateOpen(false)
      await load()
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  async function updateTicket(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!editing) return
    setSubmitting(true)
    setError(null)
    try {
      await api.put(`/maintenance/${editing.id}`, {
        status: ticketStatus,
        cost: cost === "" ? null : Number(cost),
        finishedAt: new Date(`${finishedAt}T00:00:00Z`).toISOString(),
      })
      setEditing(null)
      await load()
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  async function complete(ticket: MaintenanceTicket) {
    setSubmitting(true)
    setError(null)
    try {
      await api.post(`/maintenance/${ticket.id}/complete`)
      await load()
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <>
      <PageHeader
        eyebrow="Perawatan aset"
        title="Maintenance"
        description="Pantau tiket perawatan dan kembalikan aset ke status siap pakai setelah selesai."
        actions={<Button className="rounded-xl" onClick={startCreate}><Plus className="size-4" /> Tiket baru</Button>}
      />
      {error && <ErrorState message={error} onRetry={() => void load()} />}
      <Card className="rounded-2xl border-slate-200/80 shadow-sm">
        <div className="flex flex-col gap-3 border-b border-slate-100 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-2 text-sm font-semibold text-slate-700"><Wrench className="size-4 text-slate-400" /> Tiket maintenance <span className="rounded-md bg-slate-100 px-2 py-0.5 text-xs text-slate-500">{tickets.length}</span></div>
          <div className="grid gap-2 sm:grid-cols-2">
            <select aria-label="Filter status tiket" className="form-select" value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)}>
              <option value="">Semua status</option><option value="Reported">Reported</option><option value="InProgress">In Progress</option><option value="Done">Done</option><option value="Cancelled">Cancelled</option>
            </select>
            <select aria-label="Filter aset" className="form-select" value={assetFilter} onChange={(event) => setAssetFilter(event.target.value)}>
              <option value="">Semua aset</option>{assets.map((asset) => <option key={asset.id} value={asset.id}>{asset.assetCode} — {asset.name}</option>)}
            </select>
          </div>
        </div>
        {loading ? <LoadingBlock /> : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader><TableRow className="bg-slate-50/70"><TableHead className="pl-5">Aset / keluhan</TableHead><TableHead>Status</TableHead><TableHead>Dilaporkan</TableHead><TableHead>Biaya</TableHead><TableHead>Selesai</TableHead><TableHead className="pr-5 text-right">Aksi</TableHead></TableRow></TableHeader>
              <TableBody>
                {tickets.map((ticket) => <TableRow key={ticket.id}>
                  <TableCell className="max-w-sm py-3.5 pl-5"><p className="font-medium text-slate-800">{ticket.assetCode} · {ticket.assetName}</p><p className="mt-1 truncate text-xs text-slate-400">{ticket.description}</p></TableCell>
                  <TableCell><StatusBadge status={ticket.status} /></TableCell>
                  <TableCell className="whitespace-nowrap text-xs text-slate-500">{formatDate(ticket.reportedAt)}</TableCell>
                  <TableCell className="whitespace-nowrap text-xs text-slate-500">{ticket.cost === null ? "—" : formatCurrency(ticket.cost)}</TableCell>
                  <TableCell className="whitespace-nowrap text-xs text-slate-500">{formatDate(ticket.finishedAt)}</TableCell>
                  <TableCell className="pr-5 text-right">
                    <div className="flex justify-end gap-1">
                      {ticket.status !== "Done" && ticket.status !== "Cancelled" && <Button size="sm" variant="outline" className="h-8 rounded-lg" disabled={submitting} onClick={() => startEdit(ticket)}>Ubah</Button>}
                      {ticket.status !== "Done" && ticket.status !== "Cancelled" && <Button size="sm" className="h-8 rounded-lg bg-emerald-600 hover:bg-emerald-700" disabled={submitting} onClick={() => void complete(ticket)}><CheckCircle2 className="size-3.5" /> Selesai</Button>}
                    </div>
                  </TableCell>
                </TableRow>)}
                {!tickets.length && <TableRow><TableCell colSpan={6} className="py-16 text-center text-sm text-slate-400">Tidak ada tiket yang cocok dengan filter.</TableCell></TableRow>}
              </TableBody>
            </Table>
          </div>
        )}
      </Card>
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent>
          <DialogHeader><DialogTitle>Buat tiket maintenance</DialogTitle><DialogDescription>Aset akan ditandai sedang dalam maintenance.</DialogDescription></DialogHeader>
          <form onSubmit={(event) => void createTicket(event)} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="maintenance-asset">Aset</Label><select id="maintenance-asset" required className="form-select" value={assetId} onChange={(event) => setAssetId(event.target.value)}><option value="">Pilih aset</option>{assets.map((asset) => <option key={asset.id} value={asset.id}>{asset.assetCode} — {asset.name}</option>)}</select></div>
            <div className="space-y-2"><Label htmlFor="maintenance-vendor">Vendor (opsional)</Label><select id="maintenance-vendor" className="form-select" value={vendorId} onChange={(event) => setVendorId(event.target.value)}><option value="">Tanpa vendor</option>{vendors.map((vendor) => <option key={vendor.id} value={vendor.id}>{vendor.name}</option>)}</select></div>
            <div className="space-y-2"><Label htmlFor="maintenance-description">Deskripsi kerusakan / perawatan</Label><Textarea id="maintenance-description" required maxLength={2000} rows={4} value={description} onChange={(event) => setDescription(event.target.value)} /></div>
            <DialogFooter><Button type="button" variant="outline" onClick={() => setCreateOpen(false)}>Batal</Button><Button type="submit" disabled={submitting}>{submitting ? "Menyimpan..." : "Buat tiket"}</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
      <Dialog open={Boolean(editing)} onOpenChange={(open) => !open && setEditing(null)}>
        <DialogContent>
          <DialogHeader><DialogTitle>Ubah tiket maintenance</DialogTitle><DialogDescription>{editing?.assetCode} · {editing?.assetName}</DialogDescription></DialogHeader>
          <form onSubmit={(event) => void updateTicket(event)} className="space-y-4">
            <div className="space-y-2"><Label htmlFor="ticket-status">Status</Label><select id="ticket-status" className="form-select" value={ticketStatus} onChange={(event) => setTicketStatus(event.target.value as MaintenanceStatus)}><option value="Reported">Reported</option><option value="InProgress">In Progress</option><option value="Cancelled">Cancelled</option></select></div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2"><Label htmlFor="ticket-cost">Biaya (Rp)</Label><Input id="ticket-cost" type="number" min="0" step="1000" value={cost} onChange={(event) => setCost(event.target.value)} /></div>
              <div className="space-y-2"><Label htmlFor="ticket-finished-at">Tanggal selesai</Label><Input id="ticket-finished-at" type="date" required value={finishedAt} onChange={(event) => setFinishedAt(event.target.value)} /></div>
            </div>
            <DialogFooter><Button type="button" variant="outline" onClick={() => setEditing(null)}>Batal</Button><Button type="submit" disabled={submitting}>{submitting ? "Menyimpan..." : "Simpan perubahan"}</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  )
}
