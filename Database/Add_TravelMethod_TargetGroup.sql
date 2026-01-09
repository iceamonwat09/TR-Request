-- =====================================================
-- Add TravelMethod and TargetGroup Columns
-- Purpose: เพิ่มช่อง การเดินทาง และ กลุ่มเป้าหมาย
-- Date: 2026-01-09
-- =====================================================

USE [HRDSYSTEM]
GO

-- เพิ่ม Column TravelMethod (การเดินทาง: จัดรถรับส่ง / เดินทางเอง)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'TrainingRequests' AND COLUMN_NAME = 'TravelMethod')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests]
    ADD [TravelMethod] NVARCHAR(20) NULL;
    PRINT '✅ Column TravelMethod added successfully!';
END
ELSE
BEGIN
    PRINT '⚠️ Column TravelMethod already exists.';
END
GO

-- เพิ่ม Column TargetGroup (กลุ่มเป้าหมาย)
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_NAME = 'TrainingRequests' AND COLUMN_NAME = 'TargetGroup')
BEGIN
    ALTER TABLE [dbo].[TrainingRequests]
    ADD [TargetGroup] NVARCHAR(200) NULL;
    PRINT '✅ Column TargetGroup added successfully!';
END
ELSE
BEGIN
    PRINT '⚠️ Column TargetGroup already exists.';
END
GO

PRINT '✅ Migration completed!';
GO
