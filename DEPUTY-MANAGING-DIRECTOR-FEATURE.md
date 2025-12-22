# การเพิ่มฟีเจอร์ Deputy Managing Director (รองกรรมการผู้จัดการ)

## สารบัญ
1. [ภาพรวมของฟีเจอร์](#ภาพรวมของฟีเจอร์)
2. [โครงสร้างการอนุมัติ 6 ระดับ](#โครงสร้างการอนุมัติ-6-ระดับ)
3. [การเปลี่ยนแปลงฐานข้อมูล](#การเปลี่ยนแปลงฐานข้อมูล)
4. [โมเดลและคุณสมบัติ](#โมเดลและคุณสมบัติ)
5. [บริการและ Logic การทำงาน](#บริการและ-logic-การทำงาน)
6. [Controllers และการประมวลผล](#controllers-และการประมวลผล)
7. [Views และส่วนติดต่อผู้ใช้](#views-และส่วนติดต่อผู้ใช้)
8. [ฟีเจอร์การข้ามขั้นตอน (Skip)](#ฟีเจอร์การข้ามขั้นตอน-skip)
9. [ระบบแจ้งเตือนทางอีเมล](#ระบบแจ้งเตือนทางอีเมล)
10. [การแก้ไขปัญหาที่พบ](#การแก้ไขปัญหาที่พบ)
11. [คู่มือการทดสอบ](#คู่มือการทดสอบ)

---

## ภาพรวมของฟีเจอร์

### วัตถุประสงค์
เพิ่ม **Deputy Managing Director (รองกรรมการผู้จัดการ)** เป็นผู้อนุมัติระดับที่ 6 และเป็นขั้นตอน**สุดท้าย**ในกระบวนการอนุมัติคำขออบรม

### ความสำคัญ
- Deputy Managing Director เป็นผู้อนุมัติขั้นสุดท้าย เมื่ออนุมัติแล้ว สถานะจะเปลี่ยนเป็น **"APPROVED"** (อนุมัติเรียบร้อย)
- รองรับการข้ามขั้นตอน (Skip) โดยใช้ข้อความ **"ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"**
- Human Resources Department (HRD) Admin และ HRD Confirmation ยังคงเป็น**ผู้อนุมัติที่จำเป็น** (ไม่สามารถข้ามได้)

### ผลกระทบต่อระบบ
- **ฐานข้อมูล**: เพิ่ม 4 คอลัมน์ใหม่สำหรับ Deputy Managing Director
- **Models**: อัพเดต ViewModels ให้รองรับข้อมูลใหม่
- **Services**: แก้ไข Workflow Logic ให้ Deputy Managing Director เป็นขั้นตอนสุดท้าย
- **Controllers**: อัพเดตการ Insert, Update, และ Approval Logic
- **Views**: เพิ่มส่วนแสดงผลและฟอร์มสำหรับ Deputy Managing Director
- **Email**: อัพเดตเทมเพลตอีเมลให้แสดงครบ 6 ระดับ

---

## โครงสร้างการอนุมัติ 6 ระดับ

### ลำดับการอนุมัติ (Approval Flow)

```
1. Section Manager (หัวหน้าแผนก)
   ↓
2. Department Manager (ผู้จัดการฝ่าย)
   ↓
3. HRD Admin (ผู้ดูแลระบบ HRD) ⚠️ จำเป็น ไม่สามารถข้ามได้
   ↓
4. HRD Confirmation (ผู้ยืนยันจาก HRD) ⚠️ จำเป็น ไม่สามารถข้ามได้
   ↓
5. Managing Director (กรรมการผู้จัดการ)
   ↓
6. Deputy Managing Director (รองกรรมการผู้จัดการ) 🆕 ขั้นตอนสุดท้าย
   ↓
   ✅ APPROVED (อนุมัติเรียบร้อย)
```

### สถานะของระบบ (System Status)

| ลำดับที่ | ผู้อนุมัติ | สถานะระบบ | สามารถข้ามได้ |
|---------|----------|-----------|--------------|
| 1 | Section Manager | `WAITING_FOR_SECTION_MANAGER` | ✅ ได้ |
| 2 | Department Manager | `WAITING_FOR_DEPARTMENT_MANAGER` | ✅ ได้ |
| 3 | HRD Admin | `WAITING_FOR_HRD_ADMIN` | ❌ ไม่ได้ |
| 4 | HRD Confirmation | `WAITING_FOR_HRD_CONFIRMATION` | ❌ ไม่ได้ |
| 5 | Managing Director | `WAITING_FOR_MANAGING_DIRECTOR` | ✅ ได้ |
| 6 | Deputy Managing Director | `WAITING_FOR_DEPUTY_MANAGING_DIRECTOR` | ✅ ได้ |
| - | สุดท้าย | `APPROVED` | - |

---

## การเปลี่ยนแปลงฐานข้อมูล

### สคริปต์การเพิ่มคอลัมน์ใหม่

**ไฟล์**: `Database/AddDeputyManagingDirector.sql`

```sql
ALTER TABLE [dbo].[TrainingRequests]
ADD
    -- ผู้อนุมัติ: รองกรรมการผู้จัดการ
    DeputyManagingDirectorId NVARCHAR(100) NULL,

    -- สถานะการอนุมัติ: Pending, Approved, Rejected, Revise
    Status_DeputyManagingDirector NVARCHAR(20) NULL,

    -- ความคิดเห็นของผู้อนุมัติ
    Comment_DeputyManagingDirector NVARCHAR(500) NULL,

    -- ข้อมูลการอนุมัติ: ผู้อนุมัติ / วันที่ / เวลา
    ApproveInfo_DeputyManagingDirector NVARCHAR(200) NULL;
```

### คำอธิบายคอลัมน์

| คอลัมน์ | ชนิดข้อมูล | ความหมาย | ตัวอย่างข้อมูล |
|---------|-----------|----------|---------------|
| `DeputyManagingDirectorId` | NVARCHAR(100) | อีเมลของผู้อนุมัติ หรือค่า "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ" | `deputy@company.com` หรือ `ผู้บังคับบัญชาลำดับถัดไป อนุมัติ` |
| `Status_DeputyManagingDirector` | NVARCHAR(20) | สถานะการอนุมัติ | `Pending`, `Approved`, `Rejected`, `Revise` |
| `Comment_DeputyManagingDirector` | NVARCHAR(500) | ความคิดเห็นที่ให้ไว้เมื่ออนุมัติ | `ได้รับการอนุมัติแล้ว` |
| `ApproveInfo_DeputyManagingDirector` | NVARCHAR(200) | ข้อมูลการอนุมัติ | `deputy@company.com / 20/12/2025 / 09:43` |

### สคริปต์อัพเดตข้อมูลเดิม

**ไฟล์**: `Database/UpdateExistingRecords_DeputyMD.sql`

```sql
-- อัพเดตข้อมูลที่มีอยู่แล้วให้มีสถานะ Pending
UPDATE [dbo].[TrainingRequests]
SET
    Status_SectionManager = ISNULL(Status_SectionManager, 'Pending'),
    Status_DepartmentManager = ISNULL(Status_DepartmentManager, 'Pending'),
    Status_HRDAdmin = ISNULL(Status_HRDAdmin, 'Pending'),
    Status_HRDConfirmation = ISNULL(Status_HRDConfirmation, 'Pending'),
    Status_ManagingDirector = ISNULL(Status_ManagingDirector, 'Pending'),
    Status_DeputyManagingDirector = ISNULL(Status_DeputyManagingDirector, 'Pending')
WHERE IsActive = 1;
```

### ความเข้ากันได้แบบย้อนหลัง (Backward Compatibility)

**ปัญหา**: Records เก่าที่สร้างก่อนมีฟีเจอร์ Deputy Managing Director จะมี `DeputyManagingDirectorId = NULL`

**วิธีแก้ไข**: Logic ในระบบถือว่า `NULL` หรือ `Empty String` = **Skip (ข้ามขั้นตอนนี้)**

```csharp
private bool IsSkipApprover(string approverId)
{
    // NULL หรือ empty string = SKIP (เพื่อ backward compatibility)
    if (string.IsNullOrWhiteSpace(approverId))
        return true;

    // เช็คว่าเป็นค่า SKIP_APPROVER หรือไม่
    return string.Equals(approverId.Trim(), SKIP_APPROVER, StringComparison.OrdinalIgnoreCase);
}
```

---

## โมเดลและคุณสมบัติ

### TrainingRequestEditViewModel

**ไฟล์**: `Models/TrainingRequestEditViewModel.cs`

เพิ่มคุณสมบัติ 4 ตัวสำหรับ Deputy Managing Director:

```csharp
public class TrainingRequestEditViewModel
{
    // ... คุณสมบัติอื่นๆ ...

    // ========================================
    // Level 6: Deputy Managing Director (รองกรรมการผู้จัดการ)
    // ========================================

    /// <summary>
    /// อีเมลของ Deputy Managing Director หรือค่า "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"
    /// </summary>
    public string? DeputyManagingDirectorId { get; set; }

    /// <summary>
    /// สถานะการอนุมัติของ Deputy Managing Director
    /// ค่าที่เป็นไปได้: Pending, Approved, Rejected, Revise
    /// </summary>
    public string? Status_DeputyManagingDirector { get; set; }

    /// <summary>
    /// ความคิดเห็นของ Deputy Managing Director เมื่ออนุมัติหรือปฏิเสธ
    /// </summary>
    public string? Comment_DeputyManagingDirector { get; set; }

    /// <summary>
    /// ข้อมูลการอนุมัติ: ผู้อนุมัติ / วันที่ / เวลา
    /// รูปแบบ: "deputy@company.com / 20/12/2025 / 09:43"
    /// </summary>
    public string? ApproveInfo_DeputyManagingDirector { get; set; }
}
```

### คำอธิบายคุณสมบัติ

1. **DeputyManagingDirectorId**:
   - เก็บอีเมลของผู้อนุมัติ เช่น `deputy@company.com`
   - หรือค่า `"ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"` ถ้าต้องการข้ามขั้นตอนนี้
   - หรือ `NULL` สำหรับ records เก่า (ถือว่า Skip)

2. **Status_DeputyManagingDirector**:
   - `"Pending"`: รอการอนุมัติ
   - `"Approved"`: อนุมัติแล้ว
   - `"Rejected"`: ปฏิเสธ
   - `"Revise"`: ส่งกลับให้แก้ไข

3. **Comment_DeputyManagingDirector**:
   - ความคิดเห็นที่ผู้อนุมัติให้ไว้
   - สามารถเป็น `NULL` ได้

4. **ApproveInfo_DeputyManagingDirector**:
   - เก็บข้อมูล: `"อีเมล / วันที่ / เวลา"`
   - สร้างอัตโนมัติเมื่อมีการอนุมัติหรือปฏิเสธ

---

## บริการและ Logic การทำงาน

### ApprovalWorkflowService.cs

#### 1. ค่าคงที่สำหรับการข้ามขั้นตอน

```csharp
// ค่าคงที่สำหรับการข้ามผู้อนุมัติ
private const string SKIP_APPROVER = "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ";
```

#### 2. ฟังก์ชันตรวจสอบการข้ามขั้นตอน

```csharp
/// <summary>
/// ตรวจสอบว่าผู้อนุมัติคนนี้ถูกตั้งค่าให้ข้ามหรือไม่
/// </summary>
/// <param name="approverId">อีเมลของผู้อนุมัติ</param>
/// <returns>true ถ้าเป็นการข้ามขั้นตอน, false ถ้าไม่ใช่</returns>
private bool IsSkipApprover(string approverId)
{
    // กรณีที่ 1: NULL หรือ empty string = SKIP
    // (สำหรับ backward compatibility กับ records เก่า)
    if (string.IsNullOrWhiteSpace(approverId))
        return true;

    // กรณีที่ 2: เช็คว่าเป็นค่า SKIP_APPROVER หรือไม่
    // ใช้ StringComparison.OrdinalIgnoreCase เพื่อไม่สนใจตัวพิมพ์เล็ก-ใหญ่
    return string.Equals(approverId.Trim(), SKIP_APPROVER, StringComparison.OrdinalIgnoreCase);
}
```

**Logic การทำงาน**:
1. ถ้า `approverId` เป็น `NULL`, `""`, หรือ `"   "` (ช่องว่าง) → คืนค่า `true` (ข้าม)
2. ถ้า `approverId` เท่ากับ `"ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"` → คืนค่า `true` (ข้าม)
3. ถ้าไม่ตรงเงื่อนไขข้างต้น → คืนค่า `false` (ไม่ข้าม)

#### 3. ฟังก์ชันหาสถานะถัดไป (พร้อมการข้ามขั้นตอน)

```csharp
/// <summary>
/// หาสถานะถัดไปของ Workflow โดยคำนึงถึงการข้ามผู้อนุมัติ
/// </summary>
/// <param name="request">ข้อมูลคำขออบรม</param>
/// <param name="currentStatus">สถานะปัจจุบัน</param>
/// <returns>สถานะถัดไป</returns>
public string GetNextApprovalStatusWithSkip(TrainingRequestEditViewModel request, string currentStatus)
{
    switch (currentStatus)
    {
        // ระดับ 1: Section Manager
        case "WAITING_FOR_SECTION_MANAGER":
            // ถ้า Department Manager ไม่ได้ถูก Skip → ไปที่ Department Manager
            if (!IsSkipApprover(request.DepartmentManagerId))
                return "WAITING_FOR_DEPARTMENT_MANAGER";
            // ถ้า Skip → ไปที่ HRD Admin (ข้าม Department Manager)
            // HRD Admin ไม่สามารถ Skip ได้ จึงไม่ต้องเช็ค
            return "WAITING_FOR_HRD_ADMIN";

        // ระดับ 2: Department Manager
        case "WAITING_FOR_DEPARTMENT_MANAGER":
            // HRD Admin เป็นผู้อนุมัติที่จำเป็น ไม่สามารถ Skip ได้
            return "WAITING_FOR_HRD_ADMIN";

        // ระดับ 3: HRD Admin
        case "WAITING_FOR_HRD_ADMIN":
            // HRD Confirmation เป็นผู้อนุมัติที่จำเป็น ไม่สามารถ Skip ได้
            return "WAITING_FOR_HRD_CONFIRMATION";

        // ระดับ 4: HRD Confirmation
        case "WAITING_FOR_HRD_CONFIRMATION":
            // ถ้า Managing Director ไม่ได้ถูก Skip → ไปที่ Managing Director
            if (!IsSkipApprover(request.ManagingDirectorId))
                return "WAITING_FOR_MANAGING_DIRECTOR";
            // ถ้า MD ถูก Skip แต่ Deputy MD ไม่ถูก Skip → ไปที่ Deputy MD
            if (!IsSkipApprover(request.DeputyManagingDirectorId))
                return "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR";
            // ถ้าทั้ง MD และ Deputy MD ถูก Skip → อนุมัติเลย
            return "APPROVED";

        // ระดับ 5: Managing Director
        case "WAITING_FOR_MANAGING_DIRECTOR":
            // ถ้า Deputy MD ไม่ได้ถูก Skip → ไปที่ Deputy MD
            if (!IsSkipApprover(request.DeputyManagingDirectorId))
                return "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR";
            // ถ้า Deputy MD ถูก Skip → อนุมัติเลย (Deputy MD คือขั้นตอนสุดท้าย)
            return "APPROVED";

        // ระดับ 6: Deputy Managing Director (ขั้นตอนสุดท้าย)
        case "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR":
            // Deputy Managing Director เป็นผู้อนุมัติคนสุดท้าย
            // เมื่ออนุมัติแล้ว → สถานะเป็น APPROVED
            return "APPROVED";

        default:
            // ถ้าสถานะไม่ตรงกับที่กำหนด → คงสถานะเดิม
            return currentStatus;
    }
}
```

**ตัวอย่างการทำงาน**:

**กรณีที่ 1**: ไม่มีการ Skip
```
Pending
→ WAITING_FOR_SECTION_MANAGER
→ WAITING_FOR_DEPARTMENT_MANAGER
→ WAITING_FOR_HRD_ADMIN
→ WAITING_FOR_HRD_CONFIRMATION
→ WAITING_FOR_MANAGING_DIRECTOR
→ WAITING_FOR_DEPUTY_MANAGING_DIRECTOR
→ APPROVED ✅
```

**กรณีที่ 2**: Skip Section Manager และ Department Manager
```
Pending
→ WAITING_FOR_SECTION_MANAGER (Skip → ไปต่อ)
→ WAITING_FOR_HRD_ADMIN
→ WAITING_FOR_HRD_CONFIRMATION
→ WAITING_FOR_MANAGING_DIRECTOR
→ WAITING_FOR_DEPUTY_MANAGING_DIRECTOR
→ APPROVED ✅
```

**กรณีที่ 3**: Skip Managing Director และ Deputy Managing Director
```
Pending
→ WAITING_FOR_SECTION_MANAGER
→ WAITING_FOR_DEPARTMENT_MANAGER
→ WAITING_FOR_HRD_ADMIN
→ WAITING_FOR_HRD_CONFIRMATION
→ WAITING_FOR_MANAGING_DIRECTOR (Skip)
→ WAITING_FOR_DEPUTY_MANAGING_DIRECTOR (Skip)
→ APPROVED ✅
```

#### 4. ฟังก์ชันหาอีเมลผู้อนุมัติคนถัดไป

```csharp
/// <summary>
/// หาอีเมลของผู้อนุมัติคนถัดไป
/// </summary>
/// <param name="nextStatus">สถานะถัดไป</param>
/// <param name="request">ข้อมูลคำขออบรม</param>
/// <returns>อีเมลของผู้อนุมัติ หรือ NULL ถ้าเป็นการ Skip</returns>
public string GetNextApproverEmail(string nextStatus, TrainingRequestEditViewModel request)
{
    string email = nextStatus switch
    {
        "WAITING_FOR_SECTION_MANAGER" => request.SectionManagerId,
        "WAITING_FOR_DEPARTMENT_MANAGER" => request.DepartmentManagerId,
        "WAITING_FOR_HRD_ADMIN" => request.HRDAdminId,
        "WAITING_FOR_HRD_CONFIRMATION" => request.HRDConfirmationId,
        "WAITING_FOR_MANAGING_DIRECTOR" => request.ManagingDirectorId,
        "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR" => request.DeputyManagingDirectorId,
        _ => null
    };

    // ถ้าเป็น Skip Approver → คืนค่า NULL (ไม่ส่งอีเมล)
    if (IsSkipApprover(email))
        return null;

    // คืนค่าอีเมล (ตัดช่องว่างออก)
    return email?.Trim();
}
```

**Logic การทำงาน**:
1. หาอีเมลตามสถานะถัดไป
2. ถ้าเป็น Skip Approver → คืนค่า `NULL` (ไม่ส่งอีเมลแจ้งเตือน)
3. ถ้าไม่ใช่ Skip → คืนค่าอีเมล

#### 5. ฟังก์ชันหาชื่อผู้อนุมัติคนถัดไป

```csharp
/// <summary>
/// หาชื่อผู้อนุมัติคนถัดไป (สำหรับแสดงในอีเมล)
/// </summary>
/// <param name="nextStatus">สถานะถัดไป</param>
/// <returns>ชื่อตำแหน่งของผู้อนุมัติ</returns>
public string GetNextApproverName(string nextStatus)
{
    return nextStatus switch
    {
        "WAITING_FOR_SECTION_MANAGER" => "Section Manager",
        "WAITING_FOR_DEPARTMENT_MANAGER" => "Department Manager",
        "WAITING_FOR_HRD_ADMIN" => "HRD Admin",
        "WAITING_FOR_HRD_CONFIRMATION" => "HRD Confirmation",
        "WAITING_FOR_MANAGING_DIRECTOR" => "Managing Director",
        "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR" => "Deputy Managing Director",
        "APPROVED" => "Approved",
        _ => "Unknown"
    };
}
```

#### 6. ฟังก์ชันประมวลผลการ Revise

```csharp
/// <summary>
/// ประมวลผลเมื่อผู้อนุมัติส่งกลับให้แก้ไข (Revise)
/// </summary>
/// <param name="currentStatus">สถานะปัจจุบัน</param>
/// <returns>สถานะใหม่หลังจาก Revise</returns>
public string ProcessRevise(string currentStatus)
{
    // ถ้า Deputy Managing Director หรือ Managing Director ส่ง Revise
    // → กลับไปที่ "Revision Admin" เพื่อให้ผู้ยื่นคำขอแก้ไข
    if (currentStatus == "WAITING_FOR_MANAGING_DIRECTOR" ||
        currentStatus == "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR")
    {
        return "Revision Admin";
    }

    // กรณีอื่นๆ → ส่งกลับไปให้ HRD Admin แก้ไข
    return "WAITING_FOR_HRD_ADMIN";
}
```

**Logic การทำงาน**:
- ถ้า Managing Director หรือ Deputy Managing Director ส่ง Revise → สถานะเป็น `"Revision Admin"` (ให้ผู้ยื่นคำขอแก้ไข)
- กรณีอื่นๆ → กลับไปที่ `"WAITING_FOR_HRD_ADMIN"`

---

## Controllers และการประมวลผล

### TrainingRequestController.cs

#### 1. การสร้างคำขออบรมใหม่ (Create)

**เมธอด**: `InsertTrainingRequest()`

**การเพิ่มข้อมูล Deputy Managing Director**:

```csharp
string query = @"
    INSERT INTO [HRDSYSTEM].[dbo].[TrainingRequests] (
        DocNo, Company, Division, Department, Section, Position,
        EmployeeName, EmployeeID, Email, SeminarTitle, StartDate, EndDate,
        SeminarLocation, TotalPeople, CourseFee, AccommodationFee, TravelFee,
        OtherExpenses, TotalExpenses, CCEmail, TrainingObjective, ExpectedResult,
        Status, CreatedDate, CreatedBy, IsActive, TotalPeople,

        -- ผู้อนุมัติระดับที่ 1: Section Manager
        SectionManagerId, Status_SectionManager,

        -- ผู้อนุมัติระดับที่ 2: Department Manager
        DepartmentManagerId, Status_DepartmentManager,

        -- ผู้อนุมัติระดับที่ 3: HRD Admin
        HRDAdminId, Status_HRDAdmin,

        -- ผู้อนุมัติระดับที่ 4: HRD Confirmation
        HRDConfirmationId, Status_HRDConfirmation,

        -- ผู้อนุมัติระดับที่ 5: Managing Director
        ManagingDirectorId, Status_ManagingDirector,

        -- 🆕 ผู้อนุมัติระดับที่ 6: Deputy Managing Director
        DeputyManagingDirectorId, Status_DeputyManagingDirector
    )
    VALUES (
        @DocNo, @Company, @Division, @Department, @Section, @Position,
        @EmployeeName, @EmployeeID, @Email, @SeminarTitle, @StartDate, @EndDate,
        @SeminarLocation, @TotalPeople, @CourseFee, @AccommodationFee, @TravelFee,
        @OtherExpenses, @TotalExpenses, @CCEmail, @TrainingObjective, @ExpectedResult,

        'Pending', GETDATE(), @CreatedBy, 1, @TotalPeople,

        -- กำหนดสถานะเริ่มต้นทุกระดับเป็น 'Pending'
        @SectionManagerId, 'Pending',
        @DepartmentManagerId, 'Pending',
        @HRDAdminId, 'Pending',
        @HRDConfirmationId, 'Pending',
        @ManagingDirectorId, 'Pending',
        @DeputyManagingDirectorId, 'Pending'  -- 🆕
    )";
```

**สิ่งสำคัญ**:
- ต้องเพิ่ม `DeputyManagingDirectorId` และ `Status_DeputyManagingDirector` ใน INSERT query
- สถานะเริ่มต้นของ Deputy Managing Director = `'Pending'`
- ถ้าไม่เพิ่ม → สถานะจะเป็น `NULL` ทำให้ ApprovalFlow แสดงผลไม่ถูกต้อง

#### 2. การดึงข้อมูลสำหรับ ApprovalFlow

**เมธอด**: `ApprovalFlow(string docNo)`

**SQL Query ที่อัพเดต**:

```csharp
string query = @"
    SELECT
        Id, DocNo, Company, SeminarTitle, StartDate, EndDate,
        Status, CreatedBy, CreatedDate,

        -- ผู้อนุมัติระดับที่ 1-5
        SectionManagerId, Status_SectionManager, Comment_SectionManager, ApproveInfo_SectionManager,
        DepartmentManagerId, Status_DepartmentManager, Comment_DepartmentManager, ApproveInfo_DepartmentManager,
        HRDAdminId, Status_HRDAdmin, Comment_HRDAdmin, ApproveInfo_HRDAdmin,
        HRDConfirmationId, Status_HRDConfirmation, Comment_HRDConfirmation, ApproveInfo_HRDConfirmation,
        ManagingDirectorId, Status_ManagingDirector, Comment_ManagingDirector, ApproveInfo_ManagingDirector,

        -- 🆕 ผู้อนุมัติระดับที่ 6: Deputy Managing Director
        DeputyManagingDirectorId, Status_DeputyManagingDirector,
        Comment_DeputyManagingDirector, ApproveInfo_DeputyManagingDirector

    FROM [HRDSYSTEM].[dbo].[TrainingRequests]
    WHERE DocNo = @DocNo AND IsActive = 1";
```

**การ Map ข้อมูลเข้า Model**:

```csharp
var model = new TrainingRequestEditViewModel
{
    // ... คุณสมบัติอื่นๆ ...

    // 🆕 Deputy Managing Director
    DeputyManagingDirectorId = reader["DeputyManagingDirectorId"]?.ToString(),
    Status_DeputyManagingDirector = reader["Status_DeputyManagingDirector"]?.ToString(),
    Comment_DeputyManagingDirector = reader["Comment_DeputyManagingDirector"]?.ToString(),
    ApproveInfo_DeputyManagingDirector = reader["ApproveInfo_DeputyManagingDirector"]?.ToString()
};
```

**ปัญหาที่เคยพบ**:
- ถ้าไม่เพิ่ม Deputy Managing Director columns ใน SELECT query
- ถ้าไม่ Map ข้อมูลเข้า Model
- → ApprovalFlow จะแสดง "ยังไม่ระบุผู้อนุมัติ" แม้ว่าฐานข้อมูลมีข้อมูล

---

## Views และส่วนติดต่อผู้ใช้

### 1. Create.cshtml - ฟอร์มสร้างคำขออบรม

#### HTML สำหรับ Deputy Managing Director

```html
<!-- ผู้อนุมัติระดับที่ 6: Deputy Managing Director -->
<div class="form-group mt-3">
    <label class="form-label fw-bold">
        👤 Deputy Managing Director
        <small class="text-muted">(รอง MD / Deputy MD)</small>
    </label>

    <!-- Dropdown แบบ Select2 พร้อม AJAX -->
    <select id="deputyManagingDirectorSelect"
            class="form-select form-select-lg"
            style="width: 100%; border-radius: 10px;"
            required>
        <option value="">-- เลือก Deputy Managing Director --</option>
        <option value="ผู้บังคับบัญชาลำดับถัดไป อนุมัติ">
            ⏭️ ผู้บังคับบัญชาลำดับถัดไป อนุมัติ
        </option>
    </select>

    <!-- Hidden field เก็บค่าจริง -->
    <input type="hidden"
           id="deputyManagingDirectorIdHidden"
           name="DeputyManagingDirectorId"
           value="" />
</div>
```

#### JavaScript - Select2 AJAX Configuration

```javascript
// 4️⃣ Deputy Managing Director Select2
$('#deputyManagingDirectorSelect').select2({
    theme: 'bootstrap-5',
    placeholder: 'พิมพ์ชื่อเพื่อค้นหา Deputy Managing Director...',
    allowClear: true,
    multiple: false,
    width: '100%',
    ajax: {
        url: '/api/employees/approvers/director',
        dataType: 'json',
        delay: 300,
        data: function (params) {
            console.log('🔍 Searching Deputy Managing Director:', params.term);
            return {
                q: params.term || ''
            };
        },
        processResults: function (data) {
            console.log('📋 Deputy Managing Directors found:', data.length);

            // 🆕 เพิ่ม SKIP option เป็นตัวเลือกแรก
            var results = [{
                id: 'ผู้บังคับบัญชาลำดับถัดไป อนุมัติ',
                text: '⏭️ ผู้บังคับบัญชาลำดับถัดไป อนุมัติ'
            }];

            // เพิ่มข้อมูลพนักงานจาก API
            results = results.concat(data.map(function(item) {
                return {
                    id: item.email,
                    text: item.email + ' (' + item.name + ' - ' + item.level + ')'
                };
            }));

            return { results: results };
        },
        cache: false
    },
    minimumInputLength: 0
});

// บันทึกค่าที่เลือกลง Hidden Field
$('#deputyManagingDirectorSelect').on('change', function() {
    const selectedValue = $(this).val();
    $('#deputyManagingDirectorIdHidden').val(selectedValue || '');
    console.log('✅ Deputy Managing Director selected:', selectedValue);
});
```

**Logic การทำงานของ Select2 AJAX**:

1. **ปัญหาเดิม**: HTML มี `<option>` SKIP แต่ Select2 AJAX แทนที่ด้วยข้อมูลจาก API
   ```html
   <!-- ตัวเลือกนี้จะหายเมื่อ Select2 AJAX โหลดข้อมูล -->
   <option value="ผู้บังคับบัญชาลำดับถัดไป อนุมัติ">...</option>
   ```

2. **วิธีแก้ไข**: เพิ่ม SKIP option ใน `processResults()` แทน
   ```javascript
   processResults: function (data) {
       // สร้าง array เริ่มต้นด้วย SKIP option
       var results = [{
           id: 'ผู้บังคับบัญชาลำดับถัดไป อนุมัติ',
           text: '⏭️ ผู้บังคับบัญชาลำดับถัดไป อนุมัติ'
       }];

       // เพิ่มข้อมูลพนักงานต่อท้าย
       results = results.concat(data.map(...));

       return { results: results };
   }
   ```

3. **ผลลัพธ์**: Dropdown จะแสดง SKIP เป็นตัวเลือกแรก ตามด้วยรายชื่อพนักงาน

### 2. Edit.cshtml - ฟอร์มแก้ไขและอนุมัติ

#### HTML สำหรับ Deputy Managing Director

```html
<!-- ผู้อนุมัติระดับที่ 6: Deputy Managing Director -->
<hr class="my-4">
<h5 class="fw-bold mb-3">👤 ผู้อนุมัติระดับที่ 6: Deputy Managing Director</h5>

<div class="row g-3">
    <!-- Approver Dropdown -->
    <div class="col-md-12">
        <label class="form-label fw-bold">Deputy Managing Director</label>
        <select id="deputyManagingDirectorSelect"
                class="form-select form-select-lg"
                style="width: 100%; border-radius: 10px;"
                required>
            <option value="">พิมพ์ชื่อเพื่อค้นหา...</option>
            <option value="ผู้บังคับบัญชาลำดับถัดไป อนุมัติ">
                ⏭️ ผู้บังคับบัญชาลำดับถัดไป อนุมัติ
            </option>
        </select>
        <input type="hidden"
               id="deputyManagingDirectorIdHidden"
               name="DeputyManagingDirectorId"
               value="@Model.DeputyManagingDirectorId" />
    </div>

    <!-- Status (Read-only) -->
    <div class="col-md-4">
        <label class="form-label fw-bold">สถานะ</label>
        <input type="text"
               name="Status_DeputyManagingDirector"
               class="form-control form-control-lg"
               value="@Model.Status_DeputyManagingDirector"
               readonly
               style="background-color: #e9ecef;" />
    </div>

    <!-- Comment (Read-only) -->
    <div class="col-md-8">
        <label class="form-label fw-bold">ความคิดเห็น</label>
        <textarea name="Comment_DeputyManagingDirector"
                  class="form-control form-control-lg"
                  rows="2"
                  readonly
                  style="background-color: #e9ecef;">@Model.Comment_DeputyManagingDirector</textarea>
    </div>

    <!-- Approval Info (Read-only) -->
    <div class="col-md-12">
        <label class="form-label fw-bold">ข้อมูลการอนุมัติ</label>
        <input type="text"
               name="ApproveInfo_DeputyManagingDirector"
               class="form-control form-control-lg"
               value="@Model.ApproveInfo_DeputyManagingDirector"
               readonly
               style="background-color: #e9ecef;" />
    </div>
</div>
```

**หมายเหตุ**:
- Fields `Status`, `Comment`, และ `ApproveInfo` เป็น **readonly** เพราะถูกสร้างโดยระบบเมื่อมีการอนุมัติ
- มีเพียง Dropdown เท่านั้นที่แก้ไขได้

### 3. ApprovalFlow.cshtml - Timeline การอนุมัติ

#### Helper Functions

```csharp
@{
    // ค่าคงที่สำหรับการข้ามผู้อนุมัติ
    const string SKIP_APPROVER = "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ";

    /// <summary>
    /// ตรวจสอบว่าเป็น Skip Approver หรือไม่
    /// </summary>
    bool IsSkipApprover(string approverId)
    {
        // 🆕 NULL หรือ Empty = SKIP (Backward Compatibility)
        if (string.IsNullOrWhiteSpace(approverId))
            return true;

        // เช็คว่าเป็นค่า SKIP_APPROVER หรือไม่
        return string.Equals(approverId?.Trim(), SKIP_APPROVER, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// หา CSS Class สำหรับแสดงสถานะ
    /// </summary>
    string GetStatusClass(string levelStatus, string mainStatus, string waitingStatus)
    {
        if (string.Equals(levelStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            return "approved";
        if (string.Equals(levelStatus, "Reject", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(levelStatus, "Rejected", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(levelStatus, "Revise", StringComparison.OrdinalIgnoreCase))
            return "rejected";
        if (mainStatus == waitingStatus)
            return "waiting";
        return "pending";
    }

    /// <summary>
    /// แปลงสถานะเป็นข้อความแสดงผล
    /// </summary>
    string GetStatusDisplay(string levelStatus, string mainStatus, string waitingStatus)
    {
        if (string.Equals(levelStatus, "Approved", StringComparison.OrdinalIgnoreCase))
            return "Approved";
        if (string.Equals(levelStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
            return "Rejected";
        if (string.Equals(levelStatus, "Revise", StringComparison.OrdinalIgnoreCase))
            return "Revise";
        if (mainStatus == waitingStatus)
            return "Waiting";
        return "Pending";
    }
}
```

#### Timeline สำหรับ Deputy Managing Director

```html
<!-- Level 6: Deputy Managing Director -->
@{
    // กำหนดค่าเริ่มต้น
    var dmdStatus = Model.Status_DeputyManagingDirector ?? "Pending";
    var dmdClass = GetStatusClass(dmdStatus, Model.Status, "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR");
}
<div class="timeline-item">
    <!-- Marker (ตัวเลขระดับ) -->
    <div class="timeline-marker @dmdClass">6</div>

    <!-- Content -->
    <div class="timeline-content @dmdClass">
        <!-- Header: ชื่อระดับ + สถานะ -->
        <div class="timeline-header">
            <div class="timeline-title">🔹 Deputy Managing Director</div>
            <span class="status-badge @dmdClass">
                @GetStatusDisplay(dmdStatus, Model.Status, "WAITING_FOR_DEPUTY_MANAGING_DIRECTOR")
            </span>
        </div>

        <!-- Approver Info -->
        <div class="approver-info">
            <div class="approver-email">
                @if (IsSkipApprover(Model.DeputyManagingDirectorId))
                {
                    <!-- กรณี SKIP -->
                    <span class="text-warning">⏭️ ข้ามขั้นตอนนี้ (Skip)</span>
                }
                else if (!string.IsNullOrEmpty(Model.DeputyManagingDirectorId))
                {
                    <!-- กรณีมีอีเมล -->
                    <span>📧 @Model.DeputyManagingDirectorId</span>
                }
                else
                {
                    <!-- กรณีไม่มีข้อมูล -->
                    <span class="text-muted">ยังไม่ระบุผู้อนุมัติ</span>
                }
            </div>

            <!-- Approval Date/Time -->
            @if (!string.IsNullOrEmpty(Model.ApproveInfo_DeputyManagingDirector))
            {
                <div class="approval-date">
                    ✅ @Model.ApproveInfo_DeputyManagingDirector
                </div>
            }
        </div>

        <!-- Comment -->
        @if (!string.IsNullOrEmpty(Model.Comment_DeputyManagingDirector))
        {
            <div class="comment-box">
                <div class="comment-label">💬 ความคิดเห็น:</div>
                <div>@Model.Comment_DeputyManagingDirector</div>
            </div>
        }
    </div>
</div>
```

**Logic การแสดงผล**:

1. **กรณี SKIP** (NULL, Empty, หรือ "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"):
   ```
   ⏭️ ข้ามขั้นตอนนี้ (Skip)
   ```

2. **กรณีมีอีเมล**:
   ```
   📧 deputy@company.com
   ✅ deputy@company.com / 20/12/2025 / 09:43
   💬 อนุมัติแล้ว
   ```

3. **กรณีไม่มีข้อมูล**:
   ```
   ยังไม่ระบุผู้อนุมัติ
   ```

---

## ฟีเจอร์การข้ามขั้นตอน (Skip)

### ภาพรวม

**ฟีเจอร์การข้ามขั้นตอน** อนุญาตให้ผู้สร้างคำขออบรมสามารถกำหนดให้ข้ามผู้อนุมัติบางระดับได้ โดยใช้ข้อความพิเศษ **"ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"**

### ผู้อนุมัติที่สามารถข้ามได้

| ระดับ | ผู้อนุมัติ | สามารถข้ามได้ | เหตุผล |
|------|----------|--------------|--------|
| 1 | Section Manager | ✅ ได้ | ขึ้นอยู่กับโครงสร้างองค์กร |
| 2 | Department Manager | ✅ ได้ | ขึ้นอยู่กับโครงสร้างองค์กร |
| 3 | HRD Admin | ❌ ไม่ได้ | จำเป็นต้องตรวจสอบทุกครั้ง |
| 4 | HRD Confirmation | ❌ ไม่ได้ | จำเป็นต้องยืนยันทุกครั้ง |
| 5 | Managing Director | ✅ ได้ | บางกรณีไม่ต้องการ MD อนุมัติ |
| 6 | Deputy Managing Director | ✅ ได้ | บางกรณีไม่ต้องการ Deputy MD อนุมัติ |

### วิธีการข้ามขั้นตอน

#### 1. ในฟอร์ม Create/Edit

เลือก **"⏭️ ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"** จาก Dropdown

```html
<select id="sectionManagerSelect" required>
    <option value="">-- เลือก Section Manager --</option>
    <option value="ผู้บังคับบัญชาลำดับถัดไป อนุมัติ">
        ⏭️ ผู้บังคับบัญชาลำดับถัดไป อนุมัติ
    </option>
    <!-- ตัวเลือกพนักงานอื่นๆ จาก API -->
</select>
```

#### 2. การเก็บข้อมูลในฐานข้อมูล

เมื่อเลือก SKIP → เก็บค่า `"ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"` ลงฐานข้อมูล

```sql
UPDATE TrainingRequests
SET SectionManagerId = 'ผู้บังคับบัญชาลำดับถัดไป อนุมัติ'
WHERE DocNo = 'TR-2025-001';
```

#### 3. การตรวจสอบการข้ามขั้นตอน

```csharp
private bool IsSkipApprover(string approverId)
{
    // กรณีที่ 1: NULL หรือ Empty String
    // (สำหรับ backward compatibility กับ records เก่า)
    if (string.IsNullOrWhiteSpace(approverId))
        return true;

    // กรณีที่ 2: ค่าเท่ากับ "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"
    return string.Equals(approverId.Trim(), SKIP_APPROVER, StringComparison.OrdinalIgnoreCase);
}
```

**3 กรณีที่ถือว่า SKIP**:
1. `approverId = NULL`
2. `approverId = ""`
3. `approverId = "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"`

### ตัวอย่างการทำงาน

**ตัวอย่างที่ 1**: Skip Section Manager และ Deputy Managing Director

```
Input:
- SectionManagerId = "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"
- DepartmentManagerId = "dept@company.com"
- ManagingDirectorId = "md@company.com"
- DeputyManagingDirectorId = "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"

Workflow:
1. Pending
2. WAITING_FOR_SECTION_MANAGER → อนุมัติอัตโนมัติ (SKIP)
3. WAITING_FOR_DEPARTMENT_MANAGER → รออนุมัติจาก dept@company.com
4. WAITING_FOR_HRD_ADMIN → รออนุมัติ
5. WAITING_FOR_HRD_CONFIRMATION → รออนุมัติ
6. WAITING_FOR_MANAGING_DIRECTOR → รออนุมัติจาก md@company.com
7. WAITING_FOR_DEPUTY_MANAGING_DIRECTOR → อนุมัติอัตโนมัติ (SKIP)
8. APPROVED ✅
```

**ตัวอย่างที่ 2**: ไม่ Skip ใครเลย

```
Input:
- ทุกระดับมีอีเมลจริง (ไม่มี SKIP)

Workflow:
1. Pending
2. WAITING_FOR_SECTION_MANAGER → รออนุมัติ
3. WAITING_FOR_DEPARTMENT_MANAGER → รออนุมัติ
4. WAITING_FOR_HRD_ADMIN → รออนุมัติ
5. WAITING_FOR_HRD_CONFIRMATION → รออนุมัติ
6. WAITING_FOR_MANAGING_DIRECTOR → รออนุมัติ
7. WAITING_FOR_DEPUTY_MANAGING_DIRECTOR → รออนุมัติ
8. APPROVED ✅
```

---

## ระบบแจ้งเตือนทางอีเมล

### EmailService.cs

#### 1. เทมเพลตอีเมลสำหรับ Deputy Managing Director

```csharp
public async Task<bool> SendApprovalEmail(
    string toEmail,
    string approverName,
    TrainingRequestEditViewModel request,
    string currentStatus)
{
    // สร้างหัวข้ออีเมล
    string subject = $"[Training Request] คำขออบรม {request.DocNo} - รอการอนุมัติจาก {approverName}";

    // สร้างเนื้อหาอีเมล
    string body = $@"
    <html>
    <body style='font-family: Arial, sans-serif;'>
        <h2 style='color: #2c3e50;'>🔔 แจ้งเตือน: คำขออบรมรอการอนุมัติ</h2>

        <p>เรียน <strong>{approverName}</strong>,</p>

        <p>มีคำขออบรมใหม่รอการอนุมัติจากท่าน</p>

        <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
            <h3 style='color: #495057; margin-top: 0;'>📋 รายละเอียดคำขออบรม</h3>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px 0; color: #6c757d;'><strong>เลขที่เอกสาร:</strong></td>
                    <td style='padding: 8px 0;'>{request.DocNo}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6c757d;'><strong>หัวข้อการอบรม:</strong></td>
                    <td style='padding: 8px 0;'>{request.SeminarTitle}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6c757d;'><strong>ผู้ขออบรม:</strong></td>
                    <td style='padding: 8px 0;'>{request.EmployeeName}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6c757d;'><strong>วันที่อบรม:</strong></td>
                    <td style='padding: 8px 0;'>{request.StartDate:dd/MM/yyyy} - {request.EndDate:dd/MM/yyyy}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6c757d;'><strong>บริษัท:</strong></td>
                    <td style='padding: 8px 0;'>{request.Company}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6c757d;'><strong>ฝ่าย:</strong></td>
                    <td style='padding: 8px 0;'>{request.Department}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6c757d;'><strong>แผนก:</strong></td>
                    <td style='padding: 8px 0;'>{request.Section}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6c757d;'><strong>จำนวนค่าใช้จ่าย:</strong></td>
                    <td style='padding: 8px 0;'>{request.TotalExpenses:N2} บาท</td>
                </tr>
            </table>
        </div>

        <div style='background-color: #e3f2fd; padding: 15px; border-left: 4px solid #2196f3; margin: 20px 0;'>
            <p style='margin: 0;'><strong>🔗 กรุณาคลิกลิงก์ด้านล่างเพื่ออนุมัติหรือปฏิเสธคำขออบรม:</strong></p>
            <p style='margin: 10px 0 0 0;'>
                <a href='{GetApprovalUrl(request.DocNo)}'
                   style='display: inline-block; background-color: #007bff; color: white;
                          padding: 12px 24px; text-decoration: none; border-radius: 5px;
                          font-weight: bold;'>
                    ดูรายละเอียดและอนุมัติ
                </a>
            </p>
        </div>

        <!-- 🆕 สถานะการอนุมัติทั้ง 6 ระดับ -->
        {GenerateApprovalStatusHtml(request)}

        <hr style='border: none; border-top: 1px solid #dee2e6; margin: 30px 0;'>

        <p style='color: #6c757d; font-size: 12px;'>
            อีเมลนี้ส่งมาจากระบบจัดการคำขออบรม (Training Request System)<br>
            กรุณาอย่าตอบกลับอีเมลนี้
        </p>
    </body>
    </html>";

    return await SendEmail(toEmail, subject, body);
}
```

#### 2. ฟังก์ชันสร้างตาราง สถานะการอนุมัติ

```csharp
/// <summary>
/// สร้าง HTML แสดงสถานะการอนุมัติทั้ง 6 ระดับ
/// </summary>
private string GenerateApprovalStatusHtml(TrainingRequestEditViewModel request)
{
    return $@"
    <div style='margin: 20px 0;'>
        <h4 style='color: #495057;'>📊 สถานะการอนุมัติ</h4>
        <table style='width: 100%; border-collapse: collapse; margin-top: 10px;'>
            <thead>
                <tr style='background-color: #f8f9fa;'>
                    <th style='padding: 12px; text-align: left; border: 1px solid #dee2e6;'>ระดับ</th>
                    <th style='padding: 12px; text-align: left; border: 1px solid #dee2e6;'>ผู้อนุมัติ</th>
                    <th style='padding: 12px; text-align: left; border: 1px solid #dee2e6;'>สถานะ</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>1. Section Manager</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetApproverDisplay(request.SectionManagerId)}</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetStatusBadge(request.Status_SectionManager)}</td>
                </tr>
                <tr>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>2. Department Manager</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetApproverDisplay(request.DepartmentManagerId)}</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetStatusBadge(request.Status_DepartmentManager)}</td>
                </tr>
                <tr>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>3. HRD Admin</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetApproverDisplay(request.HRDAdminId)}</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetStatusBadge(request.Status_HRDAdmin)}</td>
                </tr>
                <tr>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>4. HRD Confirmation</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetApproverDisplay(request.HRDConfirmationId)}</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetStatusBadge(request.Status_HRDConfirmation)}</td>
                </tr>
                <tr>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>5. Managing Director</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetApproverDisplay(request.ManagingDirectorId)}</td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'>{GetStatusBadge(request.Status_ManagingDirector)}</td>
                </tr>
                <tr style='background-color: #fff3cd;'>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'><strong>6. Deputy Managing Director</strong></td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'><strong>{GetApproverDisplay(request.DeputyManagingDirectorId)}</strong></td>
                    <td style='padding: 10px; border: 1px solid #dee2e6;'><strong>{GetStatusBadge(request.Status_DeputyManagingDirector)}</strong></td>
                </tr>
            </tbody>
        </table>
    </div>";
}

/// <summary>
/// แสดงชื่อผู้อนุมัติหรือ "Skip" ถ้าข้ามขั้นตอน
/// </summary>
private string GetApproverDisplay(string approverId)
{
    if (IsSkipApprover(approverId))
        return "<span style='color: #ff9800;'>⏭️ ข้ามขั้นตอน</span>";

    if (string.IsNullOrEmpty(approverId))
        return "<span style='color: #9e9e9e;'>ยังไม่ระบุ</span>";

    return approverId;
}

/// <summary>
/// สร้าง Badge แสดงสถานะ
/// </summary>
private string GetStatusBadge(string status)
{
    return status?.ToUpper() switch
    {
        "APPROVED" => "<span style='background-color: #28a745; color: white; padding: 4px 8px; border-radius: 4px;'>✅ Approved</span>",
        "REJECTED" => "<span style='background-color: #dc3545; color: white; padding: 4px 8px; border-radius: 4px;'>❌ Rejected</span>",
        "REVISE" => "<span style='background-color: #ffc107; color: black; padding: 4px 8px; border-radius: 4px;'>🔄 Revise</span>",
        "PENDING" => "<span style='background-color: #6c757d; color: white; padding: 4px 8px; border-radius: 4px;'>⏳ Pending</span>",
        _ => "<span style='background-color: #e9ecef; color: #495057; padding: 4px 8px; border-radius: 4px;'>-</span>"
    };
}
```

### การส่งอีเมลเมื่อข้ามขั้นตอน

```csharp
// ในฟังก์ชัน ProcessApproval()
string nextApproverEmail = _workflowService.GetNextApproverEmail(nextStatus, model);

// ถ้าเป็น Skip Approver → GetNextApproverEmail() จะคืนค่า NULL
if (!string.IsNullOrEmpty(nextApproverEmail))
{
    // ส่งอีเมลแจ้งเตือน
    await _emailService.SendApprovalEmail(
        nextApproverEmail,
        _workflowService.GetNextApproverName(nextStatus),
        model,
        nextStatus
    );
}
else
{
    // ข้ามการส่งอีเมล (เพราะเป็น Skip Approver)
    Console.WriteLine($"⏭️ Skipped sending email (Skip Approver)");
}
```

---

## การแก้ไขปัญหาที่พบ

### ปัญหาที่ 1: Login ไม่ได้

**อาการ**: ผู้ใช้ไม่สามารถ Login เข้าระบบได้ แสดงข้อความ "An error occurred while processing your request"

**สาเหตุ**:
- Database ยังไม่ได้ทำการ Migrate
- ไม่มีคอลัมน์ Deputy Managing Director ในตาราง TrainingRequests
- Code พยายามอ่านคอลัมน์ที่ไม่มีอยู่

**วิธีแก้ไข**:
1. รัน Migration Script:
   ```sql
   -- Database/AddDeputyManagingDirector.sql
   ALTER TABLE [dbo].[TrainingRequests]
   ADD
       DeputyManagingDirectorId NVARCHAR(100) NULL,
       Status_DeputyManagingDirector NVARCHAR(20) NULL,
       Comment_DeputyManagingDirector NVARCHAR(500) NULL,
       ApproveInfo_DeputyManagingDirector NVARCHAR(200) NULL;
   ```

2. Restart Application

### ปัญหาที่ 2: Status_DeputyManagingDirector ไม่แสดง "Pending"

**อาการ**: สถานะของ Deputy Managing Director และระดับอื่นๆ แสดงเป็น NULL แทนที่จะเป็น "Pending"

**สาเหตุ**:
- INSERT query ไม่ได้กำหนดค่าสถานะเริ่มต้น
- ขาดการ Initialize Status Columns

**วิธีแก้ไข**:
1. แก้ไข `InsertTrainingRequest()` ใน TrainingRequestController.cs:
   ```csharp
   INSERT INTO ... (
       ...,
       SectionManagerId, Status_SectionManager,
       DepartmentManagerId, Status_DepartmentManager,
       HRDAdminId, Status_HRDAdmin,
       HRDConfirmationId, Status_HRDConfirmation,
       ManagingDirectorId, Status_ManagingDirector,
       DeputyManagingDirectorId, Status_DeputyManagingDirector
   )
   VALUES (
       ...,
       @SectionManagerId, 'Pending',
       @DepartmentManagerId, 'Pending',
       @HRDAdminId, 'Pending',
       @HRDConfirmationId, 'Pending',
       @ManagingDirectorId, 'Pending',
       @DeputyManagingDirectorId, 'Pending'
   )
   ```

2. รัน Update Script สำหรับ Records เก่า:
   ```sql
   -- Database/UpdateExistingRecords_DeputyMD.sql
   UPDATE [dbo].[TrainingRequests]
   SET
       Status_SectionManager = ISNULL(Status_SectionManager, 'Pending'),
       ...
       Status_DeputyManagingDirector = ISNULL(Status_DeputyManagingDirector, 'Pending')
   WHERE IsActive = 1;
   ```

### ปัญหาที่ 3: ApprovalFlow ไม่แสดงข้อมูล Deputy Managing Director

**อาการ**:
- ฐานข้อมูลมีข้อมูล `DeputyManagingDirectorId = "deputy@company.com"`
- แต่ ApprovalFlow แสดง "ยังไม่ระบุผู้อนุมัติ"

**สาเหตุ**:
- `ApprovalFlow()` method ใน Controller ไม่ได้ SELECT คอลัมน์ Deputy Managing Director จาก Database
- Model ไม่ได้รับข้อมูล

**วิธีแก้ไข**:
```csharp
// แก้ไข ApprovalFlow() ใน TrainingRequestController.cs

// 1. เพิ่ม Deputy MD columns ใน SELECT query
string query = @"
    SELECT
        ...,
        ManagingDirectorId, Status_ManagingDirector, Comment_ManagingDirector, ApproveInfo_ManagingDirector,
        DeputyManagingDirectorId, Status_DeputyManagingDirector, Comment_DeputyManagingDirector, ApproveInfo_DeputyManagingDirector
    FROM [HRDSYSTEM].[dbo].[TrainingRequests]
    WHERE DocNo = @DocNo AND IsActive = 1";

// 2. เพิ่ม Deputy MD property assignments
var model = new TrainingRequestEditViewModel
{
    ...,
    DeputyManagingDirectorId = reader["DeputyManagingDirectorId"]?.ToString(),
    Status_DeputyManagingDirector = reader["Status_DeputyManagingDirector"]?.ToString(),
    Comment_DeputyManagingDirector = reader["Comment_DeputyManagingDirector"]?.ToString(),
    ApproveInfo_DeputyManagingDirector = reader["ApproveInfo_DeputyManagingDirector"]?.ToString()
};
```

### ปัญหาที่ 4: SKIP Option ไม่แสดงใน Create Form

**อาการ**:
- แม้ HTML มี `<option>` SKIP
- แต่ Dropdown ไม่แสดงตัวเลือก "⏭️ ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"

**สาเหตุ**:
- Select2 ใช้ AJAX ดึงข้อมูลแบบ Dynamic
- Static HTML `<option>` ถูกแทนที่ด้วยข้อมูลจาก API
- `processResults()` ไม่ได้เพิ่ม SKIP option

**วิธีแก้ไข**:
```javascript
// แก้ไข processResults ใน Create.cshtml

processResults: function (data) {
    console.log('📋 Section Managers found:', data.length);

    // 1. สร้าง array เริ่มต้นด้วย SKIP option
    var results = [{
        id: 'ผู้บังคับบัญชาลำดับถัดไป อนุมัติ',
        text: '⏭️ ผู้บังคับบัญชาลำดับถัดไป อนุมัติ'
    }];

    // 2. เพิ่มข้อมูลพนักงานต่อท้าย
    results = results.concat(data.map(function(item) {
        return {
            id: item.email,
            text: item.email + ' (' + item.name + ' - ' + item.level + ')'
        };
    }));

    return { results: results };
}
```

**จำนวน Select2 ที่ต้องแก้ไข**:
- Create.cshtml: 7 instances
- Edit.cshtml: 7 instances

### ปัญหาที่ 5: NULL/Empty String ไม่ถูกจัดการเป็น SKIP

**อาการ**:
- Records เก่าที่สร้างก่อนมีฟีเจอร์ Deputy MD มี `DeputyManagingDirectorId = NULL`
- ApprovalFlow แสดง "ยังไม่ระบุผู้อนุมัติ" แต่สถานะเป็น "APPROVED" (ไม่สอดคล้องกัน)

**สาเหตุ**:
- `IsSkipApprover()` คืนค่า FALSE สำหรับ NULL/Empty
- Logic คิดว่ายังต้องรออนุมัติจาก Deputy MD

**วิธีแก้ไข**:
```csharp
// แก้ไข IsSkipApprover() ใน ApprovalWorkflowService.cs

private bool IsSkipApprover(string approverId)
{
    // 🆕 NULL หรือ empty string = SKIP (backward compatibility)
    if (string.IsNullOrWhiteSpace(approverId))
        return true;

    // เช็คว่าเป็นค่า SKIP_APPROVER หรือไม่
    return string.Equals(approverId.Trim(), SKIP_APPROVER, StringComparison.OrdinalIgnoreCase);
}
```

**ต้องแก้ไขใน 2 ที่**:
1. `Services/ApprovalWorkflowService.cs` - Logic ฝั่ง Backend
2. `Views/TrainingRequest/ApprovalFlow.cshtml` - Helper function ฝั่ง View

---

## คู่มือการทดสอบ

### การทดสอบก่อนใช้งาน

#### 1. ทดสอบ Database Migration

```sql
-- 1. เช็คว่ามีคอลัมน์ Deputy MD แล้วหรือยัง
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequests'
    AND COLUMN_NAME LIKE '%Deputy%'
ORDER BY ORDINAL_POSITION;

-- ผลลัพธ์ที่ต้องการ:
-- DeputyManagingDirectorId          NVARCHAR  100   YES
-- Status_DeputyManagingDirector     NVARCHAR  20    YES
-- Comment_DeputyManagingDirector    NVARCHAR  500   YES
-- ApproveInfo_DeputyManagingDirector NVARCHAR 200   YES

-- 2. เช็คว่าข้อมูลเดิมมีสถานะ Pending หรือยัง
SELECT
    DocNo,
    Status_SectionManager,
    Status_DepartmentManager,
    Status_HRDAdmin,
    Status_HRDConfirmation,
    Status_ManagingDirector,
    Status_DeputyManagingDirector
FROM [dbo].[TrainingRequests]
WHERE IsActive = 1
    AND Status_DeputyManagingDirector IS NULL;

-- ถ้ามี records ที่ Status เป็น NULL → รัน UpdateExistingRecords_DeputyMD.sql
```

#### 2. ทดสอบการสร้างคำขออบรมใหม่

**Test Case 1**: สร้างคำขอโดยไม่ Skip ใครเลย

```
1. ไปที่หน้า Create
2. กรอกข้อมูลทุกฟิลด์
3. เลือกผู้อนุมัติทุกระดับจาก Dropdown (ไม่เลือก SKIP)
4. บันทึก

Expected:
✅ บันทึกสำเร็จ
✅ ทุกระดับมี Status = "Pending"
✅ Deputy Managing Director มีอีเมลที่เลือกไว้
```

**Test Case 2**: สร้างคำขอโดย Skip Deputy Managing Director

```
1. ไปที่หน้า Create
2. กรอกข้อมูล
3. เลือก "⏭️ ผู้บังคับบัญชาลำดับถัดไป อนุมัติ" สำหรับ Deputy MD
4. บันทึก

Expected:
✅ บันทึกสำเร็จ
✅ DeputyManagingDirectorId = "ผู้บังคับบัญชาลำดับถัดไป อนุมัติ"
✅ Status_DeputyManagingDirector = "Pending"
```

#### 3. ทดสอบ Workflow การอนุมัติ

**Test Case 3**: อนุมัติครบทุกระดับ (ไม่ Skip)

```
1. สร้างคำขอโดยไม่ Skip ใครเลย
2. อนุมัติจาก Section Manager → สถานะเปลี่ยนเป็น WAITING_FOR_DEPARTMENT_MANAGER
3. อนุมัติจาก Department Manager → สถานะเปลี่ยนเป็น WAITING_FOR_HRD_ADMIN
4. อนุมัติจาก HRD Admin → สถานะเปลี่ยนเป็น WAITING_FOR_HRD_CONFIRMATION
5. อนุมัติจาก HRD Confirmation → สถานะเปลี่ยนเป็น WAITING_FOR_MANAGING_DIRECTOR
6. อนุมัติจาก Managing Director → สถานะเปลี่ยนเป็น WAITING_FOR_DEPUTY_MANAGING_DIRECTOR
7. อนุมัติจาก Deputy Managing Director → สถานะเปลี่ยนเป็น APPROVED ✅

Expected:
✅ ทุกขั้นตอนทำงานถูกต้อง
✅ สถานะสุดท้าย = "APPROVED"
✅ Deputy Managing Director เป็นคนสุดท้ายที่อนุมัติ
```

**Test Case 4**: อนุมัติโดย Skip Deputy MD

```
1. สร้างคำขอโดย Skip Deputy MD
2. อนุมัติครบทุกระดับจนถึง Managing Director
3. เมื่อ Managing Director อนุมัติ → สถานะเปลี่ยนเป็น APPROVED โดยตรง ✅

Expected:
✅ ข้ามขั้นตอน Deputy MD
✅ สถานะสุดท้าย = "APPROVED"
✅ ไม่ต้องรอ Deputy MD อนุมัติ
```

**Test Case 5**: Skip หลายระดับ

```
1. สร้างคำขอโดย:
   - Skip Section Manager
   - Skip Department Manager
   - Skip Managing Director
   - Skip Deputy Managing Director
2. ส่งคำขอ
3. อนุมัติจาก HRD Admin
4. อนุมัติจาก HRD Confirmation
5. สถานะเปลี่ยนเป็น APPROVED ทันที ✅

Expected:
✅ ข้ามขั้นตอนที่ Skip ได้ทั้งหมด
✅ ผ่านเฉพาะ HRD Admin และ HRD Confirmation (ไม่สามารถ Skip ได้)
```

#### 4. ทดสอบ ApprovalFlow Display

**Test Case 6**: แสดงผลสำหรับ SKIP Approver

```
1. สร้างคำขอโดย Skip Deputy MD
2. ไปที่หน้า ApprovalFlow
3. ดูส่วน Deputy Managing Director

Expected:
✅ แสดง "⏭️ ข้ามขั้นตอนนี้ (Skip)"
✅ ไม่แสดงอีเมล
✅ สถานะแสดง "Pending" หรือ "Approved" (ขึ้นกับว่าผ่านมาแล้วหรือยัง)
```

**Test Case 7**: แสดงผลสำหรับ Records เก่า (NULL)

```
1. ดึง records ที่สร้างก่อนมีฟีเจอร์ Deputy MD (DeputyManagingDirectorId = NULL)
2. ไปที่หน้า ApprovalFlow

Expected:
✅ แสดง "⏭️ ข้ามขั้นตอนนี้ (Skip)"
✅ ไม่แสดง "ยังไม่ระบุผู้อนุมัติ"
✅ Backward compatibility ทำงานถูกต้อง
```

#### 5. ทดสอบ Email Notifications

**Test Case 8**: ส่งอีเมลแจ้งเตือน Deputy MD

```
1. สร้างคำขอโดยระบุอีเมล Deputy MD
2. อนุมัติครบจนถึง Managing Director
3. เช็คอีเมลของ Deputy MD

Expected:
✅ Deputy MD ได้รับอีเมลแจ้งเตือน
✅ อีเมลมีข้อมูลครบถ้วน
✅ ตารางสถานะแสดงครบ 6 ระดับ
✅ Deputy MD highlight ด้วยสีเหลือง
```

**Test Case 9**: ไม่ส่งอีเมลถ้า Skip

```
1. สร้างคำขอโดย Skip Deputy MD
2. อนุมัติครบจนถึง Managing Director
3. เช็คว่ามีการส่งอีเมลหรือไม่

Expected:
✅ ไม่ส่งอีเมลไปที่ Deputy MD
✅ สถานะเปลี่ยนเป็น APPROVED โดยตรง
✅ Log แสดง "Skipped sending email (Skip Approver)"
```

#### 6. ทดสอบ Revise Flow

**Test Case 10**: Deputy MD ส่ง Revise

```
1. สร้างคำขอ
2. อนุมัติจนถึง Deputy MD
3. Deputy MD เลือก "Revise" พร้อมระบุความคิดเห็น
4. เช็คสถานะ

Expected:
✅ สถานะเปลี่ยนเป็น "Revision Admin"
✅ ผู้ยื่นคำขอได้รับอีเมลแจ้งให้แก้ไข
✅ ความคิดเห็นของ Deputy MD ถูกบันทึก
```

### SQL Queries สำหรับการทดสอบ

```sql
-- 1. ดูข้อมูล Deputy MD ทั้งหมด
SELECT
    Id,
    DocNo,
    DeputyManagingDirectorId,
    Status_DeputyManagingDirector,
    Comment_DeputyManagingDirector,
    ApproveInfo_DeputyManagingDirector,
    Status
FROM [dbo].[TrainingRequests]
WHERE IsActive = 1
ORDER BY CreatedDate DESC;

-- 2. หา Records ที่ Skip Deputy MD
SELECT
    DocNo,
    DeputyManagingDirectorId,
    Status
FROM [dbo].[TrainingRequests]
WHERE DeputyManagingDirectorId = 'ผู้บังคับบัญชาลำดับถัดไป อนุมัติ'
    AND IsActive = 1;

-- 3. หา Records เก่าที่มี Deputy MD = NULL
SELECT
    DocNo,
    DeputyManagingDirectorId,
    Status_DeputyManagingDirector,
    Status,
    CreatedDate
FROM [dbo].[TrainingRequests]
WHERE DeputyManagingDirectorId IS NULL
    AND IsActive = 1
ORDER BY CreatedDate DESC;

-- 4. เช็คสถานะทั้ง 6 ระดับ
SELECT
    DocNo,
    Status,
    Status_SectionManager AS [L1_SM],
    Status_DepartmentManager AS [L2_DM],
    Status_HRDAdmin AS [L3_HRD_Admin],
    Status_HRDConfirmation AS [L4_HRD_Conf],
    Status_ManagingDirector AS [L5_MD],
    Status_DeputyManagingDirector AS [L6_Deputy_MD]
FROM [dbo].[TrainingRequests]
WHERE IsActive = 1
ORDER BY CreatedDate DESC;
```

---

## สรุป

### การเปลี่ยนแปลงหลัก

1. **Database**: เพิ่ม 4 คอลัมน์สำหรับ Deputy Managing Director
2. **Models**: เพิ่ม 4 คุณสมบัติใน TrainingRequestEditViewModel
3. **Services**: อัพเดต Workflow Logic ให้ Deputy MD เป็นขั้นตอนสุดท้าย
4. **Controllers**: แก้ไข Insert, Select, และ Approval Logic
5. **Views**: เพิ่มส่วนแสดงผลและฟอร์มสำหรับ Deputy MD
6. **Email**: อัพเดตเทมเพลตให้แสดงครบ 6 ระดับ

### ฟีเจอร์สำคัญ

- ✅ Deputy Managing Director เป็นผู้อนุมัติขั้นสุดท้าย
- ✅ รองรับการข้ามขั้นตอน (Skip) ทุกระดับ ยกเว้น HRD
- ✅ Backward compatibility กับ Records เก่า
- ✅ Select2 AJAX Dropdowns พร้อม SKIP option
- ✅ ApprovalFlow Timeline แสดงครบ 6 ระดับ
- ✅ Email Notifications ครบถ้วน

### Commits ทั้งหมด

1. Initial Deputy MD implementation
2. Database migration scripts
3. Models update
4. Services logic update
5. Controllers update
6. Views - ApprovalFlow timeline
7. Views - Status initialization fix
8. Views - ApprovalFlow SKIP display
9. Views - SKIP options in dropdowns
10. Views - Consistent SKIP detection
11. Service - Backward compatibility fix
12. Controller - ApprovalFlow data loading fix
13. Views - Create form SKIP options in AJAX
14. Views - Edit form SKIP options in AJAX

---

**เอกสารนี้สร้างขึ้นเมื่อ**: 22 ธันวาคม 2568
**เวอร์ชัน**: 1.0
**ผู้เขียน**: Claude (AI Assistant)
**วัตถุประสงค์**: เอกสารประกอบการพัฒนาและบำรุงรักษาระบบ
