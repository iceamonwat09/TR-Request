-- =====================================================
-- Add Deputy Managing Director to TrainingRequests
-- Purpose: เพิ่มลำดับการอนุมัติของรองกรรมการผู้จัดการ (ท้ายสุด)
-- Date: 2025-12-19
-- =====================================================

USE [HRDSYSTEM]
GO

-- ตรวจสอบว่า columns ยังไม่มีอยู่ก่อน
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'TrainingRequests'
    AND COLUMN_NAME = 'DeputyManagingDirectorId'
)
BEGIN
    PRINT '🔄 Adding Deputy Managing Director columns...'

    -- เพิ่ม 4 columns สำหรับ Deputy Managing Director
    ALTER TABLE [dbo].[TrainingRequests]
    ADD
        DeputyManagingDirectorId NVARCHAR(100) NULL,
        Status_DeputyManagingDirector NVARCHAR(20) NULL,
        Comment_DeputyManagingDirector NVARCHAR(500) NULL,
        ApproveInfo_DeputyManagingDirector NVARCHAR(200) NULL;

    PRINT '✅ Deputy Managing Director columns added successfully!'
END
ELSE
BEGIN
    PRINT '⚠️ Deputy Managing Director columns already exist - skipping...'
END
GO

-- ตรวจสอบผลลัพธ์
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequests'
    AND COLUMN_NAME LIKE '%DeputyManaging%'
ORDER BY ORDINAL_POSITION;
GO

PRINT '✅ Migration completed successfully!'
GO
