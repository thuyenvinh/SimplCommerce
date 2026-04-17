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
