import { AlertCircle, LoaderCircle } from "lucide-react"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { Button } from "@/components/ui/button"

export function LoadingBlock({ label = "Memuat data..." }: { label?: string }) {
  return (
    <div className="flex min-h-48 items-center justify-center gap-3 text-sm text-slate-500">
      <LoaderCircle className="size-5 animate-spin text-teal-600" />
      {label}
    </div>
  )
}

export function ErrorState({
  message,
  onRetry,
}: {
  message: string
  onRetry?: () => void
}) {
  return (
    <Alert variant="destructive" className="my-4">
      <AlertCircle />
      <AlertTitle>Gagal memuat data</AlertTitle>
      <AlertDescription className="flex flex-wrap items-center justify-between gap-3">
        <span>{message}</span>
        {onRetry && (
          <Button size="sm" variant="outline" onClick={onRetry}>
            Coba lagi
          </Button>
        )}
      </AlertDescription>
    </Alert>
  )
}

export function EmptyState({
  title,
  description,
}: {
  title: string
  description: string
}) {
  return (
    <div className="flex min-h-44 flex-col items-center justify-center px-6 text-center">
      <div className="mb-3 grid size-11 place-items-center rounded-2xl bg-slate-100 text-slate-400">
        <span className="text-lg">—</span>
      </div>
      <h3 className="font-medium text-slate-800">{title}</h3>
      <p className="mt-1 max-w-sm text-sm text-slate-500">{description}</p>
    </div>
  )
}
