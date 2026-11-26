import { useEffect, useMemo, useState, type FormEvent } from "react"
import { Pencil, Plus, Trash2, Users } from "lucide-react"
import api, { getErrorMessage, unwrap } from "@/lib/api"
import type { AdminUser, ApiEnvelope, Category, Location, Page, Vendor } from "@/types"
import { ErrorState, LoadingBlock } from "@/components/feedback"
import { PageHeader } from "@/components/page-header"
import { Button } from "@/components/ui/button"
import { Card } from "@/components/ui/card"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Textarea } from "@/components/ui/textarea"

type MasterRow = Category | Location | Vendor | AdminUser
type MasterKind = "categories" | "locations" | "vendors" | "users"
type Field = { key: string; label: string; type?: "email" | "password" | "textarea" | "select"; required?: boolean; options?: string[] }

const fields: Record<MasterKind, Field[]> = {
  categories: [
    { key: "name", label: "Nama kategori", required: true },
    { key: "description", label: "Deskripsi", type: "textarea", required: true },
  ],
  locations: [
    { key: "name", label: "Nama lokasi", required: true },
    { key: "building", label: "Gedung", required: true },
    { key: "floor", label: "Lantai", required: true },
    { key: "description", label: "Deskripsi", type: "textarea", required: true },
  ],
  vendors: [
    { key: "name", label: "Nama vendor", required: true },
    { key: "contactPerson", label: "Kontak person", required: true },
    { key: "email", label: "Email", type: "email", required: true },
    { key: "phone", label: "Telepon", required: true },
    { key: "address", label: "Alamat", type: "textarea", required: true },
  ],
  users: [
    { key: "fullName", label: "Nama lengkap", required: true },
    { key: "email", label: "Email", type: "email", required: true },
    { key: "password", label: "Kata sandi", type: "password" },
    { key: "role", label: "Role", type: "select", required: true, options: ["Admin", "Staff", "User"] },
    { key: "department", label: "Departemen", required: true },
    { key: "phone", label: "Telepon", required: true },
    { key: "isActive", label: "Status akun", type: "select", required: true, options: ["true", "false"] },
  ],
}

const labels: Record<MasterKind, string> = {
  categories: "Kategori",
  locations: "Lokasi",
  vendors: "Vendor",
  users: "Pengguna",
}

function initialValues(kind: MasterKind): Record<string, string> {
  return Object.fromEntries(fields[kind].map((field) => [field.key, field.key === "role" ? "User" : field.key === "isActive" ? "true" : ""]))
}

function toValues(kind: MasterKind, row?: MasterRow) {
  const values = initialValues(kind)
  if (!row) return values
  for (const field of fields[kind]) {
    const value = Object.getOwnPropertyDescriptor(row, field.key)?.value
    if (value !== undefined && value !== null) values[field.key] = String(value)
  }
  return values
}

function toPayload(kind: MasterKind, values: Record<string, string>, editing: boolean) {
  const payload: Record<string, unknown> = {}
  for (const field of fields[kind]) {
    const value = values[field.key] ?? ""
    if (field.key === "password" && editing && !value) continue
    payload[field.key] = field.key === "isActive" ? value === "true" : value.trim()
  }
  return payload
}

export function MasterDataPage() {
  const [kind, setKind] = useState<MasterKind>("categories")
  const [rows, setRows] = useState<MasterRow[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<MasterRow | undefined>()
  const [values, setValues] = useState<Record<string, string>>(() => initialValues("categories"))
  const [submitting, setSubmitting] = useState(false)

  const dataFields = useMemo(() => fields[kind].filter((field) => field.key !== "password" || !editing), [kind, editing])

  async function load() {
    setLoading(true)
    setError(null)
    try {
      const endpoint = kind === "users" ? "/users" : `/${kind}`
      const response = await api.get<ApiEnvelope<Page<MasterRow>>>(endpoint, { params: { page: 1, pageSize: 100, sortBy: kind === "users" ? "fullName" : "name", sortDir: "asc" } })
      setRows(unwrap(response).items)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [kind])

  function selectKind(nextKind: MasterKind) {
    setKind(nextKind)
    setEditing(undefined)
  }

  function openCreate() {
    setEditing(undefined)
    setValues(initialValues(kind))
    setDialogOpen(true)
  }

  function openEdit(row: MasterRow) {
    setEditing(row)
    setValues(toValues(kind, row))
    setDialogOpen(true)
  }

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      const body = toPayload(kind, values, Boolean(editing))
      if (editing) {
        await api.put(`/${kind}/${editing.id}`, body)
      } else if (kind === "users") {
        await api.post("/auth/register", body)
      } else {
        await api.post(`/${kind}`, body)
      }
      setDialogOpen(false)
      await load()
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setSubmitting(false)
    }
  }

  async function remove(row: MasterRow) {
    const name = "name" in row ? row.name : row.fullName
    if (!window.confirm(`Hapus "${name}"? Tindakan ini tidak dapat dibatalkan.`)) return
    setError(null)
    try {
      await api.delete(`/${kind}/${row.id}`)
      await load()
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    }
  }

  return (
    <>
      <PageHeader eyebrow="Konfigurasi organisasi" title="Master data" description="Kelola kategori, lokasi, vendor, dan akun pengguna." actions={<Button className="rounded-xl" onClick={openCreate}><Plus className="size-4" /> Tambah {labels[kind].toLowerCase()}</Button>} />
      {error && <ErrorState message={error} onRetry={() => void load()} />}
      <div className="mb-5 flex flex-wrap gap-2">
        {(Object.keys(labels) as MasterKind[]).map((value) => (
          <Button key={value} variant={value === kind ? "default" : "outline"} className="rounded-xl" onClick={() => selectKind(value)}>
            {value === "users" ? <Users className="size-4" /> : null}{labels[value]}
          </Button>
        ))}
      </div>
      <Card className="rounded-2xl border-slate-200/80 shadow-sm">
        <div className="border-b border-slate-100 px-5 py-4"><h2 className="text-sm font-semibold text-slate-800">Data {labels[kind].toLowerCase()}</h2><p className="mt-1 text-xs text-slate-400">{rows.length} data ditampilkan</p></div>
        {loading ? <LoadingBlock /> : (
          <div className="overflow-x-auto">
            <Table>
              <TableHeader><TableRow className="bg-slate-50/70">
                <TableHead className="pl-5">{kind === "users" ? "Nama / email" : "Nama"}</TableHead>
                {(kind === "categories" && <TableHead>Deskripsi</TableHead>)}
                {(kind === "locations" && <><TableHead>Gedung</TableHead><TableHead>Lantai</TableHead><TableHead>Deskripsi</TableHead></>)}
                {(kind === "vendors" && <><TableHead>Kontak</TableHead><TableHead>Email</TableHead><TableHead>Telepon</TableHead></>)}
                {(kind === "users" && <><TableHead>Role</TableHead><TableHead>Departemen</TableHead><TableHead>Status</TableHead><TableHead>Bergabung</TableHead></>)}
                <TableHead className="pr-5 text-right">Aksi</TableHead>
              </TableRow></TableHeader>
              <TableBody>
                {rows.map((row) => <TableRow key={row.id}>
                  <TableCell className="py-3.5 pl-5">
                    <p className="font-medium text-slate-800">{kind === "users" ? (row as AdminUser).fullName : (row as Category | Location | Vendor).name}</p>
                    {kind === "users" && <p className="mt-0.5 text-xs text-slate-400">{(row as AdminUser).email}</p>}
                  </TableCell>
                  {kind === "categories" && <TableCell className="max-w-md text-sm text-slate-500">{(row as Category).description || "—"}</TableCell>}
                  {kind === "locations" && <><TableCell className="text-sm text-slate-600">{(row as Location).building}</TableCell><TableCell className="text-sm text-slate-600">{(row as Location).floor}</TableCell><TableCell className="max-w-sm text-sm text-slate-500">{(row as Location).description}</TableCell></>}
                  {kind === "vendors" && <><TableCell className="text-sm text-slate-600">{(row as Vendor).contactPerson}</TableCell><TableCell className="text-sm text-slate-600">{(row as Vendor).email}</TableCell><TableCell className="text-sm text-slate-600">{(row as Vendor).phone}</TableCell></>}
                  {kind === "users" && <><TableCell className="text-sm text-slate-600">{(row as AdminUser).role}</TableCell><TableCell className="text-sm text-slate-600">{(row as AdminUser).department || "—"}</TableCell><TableCell><span className={`rounded-full px-2.5 py-1 text-xs font-medium ${(row as AdminUser).isActive ? "bg-emerald-50 text-emerald-700" : "bg-slate-100 text-slate-500"}`}>{(row as AdminUser).isActive ? "Aktif" : "Nonaktif"}</span></TableCell><TableCell className="whitespace-nowrap text-xs text-slate-500">{new Date((row as AdminUser).createdAt).toLocaleDateString("id-ID")}</TableCell></>}
                  <TableCell className="pr-5 text-right">
                    <div className="flex justify-end gap-1">
                      <Button size="icon" variant="ghost" title="Ubah" onClick={() => openEdit(row)}><Pencil className="size-4 text-slate-500" /></Button>
                      <Button size="icon" variant="ghost" title="Hapus" onClick={() => void remove(row)}><Trash2 className="size-4 text-rose-500" /></Button>
                    </div>
                  </TableCell>
                </TableRow>)}
                {!rows.length && <TableRow><TableCell colSpan={kind === "locations" ? 5 : kind === "vendors" || kind === "users" ? 5 : 3} className="py-16 text-center text-sm text-slate-400">Belum ada data {labels[kind].toLowerCase()}.</TableCell></TableRow>}
              </TableBody>
            </Table>
          </div>
        )}
      </Card>
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
          <DialogHeader><DialogTitle>{editing ? `Ubah ${labels[kind].toLowerCase()}` : `Tambah ${labels[kind].toLowerCase()}`}</DialogTitle><DialogDescription>Isi informasi yang diperlukan untuk data ini.</DialogDescription></DialogHeader>
          <form onSubmit={(event) => void save(event)} className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2">
              {dataFields.map((field) => <div key={field.key} className={`space-y-2 ${field.type === "textarea" ? "sm:col-span-2" : ""}`}>
                <Label htmlFor={`master-${field.key}`} className="text-xs text-slate-600">{field.label}</Label>
                {field.type === "textarea" ? (
                  <Textarea id={`master-${field.key}`} required={field.required} value={values[field.key] ?? ""} onChange={(event) => setValues((current) => ({ ...current, [field.key]: event.target.value }))} rows={3} />
                ) : field.type === "select" ? (
                  <select id={`master-${field.key}`} className="form-select" required={field.required} value={values[field.key] ?? ""} onChange={(event) => setValues((current) => ({ ...current, [field.key]: event.target.value }))}>
                    {field.options?.map((option) => <option key={option} value={option}>{field.key === "isActive" ? option === "true" ? "Aktif" : "Nonaktif" : option}</option>)}
                  </select>
                ) : (
                  <Input id={`master-${field.key}`} type={field.type ?? "text"} required={field.required} minLength={field.key === "password" ? 8 : undefined} autoComplete={field.key === "password" ? "new-password" : undefined} value={values[field.key] ?? ""} onChange={(event) => setValues((current) => ({ ...current, [field.key]: event.target.value }))} placeholder={field.key === "password" && editing ? "Kosongkan agar tidak berubah" : undefined} />
                )}
              </div>)}
            </div>
            <DialogFooter><Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>Batal</Button><Button type="submit" disabled={submitting}>{submitting ? "Menyimpan..." : "Simpan"}</Button></DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  )
}
