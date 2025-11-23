-- แก้ไขปัญหา "String or binary data would be truncated"
-- เพิ่มขนาด Status column ให้รองรับค่า status ที่ยาวขึ้น

USE [HRDSYSTEM];
GO

-- ตรวจสอบขนาดปัจจุบันของ Status column
PRINT '=== ตรวจสอบขนาด Status Column ปัจจุบัน ===';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequests'
  AND COLUMN_NAME = 'Status';
GO

-- ตรวจสอบขนาด UpdatedBy column
PRINT '';
PRINT '=== ตรวจสอบขนาด UpdatedBy Column ปัจจุบัน ===';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequests'
  AND COLUMN_NAME = 'UpdatedBy';
GO

-- เพิ่มขนาด Status column เป็น VARCHAR(100)
PRINT '';
PRINT '=== เปลี่ยนขนาด Status Column เป็น VARCHAR(100) ===';
ALTER TABLE [HRDSYSTEM].[dbo].[TrainingRequests]
ALTER COLUMN [Status] VARCHAR(100) NULL;
GO

-- เพิ่มขนาด UpdatedBy column เป็น VARCHAR(100) (ถ้ามี)
IF EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'TrainingRequests'
      AND COLUMN_NAME = 'UpdatedBy'
)
BEGIN
    PRINT '=== เปลี่ยนขนาด UpdatedBy Column เป็น VARCHAR(100) ===';
    ALTER TABLE [HRDSYSTEM].[dbo].[TrainingRequests]
    ALTER COLUMN [UpdatedBy] VARCHAR(100) NULL;
END
ELSE
BEGIN
    PRINT '⚠️ UpdatedBy column ไม่มีในตาราง - ต้องสร้างใหม่';
    -- เพิ่ม UpdatedBy column ใหม่
    ALTER TABLE [HRDSYSTEM].[dbo].[TrainingRequests]
    ADD [UpdatedBy] VARCHAR(100) NULL;
    PRINT '✅ เพิ่ม UpdatedBy column สำเร็จ';
END
GO

-- ตรวจสอบผลลัพธ์
PRINT '';
PRINT '=== ผลลัพธ์หลังแก้ไข ===';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'TrainingRequests'
  AND COLUMN_NAME IN ('Status', 'UpdatedBy')
ORDER BY COLUMN_NAME;
GO

-- ทดสอบ UPDATE
PRINT '';
PRINT '=== ทดสอบ UPDATE Statement ===';
BEGIN TRY
    UPDATE [HRDSYSTEM].[dbo].[TrainingRequests]
    SET Status = 'WAITING_FOR_SECTION_MANAGER',
        UpdatedDate = GETDATE(),
        UpdatedBy = 'SYSTEM'
    WHERE DocNo = 'PB-2025-11-032';

    PRINT '✅ UPDATE สำเร็จ!';

    -- แสดงผลลัพธ์
    SELECT DocNo, Status, UpdatedBy, UpdatedDate
    FROM TrainingRequests
    WHERE DocNo = 'PB-2025-11-032';
END TRY
BEGIN CATCH
    PRINT '❌ UPDATE ล้มเหลว!';
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH;
GO
