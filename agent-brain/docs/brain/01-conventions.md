# Konvensi

## Naming (C#)

- Class / method / property: `PascalCase`. Variabel lokal: `camelCase`. Private field: `_camelCase`. Interface: `I` + `PascalCase` (`IAssetService`).
- DTO: `<Nama>Request` / `<Nama>Response`. Entity: nama domain tunggal (`Asset`, `Loan`).
- Endpoint: `/api/v1/<resource>` plural, lowercase (`/api/v1/assets`).

## API

- Response seragam: `{ "success": true, "data": ..., "message": "...", "errors": null }`.
- Status: 200 OK, 201 Created, 400 validasi, 401 belum login, 403 tidak berhak, 404 tidak ketemu, 409 konflik (misal aset sedang dipinjam), 500 hanya untuk error tak terduga.
- Pagination: `?page=1&pageSize=20`, response sertakan `totalCount`.
- Jangan kembalikan stack trace, password hash, atau token ke client.

## Validation & error

- Validasi di Application pakai FluentValidation. Pesan error jelas dan dalam bahasa yang sama dengan response (Indonesia untuk pesan user, English untuk nama field/kode).
- Business rule yang dilanggar = exception domain di Application, dipetakan ke status HTTP di Api. Jangan pakai `try/catch` untuk alur normal.

## EF Core

- Konfigurasi entity di `IEntityTypeConfiguration<T>`, di Infrastructure. Jangan pakai Data Annotation untuk relasi.
- Relasi + index didefinisikan eksplisit. Soft delete (kalau dipakai) lewat `IsDeleted` + global query filter, dan harus ditulis di konteks proyek.
- Migration: `dotnet ef migrations add <Nama>`. Jangan edit database manual, jangan edit file migration yang sudah di-apply.

## Umum

- `DateTime.UtcNow`, bukan `DateTime.Now`.
- Jangan hardcode URL, secret, atau connection string di kode. Pakai konfigurasi.
- Komentar hanya untuk "kenapa", bukan "apa". Kode yang jelas tidak butuh komentar panjang.
