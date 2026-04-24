# Code Review — BomApp Production Calculator

**Date:** 2026-04-23
**Reviewer:** Claude Code (agent-skills:review)
**Branch:** main

---

## Axis 1: Correctness

---

### [CRITICAL] `ProductionOrderRepository.CreateAsync` never persists `ProductionOrderLine` records

`src/BomApp.Infrastructure/Persistence/Repositories/ProductionOrderRepository.cs:112–141`

The `CreateAsync` method computes a `materials` dict via `ExpandBomAsync` inside `CreateProductionOrdersForGroupAsync` (UseCase line 218–219), but never inserts `ProductionOrderLine` rows. `GetLinesByOrderIdAsync` will always return empty for every order ever created. The `BomSnapshot` JSON captures the BOM structure but the repository's `GetLinesByOrderIdAsync` queries `ProductionOrderLines` directly.

**Fix:** Iterate `materials` after `SaveChangesAsync` and insert `ProductionOrderLine` entities; or move line creation into the repository's `CreateAsync` accepting the lines in the command.

---

### [CRITICAL] Duplicate-order protection is dead code

`src/BomApp.Application/UseCases/CalculateSalesProductionUseCase.cs:143`

`alreadyProcessed` is fetched but the result is never checked — `eligibleTransactions` is never filtered against it. Calling `SaveAsync` twice for the same date range creates duplicate production orders silently.

**Fix:** Filter `eligibleTransactions` with `.Where(t => !alreadyProcessed.Contains(t.DocNo))` before grouping.

---

### [CRITICAL] `SaveAsync` ignores `request.DryRun`

`src/BomApp.Application/UseCases/CalculateSalesProductionUseCase.cs:119`

If a caller passes a request with `DryRun=true` to `SaveAsync`, orders are still written. The CLI works by accident (it gates on `dryRun` before calling `SaveAsync`), but the contract is fragile and untested at the use-case level.

**Fix:** Add `if (request.DryRun) return Result<...>.Failure("Cannot save in DryRun mode");` at the top of `SaveAsync`, or remove `DryRun` from `CalculateSalesProductionRequest` and enforce the distinction only at call-sites.

---

### [IMPORTANT] `SaveAsync` re-fetches ERP and assignment data — TOCTOU gap

`src/BomApp.Application/UseCases/CalculateSalesProductionUseCase.cs:130–136`

`CalculateAsync` is called first, then `GetSalesTransactionsByDateRangeAsync` and `GetAssignedItemCodesAsync` are called again inside `SaveAsync`. Between the two calls, ERP data could change. This also doubles the DB round-trips with no benefit.

**Fix:** Refactor so `CalculateAsync` returns (or the use case internally reuses) the fetched transactions and assignment map rather than re-querying.

---

### [IMPORTANT] `BomRepository.CreateAsync` always leaves `ItemName` and `MaterialName` empty

`src/BomApp.Infrastructure/Persistence/Repositories/BomRepository.cs:67,79`

```csharp
ItemName = string.Empty, // ให้ caller set ถ้าต้องการ — denormalized จาก ERP
MaterialName = string.Empty, // denormalized — set by caller
```

`CreateBomCommand` and `CreateBomLineCommand` have no fields for these. All BOM list views and production order displays will show blank names.

**Fix:** Add optional `ItemName` / `MaterialName` fields to the create commands, or introduce a denormalization step that resolves them from `IErpItemRepository` before persisting.

---

### [IMPORTANT] `DryRun` unit test is vacuous

`tests/BomApp.Tests.Unit/UseCases/CalculateSalesProductionUseCaseTests.cs:305–338`

The test calls `CalculateAsync` (which never touches `IProductionOrderRepository` regardless of `DryRun`), then asserts `CreateAsync` was `Times.Never`. This passes trivially. The real risk — `SaveAsync` with `DryRun=true` writing orders — is untested.

**Fix:** Add a test: call `SaveAsync(request with DryRun=true)` and assert `IProductionOrderRepository.CreateAsync` is `Times.Never`.

---

## Axis 2: Readability

---

### [Suggestion] Dead code: `visited.Remove(bom.Id)` in `ExpandBomAsync`

`src/BomApp.Application/UseCases/CalculateSalesProductionUseCase.cs:289`

Sub-BOM recursive calls receive `new HashSet<Guid>(visited)` — a copy. The `Remove` at line 289 restores the original `visited`, but since every sub-branch already received a copy, the removal has no observable effect on the algorithm. It misleads readers into thinking backtracking shares state across branches.

**Fix:** Remove line 289, or switch to passing the original `visited` without copying and rely on `Remove` for proper DFS backtracking (pick one pattern consistently).

---

### [Suggestion] `"current-user"` hardcoded in ViewModel

`src/BomApp.UI/ViewModels/SalesCalculation/SalesCalculationViewModel.cs:200`

```csharp
CreatedBy: "current-user",
```

This placeholder will reach the database on every save. Inject a session/auth context service and read the real username from it.

---

### [Suggestion] `"SYSTEM"` hardcoded `createdBy` in `BomService`

`src/BomApp.Application/Services/BomService.cs:68`

The inline comment acknowledges the issue. Should be resolved before production use by threading the auth context through the Application layer.

---

## Axis 3: Architecture

---

### [IMPORTANT] UI ViewModels inject repository interfaces directly — bypasses application layer

`src/BomApp.UI/ViewModels/LoginViewModel.cs:4` — `IAuthRepository`
`src/BomApp.UI/ViewModels/SalesCalculation/SalesCalculationViewModel.cs:6` — `IErpSalesOrderRepository`

Per CLAUDE.md rule: "ห้าม UI layer reference Domain โดยตรง — ต้องผ่าน interface เสมอ." More specifically, repositories live in `Application.Interfaces.Repositories` and belong to the Infrastructure boundary. The UI calling repositories directly means no business-logic guard (validation, audit logging) can be enforced by the application layer.

**Fix:**
- Introduce `IAuthService` in Application with a `LoginAsync(username, password)` method; `LoginViewModel` depends on that instead of `IAuthRepository`.
- Expand `IProductionService` (or add a dedicated service) with a `GetSalesPreviewAsync` method; `SalesCalculationViewModel` uses that instead of `IErpSalesOrderRepository` directly.

---

### [IMPORTANT] Race condition in `GenerateOrderNoAsync` — duplicate order numbers under concurrency

`src/BomApp.Infrastructure/Persistence/Repositories/ProductionOrderRepository.cs:153–173`

Two concurrent `SaveAsync` calls for the same month both read the same `lastOrderInMonth`, both compute `nextSeq = N+1`, and both produce `PO-YYYYMM-0000N`. No database lock or sequence protects this.

**Fix:** Use a PostgreSQL sequence (e.g. `CREATE SEQUENCE bom.production_order_seq`) or a `SERIAL`/`BIGSERIAL` surrogate combined with a formatted composite unique key.

---

## Axis 4: Security

---

### [CRITICAL] `.env` file with plaintext database credentials committed to git

`.env:1`

```
CONNECTION_SRING=host=192.168.2.212 port=5432 dbname=productbom user=postgres password=sml ...
```

The file is tracked in git. The IP address and credentials are now in permanent git history.

**Fix (in order):**
1. Add `.env` to `.gitignore` immediately.
2. Rotate the database password.
3. Rewrite history with `git filter-repo --invert-paths --path .env` to purge the credential from all commits.

---

### [IMPORTANT] Plaintext password comparison in `sml_user_list`

`src/BomApp.Infrastructure/Auth/AuthRepository.cs:16–21`

```sql
AND user_password = @pwd
```

Passwords are stored and compared in cleartext. If the ERP schema cannot be changed, document this constraint explicitly. If the schema is under control, migrate to a hashed scheme (bcrypt / Argon2).

---

## Axis 5: Performance

---

### [IMPORTANT] N+1 queries in `CalculateAsync` inner loop

`src/BomApp.Application/UseCases/CalculateSalesProductionUseCase.cs:64`

`bomRepository.GetByIdAsync(bomId, ct)` is called once per item code inside `transactionsByItem`. For a date range with 100 unique products, this is 100 sequential DB roundtrips. The same pattern repeats in `CreateProductionOrdersForGroupAsync` (line 211).

**Fix:** Add `IBomRepository.GetByIdsAsync(IReadOnlyList<Guid> ids)` that issues a single `WHERE id = ANY(@ids)` query, then batch-load before the loop.

---

### [IMPORTANT] `GetActiveBomsAsync` loads all BOMs in memory then filters

`src/BomApp.Application/Services/BomAssignmentService.cs:18–19`

```csharp
var all = await bomRepository.GetAllAsync(ct);
var active = all.Where(b => b.Status == "Active").ToList();
```

If there are thousands of BOMs, all records are deserialized before the filter is applied.

**Fix:** Add `IBomRepository.GetByStatusAsync(string status)` with a `WHERE status = @status` clause, or add an optional filter parameter to `GetAllAsync`.

---

### [IMPORTANT] `GenerateOrderNoAsync` does a string-prefix scan without a guaranteed index

`src/BomApp.Infrastructure/Persistence/Repositories/ProductionOrderRepository.cs:158–163`

`WHERE order_no LIKE 'PO-YYYYMM-%'` requires an index on `order_no`. The current migration should be verified to include one, otherwise each new order triggers a full table scan. Replacing this with a PostgreSQL sequence also resolves this concern.

---

## CI

### [Suggestion] Integration tests run even when build or unit tests fail

`.github/workflows/ci.yml:36`

The integration test step has no `if: success()` guard. A build failure causes a noisy second failure in integration tests. Consider adding `if: success()` or using `needs:` job dependencies.

---

## Summary

| # | Severity | Axis | File | Description |
|---|----------|------|------|-------------|
| 1 | **Critical** | Security | `.env` | DB credentials committed to git |
| 2 | **Critical** | Correctness | `ProductionOrderRepository.cs:112` | `ProductionOrderLine` records never persisted |
| 3 | **Critical** | Correctness | `CalculateSalesProductionUseCase.cs:143` | Duplicate-order dedup check is dead code |
| 4 | **Critical** | Correctness | `CalculateSalesProductionUseCase.cs:119` | `SaveAsync` ignores `DryRun` flag |
| 5 | Important | Correctness | `CalculateSalesProductionUseCase.cs:130` | Double ERP fetch in `SaveAsync` (TOCTOU) |
| 6 | Important | Correctness | `BomRepository.cs:67,79` | `ItemName`/`MaterialName` always empty on create |
| 7 | Important | Correctness | `CalculateSalesProductionUseCaseTests.cs:305` | `DryRun` unit test is vacuous |
| 8 | Important | Architecture | `LoginViewModel.cs:4`, `SalesCalculationViewModel.cs:6` | UI injects repository interfaces directly |
| 9 | Important | Architecture | `ProductionOrderRepository.cs:153` | Race condition in order number generation |
| 10 | Important | Security | `AuthRepository.cs:21` | Plaintext password comparison |
| 11 | Important | Performance | `CalculateSalesProductionUseCase.cs:64` | N+1 `GetByIdAsync` in calculation loop |
| 12 | Important | Performance | `BomAssignmentService.cs:18` | `GetActiveBomsAsync` loads all BOMs in memory |
| 13 | Important | Performance | `ProductionOrderRepository.cs:158` | `GenerateOrderNoAsync` lacks guaranteed index |
| 14 | Suggestion | Readability | `CalculateSalesProductionUseCase.cs:289` | Dead `visited.Remove` in `ExpandBomAsync` |
| 15 | Suggestion | Readability | `SalesCalculationViewModel.cs:200` | Hardcoded `"current-user"` in VM |
| 16 | Suggestion | Readability | `BomService.cs:68` | Hardcoded `"SYSTEM"` for `createdBy` |
| 17 | Suggestion | CI | `ci.yml:36` | Integration tests run even on build failure |
