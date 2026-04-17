# MIGRATION DECISIONS

> Ghi lại mọi quyết định kiến trúc đáng chú ý trong quá trình migrate SimplCommerce sang .NET Aspire + Blazor. Mỗi decision là một entry theo template:

```
## DECISION-NNN: <Tiêu đề ngắn>
- **Date:** YYYY-MM-DD
- **Phase:** <phase áp dụng>
- **Context:** <bối cảnh / vấn đề cần giải quyết>
- **Decision:** <quyết định đã chọn>
- **Alternatives considered:** <các phương án khác đã cân nhắc>
- **Consequences:** <hệ quả, trade-off, follow-up cần thiết>
```

---

## DECISION-001: Target stack chốt cứng theo CLAUDE_CODE_PROMPT.md
- **Date:** 2026-04-17
- **Phase:** 0
- **Context:** Prompt migration đã chỉ định target stack. Cần xác nhận chọn & không đi chệch.
- **Decision:**
  - .NET 9 + Aspire 9.x
  - Blazor Web App: Storefront = Interactive Auto, Admin = Interactive Server
  - MudBlazor cho UI
  - MediatR 12, FluentValidation 11, EF Core 9 (SQL Server primary)
  - ASP.NET Identity (cookie cho Blazor, JWT cho API)
  - Serilog + OpenTelemetry → Seq + Aspire dashboard
  - xUnit + FluentAssertions + Testcontainers
  - Scalar (thay Swagger UI)
- **Alternatives considered:** Không có — đã được chốt trong prompt.
- **Consequences:** Mọi phase downstream phải tuân thủ. Nếu cần lệch, mở decision mới.

## DECISION-002: Branch strategy cho Phase 0 & Phase 1
- **Date:** 2026-04-17
- **Phase:** 0, 1
- **Context:** Prompt gốc (CLAUDE_CODE_PROMPT.md) đề xuất branch `aspire-migration/phase-0-discovery`, `aspire-migration/phase-1-aspire-bootstrap`, nhưng user task này chỉ định branch duy nhất `claude/phase-0-migration-pX925` cho cả 2 phase.
- **Decision:** Dùng branch duy nhất `claude/phase-0-migration-pX925` cho Phase 0 và Phase 1 theo chỉ thị của user. Tách commit theo từng phase với prefix rõ ràng (`chore(migration): phase 0 ...`, `feat(migration): phase 1 ...`).
- **Alternatives considered:** Tạo thêm sub-branch nội bộ — bị từ chối vì mâu thuẫn trực tiếp với instruction trong task.
- **Consequences:** Mất một chút visibility trong git graph giữa 2 phase, nhưng commit messages vẫn phân biệt được phase. Các phase sau (2+) sẽ dùng branch riêng theo prompt gốc.

## DECISION-003: Không tạo PR ở cuối Phase 0/1 trừ khi user yêu cầu
- **Date:** 2026-04-17
- **Phase:** 0, 1
- **Context:** Task instruction quy định "Do NOT create a pull request unless the user explicitly asks for one". Mâu thuẫn với task P0-28 và P1-22 trong MIGRATION_TODO.md (tạo PR).
- **Decision:** Không tạo PR tự động. Push branch, commit đầy đủ, chờ user yêu cầu PR mới tạo.
- **Alternatives considered:** Tạo PR draft — bị từ chối vì mâu thuẫn với instruction cao hơn.
- **Consequences:** Task P0-28 và P1-22 sẽ được đánh dấu là N/A với ghi chú trong MIGRATION_PROGRESS.md.

## DECISION-004: Sandbox không có dotnet CLI — các task build/run/migration script bị DEFER
- **Date:** 2026-04-17
- **Phase:** 0, 1
- **Context:** Môi trường Claude Code hiện tại không cài .NET SDK (`dotnet: command not found`, không có `/usr/share/dotnet` hay `~/.dotnet`). Một số task Phase 0/1 cần `dotnet` executable:
  - P0-19..P0-22: update global.json + verify .NET 9 SDK + cài Aspire workload
  - P0-23: `dotnet build SimplCommerce.sln`
  - P0-24: chạy WebHost để chụp screenshot
  - P0-25: `dotnet ef migrations script`
  - P1-02, P1-03: `dotnet new aspire-apphost / aspire-servicedefaults` (cần template Aspire)
  - P1-08: add NuGet packages Aspire
  - P1-15..P1-19: `dotnet build` + `dotnet run` AppHost + truy cập dashboard
- **Decision:**
  - Phase 0: Vẫn **update global.json** sang .NET 9 (sửa file text — không cần SDK) để hoàn thành P0-19. Các task cần runtime dotnet (P0-20..P0-25) được log là BLOCKED và yêu cầu user chạy sau. Ghi danh sách lệnh cần chạy vào `docs/migration/phase0-manual-steps.md`.
  - Phase 1: **Tự tay tạo project scaffolding** (csproj + Program.cs + sln references) theo đúng template chuẩn của Aspire 9 thay cho `dotnet new`. Code sẽ compile khi user chạy `dotnet build` sau. Các task cần runtime (P1-15..P1-19, screenshot, dashboard verify) được log BLOCKED tương tự.
- **Alternatives considered:**
  - Cài đặt .NET SDK trong sandbox — không có network / apt rights đảm bảo; rủi ro tốn nhiều thời gian sandbox.
  - Dừng và báo user ngay từ đầu — nhưng phần lớn Phase 0/1 là documentation + file editing nên vẫn tiến được đáng kể; dừng sớm lãng phí phase work.
- **Consequences:**
  - Cần user chạy runtime verification trên máy local trước khi coi Phase 1 là thực sự "green".
  - Các task BLOCKED được đánh dấu rõ trong MIGRATION_PROGRESS.md, KHÔNG tick là completed.
  - Báo cáo cuối phase phải nêu rõ trạng thái này.

## DECISION-005: SDK installed tại `/home/user/.dotnet` — build xanh
- **Date:** 2026-04-17
- **Phase:** 0, 1
- **Context:** User yêu cầu cài SDK và build. Script `dot.net/v1/dotnet-install.sh` accessible từ sandbox → đã cài `.NET SDK 9.0.313` (latest patch của kênh 9.0). `global.json` pin `9.0.100 latestMinor` ⇒ 9.0.313 được roll-forward chấp nhận.
- **Decision:** Coi DECISION-004 là partial-resolved: build task không còn blocked. Các task runtime cần Docker (aspire run + SQL container) vẫn BLOCKED vì sandbox không có Docker daemon.
- **Consequences:** Phase 0 P0-20, P0-23 ✅; P1-15 ✅. P0-24..P0-25 + P1-16..P1-19 vẫn cần Docker → user chạy local.

## DECISION-006: Aspire version 13.x, không phải 9.x
- **Date:** 2026-04-17
- **Phase:** 1
- **Context:** Prompt CLAUDE_CODE_PROMPT.md viết "Aspire 9.x (latest stable)", nhưng restore thực tế nuget.org cho thấy tất cả package `Aspire.Hosting.*` hiện đã ở version **13.2.2** (Microsoft chuyển Aspire sang semver độc lập sau GA — version number không đi cùng .NET version nữa). Version 9.0.0 chỉ có trên SDK 9.0 manifest đầu năm 2025.
- **Decision:** Dùng `Aspire.AppHost.Sdk 13.2.2` + `Aspire.Hosting.AppHost 13.2.2` + tất cả `Aspire.Hosting.*` ở 13.2.2. `CommunityToolkit.Aspire.Hosting.MailPit 13.1.1` match ecosystem.
- **Alternatives considered:** Giữ 9.0.0 pinning → restore fail hẳn. Không khả thi.
- **Consequences:** `MIGRATION_INVENTORY.md` §5 ghi Aspire 9.x → đã lỗi thời, cần update. Không ảnh hưởng code logic — API Aspire 9 → 13 backward compatible ở mức builder.Add*().

## DECISION-007: MailDev → MailPit
- **Date:** 2026-04-17
- **Phase:** 1
- **Context:** Prompt chỉ định MailDev nhưng Microsoft/CommunityToolkit **không có** package `CommunityToolkit.Aspire.Hosting.MailDev` (chỉ có các variant community non-official: `WeChooz`, `Berrevoets`, `PommaLabs`, `BCat`). Package chính thức của CommunityToolkit cho test SMTP là `CommunityToolkit.Aspire.Hosting.MailPit`.
- **Decision:** Thay MailDev → **MailPit** (https://mailpit.axllent.org/). Cùng chức năng: SMTP server + web UI để inspect email gửi ra trong dev; chỉ khác container image.
- **Alternatives considered:**
  - `WeChooz.Aspire.Hosting.MailDev 2.0.0` — maintainer chưa ổn định, chỉ 8k downloads.
  - Tự viết `AddContainer("maildev", "maildev/maildev:...")` + binding SMTP port — dài dòng hơn MailPit.
- **Consequences:**
  - Code AppHost đổi `AddMailDev("maildev")` → `AddMailPit("mail")` (tên resource ngắn hơn).
  - Khi WebHost/ApiService gọi SMTP trong dev → dùng connection string Aspire inject, protocol không đổi.
  - Nếu user insist phải MailDev → swap lại sau bằng `WeChooz` variant.

## DECISION-008: Bump toàn bộ solution sang net9.0 (không chỉ AppHost/ServiceDefaults)
- **Date:** 2026-04-17
- **Phase:** 1
- **Context:** ServiceDefaults chỉ target net9.0 (yêu cầu bởi `Microsoft.Extensions.Http.Resilience 9.0`). WebHost đang net8.0 → `error NU1201: Project SimplCommerce.ServiceDefaults is not compatible with net8.0`. Prompt Phase 1 nguyên bản muốn "WebHost chạy nguyên trạng"; prompt Phase 2 mới bump framework. Nhưng ServiceDefaults reference KHÔNG thể avoid nếu Phase 1 phải gọi `AddServiceDefaults()` (P1-14).
- **Decision:** Bump **toàn bộ** csproj sang `net9.0` ngay trong Phase 1:
  - `src/Modules/Directory.Build.props`: net8.0 → net9.0 (ảnh hưởng 41 modules)
  - `src/SimplCommerce.WebHost/SimplCommerce.WebHost.csproj`
  - `src/SimplCommerce.Infrastructure/SimplCommerce.Infrastructure.csproj`
  - Tất cả 7 project test
- **Alternatives considered:**
  - Multi-target ServiceDefaults `net8.0;net9.0` — phải simplify OpenTelemetry registrations, code branching cho 2 TFM, chi phí cao hơn việc bump toàn solution.
  - Hoãn `AddServiceDefaults()` tới Phase 2 — vi phạm task P1-14.
- **Consequences:**
  - Phase 2 scope giảm (không còn việc bump TFM).
  - Tất cả package NuGet 8.0.x transitively nâng lên 9.0 — runtime binding redirect không cần vì .NET 9 SDK tự chọn.
  - Test projects vẫn chạy với xunit hiện tại; nếu package xunit có issue trên net9.0, sẽ bump trong commit riêng.
