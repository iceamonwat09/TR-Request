# Database Migration Scripts

## ModifyBy Feature

### วิธีใช้งาน

**ขั้นตอนที่ 1:** รัน SQL Script เพื่อเพิ่ม `ModifyBy` column

```bash
# เปิด SQL Server Management Studio (SSMS)
# เชื่อมต่อกับ HRDSYSTEM database
# รัน script: add_modifyby_column.sql
```

หรือใช้ `sqlcmd`:

```bash
sqlcmd -S <server-name> -d HRDSYSTEM -i add_modifyby_column.sql
```

**ขั้นตอนที่ 2:** ตรวจสอบว่า column ถูกสร้างแล้ว

```sql
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequest_Cost'
  AND COLUMN_NAME = 'ModifyBy'
```

### คุณสมบัติ

- **ModifyBy**: เก็บข้อมูลผู้แก้ไข วันที่ และเวลา
- **รูปแบบ**: `email / DD/MM/YYYY / HH:MM`
- **ตัวอย่าง**: `jney6767@gmail.com / 30/12/2025 / 14:35`

### การทำงาน

1. เมื่อมีการแก้ไขข้อมูลโควต้าในหน้า **QuotaManagement/Edit**
2. ระบบจะบันทึก email ของผู้แก้ไข วันที่ และเวลาลงใน `ModifyBy`
3. ข้อมูลจะแสดงในหน้า **QuotaManagement/Index** ในคอลัมน์ "แก้ไขโดย"

### ไฟล์ที่เกี่ยวข้อง

- **Model**: `Models/TrainingRequestCost.cs` - เพิ่ม `ModifyBy` property
- **Controller**: `Controllers/QuotaManagementController.cs` - UPDATE และ SELECT ModifyBy
- **View**: `Views/QuotaManagement/Index.cshtml` - แสดง ModifyBy column
- **SQL**: `Database/add_modifyby_column.sql` - Migration script
