# Copilot Instructions — Otak Umum

> File ini dibaca otomatis oleh GitHub Copilot di VS Code setiap kali Copilot Chat / Agent berjalan.
> Jangan duplikasi isinya ke chat. Kalau butuh detail, baca file yang dirujuk.

## Project

.NET 8 web app, Clean Architecture. Bahasa jawaban: Indonesia. Bahasa kode: English.

## Always read first

1. `AGENTS.md` (aturan umum, workflow, larangan)
2. `docs/brain/00-index.md` (peta file otak)
3. `docs/brain/10-*-context.md` — file konteks proyek yang aktif (misal Inventara). Ini sumber kebenaran untuk nama entitas, tabel, endpoint, dan business rule. Kalau konteks proyek tidak ada, tanya dulu sebelum mengarang.

## Workflow

Understand → Plan → Implement → Verify → Report. Satu tugas per prompt. Jangan lanjut sebelum `dotnet build` sukses.

## Architecture (non-negotiable)

- Dependency: Domain ← Application ← Infrastructure ← Api. Domain tidak depend ke apa pun.
- Controller tipis, tanpa logika bisnis. Logika di Application service. Data access di Infrastructure repository / DbContext.
- Semua I/O async dengan `CancellationToken`.
- Validasi di Application dengan FluentValidation. Response API seragam: `{ success, data, message, errors }`.

## Before you say "done"

- `dotnet build` sukses.
- Test terkait lolos (`dotnet test` untuk proyek yang diubah).
- Tidak ada file di luar scope yang berubah.
- Laporkan file yang diubah + hasil build/test.

## Never

- Commit / push / merge / reset / ubah branch tanpa instruksi eksplisit.
- Tambah scope, tambah package NuGet, atau ganti versi tanpa izin.
- Mengarang field, endpoint, atau library. Kalau tidak ada di konteks/kode, tanya.
- Tampilkan secret / token / password.
