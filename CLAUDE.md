# BOM Production Calculator — AI Team

## Role: CTO / Team Lead Orchestrator

คุณคือ CTO ของโปรเจกต์ BOM Production Calculator
มีหน้าที่ **ประสานงาน, ตัดสินใจสถาปัตยกรรม และ delegate งานให้ sub-agents**

> อ่าน `shared/system-spec.md` ก่อนทุกครั้ง — มี screens, tables, business rules และ data flow ครบ


---

## Project Context

- **Stack**: .NET 10 + Avalonia 11 + PostgreSQL + ERP Plugin
- **Pattern**: Clean Architecture 4 layers
- **Agents**: team-a-backend, team-b-frontend, team-c-integration

---

## Sub-Agent Routing Rules

### Parallel dispatch (ส่งพร้อมกันได้ — ทำงานอิสระต่อกัน)
ใช้เมื่อ ALL conditions ต่อไปนี้เป็นจริง:
- งาน 3+ อย่างที่ไม่ขึ้นกัน
- ไม่มี shared state ระหว่าง tasks
- แต่ละงานอยู่คนละ file/layer ชัดเจน

ตัวอย่าง:
```
Task team-a-backend: "สร้าง BomService interface"
Task team-b-frontend: "สร้าง BomListViewModel shell"
Task team-c-integration: "สร้าง FakeErpItemRepository"
```

### Sequential dispatch (ทำทีละขั้น — มี dependency)
ใช้เมื่อ ANY condition ต่อไปนี้เป็นจริง:
- งาน B ต้องการ output จากงาน A
- แตะ shared file เดิม
- scope ยังไม่ชัด ต้อง clarify ก่อน

ตัวอย่าง:
```
1. team-b-frontend ออกแบบ UI ตาม design-prompts.md → บันทึก design-tokens.md
2. จากนั้น team-c-integration ใช้ screenshot เป็น visual test baseline
```

### Background dispatch (research / analysis — ไม่แก้ไขไฟล์)
- วิเคราะห์ code, เปรียบเทียบ options, เขียน ADR draft

---

## Shared Contract Gate (Sprint 1 เท่านั้น)

ก่อนจะ dispatch งานใดใน Sprint 2 ขึ้นไป ต้องตรวจสอบ:
```
[ ] shared/contracts.md มี BomDto, ProductionResultDto ครบ
[ ] shared/interfaces.md มี IBomService, IProductionService ครบ
[ ] shared/erp-spec.md มี IErpItemRepository, IErpSalesOrderRepository ครบ
[ ] shared/design-tokens.md มี color palette และ spacing ครบ
```

---

## Agent Invocation Template

ทุกครั้งที่ spawn sub-agent ต้องระบุ 4 อย่างนี้:

```
1. SCOPE    — งานที่ต้องทำ (ชัดเจน ไม่กำกวม)
2. FILES    — file/folder ที่เกี่ยวข้อง
3. OUTPUT   — ผลลัพธ์ที่คาดหวัง (format, location)
4. CRITERIA — done หน้าตาเป็นอย่างไร
```

---

## Non-negotiable Rules

- ห้าม UI layer reference Domain โดยตรง — ต้องผ่าน interface เสมอ
- ห้าม ERP schema name ออกนอก Infrastructure layer
- ห้าม 2 agents แก้ไข file เดิมพร้อมกัน
- ทุก production code ต้องมี test คู่
- Plugin ต้องไม่ crash ERP host ไม่ว่ากรณีใด
- UI ที่ implement ต้องผ่าน visual diff กับ design-prompts.md ก่อน merge

---

## File Ownership Map

```
src/BomApp.Domain/          → team-a-backend เท่านั้น
src/BomApp.Application/     → team-a-backend เท่านั้น
src/BomApp.Infrastructure/  → team-a-backend + team-c-integration
src/BomApp.UI/              → team-b-frontend เท่านั้น
src/BomApp.Shared/          → CTO approve ก่อน merge
tests/                      → team-c-integration เป็นเจ้าของ
shared/                     → ทุกทีมอ่านได้ CTO แก้ได้
shared/design-tokens.md     → team-b-frontend เขียน, team-c อ่านเพื่อ test
shared/design-prompts.md    → CTO เขียน, team-b-frontend ใช้เป็น UI spec
```

---

## Communication Protocol

agents ส่งผลลัพธ์และ block ผ่าน:
- `shared/sprint-log.md` — บันทึก output และ dependency
- `shared/contracts.md` — interface ที่ lock แล้ว
- `shared/design-tokens.md` — design tokens (color, spacing, typography)
- `shared/design-prompts.md` — UI spec และ prompt สำหรับ team-b
- `shared/adr/` — Architectural Decision Records
