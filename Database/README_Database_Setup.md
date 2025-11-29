# 📚 Database Setup Guide - HRDSYSTEM (Training Request System)

## 🎯 Overview
คู่มือนี้จะแนะนำวิธีการสร้างฐานข้อมูลสำหรับระบบ Training Request เพื่อนำขึ้น Production

Database Name: **HRDSYSTEM**

---

## 📋 Database Schema

### ตารางทั้งหมด (6 ตาราง)

1. **TrainingRequests** - ตารางหลักเก็บข้อมูลคำขออบรม
2. **TrainingRequestEmployees** - เก็บข้อมูลพนักงานที่เข้าร่วมอบรม
3. **TrainingRequestAttachments** - เก็บข้อมูลไฟล์แนบ
4. **TrainingRequest_Cost** - เก็บข้อมูลโควต้างบประมาณของแต่ละฝ่าย
5. **RetryEmailHistory** - เก็บ Log การ Retry Email
6. **EmailLogs** - เก็บ Log การส่ง Email

---

## 🚀 วิธีการติดตั้ง

### Option 1: ใช้ Master Script (แนะนำ)
รัน script ไฟล์เดียวที่สร้างทุกอย่างพร้อม:

```sql
-- Execute this file in SQL Server Management Studio (SSMS)
Database/99_MasterSetup_Production.sql
```

**ข้อดี:**
- ✅ รันครั้งเดียวได้ทุกอย่าง
- ✅ มีการตรวจสอบว่าตารางมีอยู่แล้วหรือไม่ (ป้องกัน duplicate)
- ✅ มีการ verify หลังสร้างเสร็จ
- ✅ แสดงผลลัพธ์ที่ชัดเจน

### Option 2: รันทีละไฟล์ (สำหรับการปรับแต่ง)
รัน scripts ตามลำดับ:

```sql
1. Database/00_CreateDatabase_Production.sql      -- สร้าง Database
2. Database/01_CreateTable_TrainingRequests.sql   -- ตารางหลัก
3. Database/02_CreateTable_TrainingRequestEmployees.sql
4. Database/03_CreateTable_TrainingRequestAttachments.sql
5. Database/04_CreateTable_TrainingRequest_Cost.sql
6. Database/05_CreateTable_RetryEmailHistory.sql
7. Database/06_CreateTable_EmailLogs.sql
```

**ข้อดี:**
- ✅ ควบคุมการสร้างแต่ละตารางได้
- ✅ Debug ง่ายถ้ามีปัญหา
- ✅ สามารถ skip ตารางที่มีอยู่แล้ว

---

## 🔍 คุณสมบัติของ Scripts

### ✨ Features

1. **Primary Keys & Identity**
   - ทุกตารางมี Primary Key กำกับ
   - ใช้ IDENTITY(1,1) สำหรับ auto-increment

2. **Foreign Keys**
   - TrainingRequestEmployees → TrainingRequests (CASCADE DELETE)
   - RetryEmailHistory → TrainingRequests (CASCADE DELETE)
   - EmailLogs → TrainingRequests (SET NULL)

3. **Indexes สำหรับ Performance**
   - DocNo, Status, Department
   - CreatedDate, StartDate (DESC)
   - Employee Code, Email

4. **Default Values**
   - CreatedDate: GETDATE()
   - IsActive: 1
   - Cost fields: 0
   - Status: 'DRAFT'

5. **Nullable Fields**
   - ทุก field เป็น NULL ได้ตาม schema ที่กำหนด
   - ยืดหยุ่นในการบันทึกข้อมูล

---

## 📊 ความสัมพันธ์ระหว่างตาราง

```
TrainingRequests (Main)
    ├── TrainingRequestEmployees (1:N)
    ├── RetryEmailHistory (1:N)
    └── EmailLogs (1:N)

TrainingRequestAttachments (ใช้ DocNo เชื่อมโยง)
TrainingRequest_Cost (Independent - Budget Master)
```

---

## 🔧 ขั้นตอนการ Deploy บน Production

### 1. Backup (ถ้ามี Database เดิม)
```sql
BACKUP DATABASE [HRDSYSTEM]
TO DISK = 'C:\Backup\HRDSYSTEM_Backup_BeforeDeploy.bak'
WITH FORMAT, COMPRESSION
```

### 2. รัน Setup Script
```sql
-- ใช้ SSMS เปิดไฟล์และรัน
Database/99_MasterSetup_Production.sql
```

### 3. Verify การติดตั้ง
```sql
-- Check tables
SELECT TABLE_NAME,
       (SELECT COUNT(*) FROM sys.indexes
        WHERE object_id = OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME)
        AND index_id > 0) as IndexCount
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = 'dbo'
ORDER BY TABLE_NAME

-- Check foreign keys
SELECT
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    tr.name AS ReferencedTable
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
ORDER BY tp.name
```

### 4. Update Connection String
อัพเดท `appsettings.json` ด้วย connection string สำหรับ Production:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_PROD_SERVER;Database=HRDSYSTEM;User Id=YOUR_USER;Password=YOUR_PASSWORD;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

---

## 📝 ตัวอย่างการใช้งาน

### Insert Sample Data
```sql
-- Insert Budget Quota
INSERT INTO TrainingRequest_Cost (Department, Year, Cost, Qhours, CreatedBy)
VALUES ('IT', '2025', 500000.00, 1000, 'admin@company.com')

-- Insert Training Request
INSERT INTO TrainingRequests (
    DocNo, Company, Department, SeminarTitle,
    StartDate, EndDate, Status, CreatedBy, CreatedDate
)
VALUES (
    'TR-2025-0001', 'ABC Company', 'IT', 'SQL Server Advanced Training',
    '2025-12-01', '2025-12-03', 'DRAFT', 'john@company.com', GETDATE()
)
```

### Query Examples
```sql
-- ดูคำขออบรมทั้งหมดที่รอการอนุมัติ
SELECT DocNo, SeminarTitle, Department, Status, CreatedDate
FROM TrainingRequests
WHERE Status IN ('PENDING_SECTION_MANAGER', 'PENDING_DEPARTMENT_MANAGER')
ORDER BY CreatedDate DESC

-- ดูงบประมาณคงเหลือของแต่ละฝ่าย
SELECT
    c.Department,
    c.Year,
    c.Cost AS TotalBudget,
    ISNULL(SUM(t.TotalCost), 0) AS UsedBudget,
    c.Cost - ISNULL(SUM(t.TotalCost), 0) AS RemainingBudget
FROM TrainingRequest_Cost c
LEFT JOIN TrainingRequests t ON c.Department = t.Department
    AND c.Year = YEAR(t.StartDate)
    AND t.Status NOT IN ('REJECTED', 'CANCELLED')
GROUP BY c.Department, c.Year, c.Cost
```

---

## ⚠️ ข้อควรระวัง

1. **Production Environment**
   - ❌ อย่า DROP TABLE บน production
   - ✅ Scripts มี comment DROP statements ไว้แล้ว (ปิดอยู่)

2. **Data Type**
   - เช็ค Decimal precision: `DECIMAL(12,2)` สำหรับ cost
   - DateTime ใช้ `DATETIME2(3)` สำหรับความแม่นยำมิลลิวินาที

3. **Character Encoding**
   - ใช้ `NVARCHAR` สำหรับ Unicode (รองรับภาษาไทย)

4. **Indexes**
   - Indexes ช่วยเพิ่มความเร็วในการ query
   - แต่อาจทำให้ INSERT/UPDATE ช้าลงเล็กน้อย

---

## 🆘 Troubleshooting

### ปัญหา: Foreign Key Error
```sql
-- ตรวจสอบว่าตาราง parent มีอยู่ก่อน
SELECT name FROM sys.tables WHERE name = 'TrainingRequests'
```

### ปัญหา: Permission Denied
```sql
-- ตรวจสอบ permission
SELECT HAS_PERMS_BY_NAME('HRDSYSTEM', 'DATABASE', 'CREATE TABLE')
```

### ปัญหา: Table Already Exists
```sql
-- Check existing tables
SELECT name FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo')

-- Drop specific table (ระวัง!)
-- DROP TABLE TrainingRequestEmployees
```

---

## 📞 Support

หากมีปัญหาในการติดตั้ง:
1. ตรวจสอบ error message ใน SSMS
2. Verify SQL Server version compatibility
3. Check user permissions

---

## 📅 Change Log

| วันที่ | เวอร์ชัน | การเปลี่ยนแปลง |
|--------|----------|----------------|
| 2025-11-29 | 1.0 | Initial Production Setup Scripts |

---

## ✅ Checklist ก่อน Deploy

- [ ] Backup database เดิม (ถ้ามี)
- [ ] Test script บน development environment
- [ ] Review connection string
- [ ] ตรวจสอบ SQL Server version
- [ ] Verify user permissions
- [ ] รัน script บน production
- [ ] Verify tables และ indexes
- [ ] Test basic CRUD operations
- [ ] Update application connection string
- [ ] Test application connectivity

---

**🎉 Good luck with your production deployment!**
