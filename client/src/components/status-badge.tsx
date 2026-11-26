import { Badge } from "@/components/ui/badge"
import type { AssetStatus, LoanStatus, MaintenanceStatus } from "@/types"

const styles: Record<string, string> = {
  Available: "border-emerald-200 bg-emerald-50 text-emerald-700",
  OnLoan: "border-sky-200 bg-sky-50 text-sky-700",
  Maintenance: "border-orange-200 bg-orange-50 text-orange-700",
  Retired: "border-slate-200 bg-slate-100 text-slate-600",
  Pending: "border-amber-200 bg-amber-50 text-amber-700",
  Approved: "border-indigo-200 bg-indigo-50 text-indigo-700",
  Rejected: "border-rose-200 bg-rose-50 text-rose-700",
  Returned: "border-emerald-200 bg-emerald-50 text-emerald-700",
  Overdue: "border-rose-200 bg-rose-50 text-rose-700",
  Reported: "border-orange-200 bg-orange-50 text-orange-700",
  InProgress: "border-sky-200 bg-sky-50 text-sky-700",
  Done: "border-emerald-200 bg-emerald-50 text-emerald-700",
  Cancelled: "border-slate-200 bg-slate-100 text-slate-600",
  Good: "border-emerald-200 bg-emerald-50 text-emerald-700",
  MinorDamage: "border-amber-200 bg-amber-50 text-amber-700",
  MajorDamage: "border-rose-200 bg-rose-50 text-rose-700",
}

export function StatusBadge({
  status,
}: {
  status: AssetStatus | LoanStatus | MaintenanceStatus | string
}) {
  return (
    <Badge variant="outline" className={styles[status] ?? "border-slate-200 bg-slate-50 text-slate-600"}>
      {status.replace(/([a-z])([A-Z])/g, "$1 $2")}
    </Badge>
  )
}
