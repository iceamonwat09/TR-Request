-- =====================================================
-- ✅ Query ที่แก้ไขแล้ว สำหรับ Export CSV
-- แก้ไขปัญหา: INNER JOIN → LEFT JOIN
-- วันที่: 2025-12-30
-- =====================================================

USE [HRDSYSTEM]
GO

-- =====================================================
-- ⚠️ Query เดิม (มีปัญหา - ใช้ INNER JOIN)
-- =====================================================
PRINT '🔴 Query เดิม (INNER JOIN - มีปัญหา):';
PRINT '----------------------------------------';

-- ตัวอย่าง Parameters
DECLARE @StartDate DATE = '2025-01-01';
DECLARE @EndDate DATE = '2025-12-31';
DECLARE @Department NVARCHAR(100) = NULL; -- NULL = ทุกฝ่าย

-- Query เดิม
SELECT
    tr.DocNo, tr.Company, tr.TrainingType, tr.Factory, tr.CCEmail,
    tr.Position, tr.Department, tr.StartDate, tr.EndDate, tr.SeminarTitle,
    tr.TrainingLocation, tr.Instructor, tr.TotalCost, tr.CostPerPerson,
    tr.PerPersonTrainingHours, tr.TrainingObjective, tr.OtherObjective,
    tr.URLSource, tr.AdditionalNotes, tr.ExpectedOutcome, tr.AttachedFilePath,
    tr.Status, tr.CreatedDate, tr.CreatedBy, tr.UpdatedDate, tr.UpdatedBy,
    tr.RegistrationCost, tr.InstructorFee, tr.EquipmentCost, tr.FoodCost,
    tr.OtherCost, tr.OtherCostDescription, tr.TotalPeople,
    emp.EmployeeCode, emp.EmployeeName, emp.Position AS EmployeePosition,
    emp.PreviousTrainingHours, emp.PreviousTrainingCost,
    emp.CurrentTrainingHours, emp.CurrentTrainingCost, emp.Notes,
    emp.Level, emp.Department AS EmployeeDepartment,
    emp.RemainingHours, emp.RemainingCost
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp  -- ⚠️ ปัญหาตรงนี้
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @StartDate
  AND tr.StartDate <= @EndDate
  AND tr.IsActive = 1
  AND (@Department IS NULL OR tr.Department = @Department)
ORDER BY tr.CreatedDate DESC, emp.EmployeeCode;

PRINT '';
PRINT 'จำนวน Rows (INNER JOIN): ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';
PRINT '';

-- =====================================================
-- ✅ Query ที่แก้ไขแล้ว (ใช้ LEFT JOIN)
-- =====================================================
PRINT '✅ Query ที่แก้ไขแล้ว (LEFT JOIN - ถูกต้อง):';
PRINT '----------------------------------------';

SELECT
    tr.DocNo, tr.Company, tr.TrainingType, tr.Factory, tr.CCEmail,
    tr.Position, tr.Department, tr.StartDate, tr.EndDate, tr.SeminarTitle,
    tr.TrainingLocation, tr.Instructor, tr.TotalCost, tr.CostPerPerson,
    tr.PerPersonTrainingHours, tr.TrainingObjective, tr.OtherObjective,
    tr.URLSource, tr.AdditionalNotes, tr.ExpectedOutcome, tr.AttachedFilePath,
    tr.Status, tr.CreatedDate, tr.CreatedBy, tr.UpdatedDate, tr.UpdatedBy,
    tr.RegistrationCost, tr.InstructorFee, tr.EquipmentCost, tr.FoodCost,
    tr.OtherCost, tr.OtherCostDescription, tr.TotalPeople,

    -- ✅ ใช้ ISNULL เพื่อจัดการกรณีไม่มีข้อมูล Employee
    ISNULL(emp.EmployeeCode, '') AS EmployeeCode,
    ISNULL(emp.EmployeeName, '') AS EmployeeName,
    ISNULL(emp.Position, '') AS EmployeePosition,
    ISNULL(emp.PreviousTrainingHours, 0) AS PreviousTrainingHours,
    ISNULL(emp.PreviousTrainingCost, 0) AS PreviousTrainingCost,
    ISNULL(emp.CurrentTrainingHours, 0) AS CurrentTrainingHours,
    ISNULL(emp.CurrentTrainingCost, 0) AS CurrentTrainingCost,
    ISNULL(emp.Notes, '') AS Notes,
    ISNULL(emp.[level], '') AS Level,  -- ✅ แก้ไข case sensitivity ใช้ [level]
    ISNULL(emp.Department, '') AS EmployeeDepartment,
    ISNULL(emp.RemainingHours, 0) AS RemainingHours,
    ISNULL(emp.RemainingCost, 0) AS RemainingCost

FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp  -- ✅ เปลี่ยนเป็น LEFT JOIN
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @StartDate
  AND tr.StartDate <= @EndDate
  AND tr.IsActive = 1
  AND (@Department IS NULL OR tr.Department = @Department)
ORDER BY tr.CreatedDate DESC, emp.EmployeeCode;

PRINT '';
PRINT 'จำนวน Rows (LEFT JOIN): ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';

-- =====================================================
-- 📊 เปรียบเทียบผลลัพธ์
-- =====================================================
PRINT '';
PRINT '========================================';
PRINT '📊 เปรียบเทียบผลลัพธ์';
PRINT '========================================';

DECLARE @InnerCount INT, @LeftCount INT, @Diff INT;

-- นับ INNER JOIN
SELECT @InnerCount = COUNT(*)
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @StartDate
  AND tr.StartDate <= @EndDate
  AND tr.IsActive = 1
  AND (@Department IS NULL OR tr.Department = @Department);

-- นับ LEFT JOIN
SELECT @LeftCount = COUNT(*)
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @StartDate
  AND tr.StartDate <= @EndDate
  AND tr.IsActive = 1
  AND (@Department IS NULL OR tr.Department = @Department);

SET @Diff = @LeftCount - @InnerCount;

PRINT 'INNER JOIN (Query เดิม):        ' + CAST(@InnerCount AS NVARCHAR(10)) + ' rows';
PRINT 'LEFT JOIN (Query แก้ไข):        ' + CAST(@LeftCount AS NVARCHAR(10)) + ' rows';
PRINT 'ผลต่าง (ข้อมูลที่จะครบขึ้น):   ' + CAST(@Diff AS NVARCHAR(10)) + ' rows';
PRINT '';

IF @Diff > 0
BEGIN
    PRINT '✅ Query แก้ไขจะทำให้ได้ข้อมูลครบมากกว่าเดิม ' + CAST(@Diff AS NVARCHAR(10)) + ' rows';
END
ELSE
BEGIN
    PRINT '✅ ข้อมูลทุก TrainingRequest มี Employee อยู่แล้ว';
END

PRINT '';
PRINT '========================================';

-- =====================================================
-- 🎯 Query สำหรับแสดงข้อมูลที่จะได้เพิ่มขึ้น
-- =====================================================
PRINT '';
PRINT '🎯 ข้อมูลที่จะได้เพิ่มขึ้นจาก LEFT JOIN:';
PRINT '----------------------------------------';

SELECT
    tr.Id,
    tr.DocNo,
    tr.Department,
    tr.SeminarTitle,
    tr.StartDate,
    tr.Status,
    tr.TotalCost,
    tr.CreatedBy,
    '⚠️ ไม่มีข้อมูล Employee' AS Note
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @StartDate
  AND tr.StartDate <= @EndDate
  AND tr.IsActive = 1
  AND (@Department IS NULL OR tr.Department = @Department)
  AND emp.Id IS NULL  -- Records ที่ไม่มี Employee
ORDER BY tr.CreatedDate DESC;

PRINT '';
PRINT 'จำนวน Records ที่ไม่มีข้อมูล Employee: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));
PRINT '';
