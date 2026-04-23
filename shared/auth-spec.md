# Authentication Database Specification
> Reference tables จาก `authentication-database` — **อ่านอย่างเดียว** ห้าม write หรือ migrate
> อัปเดตเมื่อ Auth schema เปลี่ยน — ต้องผ่าน CTO approve

---

## Connection
- **Connection name**: `authentication-database`
- **เข้าถึงผ่าน**: Infrastructure layer เท่านั้น — ผ่าน `IAuthRepository`
- **ห้าม**: Domain / Application layer reference connection นี้โดยตรง

---

## Tables

### `sml_user_list` — ข้อมูลผู้ใช้งาน

> ใช้สำหรับ: ตรวจสอบ login — หน้าจอ Login (2.1)

| Column | Type | หมายเหตุ |
|---|---|---|
| `user_code` | VARCHAR(50) | PK — ใช้เป็น username ตอน login |
| `user_name` | VARCHAR(100) | ชื่อแสดงผลของ user |
| `user_password` | VARCHAR(25) | password |
| `user_level` | SMALLINT | ระดับสิทธิ์ |
| `active_status` | SMALLINT | 1 = ใช้งานได้, 0 = ไม่ active |
| `is_lock_record` | SMALLINT | 1 = ถูก lock ห้ามเข้าใช้, 0 = ปกติ |
| `device_id` | VARCHAR(200) | Device ที่ผูกไว้ (ถ้ามี) |
| `create_date_time_now` | TIMESTAMP | วันที่สร้าง record |
| `roworder` | INT | auto-increment sequence |
| `ignore_sync` | INT | flag สำหรับ ERP sync |

**Index**: `sml_user_list_pk_user_code` (user_code) — Primary Key

---

## Auth Logic

```sql
-- Query ที่ใช้ตอน Login
SELECT user_code, user_name, user_level
FROM sml_user_list
WHERE user_code     = @username
  AND user_password = @password
  AND active_status = 1
  AND is_lock_record = 0
```

| เงื่อนไข | ผลลัพธ์ |
|---|---|
| พบ record | Login สำเร็จ → ดึง `user_name`, `user_level` มาแสดงใน session |
| ไม่พบ record | แสดง error "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง" |
| `active_status = 0` | แสดง error "บัญชีนี้ถูกปิดการใช้งาน" |
| `is_lock_record = 1` | แสดง error "บัญชีถูกระงับการใช้งาน กรุณาติดต่อผู้ดูแลระบบ" |

---

## Repository Interface

```csharp
IAuthRepository:
  Task<AuthUserDto?> ValidateUserAsync(string userCode, string password);
```

```csharp
public record AuthUserDto(
    string UserCode,
    string UserName,
    short UserLevel
);
```
