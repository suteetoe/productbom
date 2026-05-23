# Sprint Log — BOM AI Team Communication Hub

> Agents บันทึก output, dependency และ blocker ที่นี่
> CTO อ่านไฟล์นี้เพื่อ track progress และ unblock

---

## Sprint 1 — Shared Contracts + Foundation

### สถานะ Contracts
| Contract | เจ้าของ | สถานะ | พร้อมใช้ |
|---|---|---|---|
| BomDto, BomLineDto, CreateBomCommand, UpdateBomCommand | Team A | DONE | YES |
| ProductionOrderDto, ProductionResultDto, CalculateSalesProductionRequest | Team A | DONE | YES |
| ErpItemDto, ErpUnitDto, ErpSalesTransactionDto | Team A | DONE | YES |
| AuthUserDto | Team A | DONE | YES |
| CreateProductionOrderInternalCommand | Team A | DONE | YES |
| IBomService | Team A | DONE | YES |
| IProductionService | Team A | DONE | YES |
| ICalculateSalesProductionUseCase | Team A | DONE | YES |
| IBomRepository, IBomAssignmentRepository, IProductionOrderRepository | Team A | DONE | YES |
| IAuthRepository | Team A | DONE | YES |
| IErpItemRepository | Team A | DONE | YES |
| IErpSalesOrderRepository | Team A | DONE | YES |
| FakeErpItemRepository | Team C | DONE | YES |
| FakeErpSalesOrderRepository | Team C | DONE | YES |

### Pending from Team A → Team B
- [x] `IBomService` interface lock แล้ว — `src/BomApp.Application/Interfaces/IBomService.cs`
- [x] `IProductionService` interface lock แล้ว — `src/BomApp.Application/Interfaces/IProductionService.cs`
- [x] `ICalculateSalesProductionUseCase` lock แล้ว — `src/BomApp.Application/Interfaces/ICalculateSalesProductionUseCase.cs`
- [x] All DTOs lock แล้ว — `src/BomApp.Shared/Contracts/`

### Pending from Team C → Team A + B
- [x] `FakeErpItemRepository` พร้อมใช้ — `tests/BomApp.Tests.Fakes/FakeErpItemRepository.cs`
- [x] `FakeErpSalesOrderRepository` พร้อมใช้ — `tests/BomApp.Tests.Fakes/FakeErpSalesOrderRepository.cs`
- [ ] Testcontainers setup พร้อม

### CI/CD Status
- [x] GitHub Actions pipeline ทำงานได้ — `.github/workflows/ci.yml`
- [x] `dotnet build BomApp.slnx` → **0 errors** (3 warnings EF version conflict ใน CLI — ไม่กระทบ runtime)
- [x] unit tests compile ผ่าน
- [ ] unit tests pass ต่อ DB จริง (integration tests — รอ Testcontainers setup)

### Build Status (2026-04-22)
| Project | สถานะ |
|---|---|
| BomApp.Shared | ✅ 0 errors |
| BomApp.Domain | ✅ 0 errors |
| BomApp.Application | ✅ 0 errors |
| BomApp.Infrastructure | ✅ 0 errors |
| BomApp.UI | ✅ 0 errors |
| BomApp.Cli | ✅ 0 errors (3 warnings) |
| BomApp.Tests.Fakes | ✅ 0 errors |
| BomApp.Tests.Unit | ✅ 0 errors |

### Sprint 1 — DONE ✅
Sprint 2 พร้อมเริ่ม — Shared Contract Gate ผ่านครบทุกข้อ

---

---

### [Team C] — Sprint 1 Foundation — 2026-04-22
**สถานะ**: Done

**Output**:
- `tests/BomApp.Tests.Fakes/SeedData.cs` — seed data ครอบคลุม 4 scenarios (items with/without BOM, multi-unit BOX=12PCS, multi-day transactions)
- `tests/BomApp.Tests.Fakes/FakeErpItemRepository.cs` — implements IErpItemRepository ครบทุก method
- `tests/BomApp.Tests.Fakes/FakeErpSalesOrderRepository.cs` — filters by DateOnly range correctly
- `tests/BomApp.Tests.Unit/UseCases/CalculateSalesProductionUseCaseTests.cs` — 6 tests (happy path, no-BOM skip, unit conversion, daily grouping, per-document mode, dry-run guard)
- `tests/BomApp.Tests.Unit/Services/BomServiceTests.cs` — 3 tests (duplicate code, delete-active guard, circular reference)
- `.github/workflows/ci.yml` — GitHub Actions CI pipeline (.NET 10, build + unit tests + TRX artifact upload)

**ส่งให้ทีมอื่น**:
- Team A: `FakeErpItemRepository` + `FakeErpSalesOrderRepository` + `SeedData` พร้อมใช้ใน unit tests ทันที
  Seed data scenarios: PROD-001 (มี BOM, 3 units: PCS/BOX/CTN), PROD-002 (มี BOM, KG), PROD-999 (ไม่มี BOM), Day1 transactions (2 docs), Day2 transaction (1 doc)
- Team B: ไม่มี dependency ตรงใน Sprint 1

**ต้องการจากทีมอื่น**:
- จาก Team A: `src/BomApp.Application/` (IBomService, IBomRepository, ICalculateSalesProductionUseCase, CalculateSalesProductionUseCase, BomService) และ `src/BomApp.Shared/` (Contracts namespace) เพื่อให้ tests compile — deadline: end of Sprint 1

---

### [Team A] — Sprint 1 Backend Implementation — 2026-04-22
**สถานะ**: Done

**Output**:
- `src/BomApp.Shared/Contracts/BomContracts.cs` — BomDto, BomLineDto, CreateBomCommand, UpdateBomCommand (exact match contracts.md)
- `src/BomApp.Shared/Contracts/ProductionContracts.cs` — ProductionOrderDto, ProductionResultDto, MaterialRequirementDto, CalculateSalesProductionRequest, SaveMode, CancelProductionOrderCommand, CreateProductionOrderInternalCommand
- `src/BomApp.Shared/Contracts/ErpContracts.cs` — ErpItemDto, ErpUnitDto, ErpSalesTransactionDto
- `src/BomApp.Shared/Contracts/AuthContracts.cs` — AuthUserDto
- `src/BomApp.Domain/Common/Result.cs` — Result<T> + Result monads
- `src/BomApp.Domain/Entities/` — Bom, BomLine, BomAssignment, ProductionOrder, ProductionOrderLine, AuditLog
- `src/BomApp.Application/Interfaces/IBomService.cs` — exact match interfaces.md
- `src/BomApp.Application/Interfaces/IProductionService.cs` — exact match interfaces.md
- `src/BomApp.Application/Interfaces/ICalculateSalesProductionUseCase.cs` — exact match interfaces.md
- `src/BomApp.Application/Interfaces/Repositories/` — IBomRepository, IBomAssignmentRepository, IProductionOrderRepository, IAuthRepository, IErpItemRepository, IErpSalesOrderRepository
- `src/BomApp.Application/Services/BomService.cs` — code uniqueness, cannot-delete-active, circular ref DFS, activate-needs-lines
- `src/BomApp.Application/Services/ProductionService.cs` — query + cancel (Pending only)
- `src/BomApp.Application/UseCases/CalculateSalesProductionUseCase.cs` — full 5-step flow + multi-level BOM DFS expansion + SaveMode.Daily/PerDocument
- `src/BomApp.Application/DependencyInjection.cs` — AddApplicationServices()
- `src/BomApp.Infrastructure/Persistence/BomDbContext.cs` — HasDefaultSchema("bom")
- `src/BomApp.Infrastructure/Persistence/Configurations/` — 6 EF configs (TEXT[], JSONB, GIN index)
- `src/BomApp.Infrastructure/Persistence/Repositories/` — BomRepository, BomAssignmentRepository, ProductionOrderRepository
- `src/BomApp.Infrastructure/Auth/AuthDbContext.cs` + `AuthRepository.cs`
- `src/BomApp.Infrastructure/Erp/ErpDbContext.cs` + `ErpItemRepository.cs` + `ErpSalesOrderRepository.cs`
- `src/BomApp.Infrastructure/DependencyInjection.cs` — AddInfrastructureServices()
- `src/BomApp.Cli/Program.cs` — CLI skeleton using System.CommandLine

**ส่งให้ทีมอื่น**:
- Team B: IBomService, IProductionService, ICalculateSalesProductionUseCase interfaces พร้อม + DTOs ครบ — ใช้สร้าง ViewModels ได้เลย
- Team C: Interfaces + Application DI พร้อม — unit tests compile ได้

**ต้องการจากทีมอื่น**:
- ไม่มี blocker ใน Sprint 1

---

---

### [Team A] — Sprint 2 Backend — 2026-04-22
**สถานะ**: Done

**Output**:
- `src/BomApp.Cli/appsettings.json` — connection strings template (bom-database, erp-database, authentication-database)
- `src/BomApp.Cli/appsettings.Development.json` — development override (password=postgres)
- `src/BomApp.UI/appsettings.json` — same structure for UI project
- `src/BomApp.Cli/BomApp.Cli.csproj` — เพิ่ม packages: Configuration.Json, Configuration.EnvironmentVariables, DependencyInjection + Content items สำหรับ copy appsettings ไปยัง output
- `src/BomApp.Cli/Program.cs` — wired DI (AddApplicationServices + AddInfrastructureServices), ConfigurationBuilder (json + env vars), ICalculateSalesProductionUseCase, exit codes 1/3/4
- `src/BomApp.Application/Services/BomService.cs` — แก้ไข 3 จุด:
  1. UpdateAsync: เพิ่ม Active guard — ห้ามแก้ Lines/ItemCode/YieldQuantity/YieldUnit เมื่อ status=Active
  2. ActivateAsync: normalize error message เป็น "ต้องมีรายการวัตถุดิบอย่างน้อย 1 รายการ" (ตาม spec)
  3. ทุก business rule ครบ: code uniqueness, circular DFS, activate-needs-lines, delete-Active guard, update-Active guard

**Migration ตรวจสอบแล้ว** (20260422090338_InitialCreate.cs):
- ครบ 6 ตาราง: boms, bom_lines, bom_assignments, production_orders, production_order_lines, audit_logs
- `boms.code` → UNIQUE (idx_boms_code)
- `bom_assignments.item_code` → UNIQUE (idx_bom_assignments_item_code)
- `production_orders.source_so_numbers` → type `text[]`
- `production_orders.bom_snapshot` → type `jsonb`
- GIN index `idx_production_orders_source_so` บน source_so_numbers — PRESENT
- `migrationBuilder.EnsureSchema("bom")` → PRESENT
- ทุก index จาก system-spec.md section 3 → PRESENT (ไม่ต้องแก้ไข migration)

**Task 4 (IBomService Assignment)**: ไม่มี assignment methods บน IBomService — skip ตาม spec

**ส่งให้ทีมอื่น**:
- Team B: CLI พร้อมใช้งาน — `BomApp.Cli calculate --from 2024-01-01 --to 2024-01-31 --mode daily`
- Team B: appsettings.json อยู่ที่ UI project แล้ว — ใช้ connect DB ได้

**ต้องการจากทีมอื่น**:
- ไม่มี blocker

---

### [Team B] — Sprint 2 Frontend — 2026-04-22
**สถานะ**: Done

**Output**:
- `src/BomApp.UI/ViewModels/Bom/BomListViewModel.cs` — full IBomService wiring: LoadAsync, CreateNew, Edit, DeleteAsync, ActivateAsync, DeactivateAsync, SearchText → FilteredItems computed
- `src/BomApp.UI/Views/Bom/BomListView.axaml` — DataGrid (Code/Name/ItemCode/Version/Status badge/UpdatedAt/Actions), search TextBox, error banner, loading overlay, all design tokens
- `src/BomApp.UI/ViewModels/Bom/BomEditorViewModel.cs` — header fields + BomLineEditModel collection + SaveCommand + form validation
- `src/BomApp.UI/Views/Bom/BomEditorView.axaml` — 5-row layout (toolbar, error, header form, lines DataGrid, footer), NumericUpDown สำหรับ YieldQuantity
- `src/BomApp.UI/Converters/BomStatusToBrushConverter.cs` — status → color (Active=green, Inactive=red, Draft=gray)
- `src/BomApp.UI/Services/INavigationService.cs` + `NavigationService.cs` — navigation abstraction

**ส่งให้ทีมอื่น**:
- Team C: BomListView + BomEditorView พร้อมใช้เป็น visual test baseline ใน Sprint 3

**ต้องการจากทีมอื่น**:
- ไม่มี blocker

---

### [Team C] — Sprint 2 Integration — 2026-04-22
**สถานะ**: Done

**Output**:
- `tests/BomApp.Tests.Unit/UseCases/CalculateSalesProductionUseCaseTests.cs` — แก้ไข:
  1. `CalculateAsync_WhenModeIsDaily_ShouldGroupByDocDate` — เพิ่ม `CreateAsync` mock setup returning ProductionOrderDto stub
  2. `CalculateAsync_WhenModeIsPerDocument_ShouldCreateOneOrderPerDocNo` — เพิ่ม `CreateAsync` mock setup เช่นเดียวกัน
  3. `CalculateAsync_WhenUnitIsBox_ShouldConvertToBaseUnit` — เปลี่ยน BOM line qty 1m→2m, เพิ่ม assertion `RequiredQty.Should().Be(140m)`
  4. `CalculateAsync_WhenDryRunIsTrue_ShouldNotCallRepository` — Times.Never verify ยังคงถูกต้อง
- `tests/BomApp.Tests.Unit/Services/BomServiceTests.cs` — ตรวจสอบแล้ว: ทุก assertion เป็น real .Should() calls ครบ — ไม่ต้องแก้

**ผล Unit Tests**: 10/10 passed ✅

**ส่งให้ทีมอื่น**:
- ทุกทีม: unit tests ผ่านครบ พร้อมเริ่ม Sprint 3

**ต้องการจากทีมอื่น**:
- ไม่มี blocker

---

### Build Status (2026-04-22 Sprint 2)
| Project | สถานะ |
|---|---|
| BomApp.Shared | ✅ 0 errors |
| BomApp.Domain | ✅ 0 errors |
| BomApp.Application | ✅ 0 errors |
| BomApp.Infrastructure | ✅ 0 errors |
| BomApp.UI | ✅ 0 errors |
| BomApp.Cli | ✅ 0 errors |
| BomApp.Tests.Fakes | ✅ 0 errors |
| BomApp.Tests.Unit | ✅ 10/10 tests pass |

### Sprint 2 — DONE ✅
Sprint 3 พร้อมเริ่ม — BOM Assignment screen, Production List screen, Sales Calculation screen (wired)

---

### [Team A] — Sprint 3 Backend — 2026-04-22
**สถานะ**: Done

**Output**:
- `src/BomApp.Application/Interfaces/IBomAssignmentService.cs` — interface ใหม่ 4 methods: GetActiveBomsAsync, GetAssignedBomAsync, AssignAsync, RemoveAsync
- `src/BomApp.Application/Services/BomAssignmentService.cs` — implementation: primary constructor syntax, inject IBomRepository + IBomAssignmentRepository, GetActiveBomsAsync filter status="Active" client-side, AssignAsync validate BOM status ก่อน assign
- `src/BomApp.Application/DependencyInjection.cs` — เพิ่ม registration: IBomAssignmentService → BomAssignmentService
- Build: 0 errors, 0 warnings

**ส่งให้ทีมอื่น**: Team B: IBomAssignmentService พร้อมใช้ใน BomAssignmentViewModel แล้ว
**ต้องการจากทีมอื่น**: ไม่มี

---

### [Team C] — Sprint 3 Integration — 2026-04-22
**สถานะ**: Done

**Output**:
- `tests/BomApp.Tests.Integration/BomApp.Tests.Integration.csproj` — xUnit project, net10.0, packages: Testcontainers.PostgreSql 4.11.0, FluentAssertions 8.9.0, Moq 4.20.72, EF Core Design 10.0.7; refs: Infrastructure + Application + Shared
- `tests/BomApp.Tests.Integration/BomDbIntegrationTestBase.cs` — abstract base class, IAsyncLifetime, PostgreSqlContainer (postgres:16-alpine), BomDbContext with MigrateAsync()
- `tests/BomApp.Tests.Integration/Repositories/BomRepositoryIntegrationTests.cs` — 3 tests: CreateAsync persists BOM, GetAllAsync returns created BOMs, ExistsCodeAsync returns true
- `tests/BomApp.Tests.Integration/Repositories/BomAssignmentRepositoryIntegrationTests.cs` — 2 tests: AssignAsync+GetBomId round-trip, RemoveAsync clears assignment
- `.github/workflows/ci.yml` — เพิ่ม Integration Tests step (continue-on-error: true; รอ Docker service)

**ผล**:
- `BomApp.Tests.Integration` build: 0 errors / 0 warnings
- `BomApp.Infrastructure`, `BomApp.Application`, `BomApp.Domain`, `BomApp.Shared`: 0 errors
- `BomApp.Tests.Unit`, `BomApp.Tests.Fakes`: 0 errors (ไม่แตะ)
- `BomApp.UI` build errors 3 อยู่ก่อนหน้า (App.axaml.cs ไม่ได้อัปเดตหลัง Sprint 3 ViewModel เพิ่ม required constructor params) — ไม่ใช่ output ของ Team C

**ต้องการจากทีมอื่น**: ไม่มี

---

### [Team B] — Sprint 3 Frontend — 2026-04-22
**สถานะ**: Done

**Output**:
- `src/BomApp.UI/ViewModels/BomAssignment/BomAssignmentViewModel.cs` — full wire: IBomAssignmentService + IErpItemRepository; ErpItemRow record; FilteredItems (client-side search); RefreshRightPanelAsync; LoadAsync, AssignBomAsync, RemoveAssignmentAsync commands
- `src/BomApp.UI/Views/BomAssignment/BomAssignmentView.axaml` — Grid 2-col layout (left item DataGrid with status badge, right detail card with BOM info + ComboBox + action buttons), error banner, loading indicator; ลบ Design.DataContext แล้ว
- `src/BomApp.UI/ViewModels/Production/ProductionListViewModel.cs` — full wire: IProductionService; filters (dateFrom/dateTo/status/itemCode/createdVia/sourceDocNo); Orders + SelectedOrderLines; SearchAsync, CancelOrderAsync, ExportCsvCommand
- `src/BomApp.UI/Views/Production/ProductionListView.axaml` — toolbar, filter bar (DatePicker/ComboBox/TextBox), error banner, upper DataGrid (9 columns incl. status badge + via badge + actions), lower DataGrid (BOM lines, max 200 height), loading overlay; ลบ Design.DataContext แล้ว
- `src/BomApp.UI/ViewModels/SalesCalculation/SalesCalculationViewModel.cs` — full wire: ICalculateSalesProductionUseCase + IErpSalesOrderRepository; LoadSalesAsync, CalculateAsync (DryRun=true), SaveDocumentsAsync (CanExecute=HasCalculationResult), ExportCsv
- `src/BomApp.UI/Views/SalesCalculation/SalesCalculationView.axaml` — updated Section 1/2/3 bindings, RadioButton IsDaily, real DataGrid columns for ErpSalesTransactionDto + MaterialRequirementDto, Save button IsEnabled=HasCalculationResult; ลบ Design.DataContext แล้ว
- `src/BomApp.UI/App.axaml.cs` — stub registrations: StubBomAssignmentService, StubErpItemRepository, StubProductionService, StubCalculateSalesProductionUseCase, StubErpSalesOrderRepository; ViewModel factories wired with correct constructors

**Build**: `dotnet build src/BomApp.UI/BomApp.UI.csproj` → **0 errors, 0 warnings**

**หมายเหตุ**:
- `ErpItemDto` ใน BomApp.Shared มี fields `Code`/`Name`/`UnitCost` (ไม่มี `ItemCode`/`ItemName`/`Category`) — ErpItemRow ใช้ `Code`/`Name` และ Category=null
- `CancelProductionOrderCommand` มี signature `(Guid OrderId, string Reason)` ตาม ProductionContracts.cs — ไม่มี CancelledBy field
- `ErpSalesTransactionDto` มี `UnitCode` (ไม่ใช่ `Unit`) — DataGrid column ใช้ UnitCode

**ส่งให้ทีมอื่น**:
- Team C: BomAssignmentView, ProductionListView, SalesCalculationView พร้อมเป็น visual test baseline

**ต้องการจากทีมอื่น**:
- Sprint 4: real DI registration เพื่อ replace stubs ใน App.axaml.cs

---

### Build Status (2026-04-22 Sprint 3)
| Project | สถานะ |
|---|---|
| BomApp.Shared | ✅ 0 errors |
| BomApp.Domain | ✅ 0 errors |
| BomApp.Application | ✅ 0 errors |
| BomApp.Infrastructure | ✅ 0 errors |
| BomApp.UI | ✅ 0 errors, 0 warnings |
| BomApp.Cli | ✅ 0 errors |
| BomApp.Tests.Fakes | ✅ 0 errors |
| BomApp.Tests.Unit | ✅ 10/10 pass |
| BomApp.Tests.Integration | ✅ 0 errors (5 tests รอ Docker) |

### Sprint 3 — DONE ✅
Sprint 4 scope: real DI wiring (replace stubs), Login wired จริง, Navigation wired, Docker service ใน CI

---

## Sprint 4 — DI Wiring + CI

### Team B
- [x] Added BomApp.Infrastructure ProjectReference to BomApp.UI.csproj
- [x] Added Microsoft.Extensions.DependencyInjection/Configuration.Json/Configuration.Binder packages
- [x] Built real IServiceProvider in App.axaml.cs using AddApplicationServices() + AddInfrastructureServices(config)
- [x] Replaced all 6 stub classes with real sp.GetRequiredService<T>() calls in RegisterViewModels
- [x] LoginViewModel: injected IAuthRepository, replaced admin/admin placeholder with ValidateUserAsync

### Team C
- [x] Removed continue-on-error from integration tests CI step
- [x] Added timeout-minutes: 5 to integration tests step
- [x] Sprint 4 complete — all stubs replaced, CI gates enforced

---

### [CTO] — ปรับ Sales Calculation Save Flow — 2026-05-23
**สถานะ**: Done

**Output**:
- เพิ่มเอกสาร `bom_production` header: `doc_date`, `doc_no`, `doc_time`
- เพิ่ม `bom_production_detail`: `doc_no`, `item_code`, `qty`, `unit_code`
- ปรับ `CalculateSalesProductionUseCase.SaveAsync` ให้บันทึกเอกสารเบิกรายการสินค้าที่ผลิตแทนการสร้าง production order ต่อสินค้า
- เพิ่ม EF migration `20260523030943_AddBomProductionDocuments`
- อัปเดต `shared/system-spec.md`, `shared/contracts.md`, `shared/interfaces.md`

**Test coverage**:
- `dotnet test` → Unit 11/11 pass, Integration 15/15 pass

**ส่งให้ทีมอื่น**:
- Team B: `SaveAsync` คืน `IReadOnlyList<BomProductionDto>` แล้ว ข้อความ UI ยังนับจำนวนเอกสารได้เหมือนเดิม
- Team C: เพิ่มตารางใหม่ใน migration และปรับ integration test base ให้ใช้ schema `public`

---

## Template สำหรับ Agent บันทึก Output

```
### [Team X] — [Task Name] — [วันที่]
**สถานะ**: Done / In Progress / Blocked

**Output**:
- สร้าง/แก้ไขไฟล์: [path]
- Interface ใหม่ที่พร้อม: [ชื่อ]
- Test coverage: [%]

**ส่งให้ทีมอื่น**:
- Team B: [สิ่งที่ส่ง]
- Team C: [สิ่งที่ส่ง]

**ต้องการจากทีมอื่น**:
- จาก Team A: [สิ่งที่ต้องการ] — deadline: [วัน]
```
