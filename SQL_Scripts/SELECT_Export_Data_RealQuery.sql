-- =====================================================
-- 📥 SELECT ข้อมูลจริงที่จะ Export (เหมือนระบบ)
-- วันที่: 2025-12-30
-- =====================================================

USE [HRDSYSTEM]
GO

-- =====================================================
-- 🔴 Query แบบเดิม (INNER JOIN) - ข้อมูลที่ระบบ Export ปัจจุบัน
-- =====================================================
PRINT '🔴 Query แบบเดิม (INNER JOIN - ข้อมูลที่ได้ตอนนี้):';
PRINT '========================================';

SELECT
    tr.DocNo AS [เลขที่เอกสาร],
    tr.Company AS [บริษัท],
    tr.TrainingType AS [ประเภทการอบรม],
    tr.Factory AS [โรงงาน],
    tr.CCEmail AS [CC Email],
    tr.Position AS [แผนก],
    tr.Department AS [ฝ่าย],
    tr.StartDate AS [วันที่เริ่ม],
    tr.EndDate AS [วันที่สิ้นสุด],
    tr.SeminarTitle AS [หัวข้ออบรม],
    tr.TrainingLocation AS [สถานที่อบรม],
    tr.Instructor AS [วิทยากร],
    tr.TotalCost AS [ค่าใช้จ่ายรวม],
    tr.CostPerPerson AS [ค่าใช้จ่ายต่อคน],
    tr.PerPersonTrainingHours AS [ชั่วโมงอบรมต่อคน],
    tr.TrainingObjective AS [วัตถุประสงค์],
    tr.OtherObjective AS [วัตถุประสงค์อื่นๆ],
    tr.URLSource AS [แหล่งข้อมูล],
    tr.AdditionalNotes AS [หมายเหตุเพิ่มเติม],
    tr.ExpectedOutcome AS [ผลที่คาดหวัง],
    tr.AttachedFilePath AS [ไฟล์แนบ],
    tr.Status AS [สถานะ],
    tr.CreatedDate AS [วันที่สร้าง],
    tr.CreatedBy AS [ผู้สร้าง],
    tr.UpdatedDate AS [วันที่แก้ไข],
    tr.UpdatedBy AS [ผู้แก้ไข],
    tr.RegistrationCost AS [ค่าลงทะเบียน],
    tr.InstructorFee AS [ค่าวิทยากร],
    tr.EquipmentCost AS [ค่าอุปกรณ์],
    tr.FoodCost AS [ค่าอาหาร],
    tr.OtherCost AS [ค่าใช้จ่ายอื่น],
    tr.OtherCostDescription AS [รายละเอียดค่าใช้จ่ายอื่น],
    tr.TotalPeople AS [จำนวนคนทั้งหมด],
    emp.EmployeeCode AS [รหัสพนักงาน],
    emp.EmployeeName AS [ชื่อพนักงาน],
    emp.Position AS [แผนกพนักงาน],
    emp.PreviousTrainingHours AS [ชั่วโมงอบรมก่อนหน้า],
    emp.PreviousTrainingCost AS [ค่าใช้จ่ายอบรมก่อนหน้า],
    emp.CurrentTrainingHours AS [ชั่วโมงอบรมปัจจุบัน],
    emp.CurrentTrainingCost AS [ค่าใช้จ่ายอบรมปัจจุบัน],
    emp.Notes AS [หมายเหตุพนักงาน],
    emp.[level] AS [ระดับ],
    emp.Department AS [ฝ่ายพนักงาน],
    emp.RemainingHours AS [ชั่วโมงคงเหลือ],
    emp.RemainingCost AS [ค่าใช้จ่ายคงเหลือ]
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= '2025-01-01'
  AND tr.StartDate <= '2025-12-31'
  AND tr.IsActive = 1
ORDER BY tr.CreatedDate DESC, emp.EmployeeCode;

PRINT '';
PRINT 'จำนวน Rows ที่ได้: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';
PRINT '';
PRINT '';

-- =====================================================
-- ✅ Query แบบแก้ไข (LEFT JOIN) - ข้อมูลที่ควรจะได้
-- =====================================================
PRINT '✅ Query แบบแก้ไข (LEFT JOIN - ข้อมูลที่ควรจะได้):';
PRINT '========================================';

SELECT
    tr.DocNo AS [เลขที่เอกสาร],
    tr.Company AS [บริษัท],
    tr.TrainingType AS [ประเภทการอบรม],
    tr.Factory AS [โรงงาน],
    tr.CCEmail AS [CC Email],
    tr.Position AS [แผนก],
    tr.Department AS [ฝ่าย],
    tr.StartDate AS [วันที่เริ่ม],
    tr.EndDate AS [วันที่สิ้นสุด],
    tr.SeminarTitle AS [หัวข้ออบรม],
    tr.TrainingLocation AS [สถานที่อบรม],
    tr.Instructor AS [วิทยากร],
    tr.TotalCost AS [ค่าใช้จ่ายรวม],
    tr.CostPerPerson AS [ค่าใช้จ่ายต่อคน],
    tr.PerPersonTrainingHours AS [ชั่วโมงอบรมต่อคน],
    tr.TrainingObjective AS [วัตถุประสงค์],
    tr.OtherObjective AS [วัตถุประสงค์อื่นๆ],
    tr.URLSource AS [แหล่งข้อมูล],
    tr.AdditionalNotes AS [หมายเหตุเพิ่มเติม],
    tr.ExpectedOutcome AS [ผลที่คาดหวัง],
    tr.AttachedFilePath AS [ไฟล์แนบ],
    tr.Status AS [สถานะ],
    tr.CreatedDate AS [วันที่สร้าง],
    tr.CreatedBy AS [ผู้สร้าง],
    tr.UpdatedDate AS [วันที่แก้ไข],
    tr.UpdatedBy AS [ผู้แก้ไข],
    tr.RegistrationCost AS [ค่าลงทะเบียน],
    tr.InstructorFee AS [ค่าวิทยากร],
    tr.EquipmentCost AS [ค่าอุปกรณ์],
    tr.FoodCost AS [ค่าอาหาร],
    tr.OtherCost AS [ค่าใช้จ่ายอื่น],
    tr.OtherCostDescription AS [รายละเอียดค่าใช้จ่ายอื่น],
    tr.TotalPeople AS [จำนวนคนทั้งหมด],
    ISNULL(emp.EmployeeCode, '') AS [รหัสพนักงาน],
    ISNULL(emp.EmployeeName, '') AS [ชื่อพนักงาน],
    ISNULL(emp.Position, '') AS [แผนกพนักงาน],
    ISNULL(emp.PreviousTrainingHours, 0) AS [ชั่วโมงอบรมก่อนหน้า],
    ISNULL(emp.PreviousTrainingCost, 0) AS [ค่าใช้จ่ายอบรมก่อนหน้า],
    ISNULL(emp.CurrentTrainingHours, 0) AS [ชั่วโมงอบรมปัจจุบัน],
    ISNULL(emp.CurrentTrainingCost, 0) AS [ค่าใช้จ่ายอบรมปัจจุบัน],
    ISNULL(emp.Notes, '') AS [หมายเหตุพนักงาน],
    ISNULL(emp.[level], '') AS [ระดับ],
    ISNULL(emp.Department, '') AS [ฝ่ายพนักงาน],
    ISNULL(emp.RemainingHours, 0) AS [ชั่วโมงคงเหลือ],
    ISNULL(emp.RemainingCost, 0) AS [ค่าใช้จ่ายคงเหลือ],
    -- เพิ่ม Column แสดงสถานะ
    CASE
        WHEN emp.Id IS NULL THEN '⚠️ ไม่มีข้อมูล Employee'
        ELSE '✅ มีข้อมูล Employee'
    END AS [สถานะข้อมูล]
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= '2025-01-01'
  AND tr.StartDate <= '2025-12-31'
  AND tr.IsActive = 1
ORDER BY tr.CreatedDate DESC, emp.EmployeeCode;

PRINT '';
PRINT 'จำนวน Rows ที่ได้: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';
PRINT '';
PRINT '';

-- =====================================================
-- 🔍 SELECT เฉพาะข้อมูลที่จะเพิ่มขึ้น (LEFT JOIN - INNER JOIN)
-- =====================================================
PRINT '🔍 ข้อมูลที่จะได้เพิ่มขึ้นถ้าใช้ LEFT JOIN:';
PRINT '========================================';

SELECT
    tr.Id,
    tr.DocNo AS [เลขที่เอกสาร],
    tr.Department AS [ฝ่าย],
    tr.SeminarTitle AS [หัวข้ออบรม],
    tr.StartDate AS [วันที่เริ่ม],
    tr.EndDate AS [วันที่สิ้นสุด],
    tr.Status AS [สถานะ],
    tr.TotalCost AS [ค่าใช้จ่ายรวม],
    tr.TotalPeople AS [จำนวนคน],
    tr.CreatedDate AS [วันที่สร้าง],
    tr.CreatedBy AS [ผู้สร้าง],
    '⚠️ ไม่มีข้อมูล Employee - จะหายใน INNER JOIN' AS [หมายเหตุ]
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= '2025-01-01'
  AND tr.StartDate <= '2025-12-31'
  AND tr.IsActive = 1
  AND emp.Id IS NULL  -- Records ที่ไม่มี Employee
ORDER BY tr.CreatedDate DESC;

PRINT '';
PRINT 'จำนวนที่จะได้เพิ่ม: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
PRINT '';
PRINT '';
PRINT '';

-- =====================================================
-- 📊 SELECT สรุปเปรียบเทียบจำนวนข้อมูล
-- =====================================================
PRINT '📊 สรุปเปรียบเทียบจำนวนข้อมูล:';
PRINT '========================================';

DECLARE @InnerCount INT, @LeftCount INT;

SELECT @InnerCount = COUNT(*)
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= '2025-01-01'
  AND tr.StartDate <= '2025-12-31'
  AND tr.IsActive = 1;

SELECT @LeftCount = COUNT(*)
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= '2025-01-01'
  AND tr.StartDate <= '2025-12-31'
  AND tr.IsActive = 1;

SELECT
    'INNER JOIN (ระบบปัจจุบัน)' AS [Query Type],
    @InnerCount AS [จำนวน Rows]
UNION ALL
SELECT
    'LEFT JOIN (ที่ควรจะเป็น)' AS [Query Type],
    @LeftCount AS [จำนวน Rows]
UNION ALL
SELECT
    'ผลต่าง (ข้อมูลที่หาย)' AS [Query Type],
    (@LeftCount - @InnerCount) AS [จำนวน Rows];

PRINT '';
PRINT '✅ เสร็จสิ้น';
