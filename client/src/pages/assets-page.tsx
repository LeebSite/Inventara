import { useEffect, useState } from "react"
import { Download, Eye, Pencil, Plus, Search, X } from "lucide-react"
import { Link } from "react-router-dom"
import api, { getErrorMessage, unwrap } from "@/lib/api"
import { formatCurrency } from "@/lib/format"
import type { ApiEnvelope, Asset, Category, Location, Page } from "@/types"
import { useAuth } from "@/auth/auth-context"
import { AssetFormDialog } from "@/components/asset-form-dialog"
import { ErrorState, LoadingBlock } from "@/components/feedback"
import { PageHeader } from "@/components/page-header"
import { StatusBadge } from "@/components/status-badge"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"

const pageSize = 10

export function AssetsPage() {
  const { role } = useAuth()
  const canEdit = role === "Admin" || role === "Staff"
  const [assets, setAssets] = useState<Asset[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [locations, setLocations] = useState<Location[]>([])
  const [search, setSearch] = useState("")
  const [status, setStatus] = useState("")
  const [categoryId, setCategoryId] = useState("")
  const [locationId, setLocationId] = useState("")
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const [totalItems, setTotalItems] = useState(0)
  const [loading, setLoading] = useState(true)
  const [exporting, setExporting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [selectedAsset, setSelectedAsset] = useState<Asset | undefined>()

  async function loadAssets() {
    setLoading(true)
    setError(null)
    try {
      const response = await api.get<ApiEnvelope<Page<Asset>>>("/assets", {
        params: {
          search: search || undefined,
          status: status || undefined,
          categoryId: categoryId || undefined,
          locationId: locationId || undefined,
          page,
          pageSize,
          sortBy: "assetCode",
          sortDir: "asc",
        },
      })
      const result = unwrap(response)
      setAssets(result.items)
      setTotalPages(result.totalPages)
      setTotalItems(result.totalItems)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    void loadAssets()
  }, [search, status, categoryId, locationId, page])

  useEffect(() => {
    Promise.all([
      api.get<ApiEnvelope<Page<Category>>>("/categories", { params: { page: 1, pageSize: 100 } }),
      api.get<ApiEnvelope<Page<Location>>>("/locations", { params: { page: 1, pageSize: 100 } }),
    ])
      .then(([categoryResponse, locationResponse]) => {
        setCategories(unwrap(categoryResponse).items)
        setLocations(unwrap(locationResponse).items)
      })
      .catch((requestError: unknown) => setError(getErrorMessage(requestError)))
  }, [])

  function changeFilter(setter: (value: string) => void, value: string) {
    setter(value)
    setPage(1)
  }

  async function exportAssets() {
    setExporting(true)
    try {
      const response = await api.get("/assets/export", {
        params: {
          search: search || undefined,
          status: status || undefined,
          categoryId: categoryId || undefined,
          locationId: locationId || undefined,
        },
        responseType: "blob",
      })
      const url = URL.createObjectURL(response.data as Blob)
      const anchor = document.createElement("a")
      anchor.href = url
      anchor.download = `inventara-assets-${new Date().toISOString().slice(0, 10)}.xlsx`
      anchor.click()
      URL.revokeObjectURL(url)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setExporting(false)
    }
  }

  function openCreate() {
    setSelectedAsset(undefined)
    setDialogOpen(true)
  }

  function openEdit(asset: Asset) {
    setSelectedAsset(asset)
    setDialogOpen(true)
  }

  return (
    <>
      <PageHeader
        eyebrow="Inventaris"
        title="Daftar aset"
        description="Cari, filter, dan kelola seluruh aset organisasi."
        actions={
          <>
            {canEdit && (
              <Button variant="outline" className="rounded-xl" onClick={() => void exportAssets()} disabled={exporting}>
                <Download className="size-4" /> {exporting ? "Menyiapkan..." : "Export Excel"}
              </Button>
            )}
            {canEdit && (
              <Button className="rounded-xl shadow-sm shadow-teal-200" onClick={openCreate}>
                <Plus className="size-4" /> Tambah aset
              </Button>
            )}
          </>
        }
      />
      <Card className="rounded-2xl border-slate-200/80 shadow-sm shadow-slate-200/40">
        <CardContent className="p-0">
          <div className="flex flex-col gap-3 border-b border-slate-100 p-4 xl:flex-row xl:items-center xl:justify-between">
            <div className="relative w-full xl:max-w-sm">
              <Search className="absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-slate-400" />
              <Input value={search} onChange={(event) => changeFilter(setSearch, event.target.value)} placeholder="Cari nama atau kode aset..." className="h-10 rounded-xl border-slate-200 pl-10" />
            </div>
            <div className="grid gap-2 sm:grid-cols-3">
              <select aria-label="Filter status" className="form-select" value={status} onChange={(event) => changeFilter(setStatus, event.target.value)}>
                <option value="">Semua status</option>
                <option value="Available">Tersedia</option>
                <option value="OnLoan">Dipinjam</option>
                <option value="Maintenance">Maintenance</option>
                <option value="Retired">Pensiun</option>
              </select>
              <select aria-label="Filter kategori" className="form-select" value={categoryId} onChange={(event) => changeFilter(setCategoryId, event.target.value)}>
                <option value="">Semua kategori</option>
                {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
              </select>
              <select aria-label="Filter lokasi" className="form-select" value={locationId} onChange={(event) => changeFilter(setLocationId, event.target.value)}>
                <option value="">Semua lokasi</option>
                {locations.map((location) => <option key={location.id} value={location.id}>{location.name}</option>)}
              </select>
            </div>
          </div>
          {error && <div className="px-4"><ErrorState message={error} onRetry={() => void loadAssets()} /></div>}
          {loading ? (
            <LoadingBlock />
          ) : (
            <>
              <div className="overflow-x-auto">
                <Table>
                  <TableHeader>
                    <TableRow className="bg-slate-50/70 hover:bg-slate-50/70">
                      <TableHead className="pl-5">Kode / nama aset</TableHead>
                      <TableHead>Kategori</TableHead>
                      <TableHead>Lokasi</TableHead>
                      <TableHead>Status</TableHead>
                      <TableHead>Harga</TableHead>
                      <TableHead className="pr-5 text-right">Aksi</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {assets.map((asset) => (
                      <TableRow key={asset.id}>
                        <TableCell className="py-3.5 pl-5">
                          <Link to={`/assets/${asset.id}`} className="font-semibold text-slate-800 hover:text-teal-700">{asset.assetCode}</Link>
                          <p className="mt-0.5 text-xs text-slate-400">{asset.name}</p>
                        </TableCell>
                        <TableCell className="text-sm text-slate-600">{asset.categoryName}</TableCell>
                        <TableCell className="text-sm text-slate-600">{asset.locationName}</TableCell>
                        <TableCell><StatusBadge status={asset.status} /></TableCell>
                        <TableCell className="text-sm text-slate-600">{formatCurrency(asset.purchasePrice)}</TableCell>
                        <TableCell className="pr-5 text-right">
                          <div className="flex justify-end gap-1">
                            <Button asChild size="icon" variant="ghost" title="Detail aset">
                              <Link to={`/assets/${asset.id}`}><Eye className="size-4 text-slate-500" /></Link>
                            </Button>
                            {canEdit && <Button size="icon" variant="ghost" title="Edit aset" onClick={() => openEdit(asset)}><Pencil className="size-4 text-slate-500" /></Button>}
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                    {!assets.length && (
                      <TableRow><TableCell colSpan={6} className="h-48 text-center text-sm text-slate-400">
                        {search || status || categoryId || locationId ? <button className="inline-flex items-center gap-1 hover:text-slate-700" onClick={() => { setSearch(""); setStatus(""); setCategoryId(""); setLocationId("") }}>Tidak ada hasil. <X className="size-3.5" /> Hapus filter</button> : "Belum ada aset terdaftar."}
                      </TableCell></TableRow>
                    )}
                  </TableBody>
                </Table>
              </div>
              <Pagination page={page} totalPages={totalPages} totalItems={totalItems} pageSize={pageSize} onPage={setPage} />
            </>
          )}
        </CardContent>
      </Card>
      <AssetFormDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        asset={selectedAsset}
        onSaved={loadAssets}
      />
    </>
  )
}

export function Pagination({
  page,
  totalPages,
  totalItems,
  pageSize,
  onPage,
}: {
  page: number
  totalPages: number
  totalItems: number
  pageSize: number
  onPage: (page: number) => void
}) {
  const from = totalItems === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, totalItems)
  return (
    <div className="flex flex-col gap-3 border-t border-slate-100 px-5 py-3.5 sm:flex-row sm:items-center sm:justify-between">
      <p className="text-xs text-slate-400">Menampilkan <span className="font-medium text-slate-600">{from}–{to}</span> dari <span className="font-medium text-slate-600">{totalItems}</span> data</p>
      <div className="flex items-center gap-2">
        <Button size="sm" variant="outline" className="h-8 rounded-lg" disabled={page <= 1} onClick={() => onPage(page - 1)}>Sebelumnya</Button>
        <span className="min-w-16 text-center text-xs text-slate-500">{page} / {Math.max(totalPages, 1)}</span>
        <Button size="sm" variant="outline" className="h-8 rounded-lg" disabled={page >= totalPages} onClick={() => onPage(page + 1)}>Berikutnya</Button>
      </div>
    </div>
  )
}
