-- แก้ไขปัญหา EmailLogs table ไม่มี DocNo column

USE [HRDSYSTEM];
GO

-- ตรวจสอบว่า EmailLogs table มี column อะไรบ้าง
PRINT '=== EmailLogs Table Current Schema ===';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EmailLogs'
ORDER BY ORDINAL_POSITION;
GO

-- เพิ่ม DocNo column ถ้ายังไม่มี
IF NOT EXISTS (
    SELECT 1
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'EmailLogs'
      AND COLUMN_NAME = 'DocNo'
)
BEGIN
    PRINT '';
    PRINT '⚠️ DocNo column ไม่พบ - กำลังเพิ่ม...';

    ALTER TABLE [HRDSYSTEM].[dbo].[EmailLogs]
    ADD [DocNo] NVARCHAR(20) NULL;

    -- สร้าง Index สำหรับ DocNo
    CREATE INDEX [IX_EmailLogs_DocNo]
        ON [dbo].[EmailLogs]([DocNo]);

    PRINT '✅ เพิ่ม DocNo column และ Index สำเร็จ!';
END
ELSE
BEGIN
    PRINT '';
    PRINT '✅ DocNo column มีอยู่แล้ว';
END
GO

-- ตรวจสอบผลลัพธ์
PRINT '';
PRINT '=== EmailLogs Table Schema After Fix ===';
SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'EmailLogs'
ORDER BY ORDINAL_POSITION;
GO

-- แสดงข้อมูลใน EmailLogs (ถ้ามี)
PRINT '';
PRINT '=== Current EmailLogs Records ===';
SELECT TOP 10 * FROM EmailLogs ORDER BY SentDate DESC;
GO
