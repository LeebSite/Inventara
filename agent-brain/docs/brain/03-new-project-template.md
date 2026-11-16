# Template Konteks Proyek Baru

> Copy file ini jadi `docs/brain/10-<nama-proyek>-context.md`, lalu isi semua bagian.
> File ini sumber kebenaran proyek. Agent DILARANG mengarang isi yang tidak tertulis di sini.

# <Nama Proyek> — Context

## 1. Apa ini (2–3 kalimat)

- Masalah yang diselesaikan:
- Pengguna utama + perannya:
- Bukan apa (scope yang sengaja TIDAK dibuat):

## 2. Tech stack (versi pasti)

- .NET: 8.x | Database: ... | Auth: ... | Frontend: ... | Test: xUnit + ...
- Package yang sudah disetujui: (tulis nama + versi, jangan tambah tanpa izin)

## 3. Struktur solution

```
<Proyek>.Domain / .Application / .Infrastructure / .Api / .Tests
+ frontend: ...
```

## 4. Entitas & tabel

| Entitas | Tabel | Field kunci | Relasi |
|---|---|---|---|
| ... | ... | ... | ... |

## 5. Business rule (yang kalau dilanggar, fitur salah)

1. ...
2. ...
3. ...

## 6. Endpoint (`/api/v1/...`)

| Method + path | Auth | Input | Output / aturan |
|---|---|---|---|
| ... | ... | ... | ... |

## 7. Hal yang TIDAK boleh

- ...
