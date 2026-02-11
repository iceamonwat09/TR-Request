-- =====================================================
-- Script: Check Full Database Structure - HRDSYSTEM
-- Purpose: ตรวจสอบโครงสร้างทั้งหมดของ Database HRDSYSTEM
--          ครอบคลุม Tables, Columns, Data Types, Keys,
--          Indexes, Constraints, Views, Stored Procedures
-- Compatible: SQL Server 2014+
-- Database: HRDSYSTEM
-- Date: 2026-02-11
-- =====================================================

USE [HRDSYSTEM]
GO

SET NOCOUNT ON
GO

PRINT '=================================================================='
PRINT '  HRDSYSTEM - Full Database Structure Check'
PRINT '  Run Date: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================================='
PRINT ''

-- =====================================================
-- SECTION 1: Database Info
-- =====================================================
PRINT '=================================================================='
PRINT '  SECTION 1: Database Information'
PRINT '=================================================================='
PRINT ''

SELECT
    DB_NAME() AS [Database_Name],
    SUSER_SNAME() AS [Login_Name],
    d.compatibility_level AS [Compatibility_Level],
    d.collation_name AS [Collation],
    d.state_desc AS [State],
    d.recovery_model_desc AS [Recovery_Model],
    CAST(SUM(mf.size) * 8.0 / 1024 AS DECIMAL(10,2)) AS [Total_Size_MB]
FROM sys.databases d
INNER JOIN sys.master_files mf ON d.database_id = mf.database_id
WHERE d.name = DB_NAME()
GROUP BY d.compatibility_level, d.collation_name, d.state_desc, d.recovery_model_desc;

-- =====================================================
-- SECTION 2: All Tables - Summary
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 2: All Tables Summary (Table + Column Count + Row Count)'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS [Column_Count],
    SUM(ps.row_count) AS [Row_Count],
    CAST(SUM(ps.reserved_page_count) * 8.0 / 1024 AS DECIMAL(10,2)) AS [Size_MB]
FROM sys.tables t
LEFT JOIN sys.dm_db_partition_stats ps ON t.object_id = ps.object_id AND ps.index_id IN (0, 1)
WHERE t.schema_id = SCHEMA_ID('dbo')
GROUP BY t.name, t.object_id
ORDER BY t.name;

-- =====================================================
-- SECTION 3: All Columns Detail (per Table)
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 3: All Columns Detail (ทุก Table ทุก Column)'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    c.column_id AS [Col_No],
    c.name AS [Column_Name],
    ty.name AS [Data_Type],
    CASE
        WHEN ty.name IN ('nvarchar', 'nchar') AND c.max_length = -1 THEN 'MAX'
        WHEN ty.name IN ('nvarchar', 'nchar') THEN CAST(c.max_length / 2 AS VARCHAR)
        WHEN ty.name IN ('varchar', 'char', 'varbinary') AND c.max_length = -1 THEN 'MAX'
        WHEN ty.name IN ('varchar', 'char', 'varbinary') THEN CAST(c.max_length AS VARCHAR)
        WHEN ty.name IN ('decimal', 'numeric') THEN CAST(c.precision AS VARCHAR) + ',' + CAST(c.scale AS VARCHAR)
        ELSE CAST(c.max_length AS VARCHAR)
    END AS [Max_Length],
    CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS [Is_Nullable],
    CASE WHEN c.is_identity = 1 THEN 'YES' ELSE 'NO' END AS [Is_Identity],
    ISNULL(dc.definition, '') AS [Default_Value]
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
ORDER BY t.name, c.column_id;

-- =====================================================
-- SECTION 4: Primary Keys
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 4: Primary Keys'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    i.name AS [PK_Name],
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS [PK_Columns]
FROM sys.tables t
INNER JOIN sys.indexes i ON t.object_id = i.object_id AND i.is_primary_key = 1
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE t.schema_id = SCHEMA_ID('dbo')
GROUP BY t.name, i.name
ORDER BY t.name;

-- =====================================================
-- SECTION 5: Foreign Keys
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 5: Foreign Keys'
PRINT '=================================================================='
PRINT ''

SELECT
    OBJECT_NAME(fk.parent_object_id) AS [Child_Table],
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS [Child_Column],
    fk.name AS [FK_Name],
    OBJECT_NAME(fk.referenced_object_id) AS [Parent_Table],
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS [Parent_Column],
    fk.delete_referential_action_desc AS [On_Delete],
    fk.update_referential_action_desc AS [On_Update],
    CASE WHEN fk.is_disabled = 1 THEN 'DISABLED' ELSE 'ENABLED' END AS [Status]
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id IN (
    SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo')
)
ORDER BY [Child_Table], fk.name;

-- =====================================================
-- SECTION 6: Indexes (Non-PK)
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 6: Indexes (Non-Primary Key)'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    i.name AS [Index_Name],
    i.type_desc AS [Index_Type],
    CASE WHEN i.is_unique = 1 THEN 'YES' ELSE 'NO' END AS [Is_Unique],
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS [Index_Columns]
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE t.schema_id = SCHEMA_ID('dbo')
AND i.index_id > 0
AND i.is_primary_key = 0
GROUP BY t.name, i.name, i.type_desc, i.is_unique
ORDER BY t.name, i.name;

-- =====================================================
-- SECTION 7: Default Constraints
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 7: Default Constraints'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    c.name AS [Column_Name],
    dc.name AS [Constraint_Name],
    dc.definition AS [Default_Value]
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
INNER JOIN sys.tables t ON c.object_id = t.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
ORDER BY t.name, c.name;

-- =====================================================
-- SECTION 8: Check Constraints
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 8: Check Constraints'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    cc.name AS [Constraint_Name],
    cc.definition AS [Constraint_Definition],
    CASE WHEN cc.is_disabled = 1 THEN 'DISABLED' ELSE 'ENABLED' END AS [Status]
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
ORDER BY t.name, cc.name;

-- =====================================================
-- SECTION 9: Unique Constraints
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 9: Unique Constraints'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    i.name AS [Constraint_Name],
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS [Columns]
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE t.schema_id = SCHEMA_ID('dbo')
AND i.is_unique_constraint = 1
GROUP BY t.name, i.name
ORDER BY t.name, i.name;

-- =====================================================
-- SECTION 10: Identity Columns
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 10: Identity Columns'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    c.name AS [Column_Name],
    ty.name AS [Data_Type],
    IDENT_SEED(t.name) AS [Seed],
    IDENT_INCR(t.name) AS [Increment],
    IDENT_CURRENT(t.name) AS [Current_Value]
FROM sys.tables t
INNER JOIN sys.columns c ON t.object_id = c.object_id
INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
WHERE t.schema_id = SCHEMA_ID('dbo')
AND c.is_identity = 1
ORDER BY t.name;

-- =====================================================
-- SECTION 11: Views
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 11: Views'
PRINT '=================================================================='
PRINT ''

SELECT
    v.name AS [View_Name],
    v.create_date AS [Created_Date],
    v.modify_date AS [Modified_Date]
FROM sys.views v
WHERE v.schema_id = SCHEMA_ID('dbo')
ORDER BY v.name;

-- =====================================================
-- SECTION 12: Stored Procedures
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 12: Stored Procedures'
PRINT '=================================================================='
PRINT ''

SELECT
    p.name AS [Procedure_Name],
    p.create_date AS [Created_Date],
    p.modify_date AS [Modified_Date]
FROM sys.procedures p
WHERE p.schema_id = SCHEMA_ID('dbo')
ORDER BY p.name;

-- =====================================================
-- SECTION 13: Triggers
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 13: Triggers'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    tr.name AS [Trigger_Name],
    tr.type_desc AS [Type],
    CASE WHEN tr.is_disabled = 1 THEN 'DISABLED' ELSE 'ENABLED' END AS [Status],
    CASE WHEN tr.is_instead_of_trigger = 1 THEN 'INSTEAD OF' ELSE 'AFTER' END AS [Trigger_Type]
FROM sys.triggers tr
INNER JOIN sys.tables t ON tr.parent_id = t.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
ORDER BY t.name, tr.name;

-- =====================================================
-- SECTION 14: User-Defined Functions
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 14: User-Defined Functions'
PRINT '=================================================================='
PRINT ''

SELECT
    o.name AS [Function_Name],
    o.type_desc AS [Type],
    o.create_date AS [Created_Date],
    o.modify_date AS [Modified_Date]
FROM sys.objects o
WHERE o.schema_id = SCHEMA_ID('dbo')
AND o.type IN ('FN', 'IF', 'TF')
ORDER BY o.name;

-- =====================================================
-- SECTION 15: Disk Space Usage per Table
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 15: Disk Space Usage per Table'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    SUM(ps.reserved_page_count) * 8 AS [Reserved_KB],
    SUM(ps.used_page_count) * 8 AS [Used_KB],
    (SUM(ps.reserved_page_count) - SUM(ps.used_page_count)) * 8 AS [Unused_KB],
    SUM(ps.row_count) AS [Row_Count]
FROM sys.tables t
INNER JOIN sys.dm_db_partition_stats ps ON t.object_id = ps.object_id
WHERE t.schema_id = SCHEMA_ID('dbo')
GROUP BY t.name
ORDER BY [Reserved_KB] DESC;

-- =====================================================
-- SECTION 16: Column Count Comparison (Expected vs Actual)
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 16: Column Count Comparison (Expected vs Actual)'
PRINT '=================================================================='
PRINT ''

-- Expected counts based on Dev schema
DECLARE @ExpectedCounts TABLE (TableName NVARCHAR(100), ExpectedCount INT)
INSERT INTO @ExpectedCounts VALUES
    ('ApprovalHistory', 9),
    ('EmailLogs', 10),
    ('RetryEmailHistory', 7),
    ('TrainingHistory', 8),
    ('TrainingRequest_Cost', 7),
    ('TrainingRequestAttachments', 4),
    ('TrainingRequestEmployees', 14),
    ('TrainingRequests', 81);

SELECT
    ec.TableName AS [Table_Name],
    ec.ExpectedCount AS [Expected_Columns],
    ISNULL(actual.cnt, 0) AS [Actual_Columns],
    CASE
        WHEN ISNULL(actual.cnt, 0) = 0 THEN 'TABLE NOT FOUND'
        WHEN ec.ExpectedCount = actual.cnt THEN 'OK'
        WHEN actual.cnt > ec.ExpectedCount THEN 'EXTRA +' + CAST(actual.cnt - ec.ExpectedCount AS VARCHAR)
        ELSE 'MISSING -' + CAST(ec.ExpectedCount - actual.cnt AS VARCHAR)
    END AS [Status]
FROM @ExpectedCounts ec
LEFT JOIN (
    SELECT t.name, COUNT(*) AS cnt
    FROM sys.tables t
    INNER JOIN sys.columns c ON t.object_id = c.object_id
    WHERE t.schema_id = SCHEMA_ID('dbo')
    GROUP BY t.name
) actual ON ec.TableName = actual.name
ORDER BY ec.TableName;

-- =====================================================
-- SECTION 17: Tables NOT in Expected List (Extra Tables)
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SECTION 17: Extra Tables (Tables in DB but not in Expected List)'
PRINT '=================================================================='
PRINT ''

SELECT
    t.name AS [Table_Name],
    (SELECT COUNT(*) FROM sys.columns c WHERE c.object_id = t.object_id) AS [Column_Count],
    'EXTRA TABLE' AS [Status]
FROM sys.tables t
WHERE t.schema_id = SCHEMA_ID('dbo')
AND t.name NOT IN (
    'ApprovalHistory', 'EmailLogs', 'RetryEmailHistory', 'TrainingHistory',
    'TrainingRequest_Cost', 'TrainingRequestAttachments',
    'TrainingRequestEmployees', 'TrainingRequests'
)
ORDER BY t.name;

-- =====================================================
-- SUMMARY
-- =====================================================
PRINT ''
PRINT '=================================================================='
PRINT '  SUMMARY'
PRINT '=================================================================='
PRINT ''

DECLARE @TotalTables INT, @TotalColumns INT, @TotalPK INT, @TotalFK INT
DECLARE @TotalIndexes INT, @TotalDefaults INT, @TotalRows BIGINT

SELECT @TotalTables = COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo')
SELECT @TotalColumns = COUNT(*) FROM sys.columns c INNER JOIN sys.tables t ON c.object_id = t.object_id WHERE t.schema_id = SCHEMA_ID('dbo')
SELECT @TotalPK = COUNT(*) FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id WHERE t.schema_id = SCHEMA_ID('dbo') AND i.is_primary_key = 1
SELECT @TotalFK = COUNT(*) FROM sys.foreign_keys fk WHERE fk.parent_object_id IN (SELECT object_id FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo'))
SELECT @TotalIndexes = COUNT(*) FROM sys.indexes i INNER JOIN sys.tables t ON i.object_id = t.object_id WHERE t.schema_id = SCHEMA_ID('dbo') AND i.index_id > 0 AND i.is_primary_key = 0
SELECT @TotalDefaults = COUNT(*) FROM sys.default_constraints dc INNER JOIN sys.tables t ON dc.parent_object_id = t.object_id WHERE t.schema_id = SCHEMA_ID('dbo')
SELECT @TotalRows = ISNULL(SUM(ps.row_count), 0) FROM sys.tables t INNER JOIN sys.dm_db_partition_stats ps ON t.object_id = ps.object_id WHERE t.schema_id = SCHEMA_ID('dbo') AND ps.index_id IN (0, 1)

PRINT 'Total Tables:              ' + CAST(@TotalTables AS VARCHAR(10))
PRINT 'Total Columns:             ' + CAST(@TotalColumns AS VARCHAR(10))
PRINT 'Total Primary Keys:        ' + CAST(@TotalPK AS VARCHAR(10))
PRINT 'Total Foreign Keys:        ' + CAST(@TotalFK AS VARCHAR(10))
PRINT 'Total Indexes (non-PK):    ' + CAST(@TotalIndexes AS VARCHAR(10))
PRINT 'Total Default Constraints: ' + CAST(@TotalDefaults AS VARCHAR(10))
PRINT 'Total Rows (all tables):   ' + CAST(@TotalRows AS VARCHAR(20))

PRINT ''
PRINT '=================================================================='
PRINT '  Full Database Structure Check Completed!'
PRINT '  Completed: ' + CONVERT(VARCHAR(20), GETDATE(), 120)
PRINT '=================================================================='
GO
