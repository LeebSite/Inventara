import { useEffect, useState, type FormEvent } from "react"
import api, { getErrorMessage, unwrap } from "@/lib/api"
import { toDateInput } from "@/lib/format"
import type { ApiEnvelope, Asset, AssetCondition, AssetStatus, Category, Location, Page, Vendor } from "@/types"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"

interface AssetFormValues {
  name: string
  categoryId: string
  locationId: string
  vendorId: string
  serialNumber: string
  purchaseDate: string
  purchasePrice: string
  condition: AssetCondition
  status: AssetStatus
  notes: string
}

const emptyValues: AssetFormValues = {
  name: "",
  categoryId: "",
  locationId: "",
  vendorId: "",
  serialNumber: "",
  purchaseDate: "",
  purchasePrice: "",
  condition: "Good",
  status: "Available",
  notes: "",
}

function toValues(asset?: Asset): AssetFormValues {
  if (!asset) return emptyValues
  return {
    name: asset.name,
    categoryId: asset.categoryId,
    locationId: asset.locationId,
    vendorId: asset.vendorId ?? "",
    serialNumber: asset.serialNumber ?? "",
    purchaseDate: toDateInput(asset.purchaseDate),
    purchasePrice: String(asset.purchasePrice),
    condition: asset.condition,
    status: asset.status,
    notes: asset.notes ?? "",
  }
}

export function AssetFormDialog({
  open,
  onOpenChange,
  asset,
  onSaved,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  asset?: Asset
  onSaved: () => Promise<void>
}) {
  const [values, setValues] = useState<AssetFormValues>(() => toValues(asset))
  const [categories, setCategories] = useState<Category[]>([])
  const [locations, setLocations] = useState<Location[]>([])
  const [vendors, setVendors] = useState<Vendor[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!open) return
    setValues(toValues(asset))
    setError(null)
    void Promise.all([
      api.get<ApiEnvelope<Page<Category>>>("/categories", { params: { page: 1, pageSize: 100 } }),
      api.get<ApiEnvelope<Page<Location>>>("/locations", { params: { page: 1, pageSize: 100 } }),
      api.get<ApiEnvelope<Page<Vendor>>>("/vendors", { params: { page: 1, pageSize: 100 } }),
    ])
      .then(([categoryResponse, locationResponse, vendorResponse]) => {
        setCategories(unwrap(categoryResponse).items)
        setLocations(unwrap(locationResponse).items)
        setVendors(unwrap(vendorResponse).items)
      })
      .catch((requestError: unknown) => setError(getErrorMessage(requestError)))
  }, [open, asset])

  function update<K extends keyof AssetFormValues>(key: K, value: AssetFormValues[K]) {
    setValues((current) => ({ ...current, [key]: value }))
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setLoading(true)
    setError(null)
    const body = {
      name: values.name.trim(),
      categoryId: values.categoryId,
      locationId: values.locationId,
      vendorId: values.vendorId || null,
      serialNumber: values.serialNumber.trim() || null,
      purchaseDate: new Date(`${values.purchaseDate}T00:00:00Z`).toISOString(),
      purchasePrice: Number(values.purchasePrice),
      condition: values.condition,
      status: values.status,
      notes: values.notes.trim() || null,
    }

    try {
      if (asset) await api.put(`/assets/${asset.id}`, body)
      else await api.post("/assets", body)
      await onSaved()
      onOpenChange(false)
    } catch (requestError) {
      setError(getErrorMessage(requestError))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[92vh] overflow-y-auto sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle>{asset ? "Ubah data aset" : "Tambah aset baru"}</DialogTitle>
          <DialogDescription>Lengkapi spesifikasi aset. Kode aset dibuat otomatis oleh sistem.</DialogDescription>
        </DialogHeader>
        <form className="space-y-4" onSubmit={(event) => void submit(event)}>
          <div className="grid gap-4 sm:grid-cols-2">
            <FormField label="Nama aset">
              <Input required value={values.name} onChange={(event) => update("name", event.target.value)} />
            </FormField>
            <FormField label="Nomor seri">
              <Input value={values.serialNumber} onChange={(event) => update("serialNumber", event.target.value)} />
            </FormField>
            <FormField label="Kategori">
              <select required className="form-select" value={values.categoryId} onChange={(event) => update("categoryId", event.target.value)}>
                <option value="">Pilih kategori</option>
                {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
              </select>
            </FormField>
            <FormField label="Lokasi">
              <select required className="form-select" value={values.locationId} onChange={(event) => update("locationId", event.target.value)}>
                <option value="">Pilih lokasi</option>
                {locations.map((location) => <option key={location.id} value={location.id}>{location.name}</option>)}
              </select>
            </FormField>
            <FormField label="Vendor">
              <select className="form-select" value={values.vendorId} onChange={(event) => update("vendorId", event.target.value)}>
                <option value="">Tanpa vendor</option>
                {vendors.map((vendor) => <option key={vendor.id} value={vendor.id}>{vendor.name}</option>)}
              </select>
            </FormField>
            <FormField label="Tanggal pembelian">
              <Input required type="date" value={values.purchaseDate} onChange={(event) => update("purchaseDate", event.target.value)} />
            </FormField>
            <FormField label="Harga pembelian (Rp)">
              <Input required min="0" step="1000" type="number" value={values.purchasePrice} onChange={(event) => update("purchasePrice", event.target.value)} />
            </FormField>
            <FormField label="Kondisi">
              <select className="form-select" value={values.condition} onChange={(event) => update("condition", event.target.value as AssetCondition)}>
                <option value="Good">Baik</option>
                <option value="MinorDamage">Kerusakan ringan</option>
                <option value="MajorDamage">Kerusakan berat</option>
              </select>
            </FormField>
            <FormField label="Status">
              <select className="form-select" value={values.status} onChange={(event) => update("status", event.target.value as AssetStatus)}>
                <option value="Available">Tersedia</option>
                <option value="OnLoan">Dipinjam</option>
                <option value="Maintenance">Maintenance</option>
                <option value="Retired">Pensiun</option>
              </select>
            </FormField>
          </div>
          <FormField label="Catatan">
            <Textarea rows={3} value={values.notes} onChange={(event) => update("notes", event.target.value)} />
          </FormField>
          {error && <p role="alert" className="text-sm text-rose-600">{error}</p>}
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Batal</Button>
            <Button type="submit" disabled={loading}>{loading ? "Menyimpan..." : "Simpan aset"}</Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}

function FormField({ label, children }: { label: string; children: React.ReactNode }) {
  const id = label.toLowerCase().replaceAll(" ", "-")
  return (
    <div className="space-y-2">
      <Label htmlFor={id} className="text-xs font-medium text-slate-600">{label}</Label>
      <div id={id}>{children}</div>
    </div>
  )
}
