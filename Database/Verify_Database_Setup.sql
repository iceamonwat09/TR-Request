-- =====================================================
-- Database Verification Script
-- Purpose: Verify database structure after deployment
-- =====================================================

USE [HRDSYSTEM]
GO

PRINT '=================================================='
PRINT 'HRDSYSTEM - Database Verification'
PRINT 'Started: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================='
PRINT ''

-- =====================================================
-- 1. Check Database Exists
-- =====================================================
PRINT '1. Checking Database...'
IF DB_ID('HRDSYSTEM') IS NOT NULL
    PRINT '   ✅ Database HRDSYSTEM exists'
ELSE
    PRINT '   ❌ Database HRDSYSTEM NOT found!'
GO

-- =====================================================
-- 2. Check All Tables Exist
-- =====================================================
PRINT ''
PRINT '2. Checking Tables...'

DECLARE @ExpectedTables TABLE (TableName NVARCHAR(100))
INSERT INTO @ExpectedTables VALUES
    ('TrainingRequests'),
    ('TrainingRequestEmployees'),
    ('TrainingRequestAttachments'),
    ('TrainingRequest_Cost'),
    ('RetryEmailHistory'),
    ('EmailLogs')

SELECT
    et.TableName,
    CASE WHEN t.TABLE_NAME IS NOT NULL THEN '✅ EXISTS' ELSE '❌ MISSING' END AS Status,
    ISNULL((SELECT COUNT(*) FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.' + et.TableName) AND index_id > 0), 0) AS IndexCount
FROM @ExpectedTables et
LEFT JOIN INFORMATION_SCHEMA.TABLES t ON et.TableName = t.TABLE_NAME AND t.TABLE_SCHEMA = 'dbo'
ORDER BY et.TableName

-- =====================================================
-- 3. Check Primary Keys
-- =====================================================
PRINT ''
PRINT '3. Checking Primary Keys...'

SELECT
    t.name AS TableName,
    i.name AS PrimaryKeyName,
    '✅ OK' AS Status
FROM sys.tables t
INNER JOIN sys.indexes i ON t.object_id = i.object_id
WHERE i.is_primary_key = 1
AND t.schema_id = SCHEMA_ID('dbo')
ORDER BY t.name

-- =====================================================
-- 4. Check Foreign Keys
-- =====================================================
PRINT ''
PRINT '4. Checking Foreign Keys...'

SELECT
    OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    '✅ OK' AS Status
FROM sys.foreign_keys fk
WHERE fk.parent_object_id IN (
    SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo')
)
ORDER BY ChildTable

-- =====================================================
-- 5. Check Indexes
-- =====================================================
PRINT ''
PRINT '5. Checking Indexes...'

SELECT
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    '✅ OK' AS Status
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
AND i.index_id > 0
AND i.is_primary_key = 0
ORDER BY t.name, i.name

-- =====================================================
-- 6. Check Column Counts
-- =====================================================
PRINT ''
PRINT '6. Table Column Counts...'

SELECT
    TABLE_NAME,
    COUNT(*) AS ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
GROUP BY TABLE_NAME
ORDER BY TABLE_NAME

-- =====================================================
-- 7. Check Default Constraints
-- =====================================================
PRINT ''
PRINT '7. Checking Default Constraints...'

SELECT
    t.name AS TableName,
    c.name AS ColumnName,
    dc.name AS DefaultConstraintName,
    dc.definition AS DefaultValue,
    '✅ OK' AS Status
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
INNER JOIN sys.tables t ON c.object_id = t.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
ORDER BY t.name, c.name

-- =====================================================
-- 8. Check Data Types
-- =====================================================
PRINT ''
PRINT '8. Sample of Important Columns and Their Data Types...'

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE +
    CASE
        WHEN DATA_TYPE IN ('nvarchar', 'varchar', 'char', 'nchar') THEN '(' + CAST(CHARACTER_MAXIMUM_LENGTH AS VARCHAR) + ')'
        WHEN DATA_TYPE IN ('decimal', 'numeric') THEN '(' + CAST(NUMERIC_PRECISION AS VARCHAR) + ',' + CAST(NUMERIC_SCALE AS VARCHAR) + ')'
        ELSE ''
    END AS DataTypeDetail,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
AND TABLE_NAME IN ('TrainingRequests', 'TrainingRequestEmployees')
AND COLUMN_NAME IN ('Id', 'DocNo', 'TotalCost', 'CreatedDate', 'Status')
ORDER BY TABLE_NAME, ORDINAL_POSITION

-- =====================================================
-- 9. Disk Space Usage
-- =====================================================
PRINT ''
PRINT '9. Table Sizes...'

SELECT
    t.name AS TableName,
    SUM(ps.reserved_page_count) * 8 AS ReservedKB,
    SUM(ps.used_page_count) * 8 AS UsedKB,
    (SUM(ps.reserved_page_count) - SUM(ps.used_page_count)) * 8 AS UnusedKB
FROM sys.tables t
INNER JOIN sys.dm_db_partition_stats ps ON t.object_id = ps.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
GROUP BY t.name
ORDER BY ReservedKB DESC

-- =====================================================
-- 10. Row Counts
-- =====================================================
PRINT ''
PRINT '10. Current Row Counts...'

SELECT
    t.name AS TableName,
    SUM(ps.row_count) AS RowCount
FROM sys.tables t
INNER JOIN sys.dm_db_partition_stats ps ON t.object_id = ps.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
AND ps.index_id IN (0, 1)
GROUP BY t.name
ORDER BY t.name

-- =====================================================
-- Summary
-- =====================================================
PRINT ''
PRINT '=================================================='
PRINT 'Verification Summary'
PRINT '=================================================='

DECLARE @TableCount INT, @PKCount INT, @FKCount INT, @IndexCount INT

SELECT @TableCount = COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo'
SELECT @PKCount = COUNT(*) FROM sys.indexes WHERE is_primary_key = 1 AND object_id IN (SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo'))
SELECT @FKCount = COUNT(*) FROM sys.foreign_keys WHERE parent_object_id IN (SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo'))
SELECT @IndexCount = COUNT(*) FROM sys.indexes WHERE index_id > 0 AND is_primary_key = 0 AND object_id IN (SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo'))

PRINT 'Total Tables: ' + CAST(@TableCount AS VARCHAR(10))
PRINT 'Total Primary Keys: ' + CAST(@PKCount AS VARCHAR(10))
PRINT 'Total Foreign Keys: ' + CAST(@FKCount AS VARCHAR(10))
PRINT 'Total Indexes (non-PK): ' + CAST(@IndexCount AS VARCHAR(10))

PRINT ''
PRINT '=================================================='
PRINT 'Verification Completed!'
PRINT 'Completed: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================='
GO
