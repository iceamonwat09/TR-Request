-- =============================================
-- Migration Script: Add RemainingHours and RemainingCost columns
-- Date: 2025-11-16
-- Description: เพิ่ม columns สำหรับเก็บยอดคงเหลือของแต่ละพนักงาน
-- =============================================

USE [HRDSYSTEM];
GO

-- ตรวจสอบว่า columns มีอยู่แล้วหรือไม่
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'TrainingRequestEmployees'
    AND COLUMN_NAME = 'RemainingHours'
)
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees]
    ADD [RemainingHours] INT NULL;
    PRINT 'Column RemainingHours added successfully';
END
ELSE
BEGIN
    PRINT 'Column RemainingHours already exists';
END
GO

IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'TrainingRequestEmployees'
    AND COLUMN_NAME = 'RemainingCost'
)
BEGIN
    ALTER TABLE [dbo].[TrainingRequestEmployees]
    ADD [RemainingCost] DECIMAL(10,2) NULL;
    PRINT 'Column RemainingCost added successfully';
END
ELSE
BEGIN
    PRINT 'Column RemainingCost already exists';
END
GO

-- ตรวจสอบผลลัพธ์
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequestEmployees'
AND COLUMN_NAME IN ('RemainingHours', 'RemainingCost');
GO

PRINT 'Migration completed successfully!';
GO
