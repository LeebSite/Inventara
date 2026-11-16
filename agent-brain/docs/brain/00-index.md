# Index Otak — baca file ini dulu

## Peta file

| Kapan dibaca | File |
|---|---|
| Selalu, tiap tugas | `AGENTS.md`, `.github/copilot-instructions.md` |
| Sebelum ngoding C# | `.github/instructions/clean-architecture.instructions.md` |
| Sebelum nulis test | `.github/instructions/testing.instructions.md` |
| Sebelum ubah file `.md` otak | `.github/instructions/workflow.instructions.md` |
| Butuh konvensi detail | `docs/brain/01-conventions.md` |
| Sebelum bilang selesai | `docs/brain/02-definition-of-done.md` |
| Proyek baru | `docs/brain/03-new-project-template.md` |
| Konteks proyek aktif | `docs/brain/10-*-context.md` (satu file per proyek) |

## Aturan baca (hemat token)

- Jangan baca semua file sekaligus. Baca file umum + file konteks proyek + file instruksi yang `applyTo`-nya cocok dengan file yang sedang diubah.
- Kalau ada konflik: file konteks proyek menang atas aturan umum, tapi larangan di `AGENTS.md` (no commit/push, no scope creep, no secret) tidak bisa ditimpa file proyek.
- Kalau informasi yang dibutuhkan tidak ada di file mana pun: tanya user, jangan mengarang.
