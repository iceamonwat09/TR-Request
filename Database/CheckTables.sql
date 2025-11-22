-- ============================================
-- SQL Scripts สำหรับตรวจสอบและสร้าง Tables
-- ============================================

-- 1. ตรวจสอบว่า EmailLogs table มีหรือไม่
IF OBJECT_ID('[HRDSYSTEM].[dbo].[EmailLogs]', 'U') IS NOT NULL
    PRINT '✅ EmailLogs table EXISTS'
ELSE
    PRINT '❌ EmailLogs table NOT FOUND - ต้องสร้างก่อน!'
GO

-- 2. ตรวจสอบว่า ApprovalHistory table มีหรือไม่
IF OBJECT_ID('[HRDSYSTEM].[dbo].[ApprovalHistory]', 'U') IS NOT NULL
    PRINT '✅ ApprovalHistory table EXISTS'
ELSE
    PRINT '❌ ApprovalHistory table NOT FOUND - ต้องสร้างก่อน!'
GO

-- 3. ตรวจสอบข้อมูลใน EmailLogs
SELECT COUNT(*) AS EmailLogsCount FROM [HRDSYSTEM].[dbo].[EmailLogs];
GO

-- 4. ตรวจสอบข้อมูลใน ApprovalHistory
SELECT COUNT(*) AS ApprovalHistoryCount FROM [HRDSYSTEM].[dbo].[ApprovalHistory];
GO

-- 5. ตรวจสอบ Training Request ที่มีปัญหา
SELECT
    DocNo,
    Status,
    SectionManagerId,
    DepartmentManagerId,
    HRDAdminId,
    HRDConfirmationId,
    ManagingDirectorId,
    CreatedBy
FROM [HRDSYSTEM].[dbo].[TrainingRequests]
WHERE DocNo = 'PB-2025-11-032';
GO
