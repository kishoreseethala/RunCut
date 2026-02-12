-- Run this script on your PostgreSQL database (e.g. mvt-runcut-analysis)
-- to create the schema required by RunCutWeb. Then run the app again.

-- Ensure migrations history table exists (may already exist from a previous attempt)
CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

-- Optional: drop existing app tables only if you want a clean slate (WARNING: deletes all data)
-- DROP TABLE IF EXISTS "StopTimings";
-- DROP TABLE IF EXISTS "CalendarDates";
-- DROP TABLE IF EXISTS "Trips";
-- DROP TABLE IF EXISTS "Stops";
-- DROP TABLE IF EXISTS "Routes";
-- DROP TABLE IF EXISTS "DataSets";
-- DROP TABLE IF EXISTS "d_Date";

-- DataSets
CREATE TABLE IF NOT EXISTS "DataSets" (
    "Id" serial PRIMARY KEY,
    "Name" character varying(200) NOT NULL,
    "CreatedDate" timestamp NOT NULL,
    "LastModifiedDate" timestamp NULL
);

-- Routes
CREATE TABLE IF NOT EXISTS "Routes" (
    "Id" serial PRIMARY KEY,
    "DataSetId" integer NOT NULL,
    "RouteId" character varying(100) NOT NULL,
    "AgencyId" character varying(100) NULL,
    "RouteShortName" character varying(100) NULL,
    "RouteLongName" character varying(500) NULL,
    "RouteDesc" character varying(500) NULL,
    "RouteType" integer NULL,
    "RouteUrl" character varying(500) NULL,
    "RouteColor" character varying(20) NULL,
    "RouteTextColor" character varying(20) NULL,
    CONSTRAINT "FK_Routes_DataSets_DataSetId" FOREIGN KEY ("DataSetId") REFERENCES "DataSets" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_Routes_DataSetId_RouteId" ON "Routes" ("DataSetId", "RouteId");

-- Stops
CREATE TABLE IF NOT EXISTS "Stops" (
    "Id" serial PRIMARY KEY,
    "DataSetId" integer NOT NULL,
    "StopId" character varying(100) NOT NULL,
    "StopCode" character varying(100) NULL,
    "StopName" character varying(500) NULL,
    "StopDesc" character varying(500) NULL,
    "StopLat" numeric(18,6) NULL,
    "StopLon" numeric(18,6) NULL,
    "ZoneId" character varying(100) NULL,
    "StopUrl" character varying(500) NULL,
    "LocationType" integer NULL,
    "ParentStation" character varying(100) NULL,
    "StopTimeZone" character varying(100) NULL,
    "WheelchairBoarding" integer NULL,
    CONSTRAINT "FK_Stops_DataSets_DataSetId" FOREIGN KEY ("DataSetId") REFERENCES "DataSets" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_Stops_DataSetId_StopId" ON "Stops" ("DataSetId", "StopId");

-- Trips
CREATE TABLE IF NOT EXISTS "Trips" (
    "Id" serial PRIMARY KEY,
    "DataSetId" integer NOT NULL,
    "RouteId" character varying(100) NOT NULL,
    "ServiceId" character varying(100) NOT NULL,
    "TripId" character varying(100) NOT NULL,
    "TripHeadsign" character varying(500) NULL,
    "TripShortName" character varying(100) NULL,
    "DirectionId" integer NULL,
    "BlockId" character varying(100) NULL,
    "ShapeId" character varying(100) NULL,
    "WheelchairAccessible" integer NULL,
    "BikesAllowed" integer NULL,
    CONSTRAINT "FK_Trips_DataSets_DataSetId" FOREIGN KEY ("DataSetId") REFERENCES "DataSets" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_Trips_DataSetId_TripId" ON "Trips" ("DataSetId", "TripId");

-- StopTimings
CREATE TABLE IF NOT EXISTS "StopTimings" (
    "Id" serial PRIMARY KEY,
    "DataSetId" integer NOT NULL,
    "TripId" character varying(100) NOT NULL,
    "StopId" character varying(100) NOT NULL,
    "ArrivalTime" character varying(50) NULL,
    "DepartureTime" character varying(50) NULL,
    "StopSequence" integer NULL,
    "StopHeadsign" character varying(500) NULL,
    "PickupType" integer NULL,
    "DropOffType" integer NULL,
    "ShapeDistTraveled" numeric(18,6) NULL,
    "Timepoint" integer NULL,
    CONSTRAINT "FK_StopTimings_DataSets_DataSetId" FOREIGN KEY ("DataSetId") REFERENCES "DataSets" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_StopTimings_DataSetId_TripId_StopSequence" ON "StopTimings" ("DataSetId", "TripId", "StopSequence");

-- CalendarDates
CREATE TABLE IF NOT EXISTS "CalendarDates" (
    "Id" serial PRIMARY KEY,
    "DataSetId" integer NOT NULL,
    "ServiceId" character varying(100) NOT NULL,
    "Date" timestamp NOT NULL,
    "ExceptionType" integer NOT NULL,
    CONSTRAINT "FK_CalendarDates_DataSets_DataSetId" FOREIGN KEY ("DataSetId") REFERENCES "DataSets" ("Id") ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS "IX_CalendarDates_DataSetId_ServiceId_Date" ON "CalendarDates" ("DataSetId", "ServiceId", "Date");

-- d_Date (date dimension)
CREATE TABLE IF NOT EXISTS "d_Date" (
    "DateKey" integer NOT NULL,
    "Date" timestamp NOT NULL,
    "DayOfWeek" integer NOT NULL,
    "DayName" character varying(20) NULL,
    CONSTRAINT "PK_d_Date" PRIMARY KEY ("DateKey")
);

-- Mark the initial migration as applied so EF Core doesn't try to re-apply later
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260211100000_InitialCreatePostgres', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
