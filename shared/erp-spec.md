# ERP Database Specification
> Reference tables จาก `erp-database` — **อ่านอย่างเดียว** ห้าม write หรือ migrate
> อัปเดตเมื่อ ERP schema เปลี่ยน — ต้องผ่าน CTO approve

---

## Connection
- **Connection name**: `erp-database`
- **เข้าถึงผ่าน**: Infrastructure layer เท่านั้น — ผ่าน `IErpItemRepository`, `IErpSalesOrderRepository`, `IErpStockRequestProcessor`
- **ห้าม**: Domain / Application layer reference connection นี้โดยตรง

---

## ERP Web Service

### Process Stock Request

ใช้หลังจากระบบบันทึก production issue document เข้า ERP แล้ว เพื่อสั่งให้ ERP ประมวลผล stock request ของรายการสินค้าที่เกี่ยวข้อง
ก่อนเรียก endpoint นี้ `ErpProductionRepository` ต้องบันทึกเอกสารผลิตลง ERP tables `ic_trans` และ `ic_trans_detail` ให้สำเร็จก่อน โดยรายการใน `ic_trans_detail` ใช้ `trans_type = 3`, `trans_flag = 56`, และ `calc_flag = -1`

```http
POST {ERP Web Service URL}/SMLJavaWebService/rest/v1/processstockrequest
Content-Type: application/json
```

Runtime settings ที่ใช้:

| Field | Source |
|---|---|
| `providerCode` | `RuntimeAppSettings.ProviderCode` |
| `databaseName` | `RuntimeAppSettings.DatabaseConnection.DatabaseName` |
| `itemCode` | distinct `BomProductionDto.Details[].ItemCode` หลังบันทึกเอกสาร |

Payload:

```json
{
  "providerCode": "IMEXERPPOC",
  "databaseName": "imexpocdata",
  "itemCode": [
    "04000-IS4HF",
    "04000-IS6BB1"
  ]
}
```

Implementation contract:
- Application layer เรียกผ่าน `IErpStockRequestProcessor` เท่านั้น
- Infrastructure layer เป็นผู้ประกอบ URL, payload, และ HTTP POST
- ถ้า ERP web service ตอบ non-success ให้ `SaveAsync` คืน `Result.Failure` เพื่อไม่ให้ plugin crash ERP host

---

## Tables

### `ic_inventory` — ข้อมูลสินค้า / วัตถุดิบ

> ใช้สำหรับ: BOM Line lookup (MaterialCode), BOM Assignment (Product Item list)

| Column | Type | หมายเหตุ |
|---|---|---|
| `code` | VARCHAR(25) | PK — รหัสสินค้า / วัตถุดิบ |
| `name_1` | VARCHAR(255) | ชื่อสินค้า |
| `unit_cost` | VARCHAR(25) | หน่วยต้นทุน (เช่น kg, pcs, m, L) |

**ใช้ใน Screen**: BOM Editor (lookup วัตถุดิบ), BOM Assignment (แสดงรายการ Product Items)

**Repository method ที่ต้องการ**:
```csharp
IErpItemRepository:
  Task<IEnumerable<ErpItemDto>> GetAllItemsAsync();
  Task<ErpItemDto?> GetItemByCodeAsync(string code);
  Task<IEnumerable<ErpItemDto>> SearchItemsAsync(string keyword);
```

---

### `ic_unit` — ตารางหน่วยนับ (Master)

> ใช้สำหรับ: แสดงชื่อหน่วยนับในหน้าจอ BOM Editor และ Production

| Column | Type | หมายเหตุ |
|---|---|---|
| `code` | VARCHAR(25) | PK — รหัสหน่วยนับ |
| `name_1` | VARCHAR(255) | ชื่อหน่วยนับ (เช่น กิโลกรัม, ชิ้น, เมตร) |

---

### `ic_unit_use` — อัตราส่วนหน่วยนับต่อสินค้า

> ใช้สำหรับ: แปลงหน่วยนับเมื่อคำนวณวัตถุดิบใน BOM
> สินค้าหนึ่งรายการมีได้หลายหน่วย — แต่ละแถวคือ conversion rule ระหว่างหน่วยกับสินค้านั้น

| Column | Type | หมายเหตุ |
|---|---|---|
| `code` | VARCHAR(25) | PK (ร่วม) — รหัสหน่วยนับ → FK `ic_unit.code` |
| `ic_code` | VARCHAR(25) | PK (ร่วม) — รหัสสินค้า → FK `ic_inventory.code` |
| `stand_value` | NUMERIC | ค่ามาตรฐาน (ตัวตั้ง) ของการแปลงหน่วย |
| `divide_value` | NUMERIC | ค่าหาร ของการแปลงหน่วย |
| `ratio` | INTEGER | อัตราส่วนโดยรวม |
| `line_number` | INTEGER | ลำดับที่ของหน่วยในรายการสินค้านั้น |

**Primary Key**: `(code, ic_code)`

**ตัวอย่าง Unit Conversion**:
```
ic_code = "MAT-001" (วัตถุดิบ A)
  line 1: code = "PCS",  stand_value = 1,   divide_value = 1    → 1 PCS = 1 PCS (หน่วยหลัก)
  line 2: code = "BOX",  stand_value = 12,  divide_value = 1    → 1 BOX = 12 PCS
  line 3: code = "CTN",  stand_value = 144, divide_value = 1    → 1 CTN = 144 PCS
```

**ใช้ใน**: BOM Editor (dropdown เลือกหน่วยของ BOM Line), คำนวณ material requirement

**Repository method ที่ต้องการ** (เพิ่มใน `IErpItemRepository`):
```csharp
  Task<IEnumerable<ErpUnitDto>> GetUnitsByItemCodeAsync(string icCode);
  Task<IEnumerable<ErpUnitDto>> GetAllUnitsAsync();
```

```csharp
public record ErpUnitDto(
    string Code,
    string Name,
    string IcCode,
    decimal StandValue,
    decimal DivideValue,
    int Ratio,
    int LineNumber
);
```

---

### `ic_trans_detail` — รายการขายสินค้า

> ใช้สำหรับ: Sales Calculation — ดึงรายการสินค้าที่ขายตามช่วงวันที่ เพื่อคำนวณวัตถุดิบที่ต้องตัดเบิก
> **Filter บังคับ**: `trans_flag = 44` (ประเภทรายการขาย) และ `last_status = 0` (รายการที่ยังใช้งานอยู่)

| Column | Type | หมายเหตุ |
|---|---|---|
| `doc_date` | SMALLDATETIME | วันที่เอกสาร — ใช้ filter ช่วงวันที่ |
| `doc_no` | VARCHAR(50) | PK (ร่วม) — เลขที่เอกสารขาย |
| `trans_flag` | SMALLINT | PK (ร่วม) — ประเภทรายการ, **filter = 44** (รายการขาย) |
| `last_status` | SMALLINT | สถานะรายการ, **filter = 0** (active) |
| `item_code` | VARCHAR(50) | รหัสสินค้าที่ขาย → FK `ic_inventory.code` |
| `qty` | NUMERIC | จำนวนที่ขาย (ในหน่วยที่ระบุ) |
| `unit_code` | VARCHAR(50) | หน่วยนับที่ใช้ขาย → FK `ic_unit.code` |
| `stand_value` | NUMERIC | ค่ามาตรฐาน สำหรับแปลงหน่วยกลับเป็นหน่วยหลัก |
| `divide_value` | NUMERIC | ค่าหาร สำหรับแปลงหน่วยกลับเป็นหน่วยหลัก |

**Primary Key**: `(doc_no, trans_flag)`

**Query ที่ใช้ดึงรายการขาย**:
```sql
SELECT
    doc_date,
    doc_no,
    item_code,
    qty,
    unit_code,
    stand_value,
    divide_value,
    -- แปลงเป็นหน่วยหลักเพื่อคำนวณ BOM
    (qty * stand_value / NULLIF(divide_value, 0)) AS qty_in_base_unit
FROM ic_trans_detail
WHERE trans_flag  = 44
  AND last_status = 0
  AND doc_date BETWEEN @dateFrom AND @dateTo
ORDER BY doc_date, doc_no
```

**Repository method ที่ต้องการ**:
```csharp
IErpSalesOrderRepository:
  Task<IEnumerable<ErpSalesTransactionDto>> GetSalesTransactionsByDateRangeAsync(
      DateTime dateFrom, DateTime dateTo);
```

```csharp
public record ErpSalesTransactionDto(
    DateTime DocDate,
    string DocNo,
    string ItemCode,
    string ItemName,
    decimal Qty,
    string UnitCode,
    decimal StandValue,
    decimal DivideValue
)
{
    // จำนวนในหน่วยหลัก สำหรับใช้คำนวณ BOM
    public decimal QtyInBaseUnit =>
        DivideValue == 0 ? Qty : Qty * StandValue / DivideValue;
}
```

---

## Mapping — ERP field → BOM Domain

| ERP Field | BOM Usage | หมายเหตุ |
|---|---|---|
| `ic_inventory.code` | `bom_lines.material_code` | FK reference (ไม่ใช่ constraint จริง) |
| `ic_inventory.code` | `bom_assignments.item_code` | Product item ที่ผูกกับ BOM |
| `ic_inventory.name_1` | `bom_lines.material_name` | Denormalized ตอน save |
| `ic_inventory.unit_cost` | `bom_lines.unit` | หน่วยหลัก — Denormalized ตอน save |
| `ic_unit.code` | `bom_lines.unit` | หน่วยที่เลือกใน BOM Editor (อาจไม่ใช่หน่วยหลัก) |
| `ic_unit_use.stand_value / divide_value` | ใช้ตอนคำนวณ | แปลงหน่วยก่อนคูณ BOM quantity |
| `ic_trans_detail.doc_no` | `production_orders.source_so_numbers[]` | อ้างอิงย้อนกลับ |
| `ic_trans_detail.item_code` | ใช้ lookup `bom_assignments` | หา BOM ที่ผูกกับสินค้าที่ขาย |
| `ic_trans_detail.qty * stand_value / divide_value` | `production_order_lines.required_quantity` | แปลงหน่วยก่อนคูณ BOM quantity |

## Table Relationships

```
ic_inventory (1) ──── (N) ic_unit_use ──── (N) ic_unit (1)
     │                      │                      │
  ic_code                  code               unit_code
                     (stand_value,                 │
                      divide_value,                │
                      ratio)                       │
                                                   │
ic_trans_detail ── item_code → ic_inventory        │
                └─ unit_code ───────────────────────┘
                   (trans_flag=44, last_status=0)
```
