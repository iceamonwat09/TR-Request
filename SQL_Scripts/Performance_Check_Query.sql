-- =====================================================
-- 🚀 Query สำหรับตรวจสอบ Performance และ Data Volume
-- วัตถุประสงค์: ประเมินว่าควรใช้ Paging หรือไม่
-- วันที่: 2025-12-30
-- =====================================================

USE [HRDSYSTEM]
GO

PRINT '========================================';
PRINT '🚀 Performance & Data Volume Analysis';
PRINT '========================================';
PRINT '';

-- =====================================================
-- 📊 TEST 1: สถิติข้อมูลทั้งหมด
-- =====================================================
PRINT '1️⃣ สถิติข้อมูลทั้งหมด';
PRINT '----------------------------------------';

SELECT
    'TrainingRequests (Active)' AS TableInfo,
    COUNT(*) AS TotalRecords,
    MIN(CreatedDate) AS OldestRecord,
    MAX(CreatedDate) AS NewestRecord,
    DATEDIFF(DAY, MIN(CreatedDate), MAX(CreatedDate)) AS DataRangeDays
FROM [TrainingRequests]
WHERE IsActive = 1;

SELECT
    'TrainingRequestEmployees' AS TableInfo,
    COUNT(*) AS TotalRecords,
    COUNT(DISTINCT TrainingRequestId) AS UniqueTrainingRequests,
    AVG(CAST(COUNT(*) AS FLOAT)) OVER() / NULLIF((SELECT COUNT(DISTINCT TrainingRequestId) FROM [TrainingRequestEmployees]), 0) AS AvgEmployeesPerRequest
FROM [TrainingRequestEmployees];

PRINT '';
PRINT '';

-- =====================================================
-- 📈 TEST 2: จำนวน Records ตามปี
-- =====================================================
PRINT '2️⃣ จำนวน Records ตามปี';
PRINT '----------------------------------------';

SELECT
    YEAR(StartDate) AS Year,
    COUNT(*) AS TotalRequests,
    -- เมื่อ Export จะ JOIN กับ Employee → อาจได้หลาย rows ต่อ 1 request
    (SELECT COUNT(*)
     FROM [TrainingRequests] tr2
     LEFT JOIN [TrainingRequestEmployees] emp2 ON emp2.TrainingRequestId = tr2.Id
     WHERE YEAR(tr2.StartDate) = YEAR(tr.StartDate) AND tr2.IsActive = 1
    ) AS EstimatedExportRows,
    CASE
        WHEN (SELECT COUNT(*)
              FROM [TrainingRequests] tr2
              LEFT JOIN [TrainingRequestEmployees] emp2 ON emp2.TrainingRequestId = tr2.Id
              WHERE YEAR(tr2.StartDate) = YEAR(tr.StartDate) AND tr2.IsActive = 1
             ) < 1000 THEN '✅ ปลอดภัย (< 1K)'
        WHEN (SELECT COUNT(*)
              FROM [TrainingRequests] tr2
              LEFT JOIN [TrainingRequestEmployees] emp2 ON emp2.TrainingRequestId = tr2.Id
              WHERE YEAR(tr2.StartDate) = YEAR(tr.StartDate) AND tr2.IsActive = 1
             ) < 10000 THEN '⚠️ ปานกลาง (1K-10K)'
        WHEN (SELECT COUNT(*)
              FROM [TrainingRequests] tr2
              LEFT JOIN [TrainingRequestEmployees] emp2 ON emp2.TrainingRequestId = tr2.Id
              WHERE YEAR(tr2.StartDate) = YEAR(tr.StartDate) AND tr2.IsActive = 1
             ) < 100000 THEN '🟠 ควระวัง (10K-100K)'
        ELSE '🔴 อันตราย (> 100K)'
    END AS PerformanceRisk
FROM [TrainingRequests] tr
WHERE IsActive = 1
GROUP BY YEAR(StartDate)
ORDER BY Year DESC;

PRINT '';
PRINT '';

-- =====================================================
-- 🎯 TEST 3: จำนวน Records ตามฝ่าย
-- =====================================================
PRINT '3️⃣ จำนวน Records ตามฝ่าย (Top 10)';
PRINT '----------------------------------------';

SELECT TOP 10
    tr.Department,
    COUNT(DISTINCT tr.Id) AS TotalRequests,
    COUNT(emp.Id) AS TotalEmployeeRecords,
    ISNULL(AVG(CAST(emp.Id AS FLOAT)), 0) AS AvgEmployeesPerRequest,
    COUNT(emp.Id) AS EstimatedExportRows
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.IsActive = 1
GROUP BY tr.Department
ORDER BY EstimatedExportRows DESC;

PRINT '';
PRINT '';

-- =====================================================
-- 📅 TEST 4: จำนวน Records ตามช่วงเวลาที่มักใช้ Filter
-- =====================================================
PRINT '4️⃣ ประมาณการ Export Size ตามช่วงเวลาทั่วไป';
PRINT '----------------------------------------';

-- ทั้งปี 2025
DECLARE @Year2025Start DATE = '2025-01-01';
DECLARE @Year2025End DATE = '2025-12-31';

SELECT
    'ทั้งปี 2025' AS Period,
    COUNT(*) AS EstimatedExportRows,
    COUNT(*) * 45 AS TotalDataPoints, -- 45 columns
    CASE
        WHEN COUNT(*) < 1000 THEN '✅ ปลอดภัย - Export ได้เลย'
        WHEN COUNT(*) < 10000 THEN '⚠️ ปานกลาง - ใช้เวลา 5-10 วินาที'
        WHEN COUNT(*) < 50000 THEN '🟠 ค่อนข้างมาก - ใช้เวลา 30-60 วินาที'
        WHEN COUNT(*) < 100000 THEN '🔴 มาก - ควรใช้ Paging หรือ Warning'
        ELSE '🔴 มากเกินไป - ต้องใช้ Paging'
    END AS Recommendation
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= @Year2025Start
  AND tr.StartDate <= @Year2025End
  AND tr.IsActive = 1;

-- ไตรมาสล่าสุด
SELECT
    'ไตรมาสล่าสุด (3 เดือน)' AS Period,
    COUNT(*) AS EstimatedExportRows,
    COUNT(*) * 45 AS TotalDataPoints,
    CASE
        WHEN COUNT(*) < 1000 THEN '✅ ปลอดภัย'
        WHEN COUNT(*) < 10000 THEN '⚠️ ปานกลาง'
        ELSE '🔴 ควรระวัง'
    END AS Recommendation
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= DATEADD(MONTH, -3, GETDATE())
  AND tr.IsActive = 1;

-- เดือนล่าสุด
SELECT
    'เดือนล่าสุด' AS Period,
    COUNT(*) AS EstimatedExportRows,
    COUNT(*) * 45 AS TotalDataPoints,
    CASE
        WHEN COUNT(*) < 1000 THEN '✅ ปลอดภัย'
        WHEN COUNT(*) < 10000 THEN '⚠️ ปานกลาง'
        ELSE '🔴 ควรระวัง'
    END AS Recommendation
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= DATEADD(MONTH, -1, GETDATE())
  AND tr.IsActive = 1;

PRINT '';
PRINT '';

-- =====================================================
-- 🔍 TEST 5: ตรวจสอบ Records ที่มี Employee หลายคน
-- =====================================================
PRINT '5️⃣ TrainingRequests ที่มีพนักงานหลายคน (Top 10)';
PRINT '----------------------------------------';

SELECT TOP 10
    tr.Id,
    tr.DocNo,
    tr.Department,
    tr.SeminarTitle,
    COUNT(emp.Id) AS NumberOfEmployees,
    tr.StartDate,
    tr.Status
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.IsActive = 1
GROUP BY tr.Id, tr.DocNo, tr.Department, tr.SeminarTitle, tr.StartDate, tr.Status
HAVING COUNT(emp.Id) > 1
ORDER BY NumberOfEmployees DESC;

PRINT '';
PRINT 'หมายเหตุ: ถ้า 1 TrainingRequest มี Employee หลายคน';
PRINT '         จะทำให้ Export ได้หลาย rows ต่อ 1 request';
PRINT '';
PRINT '';

-- =====================================================
-- ⏱️ TEST 6: ทดสอบ Query Performance (Execution Time)
-- =====================================================
PRINT '6️⃣ ทดสอบ Query Performance';
PRINT '----------------------------------------';

-- Test 1: INNER JOIN
DECLARE @StartTime DATETIME2, @EndTime DATETIME2, @Duration INT;
SET @StartTime = SYSDATETIME();

SELECT COUNT(*)
FROM [TrainingRequests] tr
INNER JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= '2025-01-01'
  AND tr.StartDate <= '2025-12-31'
  AND tr.IsActive = 1;

SET @EndTime = SYSDATETIME();
SET @Duration = DATEDIFF(MILLISECOND, @StartTime, @EndTime);

PRINT 'INNER JOIN: ' + CAST(@Duration AS NVARCHAR(10)) + ' ms';

-- Test 2: LEFT JOIN
SET @StartTime = SYSDATETIME();

SELECT COUNT(*)
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.StartDate >= '2025-01-01'
  AND tr.StartDate <= '2025-12-31'
  AND tr.IsActive = 1;

SET @EndTime = SYSDATETIME();
SET @Duration = DATEDIFF(MILLISECOND, @StartTime, @EndTime);

PRINT 'LEFT JOIN:  ' + CAST(@Duration AS NVARCHAR(10)) + ' ms';

PRINT '';
PRINT '';

-- =====================================================
-- 💾 TEST 7: ประมาณการขนาดไฟล์ CSV
-- =====================================================
PRINT '7️⃣ ประมาณการขนาดไฟล์ CSV';
PRINT '----------------------------------------';

DECLARE @TotalRows INT, @AvgRowSize INT, @EstimatedFileSizeKB INT;

SELECT @TotalRows = COUNT(*)
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE tr.IsActive = 1;

-- สมมติว่า 1 row ประมาณ 2 KB (45 columns * ~50 bytes per column)
SET @AvgRowSize = 2; -- KB
SET @EstimatedFileSizeKB = @TotalRows * @AvgRowSize;

SELECT
    @TotalRows AS TotalRows,
    45 AS TotalColumns,
    @AvgRowSize AS AvgRowSizeKB,
    @EstimatedFileSizeKB AS EstimatedFileSizeKB,
    CAST(@EstimatedFileSizeKB / 1024.0 AS DECIMAL(10, 2)) AS EstimatedFileSizeMB,
    CASE
        WHEN @EstimatedFileSizeKB < 1024 THEN '✅ เล็กมาก (< 1 MB)'
        WHEN @EstimatedFileSizeKB < 10240 THEN '✅ เล็ก (< 10 MB)'
        WHEN @EstimatedFileSizeKB < 102400 THEN '⚠️ ปานกลาง (< 100 MB)'
        ELSE '🔴 ใหญ่มาก (> 100 MB)'
    END AS FileSizeStatus;

PRINT '';
PRINT '';

-- =====================================================
-- 📊 สรุปผลและคำแนะนำ
-- =====================================================
PRINT '========================================';
PRINT '📊 สรุปผลและคำแนะนำ';
PRINT '========================================';
PRINT '';

DECLARE @TotalActive INT, @Max2025Records INT;

SELECT @TotalActive = COUNT(*) FROM [TrainingRequests] WHERE IsActive = 1;
SELECT @Max2025Records = COUNT(*)
FROM [TrainingRequests] tr
LEFT JOIN [TrainingRequestEmployees] emp ON emp.TrainingRequestId = tr.Id
WHERE YEAR(tr.StartDate) = 2025 AND tr.IsActive = 1;

PRINT 'จำนวน TrainingRequests ทั้งหมด (Active): ' + CAST(@TotalActive AS NVARCHAR(10));
PRINT 'จำนวน Export Rows สูงสุดในปี 2025: ' + CAST(@Max2025Records AS NVARCHAR(10));
PRINT '';

-- คำแนะนำ
IF @Max2025Records < 1000
BEGIN
    PRINT '✅ คำแนะนำ: ข้อมูลไม่มาก Export ได้โดยตรง';
    PRINT '   - ไม่จำเป็นต้องใช้ Paging';
    PRINT '   - แนะนำให้แก้เฉพาะปัญหา INNER JOIN → LEFT JOIN';
END
ELSE IF @Max2025Records < 10000
BEGIN
    PRINT '⚠️ คำแนะนำ: ข้อมูลปานกลาง';
    PRINT '   - แก้ปัญหา INNER JOIN → LEFT JOIN (สำคัญ)';
    PRINT '   - อาจแสดง Warning ถ้า Export มากกว่า 5,000 rows';
    PRINT '   - เพิ่ม Loading indicator';
END
ELSE IF @Max2025Records < 50000
BEGIN
    PRINT '🟠 คำแนะนำ: ข้อมูลค่อนข้างมาก';
    PRINT '   - แก้ปัญหา INNER JOIN → LEFT JOIN (จำเป็น)';
    PRINT '   - ควรแสดง Warning message';
    PRINT '   - พิจารณาใช้ Background Job';
    PRINT '   - เพิ่ม Timeout ของ SQL Command';
END
ELSE
BEGIN
    PRINT '🔴 คำแนะนำ: ข้อมูลมากเกินไป';
    PRINT '   - แก้ปัญหา INNER JOIN → LEFT JOIN (จำเป็นมาก)';
    PRINT '   - ต้องใช้ Paging หรือ Background Job';
    PRINT '   - ห้าม Export ข้อมูลทั้งหมดพร้อมกัน';
    PRINT '   - พิจารณาใช้ Date Range Filter บังคับ';
END

PRINT '';
PRINT '========================================';
PRINT '✅ การวิเคราะห์เสร็จสิ้น';
PRINT '========================================';
