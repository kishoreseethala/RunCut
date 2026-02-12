using Microsoft.EntityFrameworkCore;
using RunCutWeb.Application.Interfaces;
using RunCutWeb.Infrastructure.Data;
using RunCutWeb.Infrastructure.Services;

namespace RunCutWeb
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Run only schema creation then exit: dotnet run -- create-schema
            if (args.Length > 0 && args.Any(a =>
                a.Equals("create-schema", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--create-schema", StringComparison.OrdinalIgnoreCase)))
            {
                await CreateSchemaOnlyAsync(args);
                return;
            }
            // Drop all tables then exit: dotnet run -- drop-all
            if (args.Length > 0 && args.Any(a =>
                a.Equals("drop-all", StringComparison.OrdinalIgnoreCase) ||
                a.Equals("--drop-all", StringComparison.OrdinalIgnoreCase)))
            {
                await DropAllTablesAsync(args);
                return;
            }

            var builder = WebApplication.CreateBuilder(args);
            builder.WebHost.ConfigureKestrel(opt => opt.Limits.MaxRequestBodySize = 524288000); // 500 MB for GTFS uploads

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            // Allow large GTFS uploads (default is ~28MB)
            builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 524288000; // 500 MB
            });

            // Add Entity Framework with performance optimizations (PostgreSQL)
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(300); // 5 minutes timeout for large imports
                    npgsqlOptions.EnableRetryOnFailure();
                })
                .EnableSensitiveDataLogging(false)
                .EnableServiceProviderCaching());

            // Register application services
            builder.Services.AddScoped<IDataImportService, DataImportService>();

            var app = builder.Build();

            // Ensure PostgreSQL schema exists (create tables if missing)
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                try
                {
                    await context.Database.CanConnectAsync();
                    var conn = context.Database.GetDbConnection();
                    await conn.OpenAsync();

                    bool dataSetsExists;
                    await using (var checkCmd = conn.CreateCommand())
                    {
                        checkCmd.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'DataSets')";
                        var result = await checkCmd.ExecuteScalarAsync();
                        dataSetsExists = result is true || (result is bool b && b);
                    }

                    if (!dataSetsExists)
                    {
                        logger.LogInformation("DataSets table not found. Creating PostgreSQL schema...");
                        var statements = GetPostgresSchemaStatements();
                        foreach (var sql in statements)
                        {
                            if (string.IsNullOrWhiteSpace(sql)) continue;
                            await using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = sql.TrimEnd();
                                if (!cmd.CommandText.EndsWith(";"))
                                    cmd.CommandText += ";";
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        logger.LogInformation("PostgreSQL schema created successfully.");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "PostgreSQL schema could not be created. Start PostgreSQL, then run: dotnet run -- create-schema");
                    // Don't throw: allow app to start; data pages will show connection errors until DB is ready
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            await app.RunAsync();
        }

        /// <summary>Standalone schema creation using connection string from config. Run: dotnet run -- create-schema</summary>
        private static async Task CreateSchemaOnlyAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.Error.WriteLine("Missing ConnectionStrings:DefaultConnection in appsettings.json or appsettings.Development.json");
                Environment.Exit(1);
            }

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);

            try
            {
                await using (var context = new ApplicationDbContext(optionsBuilder.Options))
                {
                    await context.Database.CanConnectAsync();
                }
            }
            catch (Npgsql.NpgsqlException ex) when (ex.Message.Contains("does not exist"))
            {
                var dbName = GetDatabaseNameFromConnectionString(connectionString);
                if (!string.IsNullOrEmpty(dbName))
                {
                    var connStrToPostgres = ReplaceDatabaseInConnectionString(connectionString, "postgres");
                    var adminOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connStrToPostgres).Options;
                    await using (var adminContext = new ApplicationDbContext(adminOptions))
                    {
#pragma warning disable EF1002 // dbName is from our connection string parser, not user input
                        await adminContext.Database.ExecuteSqlRawAsync($"CREATE DATABASE \"{dbName.Replace("\"", "\"\"")}\"");
#pragma warning restore EF1002
                        Console.WriteLine($"Created database: {dbName}");
                    }
                }
                else
                {
                    Console.Error.WriteLine("Could not parse database name from connection string. Create the database manually and run again.");
                    Environment.Exit(1);
                }
            }

            optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            await using (var ctx = new ApplicationDbContext(optionsBuilder.Options))
            {
                var conn = ctx.Database.GetDbConnection();
                await conn.OpenAsync();

                var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'DataSets')";
                var exists = await checkCmd.ExecuteScalarAsync();
                if (exists is true || (exists is bool b && b))
                {
                    Console.WriteLine("Tables already exist. Nothing to do.");
                    return;
                }

                Console.WriteLine("Creating PostgreSQL schema...");
                foreach (var sql in GetPostgresSchemaStatements())
                {
                    if (string.IsNullOrWhiteSpace(sql)) continue;
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = sql.TrimEnd();
                    if (!cmd.CommandText.EndsWith(";")) cmd.CommandText += ";";
                    await cmd.ExecuteNonQueryAsync();
                }
                Console.WriteLine("PostgreSQL schema created successfully.");
            }
        }

        /// <summary>Drop all application tables. Run: dotnet run -- drop-all</summary>
        private static async Task DropAllTablesAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                Console.Error.WriteLine("Missing ConnectionStrings:DefaultConnection");
                Environment.Exit(1);
            }
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(connectionString).Options;
            await using var ctx = new ApplicationDbContext(options);
            try
            {
                await ctx.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Cannot connect to database: " + ex.Message);
                Environment.Exit(1);
            }
            var tables = new[] { "StopTimings", "CalendarDates", "Trips", "Stops", "Routes", "DataSets", "d_Date", "__EFMigrationsHistory" };
            foreach (var table in tables)
            {
                await ctx.Database.ExecuteSqlRawAsync($@"DROP TABLE IF EXISTS ""{table}"" CASCADE;");
                Console.WriteLine("Dropped: " + table);
            }
            Console.WriteLine("All tables dropped. Run: dotnet run -- create-schema");
        }

        private static string? GetDatabaseNameFromConnectionString(string cs)
        {
            foreach (var part in cs.Split(';'))
            {
                var kv = part.Trim().Split('=', 2, StringSplitOptions.None);
                if (kv.Length == 2 && string.Equals(kv[0].Trim(), "Database", StringComparison.OrdinalIgnoreCase))
                    return kv[1].Trim();
            }
            return null;
        }

        private static string ReplaceDatabaseInConnectionString(string cs, string newDatabase)
        {
            var parts = cs.Split(';').Select(p =>
            {
                var kv = p.Trim().Split('=', 2, StringSplitOptions.None);
                if (kv.Length == 2 && string.Equals(kv[0].Trim(), "Database", StringComparison.OrdinalIgnoreCase))
                    return "Database=" + newDatabase;
                return p.Trim();
            });
            return string.Join(";", parts);
        }

        /// <summary>PostgreSQL DDL statements to create RunCutWeb schema. Runs only when DataSets table is missing.</summary>
        private static string[] GetPostgresSchemaStatements()
        {
            return new[]
            {
                @"CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (""MigrationId"" character varying(150) NOT NULL, ""ProductVersion"" character varying(32) NOT NULL, CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY (""MigrationId""))",
                @"CREATE TABLE IF NOT EXISTS ""DataSets"" (""Id"" serial PRIMARY KEY, ""Name"" character varying(200) NOT NULL, ""CreatedDate"" timestamp NOT NULL, ""LastModifiedDate"" timestamp NULL)",
                @"CREATE TABLE IF NOT EXISTS ""Routes"" (""Id"" serial PRIMARY KEY, ""DataSetId"" integer NOT NULL, ""RouteId"" character varying(100) NOT NULL, ""AgencyId"" character varying(100) NULL, ""RouteShortName"" character varying(100) NULL, ""RouteLongName"" character varying(500) NULL, ""RouteDesc"" character varying(500) NULL, ""RouteType"" integer NULL, ""RouteUrl"" character varying(500) NULL, ""RouteColor"" character varying(20) NULL, ""RouteTextColor"" character varying(20) NULL, CONSTRAINT ""FK_Routes_DataSets_DataSetId"" FOREIGN KEY (""DataSetId"") REFERENCES ""DataSets"" (""Id"") ON DELETE CASCADE)",
                @"CREATE INDEX IF NOT EXISTS ""IX_Routes_DataSetId_RouteId"" ON ""Routes"" (""DataSetId"", ""RouteId"")",
                @"CREATE TABLE IF NOT EXISTS ""Stops"" (""Id"" serial PRIMARY KEY, ""DataSetId"" integer NOT NULL, ""StopId"" character varying(100) NOT NULL, ""StopCode"" character varying(100) NULL, ""StopName"" character varying(500) NULL, ""StopDesc"" character varying(500) NULL, ""StopLat"" numeric(18,6) NULL, ""StopLon"" numeric(18,6) NULL, ""ZoneId"" character varying(100) NULL, ""StopUrl"" character varying(500) NULL, ""LocationType"" integer NULL, ""ParentStation"" character varying(100) NULL, ""StopTimeZone"" character varying(100) NULL, ""WheelchairBoarding"" integer NULL, CONSTRAINT ""FK_Stops_DataSets_DataSetId"" FOREIGN KEY (""DataSetId"") REFERENCES ""DataSets"" (""Id"") ON DELETE CASCADE)",
                @"CREATE INDEX IF NOT EXISTS ""IX_Stops_DataSetId_StopId"" ON ""Stops"" (""DataSetId"", ""StopId"")",
                @"CREATE TABLE IF NOT EXISTS ""Trips"" (""Id"" serial PRIMARY KEY, ""DataSetId"" integer NOT NULL, ""RouteId"" character varying(100) NOT NULL, ""ServiceId"" character varying(100) NOT NULL, ""TripId"" character varying(100) NOT NULL, ""TripHeadsign"" character varying(500) NULL, ""TripShortName"" character varying(100) NULL, ""DirectionId"" integer NULL, ""BlockId"" character varying(100) NULL, ""ShapeId"" character varying(100) NULL, ""WheelchairAccessible"" integer NULL, ""BikesAllowed"" integer NULL, CONSTRAINT ""FK_Trips_DataSets_DataSetId"" FOREIGN KEY (""DataSetId"") REFERENCES ""DataSets"" (""Id"") ON DELETE CASCADE)",
                @"CREATE INDEX IF NOT EXISTS ""IX_Trips_DataSetId_TripId"" ON ""Trips"" (""DataSetId"", ""TripId"")",
                @"CREATE TABLE IF NOT EXISTS ""StopTimings"" (""Id"" serial PRIMARY KEY, ""DataSetId"" integer NOT NULL, ""TripId"" character varying(100) NOT NULL, ""StopId"" character varying(100) NOT NULL, ""ArrivalTime"" character varying(50) NULL, ""DepartureTime"" character varying(50) NULL, ""StopSequence"" integer NULL, ""StopHeadsign"" character varying(500) NULL, ""PickupType"" integer NULL, ""DropOffType"" integer NULL, ""ShapeDistTraveled"" numeric(18,6) NULL, ""Timepoint"" integer NULL, CONSTRAINT ""FK_StopTimings_DataSets_DataSetId"" FOREIGN KEY (""DataSetId"") REFERENCES ""DataSets"" (""Id"") ON DELETE CASCADE)",
                @"CREATE INDEX IF NOT EXISTS ""IX_StopTimings_DataSetId_TripId_StopSequence"" ON ""StopTimings"" (""DataSetId"", ""TripId"", ""StopSequence"")",
                @"CREATE TABLE IF NOT EXISTS ""CalendarDates"" (""Id"" serial PRIMARY KEY, ""DataSetId"" integer NOT NULL, ""ServiceId"" character varying(100) NOT NULL, ""Date"" timestamp NOT NULL, ""ExceptionType"" integer NOT NULL, CONSTRAINT ""FK_CalendarDates_DataSets_DataSetId"" FOREIGN KEY (""DataSetId"") REFERENCES ""DataSets"" (""Id"") ON DELETE CASCADE)",
                @"CREATE INDEX IF NOT EXISTS ""IX_CalendarDates_DataSetId_ServiceId_Date"" ON ""CalendarDates"" (""DataSetId"", ""ServiceId"", ""Date"")",
                @"CREATE TABLE IF NOT EXISTS ""d_Date"" (""DateKey"" integer NOT NULL, ""Date"" timestamp NOT NULL, ""DayOfWeek"" integer NOT NULL, ""DayName"" character varying(20) NULL, CONSTRAINT ""PK_d_Date"" PRIMARY KEY (""DateKey""))",
                @"INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"") VALUES ('20260211100000_InitialCreatePostgres', '8.0.0') ON CONFLICT (""MigrationId"") DO NOTHING"
            };
        }
    }
}
