# System Specification — BOM Production Calculator
> Single source of truth สำหรับ CTO และทุกทีม
> อัปเดตทุกครั้งที่มีการเปลี่ยน scope — ต้องผ่าน CTO approve

---

## 1. ภาพรวมระบบ

โปรแกรม **BOM Production Calculator** เป็น Avalonia desktop plugin
ที่ embed อยู่ใน ERP โดยมีหน้าที่หลัก 3 อย่าง:

1. **จัดการสูตรการผลิต (BOM)** — กำหนดว่าผลิตสินค้าหนึ่งชิ้นต้องใช้วัตถุดิบอะไร เท่าไหร่
2. **เชื่อมสูตรกับสินค้า** — ผูก BOM เข้ากับ product item ที่มีอยู่ใน ERP
3. **คำนวณจำนวนสินค้าที่ต้องเบิกผลิต จากรายการขาย** — เลือกเอกสารขายตามช่วงวันที่ รวบรวมสินค้าที่ขายได้ แล้วแตกสูตร BOM เพื่อคำนวณวัตถุดิบที่ต้องตัดเบิกสำหรับการผลิต

ข้อมูลหลักเก็บใน **PostgreSQL schema `public`** ซึ่งอยู่บนฐานข้อมูลเดียวกับ ERP โดย BOM tables ทุกตารางขึ้นต้นด้วย `bom_`
ข้อมูล Items และ Sales Orders ดึงมาจาก **ERP** ผ่าน adapter

### Database Connections (Backend)

| Connection Name | ชี้ไปที่ | หมายเหตุ |
|---|---|---|
| `authentication-database` | ฐานข้อมูล Authentication แยกต่างหาก | ใช้ตรวจสอบ user — ดู `shared/auth-spec.md` |
| `erp-database` | ฐานข้อมูล ERP (`public` schema) | อ่าน ERP Items, Sales Orders และเขียน BOM domain tables (`bom_*`) — ดู `shared/erp-spec.md` และ section 3 |

> **หมายเหตุ**: `erp-database` connection เดียวรองรับทั้ง ERP read และ BOM write บน `public` schema
> BOM tables ทุกตารางขึ้นต้นด้วย `bom_` เพื่อป้องกันชนกับ ERP tables

### Runtime Configuration

Application ต้องโหลด runtime configuration ตอนเปิดโปรแกรมก่อนสร้าง DbContext สำหรับใช้งานจริง และต้อง reload configuration หลังผู้ใช้บันทึกค่าจากหน้าจอ Settings

| Setting | ใช้กับ | หมายเหตุ |
|---|---|---|
| Host | PostgreSQL host | ใช้ร่วมกันทั้ง `authentication-database` และ `erp-database` |
| Port | PostgreSQL port | default `5432` |
| Username | PostgreSQL username | ใช้ร่วมกันทั้ง `authentication-database` และ `erp-database` |
| Password | PostgreSQL password | เก็บในไฟล์เป็น Base64 encoded string (`passwordBase64`) |
| Authen Database Name | `authentication-database` | ใช้ตรวจ login จาก `sml_user_list` |
| Database Name | `erp-database` | ใช้อ่าน ERP และเขียน BOM tables |
| ERP Web Service URL | ERP web service endpoint | เก็บเป็น `erpWebServiceUrl` |
| Provider Code | ERP/provider identifier | เก็บเป็น `providerCode` |

Runtime config file:

```text
{ApplicationData}/BomApp/bomapp.settings.json
```

Platform mapping:

| Platform | ตัวอย่าง path |
|---|---|
| Windows | `%AppData%\BomApp\bomapp.settings.json` |
| macOS/Linux | path จาก `.NET Environment.SpecialFolder.ApplicationData` + `/BomApp/bomapp.settings.json` |

JSON schema:

```json
{
  "databaseConnection": {
    "host": "192.168.2.212",
    "port": 5432,
    "username": "postgres",
    "passwordBase64": "c21s",
    "authDatabaseName": "smlerpmaindebug",
    "databaseName": "productbom"
  },
  "erpWebServiceUrl": "https://erp.example.com/service",
  "providerCode": "SML"
}
```

---

## 2. หน้าจอทั้งหมด (Screens)

### 2.1 Login
**วัตถุประสงค์**: ยืนยันตัวตนก่อนเข้าใช้งาน โดยตรวจสอบกับตาราง `sml_user_list` บน `authentication-database`

| Element | รายละเอียด |
|---|---|
| Input | Username (`user_code`), Password (`user_password`) |
| Action | Login button, Remember me toggle, Settings icon button at bottom-left |
| Validation | Required fields, แสดง error message ใต้ field |
| On success | Navigate → สูตรการผลิต (BOM List) |
| Auth method | ตรวจ `user_code` + `user_password` จาก `sml_user_list` และ `active_status = 1` |
| Lock check | ถ้า `is_lock_record = 1` ให้แสดง error "บัญชีถูกระงับการใช้งาน" |

**Auth Logic**:
```
SELECT * FROM sml_user_list
WHERE user_code = @username
  AND user_password = @password
  AND active_status = 1
  AND is_lock_record = 0
```

**ViewModel**: `LoginViewModel`
**Agent**: team-b-frontend
**Auth table spec**: ดูรายละเอียดเต็มที่ `shared/auth-spec.md`

---

### 2.1a Settings
**วัตถุประสงค์**: ตั้งค่า database connection และ ERP integration ก่อน login หรือเมื่อ connection เปลี่ยน โดยเปิดจาก Settings icon ที่มุมล่างซ้ายของหน้า Login

| Element | รายละเอียด |
|---|---|
| Database Connection | Host, Port, Username, Password, Authen Database Name, Database Name |
| ERP Integration | ERP Web Service URL, Provider Code |
| Action | Save, Close |
| Storage | บันทึกลง `{ApplicationData}/BomApp/bomapp.settings.json` |
| Password storage | Password ต้อง encode เป็น Base64 ใน JSON field `passwordBase64` |
| On save | Reload runtime configuration ทันที เพื่อให้ DbContext scope ใหม่ใช้ connection ล่าสุด |
| On app start | Load runtime configuration ก่อน register/use database connections; ถ้าไม่มี runtime config ให้ fallback ไปที่ `appsettings.json` |

**ViewModel**: `SettingsViewModel`  
**Service**: `IRuntimeConfigurationService` / `RuntimeConfigurationService`

---

### 2.2 สูตรการผลิต (BOM List + Editor)
**วัตถุประสงค์**: ดู/สร้าง/แก้ไข/ลบ สูตรการผลิต

#### 2.2a BOM List
| Element | รายละเอียด |
|---|---|
| แสดงผล | DataGrid แสดง BOM ทั้งหมด (Code, Name, Version, Status, UpdatedAt) |
| Filter | Search box กรองตาม Code หรือ Name |
| Actions | New, Edit (เปิด BOM Editor), Delete (confirmation dialog), Activate/Deactivate |
| Status badge | Active = เขียว, Draft = เทา, Inactive = แดง |

**ViewModel**: `BomListViewModel`

#### 2.2b BOM Editor
| Element | รายละเอียด |
|---|---|
| Header form | Code (required, unique), Name (required), Description, Version (auto) |
| สินค้าที่ใช้สูตรนี้ | Lookup สินค้าจาก `ic_inventory` — แสดง ItemCode + ชื่อสินค้า (required) |
| จำนวนที่ผลิตได้ | Quantity (required, > 0) — ปริมาณผลผลิตที่ได้จากสูตรนี้ 1 รอบ |
| หน่วยนับที่ผลิตได้ | Dropdown หน่วยนับจาก `ic_unit_use` ของสินค้าที่เลือก (required) |
| BOM Lines | DataGrid แบบ inline edit — MaterialCode (lookup จาก ERP), Quantity, Unit |
| Footer | Save, Cancel, Add Line, Remove Line |
| Validation | Code unique, Quantity > 0, ต้องมี line อย่างน้อย 1 รายการ, ต้องเลือกสินค้า |
| Multi-level | BOM Line สามารถเป็น BOM อีกตัว (sub-assembly) |

**ViewModel**: `BomEditorViewModel`
**Agent**: team-b-frontend (UI), team-a-backend (business rules)

---

### 2.3 กำหนดสูตรการผลิต (BOM Assignment)
**วัตถุประสงค์**: ผูก BOM เข้ากับ Product Item จาก ERP

| Element | รายละเอียด |
|---|---|
| Panel ซ้าย | รายการ Product Items จาก ERP (Code, Name, Category) พร้อม search |
| Panel ขวา | BOM ที่ผูกอยู่กับ item ที่เลือก (ถ้ามี) |
| Action | Assign BOM (dropdown เลือกจาก Active BOMs), Remove assignment |
| Constraint | 1 item → 1 BOM เท่านั้น (ถ้า assign ใหม่จะ override) |
| Indicator | แสดง icon ว่า item ไหน assigned แล้ว / ยังไม่ได้ assign |

**ViewModel**: `BomAssignmentViewModel`
**Agent**: team-b-frontend (UI), team-a-backend + team-c-integration (ERP item data)

---

### 2.4 รายการผลิต (Production List)
**วัตถุประสงค์**: แสดงประวัติ Production Orders ทั้งหมด ไม่ว่าจะสร้างจาก UI หรือ CLI พร้อมรายละเอียด source documents และวัตถุดิบที่ต้องเบิก

#### DataGrid หลัก

| Column | ที่มา | หมายเหตุ |
|---|---|---|
| OrderNo | `production_orders.order_no` | PO-YYYYMM-NNNNN |
| ProductCode | `production_orders.item_code` | รหัสสินค้าที่ผลิต |
| BomCode | `boms.code` | รหัสสูตรที่ใช้ |
| Qty | `production_orders.quantity` | จำนวนรวมที่ต้องผลิต |
| Status | `production_orders.status` | Pending / Processing / Done / Cancelled |
| SourceDocCount | `array_length(source_so_numbers)` | จำนวนเอกสารขายที่รวม — `>1` = daily consolidated |
| SourceDateRange | `source_doc_date_from` – `source_doc_date_to` | ช่วงวันที่ของ source documents |
| CreatedAt | `production_orders.created_at` | |
| CreatedBy | `production_orders.created_by` | username หรือ 'SYSTEM' |
| Via | `production_orders.created_via` | badge **UI** (น้ำเงิน) / **CLI** (เทา) |

#### Filter และ Search

| Filter | รายละเอียด |
|---|---|
| Date range | กรองตาม `created_at` ของ production order |
| Status | Pending / Processing / Done / Cancelled |
| Product search | ค้นหาตาม `item_code` หรือชื่อสินค้า |
| Created via | ทั้งหมด / UI เท่านั้น / CLI เท่านั้น |
| ค้นหาเลขเอกสารขาย | ค้นหา doc_no ใน `source_so_numbers[]` (GIN index) |

#### Row detail (Expand)

**① Source Documents** — รายการเอกสารขายที่นำมาคำนวณ

| Column | ที่มา |
|---|---|
| DocNo | จาก `source_so_numbers[]` |
| DocDate | ดึงประกอบจาก `ic_trans_detail.doc_date` |
| ItemCount | จำนวน item ในเอกสารนั้นที่นำมาคำนวณ |

**② Material Breakdown** — วัตถุดิบที่ต้องเบิกสำหรับ order นี้

| Column | ที่มา |
|---|---|
| MaterialCode | `production_order_lines.material_code` |
| MaterialName | `production_order_lines.material_name` |
| RequiredQty | `production_order_lines.required_quantity` |
| Unit | `production_order_lines.unit` |

#### Actions

| Action | เงื่อนไข | หมายเหตุ |
|---|---|---|
| View detail | ทุก status | เปิด dialog แสดงข้อมูลครบ |
| Cancel order | status = Pending เท่านั้น | เปลี่ยน status → Cancelled, บันทึก audit_log |
| Export to CSV | ทุก status | export ทั้ง order header + material lines |

#### Pagination
50 rows ต่อหน้า + virtual scroll

**ViewModel**: `ProductionListViewModel`
**Agent**: team-b-frontend (UI), team-a-backend (data)

---

### 2.5 คำนวณการผลิตจากการขาย (Sales Calculation)
**วัตถุประสงค์**: เลือกรายการขายตามช่วงวันที่ กรองเฉพาะสินค้าที่มีสูตรการผลิต แล้วแตก BOM คำนวณวัตถุดิบที่ต้องตัดเบิก จากนั้นบันทึกเป็นเอกสารได้ 2 รูปแบบ

#### UI Elements

| Element | รายละเอียด |
|---|---|
| Filter วันที่ | วันที่เริ่มต้น — วันที่สิ้นสุด (ดึงจาก `ic_trans_detail.doc_date`) |
| ปุ่ม Load | ดึงรายการขายตามช่วงวันที่ที่เลือก (trans_flag=44, last_status=0) |
| DataGrid รายการขาย | แสดงรายการสินค้าที่ขายได้ (DocNo, DocDate, ItemCode, ItemName, Qty, Unit) |
| แสดง/ซ่อน | Checkbox "แสดงเฉพาะสินค้าที่มี BOM" — กรองรายการที่คำนวณได้ |
| Warning badge | แสดงจำนวนรายการสินค้าที่ไม่มี BOM assign (ข้ามไปโดยอัตโนมัติ) |
| ผลการคำนวณ | DataGrid แสดงวัตถุดิบที่ต้องตัดเบิก (MaterialCode, MaterialName, TotalQty, Unit) |
| รูปแบบบันทึก | Radio: **รวมเป็น 1 เอกสารต่อ 1 วัน** หรือ **แยกตามเอกสารขาย** |
| Actions | Calculate, บันทึกเอกสาร, Export CSV |

#### Processing Flow

```
1. Load รายการขาย
   └─ ดึง ic_trans_detail WHERE trans_flag=44 AND last_status=0
      AND doc_date BETWEEN @dateFrom AND @dateTo

2. กรองสินค้าที่เข้าข่าย
   └─ เฉพาะ item_code ที่มีใน bom_assignments (มี BOM ที่ Active)
   └─ รายการที่ไม่มี BOM → แสดง warning และข้ามไป

3. แปลงหน่วย
   └─ qty × stand_value / divide_value → qty_in_base_unit

4. คำนวณวัตถุดิบ
   └─ สำหรับแต่ละสินค้า: (qty_in_base_unit / bom.yield_quantity) × bom_line.quantity
   └─ รวมยอดวัตถุดิบเดียวกันจากทุกรายการ

5. บันทึกเอกสารเบิกรายการสินค้าที่ผลิต (เลือกรูปแบบ)
   ├─ รวม 1 เอกสารต่อวัน → สร้าง bom_productions 1 header, บันทึกรายการขายลง bom_production_orders, บันทึกของที่ต้องใช้ลง bom_production_details
   └─ แยกตามเอกสารขาย → สร้าง bom_productions ต่อบิลขาย, บันทึกรายการขายลง bom_production_orders, บันทึกของที่ต้องใช้ลง bom_production_details
```

#### Business Rules เพิ่มเติม

| Rule | รายละเอียด |
|---|---|
| สินค้าไม่มี BOM | ข้ามโดยอัตโนมัติ แสดง warning summary ก่อนบันทึก |
| BOM ต้อง Active | ใช้เฉพาะ BOM ที่ status = Active เท่านั้น |
| Yield quantity | หารด้วย `bom.yield_quantity` ก่อนคูณ BOM line เพื่อให้ได้สัดส่วนที่ถูกต้อง |
| ซ้ำซ้อน | ถ้า doc_no เดิมถูกบันทึกไปแล้ว ให้แสดง warning ว่าเอกสารนี้เคยสร้างไปแล้ว |

#### CLI Command Interface

> รองรับการเรียกผ่าน command-line เพื่อใช้เป็น cronjob ในอนาคต
> ใช้ use case เดิม (`CalculateSalesProductionUseCase`) — ไม่ duplicate logic

**Project**: `src/BomApp.Cli` (entry point แยกจาก UI)
**Library**: `System.CommandLine`

```bash
# รูปแบบคำสั่ง
BomApp.Cli calculate
  --from   <yyyy-MM-dd>          # วันที่เริ่มต้น (required)
  --to     <yyyy-MM-dd>          # วันที่สิ้นสุด (required)
  --mode   <daily|per-document>  # รูปแบบบันทึก (default: daily)
  --dry-run                      # คำนวณแต่ไม่บันทึก (optional)
  --output <csv|json|none>       # export ผลลัพธ์ (default: none)

# ตัวอย่าง — cronjob รันทุกวัน บันทึกแบบรวมต่อวัน
BomApp.Cli calculate --from 2024-01-15 --to 2024-01-15 --mode daily

# ตัวอย่าง — dry run ดูผลก่อนบันทึก
BomApp.Cli calculate --from 2024-01-01 --to 2024-01-31 --mode per-document --dry-run

# ตัวอย่าง — cronjob วันก่อนหน้า + export CSV
BomApp.Cli calculate --from yesterday --to yesterday --mode daily --output csv
```

**Exit Codes**:

| Code | ความหมาย |
|---|---|
| 0 | สำเร็จ |
| 1 | ไม่มีรายการขายในช่วงวันที่ที่ระบุ |
| 2 | มีข้อผิดพลาดจาก ERP connection |
| 3 | มีข้อผิดพลาดจาก BOM database |
| 4 | Invalid arguments |

**Architecture — shared use case**:
```
UI (SalesCalculationViewModel)  ─┐
                                  ├─→ CalculateSalesProductionUseCase (Application layer)
CLI (BomApp.Cli calculate)       ─┘        └─→ IErpSalesOrderRepository
                                            └─→ IBomRepository
                                            └─→ IProductionOrderRepository
```

> ดู ADR รายละเอียดที่ `shared/adr/001-cli-command-for-sales-calculation.md`

**ViewModel**: `SalesCalculationViewModel`
**Agent**: team-b-frontend (UI), team-a-backend (calculation engine + CLI), team-c-integration (ERP data)

---

## 3. Database Tables (PostgreSQL — schema `public`)

> ตาราง ERP (Items, SalesOrders) อยู่ใน `public` schema เช่นกัน — ไม่ได้เก็บในนี้
> ระบบนี้เก็บเฉพาะข้อมูล BOM domain ทั้งหมดอยู่ใน **schema `public`** บนฐานข้อมูลเดียวกับ ERP
> ทุก BOM table ขึ้นต้นด้วย `bom_` เพื่อแยกออกจาก ERP tables

### 3.1 `bom_boms` — หัว BOM

| Column | Type | Constraint | หมายเหตุ |
|---|---|---|---|
| `id` | UUID | PK | gen_random_uuid() |
| `code` | VARCHAR(50) | UNIQUE, NOT NULL | รหัส BOM เช่น BOM-001 |
| `name` | VARCHAR(200) | NOT NULL | ชื่อสูตร |
| `description` | TEXT | NULL | คำอธิบาย |
| `item_code` | VARCHAR(50) | NOT NULL | รหัสสินค้าที่ใช้สูตรนี้ → ref `ic_inventory.code` |
| `item_name` | VARCHAR(255) | NOT NULL | ชื่อสินค้า (denormalized จาก ERP) |
| `yield_quantity` | DECIMAL(18,6) | NOT NULL CHECK (> 0) | จำนวนที่ผลิตได้ต่อ 1 รอบ |
| `yield_unit` | VARCHAR(50) | NOT NULL | หน่วยนับที่ผลิตได้ → ref `ic_unit.code` |
| `version` | INT | NOT NULL DEFAULT 1 | เพิ่มทุกครั้งที่แก้ |
| `status` | VARCHAR(20) | NOT NULL DEFAULT 'Draft' | Draft/Active/Inactive |
| `created_at` | TIMESTAMPTZ | NOT NULL DEFAULT NOW() | |
| `updated_at` | TIMESTAMPTZ | NOT NULL DEFAULT NOW() | |
| `created_by` | VARCHAR(100) | NOT NULL | username |

**Index**: `idx_bom_boms_code` (code), `idx_bom_boms_status` (status)

---

### 3.2 `bom_lines` — รายการวัตถุดิบในสูตร

| Column | Type | Constraint | หมายเหตุ |
|---|---|---|---|
| `id` | UUID | PK | |
| `bom_id` | UUID | FK → bom_boms.id, NOT NULL | CASCADE DELETE |
| `material_code` | VARCHAR(50) | NOT NULL | รหัสวัตถุดิบจาก ERP Items |
| `material_name` | VARCHAR(200) | NOT NULL | ชื่อ (denormalized จาก ERP) |
| `quantity` | DECIMAL(18,6) | NOT NULL CHECK (> 0) | |
| `unit` | VARCHAR(20) | NOT NULL | kg, pcs, m, L, etc. |
| `sub_bom_id` | UUID | FK → bom_boms.id, NULL | ถ้าเป็น sub-assembly |
| `sort_order` | INT | NOT NULL DEFAULT 0 | ลำดับแสดงผล |
| `notes` | TEXT | NULL | |

**Index**: `idx_bom_lines_bom_id` (bom_id)
**Constraint**: ห้าม `sub_bom_id` สร้าง circular reference (ตรวจใน application layer)

---

### 3.3 `bom_assignments` — เชื่อม Product Item กับ BOM

| Column | Type | Constraint | หมายเหตุ |
|---|---|---|---|
| `id` | UUID | PK | |
| `item_code` | VARCHAR(50) | UNIQUE, NOT NULL | รหัส item จาก ERP |
| `item_name` | VARCHAR(200) | NOT NULL | ชื่อ (denormalized) |
| `bom_id` | UUID | FK → bom_boms.id, NOT NULL | |
| `assigned_at` | TIMESTAMPTZ | NOT NULL DEFAULT NOW() | |
| `assigned_by` | VARCHAR(100) | NOT NULL | |

**Index**: `idx_bom_assignments_item_code` (item_code), `idx_bom_assignments_bom_id` (bom_id)
**Note**: UNIQUE บน item_code = 1 item มีได้แค่ 1 BOM

---

### 3.4 `bom_productions` — Header เอกสารผลิต

| Column | Type | Constraint | หมายเหตุ |
|---|---|---|---|
| `id` | UUID | PK | |
| `doc_date` | DATE | NOT NULL | วันที่เอกสารผลิต |
| `doc_no` | VARCHAR(30) | UNIQUE, NOT NULL | เลขที่เอกสารผลิต เช่น BP-YYYYMMDD-NNNNN |
| `doc_time` | TIME | NOT NULL | เวลาเอกสารผลิต |

**Index**: `idx_bom_productions_doc_no` (doc_no), `idx_bom_productions_doc_date` (doc_date)

---

### 3.5 `bom_production_orders` — รายการขายที่เลือกไว้สำหรับคำนวณการผลิต

| Column | Type | Constraint | หมายเหตุ |
|---|---|---|---|
| `id` | UUID | PK | |
| `doc_no` | VARCHAR(30) | NOT NULL | เลขที่เอกสารผลิตที่ระบบสร้าง เช่น BP-YYYYMMDD-NNNNN |
| `doc_date` | DATE | NOT NULL | วันที่เอกสารผลิต |
| `ref_doc_no` | VARCHAR(50) | NOT NULL | เลขที่บิลขายจาก ERP |
| `ref_doc_date` | DATE | NOT NULL | วันที่บิลขายจาก ERP |
| `item_code` | VARCHAR(50) | NOT NULL | รหัสสินค้าที่ขายและถูกเลือกมาคำนวณ |
| `qty` | DECIMAL(18,6) | NOT NULL | จำนวนขายตามรายการที่เลือก |
| `unit_code` | VARCHAR(50) | NOT NULL | หน่วยนับของรายการขาย |

**Index**: `idx_bom_production_orders_doc_no` (doc_no), `idx_bom_production_orders_doc_date` (doc_date), `idx_bom_production_orders_ref_doc_no` (ref_doc_no), `idx_bom_production_orders_item_code` (item_code)

---

### 3.6 `bom_production_details` — รายการสินค้าที่ต้องใช้

| Column | Type | Constraint | หมายเหตุ |
|---|---|---|---|
| `id` | UUID | PK | |
| `doc_no` | VARCHAR(30) | FK → bom_productions.doc_no, NOT NULL | CASCADE DELETE |
| `item_code` | VARCHAR(50) | NOT NULL | รหัสสินค้าที่ต้องใช้ / วัตถุดิบ |
| `item_name` | VARCHAR(255) | NOT NULL | ชื่อสินค้า / วัตถุดิบ |
| `qty` | DECIMAL(18,6) | NOT NULL | จำนวนที่ต้องใช้จาก BOM expansion |
| `unit_code` | VARCHAR(50) | NOT NULL | หน่วยนับ |

**Index**: `idx_bom_production_details_doc_no` (doc_no), `idx_bom_production_details_item_code` (item_code)

---

### 3.7 `bom_audit_logs` — บันทึกการเปลี่ยนแปลง

| Column | Type | Constraint | หมายเหตุ |
|---|---|---|---|
| `id` | UUID | PK | |
| `entity_type` | VARCHAR(50) | NOT NULL | Bom/BomLine/ProductionOrder/etc. |
| `entity_id` | UUID | NOT NULL | |
| `action` | VARCHAR(20) | NOT NULL | Create/Update/Delete/Activate/etc. |
| `changed_by` | VARCHAR(100) | NOT NULL | |
| `changed_at` | TIMESTAMPTZ | NOT NULL DEFAULT NOW() | |
| `old_values` | JSONB | NULL | ก่อนเปลี่ยน |
| `new_values` | JSONB | NULL | หลังเปลี่ยน |

**Index**: `idx_bom_audit_entity` (entity_type, entity_id), `idx_bom_audit_changed_at` (changed_at DESC)

---

## 4. Entity Relationship (สรุป)

```
bom_boms (1) ──────────── (N) bom_lines
  │                               │
  │                         sub_bom_id (self-ref, optional)
  │
  └── (1) ── bom_assignments (N) ── item_code [ERP]
  │
  └── (1) ──────────── (N) bom_productions
                                      ├── (1) ── (N) bom_production_orders
                                      └── (1) ── (N) bom_production_details

[ERP] ic_inventory ──── item_code ──────── bom_assignments
[ERP] ic_trans_detail ─ doc_no ─────────── bom_production_orders.ref_doc_no
                      (trans_flag=44,
                       last_status=0)
```

---

## 5. Business Rules สำคัญ

| Rule | รายละเอียด |
|---|---|
| BOM Code unique | ห้ามซ้ำทั้งระบบ |
| BOM ต้อง Active ก่อน assign | ห้าม assign BOM ที่ status = Draft หรือ Inactive |
| Circular reference | ตรวจด้วย DFS ก่อน save bom_line ที่มี sub_bom_id |
| 1 item = 1 BOM | UNIQUE constraint บน bom_assignments.item_code |
| Production save process | เมื่อบันทึกผลการประมวลผลการขาย ให้สร้าง `bom_productions` header, เก็บรายการขายใน `bom_production_orders`, และเก็บรายการสินค้าที่ต้องใช้ใน `bom_production_details` |
| Quantity > 0 | ทุก bom_line และ production_order ต้องมี quantity > 0 |

---

## 6. Data Flow

```
[User เลือกช่วงวันที่]
        ↓
[SalesCalculationViewModel] → เรียก IErpSalesOrderRepository
        ↓ ดึง ic_trans_detail (trans_flag=44, last_status=0, doc_date BETWEEN @from AND @to)
        ↓
[กรองสินค้าที่มี BOM]
        ├─ มี BOM Active → เข้าสู่การคำนวณ
        └─ ไม่มี BOM → warning และข้ามไป
        ↓
[แปลงหน่วย]
        └─ qty × stand_value / divide_value → qty_in_base_unit
        ↓
[ExpandBomUseCase] ← ดึง BOM จาก DB (bom_assignments → boms → bom_lines)
        ↓ expand multi-level BOM (recursive)
        ↓ (qty_in_base_unit / bom.yield_quantity) × bom_line.quantity
        ↓ รวมยอดวัตถุดิบเดียวกัน
        ↓
[ProductionResultDto] → แสดงผลใน UI
        ↓ user เลือกรูปแบบบันทึก
        ├─ รวม 1 เอกสารต่อวัน
        │       ↓ สร้าง doc_no 1 ชุดต่อ doc_date
        │          bom_production_orders = รายการขายที่เลือกของวันนั้น
        │          bom_production_details = รายการสินค้าที่ต้องใช้ของวันนั้น
        └─ แยกตามเอกสารขาย
                ↓ สร้าง doc_no 1 ชุดต่อบิลขาย
                   bom_production_orders = รายการขายที่เลือกของบิลขายนั้น
                   bom_production_details = รายการสินค้าที่ต้องใช้ของบิลขายนั้น
        ↓
[bom_productions + bom_production_orders + bom_production_details] ← บันทึกลง PostgreSQL schema public
```

---

## 7. Screen → Table Mapping

| Screen | อ่าน | เขียน |
|---|---|---|
| BOM List | `boms` | — |
| BOM Editor | `boms`, `bom_lines` | `boms`, `bom_lines`, `audit_logs` |
| BOM Assignment | `bom_assignments`, [ERP items] | `bom_assignments`, `audit_logs` |
| Production List | `bom_productions`, `bom_production_orders`, `bom_production_details` | `bom_productions` (delete cascade) |
| Sales Calculation | `boms`, `bom_lines`, `bom_assignments`, [`ic_trans_detail`, `ic_inventory`] | `bom_productions`, `bom_production_orders`, `bom_production_details`, `audit_logs` |
