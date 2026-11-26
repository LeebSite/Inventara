import { useEffect, useState } from "react"
import { Filter, ScrollText } from "lucide-react"
import api, { getErrorMessage, unwrap } from "@/lib/api"
import { formatDate } from "@/lib/format"
import type { AdminUser, ApiEnvelope, AuditLog, Page } from "@/types"
import { ErrorState, LoadingBlock } from "@/components/feedback"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"

const pageSize = 20

function displayJson(value: string | null) {
  if (!value) return "—"
  try {
    return JSON.stringify(JSON.parse(value) as unknown, null, 2)
  } catch {
    return value
  }
}

export function AuditLogsPage() {
  const [logs, setLogs] = useState<AuditLog[]>([])
  const [users, setUsers] = useState<AdminUser[]>([])
  const [entityName, setEntityName] = useState("")
  const [userId, setUserId] = useState("")
  const [fromDate, setFromDate] = useState("")
  const [toDate, setToDate] = useState("")
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [totalItems, setTotalItems] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const response = await api.get<ApiEnvelope<Page<AuditLog>>>("/audit-logs", {
        params: {
          entityName: entityName || undefined,
          userId: userId || undefined,
          fromDate: fromDate ? new Date(`${fromDate}T00:00:00`).toISOString() : undefined,
          toDate: toDate ? new Date(`${toDate}T23:59:59`).toISOString() : undefined,
          page,
          pageSize,
        },
      })
      const result = unwrap(response)
      setLogs(result.items)
      setTotalPages(result.totalPages)
      setTotalItems(result.totalItems)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    api.get<ApiEnvelope<Page<AdminUser>>>("/users", { params: { page: 1, pageSize: 100, sortBy: "fullName", sortDir: "asc" } })
      .then((response) => setUsers(unwrap(response).items))
      .catch((requestError: unknown) => setError(getErrorMessage(requestError)))
  }, [])

  useEffect(() => { void load() }, [entityName, userId, fromDate, toDate, page])

  function reset() {
    setEntityName("")
    setUserId("")
    setFromDate("")
    setToDate("")
    setPage(1)
  }

  return (
    <>
      <PageHeader eyebrow="Aktivitas sistem" title="Audit log" description="Telusuri perubahan data dan transisi workflow beserta nilai sebelum dan sesudah." />
      {error && <ErrorState message={error} onRetry={() => void load()} />}
      <Card className="rounded-2xl border-slate-200/80 shadow-sm">
        <CardContent className="p-0">
          <div className="grid gap-3 border-b border-slate-100 p-4 sm:grid-cols-2 xl:grid-cols-[1fr_1.2fr_1fr_1fr_auto] xl:items-end">
            <div className="space-y-2"><label htmlFor="audit-entity" className="text-xs font-medium text-slate-600">Entitas</label><select id="audit-entity" className="form-select" value={entityName} onChange={(event) => { setEntityName(event.target.value); setPage(1) }}><option value="">Semua entitas</option><option value="Asset">Asset</option><option value="User">User</option><option value="Loan">Loan</option></select></div>
            <div className="space-y-2"><label htmlFor="audit-user" className="text-xs font-medium text-slate-600">Pengguna</label><select id="audit-user" className="form-select" value={userId} onChange={(event) => { setUserId(event.target.value); setPage(1) }}><option value="">Semua pengguna</option>{users.map((user) => <option key={user.id} value={user.id}>{user.fullName} · {user.email}</option>)}</select></div>
            <div className="space-y-2"><label htmlFor="audit-from" className="text-xs font-medium text-slate-600">Dari tanggal</label><Input id="audit-from" type="date" value={fromDate} onChange={(event) => { setFromDate(event.target.value); setPage(1) }} className="h-10 rounded-xl" /></div>
            <div className="space-y-2"><label htmlFor="audit-to" className="text-xs font-medium text-slate-600">Sampai tanggal</label><Input id="audit-to" type="date" value={toDate} onChange={(event) => { setToDate(event.target.value); setPage(1) }} className="h-10 rounded-xl" /></div>
            <Button variant="outline" className="h-10 rounded-xl" onClick={reset}><Filter className="size-4" /> Reset</Button>
          </div>
          {loading ? <LoadingBlock /> : (
            <>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader><TableRow className="bg-slate-50/70"><TableHead className="pl-5">Waktu</TableHead><TableHead>Entitas / ID</TableHead><TableHead>Aksi</TableHead><TableHead>Pengguna</TableHead><TableHead className="pr-5">Perubahan</TableHead></TableRow></TableHeader>
                  <TableBody>
                    {logs.map((log) => <TableRow key={log.id} className="align-top">
                      <TableCell className="whitespace-nowrap py-3.5 pl-5 text-xs text-slate-500">{formatDate(log.createdAt)}</TableCell>
                      <TableCell><p className="font-medium text-slate-800">{log.entityName}</p><p className="mt-1 max-w-[190px] truncate font-mono text-[10px] text-slate-400">{log.entityId}</p></TableCell>
                      <TableCell><span className="rounded-md bg-indigo-50 px-2 py-1 text-xs font-medium text-indigo-700">{log.action}</span></TableCell>
                      <TableCell className="font-mono text-xs text-slate-500">{log.userId ? users.find((user) => user.id === log.userId)?.fullName ?? log.userId : "System"}</TableCell>
                      <TableCell className="min-w-[220px] pr-5">
                        <details className="group">
                          <summary className="flex cursor-pointer items-center gap-1 text-xs font-medium text-indigo-700"><ScrollText className="size-3.5" /> Lihat nilai</summary>
                          <div className="mt-2 grid gap-2 xl:grid-cols-2">
                            <ValuePanel label="Sebelum" value={displayJson(log.oldValues)} />
                            <ValuePanel label="Sesudah" value={displayJson(log.newValues)} />
                          </div>
                        </details>
                      </TableCell>
                    </TableRow>)}
                    {!logs.length && <TableRow><TableCell colSpan={5} className="py-16 text-center text-sm text-slate-400">Tidak ada log untuk filter yang dipilih.</TableCell></TableRow>}
                  </TableBody>
                </Table>
              </div>
              <div className="flex flex-col gap-3 border-t border-slate-100 px-5 py-3.5 sm:flex-row sm:items-center sm:justify-between">
                <p className="text-xs text-slate-400">{totalItems.toLocaleString("id-ID")} catatan audit</p>
                <div className="flex items-center gap-2">
                  <Button size="sm" variant="outline" className="h-8 rounded-lg" disabled={page <= 1} onClick={() => setPage((value) => value - 1)}>Sebelumnya</Button>
                  <span className="min-w-16 text-center text-xs text-slate-500">{page} / {Math.max(totalPages, 1)}</span>
                  <Button size="sm" variant="outline" className="h-8 rounded-lg" disabled={page >= totalPages} onClick={() => setPage((value) => value + 1)}>Berikutnya</Button>
                </div>
              </div>
            </>
          )}
        </CardContent>
      </Card>
    </>
  )
}

function ValuePanel({ label, value }: { label: string; value: string }) {
  return <div className="rounded-lg border border-slate-100 bg-slate-50 p-2.5"><p className="mb-1 text-[10px] font-semibold tracking-wide text-slate-400 uppercase">{label}</p><pre className="max-h-48 overflow-auto whitespace-pre-wrap break-all font-mono text-[10px] leading-relaxed text-slate-600">{value}</pre></div>
}
