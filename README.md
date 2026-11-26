# Inventara

Inventara adalah aplikasi manajemen inventaris untuk mengelola aset, peminjaman, maintenance, pengguna, dan audit aktivitas. Solusi terdiri dari REST API ASP.NET Core dan frontend React.

## Prasyarat

- .NET SDK yang mendukung target framework solution
- Docker Desktop dengan Docker Compose
- Node.js dan npm

## Menjalankan aplikasi secara lokal

### 1. Jalankan SQL Server

Salin `.env.example` menjadi `.env`, lalu isi `MSSQL_SA_PASSWORD` dengan password SQL Server yang kuat. Setelah itu jalankan dari root repository:

```powershell
docker compose up -d
```

### 2. Konfigurasikan dan jalankan API

Konfigurasi lokal disimpan dengan .NET User Secrets, bukan di source control. Jalankan dari root repository:

```powershell
dotnet user-secrets init --project src/Inventara.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=InventaraDb;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True" --project src/Inventara.Api
dotnet user-secrets set "Jwt:SigningKey" "<random-secret-minimal-32-byte>" --project src/Inventara.Api
dotnet user-secrets set "SeedAdmin:FullName" "Inventara Admin" --project src/Inventara.Api
dotnet user-secrets set "SeedAdmin:Email" "admin@inventara.local" --project src/Inventara.Api
dotnet user-secrets set "SeedAdmin:Password" "<password-admin-yang-kuat>" --project src/Inventara.Api
```

Gunakan nilai `MSSQL_SA_PASSWORD` yang sama pada connection string dan `.env`. Ganti semua nilai placeholder sebelum menjalankan API. Seeder membuat akun Admin pertama dengan password tersebut dan menyimpan hash BCrypt; kredensial password demo tidak ditanam di repository.

Terapkan migrasi dan jalankan API:

```powershell
dotnet ef database update --project src/Inventara.Infrastructure --startup-project src/Inventara.Api
dotnet run --project src/Inventara.Api --launch-profile http
```

- API: <http://localhost:5090>
- Swagger UI: <http://localhost:5090/swagger>

### 3. Jalankan frontend

```powershell
cd client
npm install
npm run dev
```

Buka <http://localhost:5173>. Frontend menggunakan `http://localhost:5090/api/v1` secara default. Untuk mengubahnya, buat `client/.env.local`:

```dotenv
VITE_API_URL=http://localhost:5090/api/v1
```

API mengizinkan CORS dari `http://localhost:5173`. Token akses disimpan di `localStorage`; route frontend membantu navigasi berdasarkan role, sementara otorisasi tetap ditegakkan API.

## Kredensial demo

Seeder menyediakan akun Admin awal berdasarkan konfigurasi `SeedAdmin` di User Secrets. Gunakan email dan password yang Anda masukkan pada langkah konfigurasi. Tidak ada password demo tetap di repository. Admin dapat mendaftarkan akun Staff/User melalui halaman **Master Data**.

## Fitur

- Dashboard ringkasan aset, distribusi kategori, dan daftar pinjaman terlambat.
- Aset: pencarian, filter, pagination, pengelolaan aset, foto, dan export Excel.
- Detail aset dengan informasi spesifikasi dan riwayat yang diizinkan untuk role pengguna.
- Pengajuan pinjaman, keputusan Admin, pencatatan pengembalian, serta otomatisasi tiket maintenance saat aset dikembalikan dalam kondisi rusak.
- Maintenance: pembuatan tiket, pembaruan status/biaya, dan penyelesaian tiket.
- Master data kategori, lokasi, vendor, dan pengguna.
- Audit log dengan filter entitas, pengguna, dan tanggal.

## Arsitektur

- `src/Inventara.Domain`: entitas, enum, dan aturan inti domain.
- `src/Inventara.Application`: kontrak use case, service, validasi, serta DTO.
- `src/Inventara.Infrastructure`: EF Core, SQL Server, repository, migrasi, penyimpanan file, dan seeder.
- `src/Inventara.Api`: endpoint REST, autentikasi JWT, middleware, dokumentasi OpenAPI, serta logging.
- `client/`: SPA React + TypeScript menggunakan Vite, Tailwind CSS, shadcn/ui, React Router, Axios, dan Recharts.

## Build frontend

Dari folder `client/`:

```powershell
npm run build
```
