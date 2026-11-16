# Otak Agent — Template Reusable

Dua lapis. Lapis umum dipakai di SEMUA proyek. Lapis proyek dipakai di satu proyek saja.

## Struktur

```
agent-brain/
├── AGENTS.md                          # Otak umum, dibaca semua tool (Copilot, Cursor, Claude, Antigravity)
├── .github/
│   ├── copilot-instructions.md        # Otak umum, dibaca OTOMATIS oleh VS Code Copilot tiap chat
│   └── instructions/
│       ├── clean-architecture.instructions.md   # applyTo: src/**/*.cs
│       ├── testing.instructions.md              # applyTo: **/*Tests*/**/*.cs
│       └── workflow.instructions.md             # applyTo: **/*.md
├── docs/brain/
│   ├── 00-index.md                    # Peta otak: file apa dibaca kapan
│   ├── 01-conventions.md              # Naming, API, error, EF Core
│   ├── 02-definition-of-done.md       # Checklist sebelum tugas dibilang selesai
│   └── 03-new-project-template.md     # Template bikin lapis proyek baru
└── inventara/                         # CONTOH lapis proyek (Inventara)
    ├── .github/instructions/inventara-domain.instructions.md
    └── docs/brain/10-inventara-context.md
```

## Cara pakai di proyek baru (1 menit)

1. Buat folder proyek kosong di VS Code.
2. Copy SEMUA isi folder `agent-brain/` KECUALI folder `inventara/` ke root proyek.
   - Kalau proyeknya Inventara, copy juga isi `inventara/` ke root (timpa/merge).
   - Kalau proyek lain, copy `docs/brain/03-new-project-template.md`, isi, simpan jadi `docs/brain/10-<nama-proyek>-context.md`.
3. Buka Copilot Chat → mode **Agent**.
4. Prompt pertama: `Baca .github/copilot-instructions.md dan docs/brain/00-index.md, lalu ikuti aturannya.`
5. Setelah itu jalankan prompt fitur satu per satu, satu tugas = satu prompt.

## Kenapa dua lapis?

- **Umum:** arsitektur, coding style, workflow, definition of done. Ini tidak berubah walau ganti proyek. Kalau ada pelajaran baru (misal: "jangan pakai X"), ubah di sini sekali, copy ke semua proyek.
- **Proyek:** nama domain, entitas, tabel, endpoint, business rule. Ini beda tiap proyek. Agent harus baca ini supaya tidak mengarang nama field atau endpoint.

## Hemat token

- File otak dibuat pendek dan spesifik. Copilot hanya membaca file yang relevan lewat `applyTo` (misal aturan test hanya dibaca saat edit file test).
- Jangan tempel isi semua file otak ke chat. Cukup sebut nama filenya, Copilot yang baca dari disk.
- Satu prompt = satu tugas kecil. Selesai + `dotnet build` sukses, baru prompt berikutnya.

## Yang TIDAK dijamin

Tidak ada otak yang bisa menjamin "zero bug". Yang otak ini lakukan: memaksa agent kerja kecil-kecil, ikut arsitektur yang sama, dan tidak boleh bilang selesai sebelum build + test lolos. Bug tetap bisa ada, tapi ketahuan lebih cepat dan lebih gampang dilacak.
