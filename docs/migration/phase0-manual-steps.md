# Phase 0 & Phase 1 — Manual Steps cần user chạy trên máy local

> **Lý do:** sandbox Claude Code không có `dotnet` CLI (xem MIGRATION_DECISIONS.md DECISION-004). Các task yêu cầu runtime .NET SDK được liệt kê dưới đây để user chạy thủ công. Sau khi chạy, tick các task tương ứng trong `MIGRATION_PROGRESS.md`.

## Prerequisites máy local

- Windows / macOS / Linux với quyền cài đặt .NET SDK
- Docker Desktop (cho container SQL / Redis / Azurite / MailDev / Seq)
- Git

## PHASE 0 — Runtime verification

### P0-19 đã xong (đã sửa global.json trong commit Phase 0). Verify bằng:
```
dotnet --version
# expect: 9.0.x (latestMinor)
```

### P0-20 — `dotnet --info` ra .NET 9
```
dotnet --info | head -20
```
Nếu chưa có .NET 9, install từ https://dotnet.microsoft.com/download/dotnet/9.0 rồi rerun.

### P0-21 — Aspire workload
Aspire 9 không còn cần `dotnet workload install aspire` (đã standalone templates từ 9.0 GA). Thay vào đó:
```
dotnet new install Aspire.ProjectTemplates
dotnet new list aspire
```
Expect thấy `aspire-apphost`, `aspire-servicedefaults`, `aspire-starter`.

### P0-22 — verify templates (như trên)

### P0-23 — Build baseline solution
```
dotnet restore SimplCommerce.sln
dotnet build SimplCommerce.sln -c Debug --nologo -warnaserror:false
```
Expect: **PASS**. Log lại thời gian build vào `docs/migration/baseline-build-log.txt`.

### P0-24 — Chạy WebHost cũ + screenshot
```
cd src/SimplCommerce.WebHost
dotnet run
# Open http://localhost:5000
# Login admin@simplcommerce.com / 1qazZAQ!
```
Screenshot:
- Homepage (storefront)
- /admin dashboard
- /admin/products list
- /admin/orders list
Lưu vào `docs/migration/baseline-screenshots/`.

### P0-25 — Export schema SQL
```
dotnet tool install --global dotnet-ef --version 8.0.0  # nếu chưa có
cd src/SimplCommerce.WebHost
dotnet ef migrations script --idempotent --output ../../docs/migration/baseline-schema.sql
```

### P0-26 — Full package graph
```
dotnet list src/SimplCommerce.WebHost/SimplCommerce.WebHost.csproj package --include-transitive > docs/migration/baseline-packages-full.txt
```

## PHASE 1 — Runtime verification

### P1-15 — Build solution với Aspire projects
```
dotnet build SimplCommerce.sln
```
Expect: **PASS** (ServiceDefaults + AppHost biên dịch được).

### P1-16 — Aspire run
```
dotnet run --project src/AppHost/SimplCommerce.AppHost
```
- Dashboard sẽ tự mở tại URL kiểu `https://localhost:17001`
- Trên dashboard verify:
  - `sql` resource → Healthy
  - `redis` resource → Healthy
  - `storage` (Azurite) → Healthy
  - `maildev` → Running
  - `seq` → Running
  - `webhost` project → Healthy

### P1-17 — DB connectivity
- Mở resource `webhost` trong dashboard → mở URL → homepage phải load được.
- Login admin@simplcommerce.com / 1qazZAQ! → /admin dashboard load.

### P1-18 — WebHost qua URL Aspire
- Ghi nhận URL Aspire tự gán cho webhost (random port).
- Smoke test: homepage, /admin, /cart.

### P1-19 — Screenshot
Lưu vào `docs/migration/phase1-screenshots/`:
- Aspire dashboard resource list
- WebHost homepage qua URL Aspire
- /admin qua URL Aspire

## Troubleshooting

### Docker không chạy
Aspire cần Docker / Podman. Nếu machine không có Docker, cài Docker Desktop hoặc dùng `podman-compose`.

### Port conflict
Aspire tự random port — nếu có conflict xem dashboard log và đổi manual trong `AppHost/Program.cs` (`WithHttpEndpoint(port: 5051)` etc.).

### SQL container không start
Kiểm tra:
- `docker ps -a` có container `sql` không.
- `docker logs <container>` cho chi tiết.
- Mặc định Aspire set `ACCEPT_EULA=Y` và `MSSQL_SA_PASSWORD` auto-generated. Nếu lỗi password policy (8 ký tự + mixed), set thủ công.

### Connection string mismatch
Aspire inject qua `ConnectionStrings__SimplCommerce`. WebHost `Program.cs` (sau P1-13) đọc `GetConnectionString("SimplCommerce") ?? GetConnectionString("DefaultConnection")`.
