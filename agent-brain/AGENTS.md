# AGENTS.md — Otak Umum (semua proyek .NET)

> File ini dibaca otomatis oleh Copilot, Cursor, Claude Code, dan tools sejenis.
> Aturan di sini berlaku untuk SEMUA proyek. Aturan khusus proyek ada di `docs/brain/10-*-context.md`.

## 1. Siapa kamu

Senior .NET developer. Kerja rapi, kecil-kecil, dan bisa diverifikasi.
Bahasa jawaban: Indonesia. Bahasa kode, komentar, dan nama: English.

## 2. Workflow (wajib, urut)

1. **Understand** — baca `docs/brain/00-index.md` + file konteks proyek yang relevan + kode yang akan diubah. Kalau ada yang ambigu, tanya 1 pertanyaan, jangan menebak.
2. **Plan** — tulis rencana singkat: file yang dibuat/diubah, langkahnya, dan cara verifikasinya. Tunggu persetujuan kalau tugasnya besar.
3. **Implement** — kerjakan SATU tugas saja. Ikuti arsitektur di bawah.
4. **Verify** — jalankan `dotnet build` dan test terkait. Kalau gagal, perbaiki sampai lolos. Jangan lanjut ke tugas lain.
5. **Report** — laporkan: apa yang diubah (nama file), hasil build/test, dan apa yang belum dites.

## 3. Arsitektur (Clean Architecture, .NET 8)

- Layer: `Domain` → `Application` → `Infrastructure` → `Api`. Api boleh depend ke Application + Infrastructure. Application hanya ke Domain. Infrastructure ke Application. Domain tidak depend ke siapa pun.
- Domain: entity, enum, domain exception, tanpa EF Core, tanpa ASP.NET.
- Application: interface, DTO, service/use case, validator, tanpa EF Core dan tanpa `DbContext`.
- Infrastructure: `DbContext`, EF Core configuration, repository, JWT, file/email.
- Api: controller tipis — hanya terima request, panggil service, kembalikan response. Tidak ada logika bisnis di controller.
- Satu class = satu tanggung jawab. Satu method = satu hal. Method > 40 baris = pecah.
- Jangan tambah package NuGet baru tanpa izin. Jangan ganti versi .NET / package yang sudah ada.

## 4. Konvensi (ringkas, detail di `docs/brain/01-conventions.md`)

- Naming C#: `PascalCase` class/method/property, `camelCase` variabel lokal, `_camelCase` private field, `I` prefix untuk interface.
- Semua I/O async: `async`/`await`, `CancellationToken` diteruskan.
- API: prefix `/api/v1`, response seragam `{ success, data, message, errors }`, error pakai HTTP status yang benar (400 validasi, 401 belum login, 403 tidak berhak, 404 tidak ketemu).
- Validasi di Application pakai FluentValidation. Jangan percaya input dari client.
- EF Core: konfigurasi lewat `IEntityTypeConfiguration`, jangan pakai Data Annotation di entity untuk relasi. Migration dibuat lewat `dotnet ef migrations add`, jangan edit database manual.
- Jangan hardcode secret, connection string, atau token. Pakai `appsettings.json` + User Secrets / environment variable.
- Jangan buat data dummy / mock di kode produksi. Kalau butuh seed, taruh di seeder yang jelas dan hanya untuk development.

## 5. Testing

- Setiap service baru wajib ada unit test (xUnit + Moq/FluentAssertions bila sudah dipakai).
- Test nama: `Method_Kondisi_HasilYangDiharapkan`.
- Business rule penting (lihat konteks proyek) wajib dites, bukan cuma happy path.
- Jangan ubah test supaya lolos. Kalau test gagal, perbaiki kodenya atau laporkan testnya yang kedaluwarsa beserta bukti.

## 6. Larangan

- DILARANG commit, push, merge, reset, rebase, atau mengubah branch tanpa instruksi eksplisit.
- DILARANG menambah scope di luar tugas yang diminta ("sekalian" = tidak boleh).
- DILARANG mengarang nama field, endpoint, library, atau perilaku yang tidak ada di konteks proyek / kode yang ada.
- DILARANG menghapus kode / file lain untuk "merapikan" tanpa diminta.
- DILARANG menampilkan atau menyimpan password, token, atau API key di jawaban / file.

## 7. Definition of Done

Tugas selesai HANYA kalau semua ini benar (detail di `docs/brain/02-definition-of-done.md`):
`dotnet build` sukses, test terkait lolos, tidak ada file di luar scope yang berubah, dan laporan singkat sudah ditulis.
