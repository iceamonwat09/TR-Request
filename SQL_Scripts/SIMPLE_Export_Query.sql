-- =====================================================
-- 📥 Query ง่ายๆ สำหรับ SELECT ข้อมูล Export
-- Copy ไป Run ใน SQL Server Management Studio ได้เลย
-- =====================================================

USE [HRDSYSTEM]
GO

-- ตั้งค่า Parameters (แก้ไขตามต้องการ)
DECLARE @StartDate DATE = '2025-01-01';
DECLARE @EndDate DATE = '2025-12-31';
DECLARE @Department NVARCHAR(100) = NULL; -- NULL = ทุกฝ่าย, หรือใส่ชื่อฝ่าย เช่น 'IT'

-- =====================================================
-- 🔴 Query แบบเดิม (INNER JOIN) - ข้อมูลที่ระบบ Export ตอนนี้
-- =====================================================
PRINT '=== Query แบบเดิม (INNER JOIN) ===';

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
    emp.[level], emp.Department AS EmployeeDepartment,
    emp.RemainingHours, emp.RemainingCost
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
INNER JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @StartDate
  AND tr.StartDate <= @EndDate
  AND tr.IsActive = 1
  AND (@Department IS NULL OR tr.Department = @Department)
ORDER BY tr.CreatedDate DESC, emp.EmployeeCode;

PRINT 'จำนวน: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
PRINT '';

-- =====================================================
-- ✅ Query แบบแก้ไข (LEFT JOIN) - ข้อมูลที่ควรจะได้
-- =====================================================
PRINT '=== Query แบบแก้ไข (LEFT JOIN) ===';

SELECT
    tr.DocNo, tr.Company, tr.TrainingType, tr.Factory, tr.CCEmail,
    tr.Position, tr.Department, tr.StartDate, tr.EndDate, tr.SeminarTitle,
    tr.TrainingLocation, tr.Instructor, tr.TotalCost, tr.CostPerPerson,
    tr.PerPersonTrainingHours, tr.TrainingObjective, tr.OtherObjective,
    tr.URLSource, tr.AdditionalNotes, tr.ExpectedOutcome, tr.AttachedFilePath,
    tr.Status, tr.CreatedDate, tr.CreatedBy, tr.UpdatedDate, tr.UpdatedBy,
    tr.RegistrationCost, tr.InstructorFee, tr.EquipmentCost, tr.FoodCost,
    tr.OtherCost, tr.OtherCostDescription, tr.TotalPeople,
    ISNULL(emp.EmployeeCode, '') AS EmployeeCode,
    ISNULL(emp.EmployeeName, '') AS EmployeeName,
    ISNULL(emp.Position, '') AS EmployeePosition,
    ISNULL(emp.PreviousTrainingHours, 0) AS PreviousTrainingHours,
    ISNULL(emp.PreviousTrainingCost, 0) AS PreviousTrainingCost,
    ISNULL(emp.CurrentTrainingHours, 0) AS CurrentTrainingHours,
    ISNULL(emp.CurrentTrainingCost, 0) AS CurrentTrainingCost,
    ISNULL(emp.Notes, '') AS Notes,
    ISNULL(emp.[level], '') AS Level,
    ISNULL(emp.Department, '') AS EmployeeDepartment,
    ISNULL(emp.RemainingHours, 0) AS RemainingHours,
    ISNULL(emp.RemainingCost, 0) AS RemainingCost
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @StartDate
  AND tr.StartDate <= @EndDate
  AND tr.IsActive = 1
  AND (@Department IS NULL OR tr.Department = @Department)
ORDER BY tr.CreatedDate DESC, emp.EmployeeCode;

PRINT 'จำนวน: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
PRINT '';

-- =====================================================
-- 🔍 ดูข้อมูลที่จะได้เพิ่มขึ้น
-- =====================================================
PRINT '=== Records ที่ไม่มีข้อมูล Employee (จะหายใน INNER JOIN) ===';

SELECT
    tr.Id, tr.DocNo, tr.Department, tr.SeminarTitle,
    tr.StartDate, tr.Status, tr.TotalCost, tr.CreatedBy
FROM [HRDSYSTEM].[dbo].[TrainingRequests] tr
LEFT JOIN [HRDSYSTEM].[dbo].[TrainingRequestEmployees] emp
    ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @StartDate
  AND tr.StartDate <= @EndDate
  AND tr.IsActive = 1
  AND (@Department IS NULL OR tr.Department = @Department)
  AND emp.Id IS NULL
ORDER BY tr.CreatedDate DESC;

PRINT 'จำนวนที่จะหาย: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' rows';
