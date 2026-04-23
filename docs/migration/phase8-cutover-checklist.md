# Phase 8 — Cutover checklist

> ⚠ Destructive. Every item below **permanently removes** legacy code.
> Only execute when you have:
> 1. Successfully run the full stack via `aspire run` on a dev box
>    (BLOCKED in the sandbox that produced this branch — Docker daemon
>    absent).
> 2. QA-signed off on the Phase 7 hardening against a copy of production
>    data (see `docs/migration/data-migration-runbook.md` Path B).
> 3. A tagged rollback commit (`pre-cutover-backup`) pushed to origin.

The tooling for Phase 8's *non-destructive* work (Dockerfiles, CI, compose,
CHANGELOG, README, deployment docs) is already on the branch. This file
is the runbook for the *destructive* half.

## 0. Prepare rollback point

```bash
git tag pre-cutover-backup
git push origin pre-cutover-backup
```

Back up production DB independently of the script:

```bash
pwsh tools/migrate-data.ps1 -Server sql-prod -Database SimplCommerce -SignedOffBy ops-lead@example -Force
#   → writes artifacts/backups/SimplCommerce-pre-aspire-<timestamp>.bak
```

## 1. Remove the legacy WebHost (P8-03, P8-04, P8-05)

```bash
# 1.1 — drop the project from the solution
dotnet sln SimplCommerce.sln remove src/SimplCommerce.WebHost/SimplCommerce.WebHost.csproj

# 1.2 — remove the ProjectReference + AddProject call from AppHost
#      (search: SimplCommerce_WebHost)
```

Edit `src/AppHost/SimplCommerce.AppHost/SimplCommerce.AppHost.csproj` —
delete the WebHost `<ProjectReference>` line.

Edit `src/AppHost/SimplCommerce.AppHost/Program.cs` — delete the
`builder.AddProject<Projects.SimplCommerce_WebHost>("webhost")` block.

Then delete the filesystem:

```bash
git rm -rf src/SimplCommerce.WebHost
git rm Dockerfile Dockerfile-sqlite docker-entrypoint.sh
```

## 2. Remove AngularJS admin templates (P8-06)

```bash
git rm -rf src/Modules/*/wwwroot/admin
```

Double-check no module still references them at runtime (the new Admin
Blazor host does not). Any leftover `.bundleconfig.json` in modules can
also go:

```bash
git rm src/Modules/*/bundleconfig.json 2>/dev/null || true
```

## 3. Remove Razor Views (P8-07)

Each module keeps its `Areas/*/` folder shell for Phase 2 layering
backward-compat, but the `Views/` tree is dead code now:

```bash
git rm -rf src/Modules/*/Areas/*/Views
git rm -rf src/Modules/*/Views 2>/dev/null || true
```

## 4. Remove MVC controllers (P8-08)

The 104 legacy controllers have been replaced by `Endpoints/` groups in
each module + `SimplCommerce.ApiService`. Remove the old folder:

```bash
git rm -rf src/Modules/*/Areas/*/Controllers
git rm -rf src/Modules/*/Areas/*/ViewModels  # MVC-only ViewModels
```

Any `Areas/<Name>` folder that becomes empty can also be deleted.

## 5. Remove bundling tooling (P8-09)

`BuildBundlerMinifier` was only driving `bundleconfig.json` + gulpfile
for the AngularJS assets. With those gone:

```bash
# Each module's csproj: strip the BuildBundlerMinifier PackageReference
# (tighten with a grep first)
grep -rl "BuildBundlerMinifier" src/ --include=*.csproj
# remove the matching <PackageReference ... /> lines.

# Delete leftover gulp / libman artifacts if any linger:
git rm libman.json 2>/dev/null || true
git rm gulpfile.js 2>/dev/null || true
```

## 6. Remove the static manifest's WebHost-only entries (P8-10)

`modules.json` was already moved to `docs/migration/legacy/` during
Phase 2. The `ModuleConfigurationManager` static list now contains all
43 modules; strip the ones that were WebHost-specific (none today — list
is already correct).

`CustomAssemblyLoadContextProvider` never existed in this codebase
(verified in Phase 2); nothing to remove.

## 7. Rebuild and smoke-test

```bash
dotnet build SimplCommerce.sln                # expect 0/0
dotnet test  SimplCommerce.sln --no-build     # expect all pass
dotnet run --project src/AppHost/SimplCommerce.AppHost
#   → open https://localhost:7100 storefront
#   → open https://localhost:7200 admin, log in admin@simplcommerce.com
#   → add a product / place a COD order / verify in /admin/orders
```

If any step fails: `git reset --hard pre-cutover-backup`, investigate,
retry.

## 8. Publish deployment artifacts (P8-11..P8-15)

```bash
# Aspire manifest for Azure Container Apps
dotnet aspire publish src/AppHost/SimplCommerce.AppHost \
    --output ./publish --publisher container-apps

# Or a Kubernetes manifest
dotnet aspire publish src/AppHost/SimplCommerce.AppHost \
    --output ./publish --publisher kubernetes
```

Dockerfiles + compose.yaml + CI configs are already in the branch from
the Phase 8 non-destructive commit.

## 9. Release (P8-21..P8-25)

- Update `MIGRATION_PROGRESS.md` to 100 % done.
- Tag `v2.0.0-aspire-blazor` on the merge commit.
- Open the final PR against `master` referencing `CHANGELOG.md` for
  release notes.
- Keep the old `pre-cutover-backup` tag around for at least one
  production-cycle as a paranoia rollback target.

## Counts to expect

| Removed artefact | Count (Phase 0 inventory) |
|---|---|
| WebHost project | 1 |
| Legacy Dockerfiles + entrypoint | 3 |
| AngularJS `.html` templates | 98 |
| Razor `.cshtml` views | 181 (modules 137 + WebHost 44) |
| Controllers (`Controller` / `ControllerBase`) | 104 |
| `bundleconfig.json` | varies per module |

Cross-check each of these against `docs/migration/api-inventory.md` +
`docs/migration/ui-inventory.md` so the final commit diff matches the
counts in Phase 0 inventory.
