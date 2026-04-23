# ADR-001: ERP Connection Mode — Direct DB vs REST API

**วันที่**: Sprint 1  
**สถานะ**: PROPOSED — รอ CTO approve

## บริบท
Team C ต้องเลือก implementation mode สำหรับ ERP Adapter
ตัวเลือกมี 2 แบบที่ trade-off กันชัดเจน

## ตัวเลือกที่พิจารณา

### ตัวเลือก A: Direct DB (Dapper)
**ข้อดี**: เร็วกว่า, ไม่ต้องมี API endpoint, query ยืดหยุ่น  
**ข้อเสีย**: ต้องรู้ ERP schema, กระทบเมื่อ ERP upgrade, ต้องการ DB access

### ตัวเลือก B: REST API (HttpClient)
**ข้อดี**: ERP schema เปลี่ยนได้โดยไม่กระทบ, มี versioning, ปลอดภัยกว่า  
**ข้อเสีย**: ช้ากว่า, ต้องมี API endpoint จาก ERP vendor, ต้องจัดการ auth token

## การตัดสินใจ
[รอ CTO approve]

## ผลกระทบ
- Team A: [TBD]
- Team B: [TBD]
- Team C: implement ตาม mode ที่เลือก

## Risks และ Mitigation
- ถ้าเลือก Direct DB: สร้าง abstraction layer เพื่อรับ schema change
- ถ้าเลือก REST: ต้องมี ERP stub สำหรับ dev/test environment

---

# ADR Template

```markdown
# ADR-[N]: [ชื่อ Decision]

**วันที่**: [Sprint X / วันที่]  
**สถานะ**: PROPOSED | ACCEPTED | SUPERSEDED

## บริบท
[ปัญหาหรือ context ที่นำมาสู่การตัดสินใจนี้]

## ตัวเลือกที่พิจารณา
### ตัวเลือก A: [ชื่อ]
**ข้อดี**: ...  **ข้อเสีย**: ...

### ตัวเลือก B: [ชื่อ]
**ข้อดี**: ...  **ข้อเสีย**: ...

## การตัดสินใจ
[เลือกตัวเลือกใด และเพราะอะไร]

## ผลกระทบ
- Team A: [สิ่งที่ต้องทำ]
- Team B: [สิ่งที่ต้องทำ]
- Team C: [สิ่งที่ต้องทำ]

## Risks และ Mitigation
[ความเสี่ยงที่เหลือและวิธีลด]
```
