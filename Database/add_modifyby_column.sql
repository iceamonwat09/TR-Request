-- =============================================
-- Script: Add ModifyBy column to TrainingRequest_Cost table
-- Date: 2025-12-30
-- Description: Add ModifyBy column to track who modified the quota and when
--              Format: email / DD/MM/YYYY / HH:MM
-- =============================================

USE [HRDSYSTEM]
GO

-- Check if ModifyBy column exists, if not add it
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_SCHEMA = 'dbo'
    AND TABLE_NAME = 'TrainingRequest_Cost'
    AND COLUMN_NAME = 'ModifyBy'
)
BEGIN
    ALTER TABLE [dbo].[TrainingRequest_Cost]
    ADD [ModifyBy] NVARCHAR(200) NULL

    PRINT 'Column ModifyBy added successfully to TrainingRequest_Cost table'
END
ELSE
BEGIN
    PRINT 'Column ModifyBy already exists in TrainingRequest_Cost table'
END
GO

-- Verify the column was added
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
  AND TABLE_NAME = 'TrainingRequest_Cost'
  AND COLUMN_NAME IN ('CreatedBy', 'ModifyBy')
ORDER BY ORDINAL_POSITION
GO
