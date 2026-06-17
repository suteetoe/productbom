# Design Prompts — Smart BOM
> team-b-frontend ใช้ไฟล์นี้เป็น UI spec สำหรับ implement แต่ละ screen
> หลัง implement แต่ละ screen ให้บันทึกใน `shared/design-tokens.md`

---

## Global Design System (ใส่ใน System Prompt ทุกครั้ง)

```
Design system: Desktop ERP plugin built with Avalonia UI on Windows.
Style: Clean, professional, enterprise-grade. Light theme only.
Primary color: #3B82F6 (blue). Danger: #EF4444. Success: #22C55E. Warning: #F59E0B.
Background: #FFFFFF surface, #F8FAFC secondary. Border: #E2E8F0.
Text: #0F172A primary, #64748B secondary, #94A3B8 muted.
Font size: 14px base, 12px small, 16px heading.
Border radius: 6px cards, 4px inputs. Spacing unit: 8px.
Toolbar height: 44px. Sidebar width: 220px. DataGrid row height: 36px.
Language: Thai labels, English field names where appropriate.
```

---

## Screen 1 — Login (2.1)

```
Design a desktop login screen for a Thai ERP plugin called "Smart BOM".

Layout:
- Centered card on a light gray background (#F8FAFC)
- Card width: 400px, with subtle shadow and 8px border radius
- App logo/icon at the top of the card
- App title: "Smart BOM" in Thai below the logo

Form fields (top to bottom inside card):
1. Label "ชื่อผู้ใช้" with text input (placeholder: "กรอกชื่อผู้ใช้")
2. Label "รหัสผ่าน" with password input (placeholder: "กรอกรหัสผ่าน") — show/hide toggle icon on right
3. "Remember me" checkbox with label "จดจำการเข้าสู่ระบบ"
4. Primary button "เข้าสู่ระบบ" full width, blue (#3B82F6)

Error states (show below respective field in red #EF4444, small text):
- Empty username: "กรุณากรอกชื่อผู้ใช้"
- Empty password: "กรุณากรอกรหัสผ่าน"
- Login failed banner (red, full width inside card): "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง"
- Account locked banner: "บัญชีถูกระงับการใช้งาน กรุณาติดต่อผู้ดูแลระบบ"

Loading state: button shows spinner while authenticating.
Version number in small muted text at bottom of card.
```

---

## Screen 2a — BOM List (2.2a)

```
Design a desktop BOM List screen for a Thai ERP plugin. Full window layout with left sidebar navigation.

Left sidebar (220px wide, dark or light):
- Navigation items: ล็อกอิน, สูตรการผลิต (active/highlighted), กำหนดสูตร, รายการผลิต, คำนวณการผลิต
- App title at top of sidebar
- User info + logout button at bottom

Main content area:
Top toolbar (44px):
- Page title: "สูตรการผลิต" (h2)
- Right side: search input (placeholder "ค้นหา รหัส / ชื่อสูตร") + blue button "สร้างสูตรใหม่"

DataGrid below toolbar showing BOM records with columns:
- รหัสสูตร (BOM Code) — bold, monospace
- ชื่อสูตร (Name)
- สินค้าที่ผลิต (Item) — item code + name
- เวอร์ชัน (Version) — number badge
- สถานะ (Status) — colored badge: Active=green, Draft=gray, Inactive=red
- แก้ไขล่าสุด (Updated At) — date format DD/MM/YYYY

Action column on far right per row (icon buttons): Edit (pencil), Activate/Deactivate (toggle), Delete (trash, red on hover).

Empty state illustration when no records: icon + text "ยังไม่มีสูตรการผลิต กดปุ่ม 'สร้างสูตรใหม่' เพื่อเริ่มต้น"

Show 3 sample rows with realistic Thai data.
```

---

## Screen 2b — BOM Editor (2.2b)

```
Design a desktop BOM Editor screen (full screen or large modal/dialog) for a Thai ERP plugin.

Layout: Full page editor with two sections — Header Form (top) and BOM Lines (bottom).

HEADER FORM SECTION (card with padding):
Title: "สร้างสูตรการผลิต" or "แก้ไขสูตรการผลิต"
Grid layout (2 columns):
Row 1:
  - รหัสสูตร* (required): text input, placeholder "BOM-001"
  - ชื่อสูตร* (required): text input
Row 2:
  - สินค้าที่ผลิต* (required): searchable lookup field showing "ItemCode — ชื่อสินค้า" with search icon
  - คำอธิบาย: text input (optional)
Row 3:
  - จำนวนที่ผลิตได้ต่อรอบ* (required): numeric input, > 0
  - หน่วยนับ* (required): dropdown (populated based on selected item)
  - เวอร์ชัน: read-only badge "v1"

BOM LINES SECTION (card below header):
Section title: "รายการวัตถุดิบ" with "+ เพิ่มวัตถุดิบ" button (blue, top right of section)

Inline-editable DataGrid with columns:
- # (row number)
- รหัสวัตถุดิบ* — searchable lookup input, shows code + name from ERP
- ชื่อวัตถุดิบ — auto-filled after lookup (read-only)
- จำนวน* — numeric input (> 0)
- หน่วย* — dropdown
- Sub-BOM — optional lookup (for sub-assembly), icon to indicate if set
- ลบ — red trash icon button

Empty state for BOM lines: "กดปุ่ม '+ เพิ่มวัตถุดิบ' เพื่อเพิ่มรายการ"

Validation error: red border + message under field.

Footer action bar (sticky at bottom):
- Left: gray "ยกเลิก" button
- Right: blue "บันทึก" button

Show 3 sample BOM line rows with realistic data.
```

---

## Screen 3 — BOM Assignment (2.3)

```
Design a desktop BOM Assignment screen for a Thai ERP plugin. Split-panel layout.

Left sidebar navigation same as Screen 2a. "กำหนดสูตร" menu item is active.

Main content: Horizontal split — Left panel (50%) and Right panel (50%)

LEFT PANEL — "รายการสินค้า ERP":
- Panel title + search input "ค้นหาสินค้า"
- List/DataGrid of ERP items with columns:
  - รหัสสินค้า (Item Code)
  - ชื่อสินค้า (Item Name)
  - สถานะ BOM: icon indicator — green checkmark = assigned, gray dash = unassigned
- Highlighted/selected row in blue
- Pagination or virtual scroll for long lists

RIGHT PANEL — "สูตรที่กำหนดให้สินค้านี้":
- Panel title showing selected item: "สินค้า: [ItemCode] — [ItemName]"
- If assigned: card showing BOM details (Code, Name, Version, Status badge, Yield quantity + unit)
  - Action button: red "ยกเลิกการกำหนด" (remove assignment)
  - Action button: blue "เปลี่ยนสูตร" (reassign)
- If not assigned: empty state illustration + dropdown "เลือกสูตรการผลิต" (shows Active BOMs only) + blue "กำหนดสูตร" button
- Warning message if trying to assign non-Active BOM

Show the left panel with 5 sample items (mix of assigned and unassigned).
```

---

## Screen 4 — Production List (2.4)

```
Design a desktop Production List screen for a Thai ERP plugin showing production order history.

Left sidebar navigation same as Screen 2a. "รายการผลิต" menu item is active.

Main content:
TOP FILTER BAR (card/panel):
Row 1 filters side by side:
  - วันที่สร้าง: date range picker (from — to)
  - สถานะ: dropdown multi-select (Pending/Processing/Done/Cancelled)
  - สินค้า: text search input
  - สร้างโดย: radio or segmented control "ทั้งหมด / UI / CLI"
  - ค้นหาเลขเอกสารขาย: text input (search inside source doc numbers)
  - "ค้นหา" blue button

MAIN DATAGRID:
Columns:
- เลขที่ใบผลิต (OrderNo) — monospace font, link style
- รหัสสินค้า (ProductCode)
- รหัสสูตร (BomCode)
- จำนวน (Qty) — right-aligned number
- สถานะ (Status) — badge: Pending=yellow, Processing=blue, Done=green, Cancelled=gray
- จำนวนเอกสารขาย (SourceDocCount) — number pill, gray
- ช่วงวันที่ขาย (SourceDateRange) — small text "DD/MM — DD/MM"
- วันที่สร้าง (CreatedAt)
- สร้างโดย (CreatedBy) — username text
- ช่องทาง (Via) — small badge: UI=blue, CLI=gray

EXPANDABLE ROW DETAIL (when row is clicked/expanded):
Two-column layout inside expanded area:
  Left: "เอกสารขายที่นำมาคำนวณ" — small table (DocNo, DocDate, ItemCount)
  Right: "วัตถุดิบที่ต้องเบิก" — small table (MaterialCode, MaterialName, RequiredQty, Unit)

ROW ACTION BUTTONS (far right, per row):
  - View icon (always shown)
  - Cancel button (red, only if Pending)
  - CSV export icon (always shown)

PAGINATION: "แสดง 50 รายการต่อหน้า" pagination control at bottom.

Show 4 sample rows with 1 row expanded, mix of statuses and CLI/UI badges.
```

---

## Screen 5 — Sales Calculation (2.5)

```
Design a desktop Sales Calculation screen for a Thai ERP plugin. This is the main calculation workflow screen.

Left sidebar navigation same as Screen 2a. "คำนวณการผลิต" menu item is active.

Main content divided into 3 vertical sections:

SECTION 1 — FILTER BAR (top card):
Horizontal layout:
  - Label "วันที่เริ่มต้น" + date picker
  - Label "ถึงวันที่" + date picker  
  - Checkbox "แสดงเฉพาะสินค้าที่มี BOM"
  - Blue button "โหลดรายการขาย" (Load)
  - Warning pill (orange): "X รายการไม่มีสูตรการผลิต" (shown after load)

SECTION 2 — SALES TRANSACTIONS TABLE (middle, ~50% height):
Title: "รายการขาย"
DataGrid columns:
  - เลขที่เอกสาร (DocNo)
  - วันที่ (DocDate) — DD/MM/YYYY
  - รหัสสินค้า (ItemCode)
  - ชื่อสินค้า (ItemName)
  - จำนวน (Qty) — right-aligned
  - หน่วย (Unit)
  - มี BOM — icon column: green checkmark = has active BOM, orange warning = no BOM

Show 5 sample rows (4 with BOM, 1 without BOM shown as warning row in light orange tint).

SECTION 3 — RESULT + ACTIONS (bottom card):
Left half: "วัตถุดิบที่ต้องตัดเบิก" result DataGrid (MaterialCode, MaterialName, TotalQty, Unit) — initially empty/greyed out before calculation.
Right half:
  - "รูปแบบบันทึกเอกสาร" radio group:
    ○ รวมเป็น 1 เอกสารต่อ 1 วัน (selected by default)
    ○ แยกตามเลขที่เอกสารขาย
  - Action buttons stacked:
    - Blue "คำนวณ" button (trigger calculation)
    - Green "บันทึกเอกสาร" button (enabled after calculation)
    - Outlined "Export CSV" button

Show the result section in a "after calculation" state with 3 sample material rows.
```

---

## Screen 6 — Product Destruction (2.6)

```
Design a desktop Product Destruction screen for a Thai ERP plugin used to record damaged or destroyed stock.

Left sidebar navigation same as Screen 2a. "เบิกของเสีย" menu item is active.

Main content uses a two-panel work layout:

LEFT DOCUMENT LIST:
  - Compact filter row: date range, document number search, Search button
  - Document list with rows showing DocNo, DocDate, item count, picture count
  - Row actions: select/view and delete
  - Pagination at the bottom

RIGHT DETAIL/EDITOR PANEL:
The detail panel is always visible, even when no document is selected.

Header form:
  - DocNo text input
  - DocDate date picker
  - WhCode text input
  - ShelfCode text input
  - Remark text input
  - Actions: New, Save

Pictures section:
  - Thumbnail grid of attached images
  - Add picture button
  - Remove picture icon per image

Item lines section:
  - Editable item table/list with ItemCode, ItemName, Qty, UnitCode, WhCode, ShelfCode
  - Add item button
  - Remove item icon per row
  - Qty is right-aligned and must visually reject zero or negative values

Show an example selected document with 2 pictures and 3 item lines.
Use a practical ERP operations style: dense, readable, and calm; avoid marketing-style hero layout.
```

---

## ลำดับการ Implement แนะนำ (สำหรับ team-b)

| ลำดับ | Screen | เหตุผล |
|---|---|---|
| 1 | Screen 1 — Login | ง่าย, สร้าง color/font baseline |
| 2 | Screen 2a — BOM List | สร้าง Sidebar + DataGrid pattern ใช้ซ้ำ |
| 3 | Screen 4 — Production List | DataGrid ซับซ้อน + expandable row |
| 4 | Screen 5 — Sales Calculation | หน้าจอหลัก, 3 sections |
| 5 | Screen 2b — BOM Editor | Form + inline DataGrid |
| 6 | Screen 3 — BOM Assignment | Split panel |
| 7 | Screen 6 — Product Destruction | เอกสารทำลายสินค้า + รูปภาพ + editable item lines |

> หลัง implement Screen 2a ให้อัปเดต `shared/design-tokens.md` ก่อน implement screen ถัดไป
