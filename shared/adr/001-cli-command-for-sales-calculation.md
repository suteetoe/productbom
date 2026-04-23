# ADR-001: CLI Command Entry Point สำหรับ Sales Calculation

**Status**: Accepted
**Date**: 2026-04-16
**Deciders**: CTO
**Tags**: architecture, cli, cronjob, clean-architecture

---

## Context

หน้าจอ 2.5 Sales Calculation ต้องรองรับ 2 modes ของการเรียกใช้งาน:

1. **Interactive UI** — user เปิดโปรแกรม เลือกวันที่ และกดปุ่มคำนวณ
2. **Automated/Cronjob** — ระบบ scheduler เรียกผ่าน command-line โดยไม่มี user interaction

ปัญหาคือถ้า calculation logic ถูก implement ไว้ใน `SalesCalculationViewModel` โดยตรง จะทำให้ไม่สามารถเรียกจาก CLI ได้โดยไม่ duplicate code

---

## Decision

### 1. แยก Calculation Logic ออกมาเป็น Use Case ใน Application Layer

สร้าง `CalculateSalesProductionUseCase` ใน `src/BomApp.Application/UseCases/`
โดย ViewModel และ CLI ต่างเรียก use case เดิม ไม่มีการ duplicate logic

```
src/
  BomApp.Application/
    UseCases/
      CalculateSalesProductionUseCase.cs   ← core logic อยู่ที่นี่
      CalculateSalesProductionRequest.cs   ← input parameters
      CalculateSalesProductionResult.cs    ← output / ProductionResultDto

  BomApp.UI/
    ViewModels/
      SalesCalculationViewModel.cs         ← เรียก use case, จัดการ UI state

  BomApp.Cli/                              ← project ใหม่ (console app)
    Commands/
      CalculateCommand.cs                  ← parse args → เรียก use case → print result
    Program.cs
```

### 2. สร้าง BomApp.Cli Project

- **Type**: .NET 8 Console Application
- **Library**: `System.CommandLine` (official Microsoft CLI library)
- **DI**: ใช้ `Microsoft.Extensions.Hosting` เพื่อ share DI container กับ Application layer
- **Config**: อ่าน connection strings จาก `appsettings.json` / environment variables

### 3. Input Parameters

| Parameter | Type | Required | Default | หมายเหตุ |
|---|---|:---:|---|---|
| `--from` | `yyyy-MM-dd` หรือ `yesterday` | ✓ | — | วันที่เริ่มต้น |
| `--to` | `yyyy-MM-dd` หรือ `yesterday` | ✓ | — | วันที่สิ้นสุด |
| `--mode` | `daily` / `per-document` | — | `daily` | รูปแบบบันทึกเอกสาร |
| `--dry-run` | flag | — | false | คำนวณแต่ไม่ write DB |
| `--output` | `csv` / `json` / `none` | — | `none` | export ผลลัพธ์ |

### 4. Exit Codes

| Code | ความหมาย |
|---|---|
| 0 | สำเร็จ — บันทึก production orders เรียบร้อย |
| 1 | ไม่มีรายการขายในช่วงวันที่ที่ระบุ (ไม่ใช่ error) |
| 2 | ERP connection error |
| 3 | BOM database error |
| 4 | Invalid arguments |
| 5 | ทุกรายการข้ามหมด (ไม่มีสินค้าที่มี BOM) |

### 5. Logging

- ใช้ `Microsoft.Extensions.Logging` → output ไปที่ console + file
- Log level ปรับได้ผ่าน `--verbosity` flag หรือ environment variable `BOM_LOG_LEVEL`
- Format: structured JSON เมื่อ redirect stdout (เพื่อให้ log aggregator อ่านได้)

---

## Consequences

**ข้อดี**:
- UI และ CLI ใช้ logic เดิม — ไม่มี divergence
- Testable: use case สามารถ unit test ได้โดยไม่ต้องเปิด UI
- Cronjob friendly: exit code ชัดเจน, output สามารถ pipe ได้
- Dry-run mode ช่วย QA ตรวจผลก่อน schedule จริง

**ข้อเสีย / ต้องระวัง**:
- เพิ่ม project `BomApp.Cli` → ต้อง maintain build pipeline เพิ่ม
- ต้อง deploy `BomApp.Cli` binary ไปที่ server ที่รัน cronjob ด้วย
- Connection strings ต้องจัดการ secrets อย่างระมัดระวัง (ไม่ hardcode)

---

## ตัวอย่าง Cronjob (Linux crontab)

```bash
# รันทุกวันตี 1 — คำนวณรายการขายของวันก่อนหน้า
0 1 * * * /opt/bomapp/BomApp.Cli calculate \
    --from yesterday \
    --to yesterday \
    --mode daily \
    >> /var/log/bomapp/calculate.log 2>&1

# รันทุกวันจันทร์ตี 2 — คำนวณรายการขายทั้งสัปดาห์ที่ผ่านมา
0 2 * * 1 /opt/bomapp/BomApp.Cli calculate \
    --from $(date -d 'last monday' +%Y-%m-%d) \
    --to $(date -d 'last sunday' +%Y-%m-%d) \
    --mode daily \
    --output csv \
    >> /var/log/bomapp/weekly.log 2>&1
```

---

## Alternatives Considered

| Option | เหตุผลที่ไม่เลือก |
|---|---|
| Implement logic ใน ViewModel แล้ว expose HTTP API | Overkill สำหรับ desktop plugin, เพิ่ม attack surface |
| ใช้ Windows Task Scheduler เรียก UI แบบ headless | Avalonia ไม่รองรับ headless mode ได้ดี |
| ใช้ Hangfire / Quartz.NET ใน background service | ซับซ้อนเกินไป, จัดการ cronjob ที่ OS level ดีกว่า |
