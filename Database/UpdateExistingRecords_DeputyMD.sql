-- =====================================================
-- Update Existing Training Requests with Deputy MD Status
-- Purpose: Set Status_DeputyManagingDirector = 'Pending' for all existing records
-- Date: 2025-12-20
-- =====================================================

USE [HRDSYSTEM]
GO

PRINT '🔄 Updating existing TrainingRequests with Deputy MD status...'

-- Update all existing records to have Pending status for Deputy MD
-- (and other levels that might be missing)
UPDATE [dbo].[TrainingRequests]
SET
    Status_SectionManager = ISNULL(Status_SectionManager, 'Pending'),
    Status_DepartmentManager = ISNULL(Status_DepartmentManager, 'Pending'),
    Status_HRDAdmin = ISNULL(Status_HRDAdmin, 'Pending'),
    Status_HRDConfirmation = ISNULL(Status_HRDConfirmation, 'Pending'),
    Status_ManagingDirector = ISNULL(Status_ManagingDirector, 'Pending'),
    Status_DeputyManagingDirector = ISNULL(Status_DeputyManagingDirector, 'Pending')
WHERE IsActive = 1;

-- แสดงผลลัพธ์
DECLARE @UpdatedCount INT = @@ROWCOUNT;
PRINT '✅ Updated ' + CAST(@UpdatedCount AS NVARCHAR(10)) + ' records'

-- ตรวจสอบผลลัพธ์
SELECT
    COUNT(*) AS TotalRecords,
    SUM(CASE WHEN Status_DeputyManagingDirector = 'Pending' THEN 1 ELSE 0 END) AS DeputyMD_Pending,
    SUM(CASE WHEN Status_DeputyManagingDirector IS NULL THEN 1 ELSE 0 END) AS DeputyMD_Null,
    SUM(CASE WHEN Status_DeputyManagingDirector = 'APPROVED' THEN 1 ELSE 0 END) AS DeputyMD_Approved
FROM [dbo].[TrainingRequests]
WHERE IsActive = 1;

PRINT '✅ Migration completed successfully!'
GO
