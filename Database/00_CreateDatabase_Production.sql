-- =====================================================
-- Production Database Setup Script
-- Database: HRDSYSTEM (Training Request System)
-- Description: Complete database creation script for production deployment
-- Created: 2025-11-29
-- =====================================================

USE [master]
GO

-- =====================================================
-- Create Database (if not exists)
-- =====================================================
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'HRDSYSTEM')
BEGIN
    CREATE DATABASE [HRDSYSTEM]
    PRINT '✅ Database HRDSYSTEM created successfully!'
END
ELSE
BEGIN
    PRINT '⚠️  Database HRDSYSTEM already exists.'
END
GO

USE [HRDSYSTEM]
GO

PRINT '=================================================='
PRINT 'Starting Production Database Setup...'
PRINT '=================================================='
GO
