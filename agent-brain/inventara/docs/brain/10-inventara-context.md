# Inventara — Context

> Copy file ini ke `docs/brain/10-inventara-context.md` di root proyek Inventara.
> Ini sumber kebenaran Inventara. Kalau ada konflik dengan aturan umum, file ini yang menang — kecuali larangan di `AGENTS.md`.

## 1. Apa ini

Sistem manajemen aset & inventaris untuk organisasi. Admin mengelola master data dan aset, user mengajukan peminjaman, admin menyetujui, dan pengembalian yang rusak otomatis jadi tiket maintenance.

- Pengguna: Admin (kelola semua), User (pinjam + lihat aset).
- Bukan: sistem kasir, bukan HR, bukan SIMIT (sistem magang Pertamina — nama itu tidak dipakai di sini).

## 2. Tech stack

- .NET 8, ASP.NET Core Web API, Clean Architecture
- EF Core + SQL Server, JWT (Bearer), BCrypt.Net-Next, FluentValidation, ClosedXML (export)
- Test: xUnit. Frontend: React (Vite) — dibuat belakangan, setelah API stabil.
- Solution: `Inventara.Domain / .Application / .Infrastructure / .Api / .Tests`

## 3. Entitas & tabel (9 tabel)

| Entitas | Tabel | Field kunci | Relasi |
|---|---|---|---|
| User | Users | Id, Name, Email, PasswordHash, Role (Admin/User) | 1 User → banyak Loan |
| Category | Categories | Id, Name | 1 Category → banyak Asset |
| Location | Locations | Id, Name | 1 Location → banyak Asset |
| Asset | Assets | Id, AssetCode (unik), Name, CategoryId, LocationId, Status (Available/Borrowed/Maintenance/Retired), Condition | Milik 1 Category + 1 Location |
| Loan | Loans | Id, UserId, Status (Pending/Approved/Rejected/Returned), LoanDate, ReturnDate | 1 Loan → banyak LoanItem; 1 User → banyak Loan |
| LoanItem | LoanItems | Id, LoanId, AssetId, ConditionOnReturn | Milik 1 Loan + 1 Asset |
| Maintenance | Maintenances | Id, AssetId, Description, Status (Open/InProgress/Done), CreatedAt | Milik 1 Asset |
| AuditLog | AuditLogs | Id, UserId, Action, EntityName, EntityId, CreatedAt | Catat setiap create/update/delete penting |
| (seed) | — | Admin default + master Category/Location contoh | Hanya untuk development |

## 4. Business rule (wajib dites)

1. Satu aset tidak bisa dipinjam dua orang sekaligus — aset berstatus `Borrowed` tidak bisa masuk Loan baru.
2. Approval peminjaman mengubah status Loan DAN status Asset dalam SATU transaksi. Gagal salah satu = batal semua.
3. Pengembalian dengan kondisi rusak otomatis membuat tiket Maintenance berstatus `Open` untuk aset itu.
4. Hanya Admin yang bisa approve/reject loan, ubah master data, dan lihat AuditLog.
5. AssetCode unik. Loan yang masih `Pending` tidak boleh dihapus, hanya dibatalkan.

## 5. Endpoint (`/api/v1/...`)

| Method + path | Auth | Aturan |
|---|---|---|
| POST `/auth/register`, POST `/auth/login` | Public / — | Login kembalikan JWT |
| GET/POST/PUT/DELETE `/categories`, `/locations` | Admin (POST/PUT/DELETE), login (GET) | Master data |
| GET/POST/PUT/DELETE `/assets` | Admin tulis, login baca | `GET /assets?status=&categoryId=&search=` |
| POST `/loans`, GET `/loans`, GET `/loans/{id}` | Login | User hanya lihat loan miliknya; Admin lihat semua |
| POST `/loans/{id}/approve`, POST `/loans/{id}/reject` | Admin | Ikut rule 2 |
| POST `/loans/{id}/return` | Admin | Ikut rule 3 |
| GET/POST/PUT `/maintenances` | Admin | — |
| GET `/dashboard/summary` | Admin | Total aset, dipinjam, maintenance terbuka |
| GET `/audit-logs` | Admin | — |

## 6. Hal yang TIDAK boleh

- Jangan klaim fitur di luar tabel/endpoint di atas.
- Jangan pakai nama SIMIT untuk proyek ini.
- Jangan tambah field "sekalian" ke entitas tanpa diminta.
