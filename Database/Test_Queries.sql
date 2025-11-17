-- =============================================
-- SQL Queries สำหรับทดสอบและตรวจสอบข้อมูล
-- Date: 2025-11-17
-- =============================================

USE [HRDSYSTEM];
GO

-- =============================================
-- 1. ตรวจสอบว่า columns RemainingHours และ RemainingCost มีหรือยัง
-- =============================================
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequestEmployees'
AND COLUMN_NAME IN ('RemainingHours', 'RemainingCost');
GO

-- =============================================
-- 2. ดูข้อมูลพนักงานทั้งหมดในฟอร์มล่าสุด (รวม Remaining)
-- =============================================
SELECT TOP 10
    tre.TrainingRequestId,
    tr.DocNo,
    tr.TrainingTitle,
    tr.Department,
    tr.TrainingType,
    tr.Status,
    tr.CreatedDate,
    tre.EmployeeCode,
    tre.EmployeeName,
    tre.CurrentTrainingHours,
    tre.CurrentTrainingCost,
    tre.RemainingHours,
    tre.RemainingCost
FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr
    ON tre.TrainingRequestId = tr.TrainingRequestId
ORDER BY tr.CreatedDate DESC;
GO

-- =============================================
-- 3. ดูฟอร์มที่มีพนักงานหลายคน (เพื่อทดสอบ accumulated calculation)
-- =============================================
SELECT
    tr.DocNo,
    tr.TrainingTitle,
    tr.Department,
    tr.TrainingType,
    tr.Status,
    COUNT(tre.EmployeeCode) AS EmployeeCount,
    SUM(tre.CurrentTrainingCost) AS TotalCost,
    SUM(tre.CurrentTrainingHours) AS TotalHours
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
    ON tr.TrainingRequestId = tre.TrainingRequestId
WHERE tr.TrainingType = 'PUBLIC'
GROUP BY tr.DocNo, tr.TrainingTitle, tr.Department, tr.TrainingType, tr.Status
HAVING COUNT(tre.EmployeeCode) >= 2
ORDER BY tr.CreatedDate DESC;
GO

-- =============================================
-- 4. ดูรายละเอียดพนักงานในฟอร์มเฉพาะ (แทนที่ DocNo)
-- =============================================
DECLARE @DocNo VARCHAR(50) = 'PB-2025-11-017'; -- ⬅️ เปลี่ยนเป็นเลขฟอร์มที่ต้องการดู

SELECT
    ROW_NUMBER() OVER (ORDER BY tre.EmployeeCode) AS RowNum,
    tre.EmployeeCode,
    tre.EmployeeName,
    tre.Position,
    tre.Department,
    tre.PreviousTrainingHours,
    tre.PreviousTrainingCost,
    tre.CurrentTrainingHours,
    tre.CurrentTrainingCost,
    tre.RemainingHours,
    tre.RemainingCost,
    tre.Notes
FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr
    ON tre.TrainingRequestId = tr.TrainingRequestId
WHERE tr.DocNo = @DocNo
ORDER BY tre.EmployeeCode;
GO

-- =============================================
-- 5. ดูข้อมูลฝ่ายและโควต้า (จาก Master_Department)
-- =============================================
SELECT
    DepartmentName,
    Qhours,
    Quota
FROM [HRDSYSTEM].[dbo].[Master_Department]
WHERE DepartmentName IS NOT NULL
ORDER BY DepartmentName;
GO

-- =============================================
-- 6. คำนวณยอดใช้ไปของฝ่ายในปีนี้ (Status = APPROVED/RESCHEDULED/COMPLETE)
-- =============================================
DECLARE @Department VARCHAR(100) = 'Production'; -- ⬅️ เปลี่ยนเป็นชื่อฝ่ายที่ต้องการดู
DECLARE @Year INT = YEAR(GETDATE());

SELECT
    tr.Department,
    COUNT(DISTINCT tr.DocNo) AS TotalForms,
    COUNT(tre.EmployeeCode) AS TotalEmployees,
    SUM(tre.CurrentTrainingHours) AS TotalHoursUsed,
    SUM(tre.CurrentTrainingCost) AS TotalCostUsed
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
    ON tr.TrainingRequestId = tre.TrainingRequestId
WHERE tr.Department = @Department
    AND YEAR(tr.TrainingDate) = @Year
    AND tr.Status IN ('APPROVED', 'RESCHEDULED', 'COMPLETE')
    AND tr.TrainingType = 'PUBLIC'
GROUP BY tr.Department;
GO

-- =============================================
-- 7. เปรียบเทียบโควต้ากับยอดใช้ไปของแต่ละฝ่าย
-- =============================================
SELECT
    md.DepartmentName,
    md.Qhours AS QuotaHours,
    md.Quota AS QuotaCost,
    ISNULL(SUM(tre.CurrentTrainingHours), 0) AS UsedHours,
    ISNULL(SUM(tre.CurrentTrainingCost), 0) AS UsedCost,
    md.Qhours - ISNULL(SUM(tre.CurrentTrainingHours), 0) AS RemainingHours,
    md.Quota - ISNULL(SUM(tre.CurrentTrainingCost), 0) AS RemainingCost
FROM [HRDSYSTEM].[dbo].[Master_Department] md
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr
    ON md.DepartmentName = tr.Department
    AND YEAR(tr.TrainingDate) = YEAR(GETDATE())
    AND tr.Status IN ('APPROVED', 'RESCHEDULED', 'COMPLETE')
    AND tr.TrainingType = 'PUBLIC'
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
    ON tr.TrainingRequestId = tre.TrainingRequestId
GROUP BY md.DepartmentName, md.Qhours, md.Quota
ORDER BY md.DepartmentName;
GO

-- =============================================
-- 8. ตรวจสอบฟอร์มที่ RemainingHours/RemainingCost เป็น NULL
-- =============================================
SELECT
    tr.DocNo,
    tr.TrainingTitle,
    tr.Department,
    tr.CreatedDate,
    COUNT(tre.EmployeeCode) AS TotalEmployees,
    COUNT(CASE WHEN tre.RemainingHours IS NULL THEN 1 END) AS NullRemainingHours,
    COUNT(CASE WHEN tre.RemainingCost IS NULL THEN 1 END) AS NullRemainingCost
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
    ON tr.TrainingRequestId = tre.TrainingRequestId
GROUP BY tr.DocNo, tr.TrainingTitle, tr.Department, tr.CreatedDate
HAVING COUNT(CASE WHEN tre.RemainingHours IS NULL THEN 1 END) > 0
    OR COUNT(CASE WHEN tre.RemainingCost IS NULL THEN 1 END) > 0
ORDER BY tr.CreatedDate DESC;
GO

-- =============================================
-- 9. ดูข้อมูลพนักงานที่มี Remaining เป็นค่าลบ
-- =============================================
SELECT
    tr.DocNo,
    tr.Department,
    tre.EmployeeCode,
    tre.EmployeeName,
    tre.CurrentTrainingCost,
    tre.RemainingCost,
    tre.CurrentTrainingHours,
    tre.RemainingHours
FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr
    ON tre.TrainingRequestId = tr.TrainingRequestId
WHERE (tre.RemainingCost < 0 OR tre.RemainingHours < 0)
    AND tre.RemainingCost IS NOT NULL
ORDER BY tr.CreatedDate DESC;
GO

-- =============================================
-- 10. ทดสอบคำนวณ Accumulated Pattern (Manual Calculation)
-- =============================================
DECLARE @TestDocNo VARCHAR(50) = 'PB-2025-11-017'; -- ⬅️ เปลี่ยนเป็นเลขฟอร์มที่ต้องการทดสอบ

-- ดึงข้อมูลฝ่ายและโควต้า
DECLARE @TestDept VARCHAR(100);
DECLARE @TestQuota DECIMAL(10,2);
DECLARE @TestQhours INT;

SELECT
    @TestDept = tr.Department,
    @TestQuota = md.Quota,
    @TestQhours = md.Qhours
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[Master_Department] md
    ON tr.Department = md.DepartmentName
WHERE tr.DocNo = @TestDocNo;

-- ดึงยอดใช้ไปของฝ่าย (ไม่รวมฟอร์มนี้)
DECLARE @DeptUsedCost DECIMAL(10,2);
DECLARE @DeptUsedHours INT;

SELECT
    @DeptUsedCost = ISNULL(SUM(tre.CurrentTrainingCost), 0),
    @DeptUsedHours = ISNULL(SUM(tre.CurrentTrainingHours), 0)
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
    ON tr.TrainingRequestId = tre.TrainingRequestId
WHERE tr.Department = @TestDept
    AND YEAR(tr.TrainingDate) = YEAR(GETDATE())
    AND tr.Status IN ('APPROVED', 'RESCHEDULED', 'COMPLETE')
    AND tr.DocNo != @TestDocNo;

-- คำนวณ Remaining แบบ Accumulated
WITH EmployeeAccumulated AS (
    SELECT
        tre.EmployeeCode,
        tre.EmployeeName,
        tre.CurrentTrainingHours,
        tre.CurrentTrainingCost,
        tre.RemainingHours AS SavedRemainingHours,
        tre.RemainingCost AS SavedRemainingCost,
        SUM(tre.CurrentTrainingCost) OVER (
            ORDER BY tre.EmployeeCode
            ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
        ) AS AccumulatedCostBefore,
        SUM(tre.CurrentTrainingHours) OVER (
            ORDER BY tre.EmployeeCode
            ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
        ) AS AccumulatedHoursBefore
    FROM [HRDSYSTEM].[dbo].[TrainingRequestEmployees] tre
    INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequests] tr
        ON tre.TrainingRequestId = tr.TrainingRequestId
    WHERE tr.DocNo = @TestDocNo
)
SELECT
    EmployeeCode,
    EmployeeName,
    CurrentTrainingHours,
    CurrentTrainingCost,
    SavedRemainingHours,
    SavedRemainingCost,
    -- คำนวณใหม่
    @TestQhours - @DeptUsedHours - ISNULL(AccumulatedHoursBefore, 0) - CurrentTrainingHours AS CalculatedRemainingHours,
    @TestQuota - @DeptUsedCost - ISNULL(AccumulatedCostBefore, 0) - CurrentTrainingCost AS CalculatedRemainingCost,
    -- เปรียบเทียบ
    CASE
        WHEN SavedRemainingHours = @TestQhours - @DeptUsedHours - ISNULL(AccumulatedHoursBefore, 0) - CurrentTrainingHours
        THEN '✅ ตรงกัน'
        ELSE '❌ ไม่ตรงกัน'
    END AS HoursMatch,
    CASE
        WHEN SavedRemainingCost = @TestQuota - @DeptUsedCost - ISNULL(AccumulatedCostBefore, 0) - CurrentTrainingCost
        THEN '✅ ตรงกัน'
        ELSE '❌ ไม่ตรงกัน'
    END AS CostMatch
FROM EmployeeAccumulated
ORDER BY EmployeeCode;

-- แสดงข้อมูลสรุป
SELECT
    'Department' AS Info,
    @TestDept AS Value
UNION ALL
SELECT 'Quota Hours', CAST(@TestQhours AS VARCHAR)
UNION ALL
SELECT 'Quota Cost', CAST(@TestQuota AS VARCHAR)
UNION ALL
SELECT 'Used Hours (Dept, excluding this form)', CAST(@DeptUsedHours AS VARCHAR)
UNION ALL
SELECT 'Used Cost (Dept, excluding this form)', CAST(@DeptUsedCost AS VARCHAR);
GO

PRINT 'All test queries completed!';
GO
