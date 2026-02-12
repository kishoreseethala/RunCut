-- Run this script on database: mvt-runcut-analysis (or runcutweb)
-- to drop all tables so you can recreate the schema with: dotnet run -- create-schema
-- Execute in pgAdmin, Azure Data Studio, or: psql "Host=...;Database=mvt-runcut-analysis;..." -f DropAllTablesPostgres.sql

-- Drop application tables (child tables first due to foreign keys)
DROP TABLE IF EXISTS "StopTimings"     CASCADE;
DROP TABLE IF EXISTS "CalendarDates"   CASCADE;
DROP TABLE IF EXISTS "Trips"           CASCADE;
DROP TABLE IF EXISTS "Stops"            CASCADE;
DROP TABLE IF EXISTS "Routes"           CASCADE;
DROP TABLE IF EXISTS "DataSets"         CASCADE;
DROP TABLE IF EXISTS "d_Date"          CASCADE;
DROP TABLE IF EXISTS "__EFMigrationsHistory" CASCADE;
