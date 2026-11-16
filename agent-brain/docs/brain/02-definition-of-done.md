# Definition of Done — checklist sebelum bilang selesai

Tugas selesai HANYA kalau semua kotak ini tercentang:

- [ ] `dotnet build` sukses (tanpa warning baru).
- [ ] `dotnet test` untuk proyek yang diubah lolos.
- [ ] Endpoint baru dites manual minimal 1x (happy path + 1 kasus gagal) — catat hasilnya di laporan.
- [ ] Tidak ada file di luar scope tugas yang berubah (`git status` dicek).
- [ ] Tidak ada secret / token / password di kode atau jawaban.
- [ ] Tidak ada package baru, tidak ada commit/push.
- [ ] Laporan singkat ditulis: file yang diubah, hasil build/test, dan apa yang belum dites.

## Kalau build/test gagal

1. Baca errornya sampai habis, perbaiki penyebabnya, jangan menebak.
2. Perbaiki di SATU tempat, jalankan ulang build/test.
3. Kalau setelah 2x percobaan masih gagal, berhenti dan laporkan error + apa yang sudah dicoba. Jangan mengarang solusi.
