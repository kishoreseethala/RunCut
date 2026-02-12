# PostgreSQL schema setup for RunCutWeb

The app uses **PostgreSQL**. Tables are created automatically when the app starts (if missing), or you can create them once with the command below.

## 1. Start PostgreSQL

Make sure PostgreSQL is running on your machine (e.g. start the Windows service, or start it from your PostgreSQL installer).

## 2. Connection string (local development)

`appsettings.Development.json` is set for local PostgreSQL:

- **Host:** localhost  
- **Port:** 5432  
- **Database:** runcutweb  
- **Username:** postgres  
- **Password:** postgres  

If your local `postgres` user has a different password, edit `appsettings.Development.json` and update `DefaultConnection`.

## 3. Create database and tables (one-time)

From the project folder (`RunCutWeb\RunCutWeb`), run:

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run -- create-schema
```

This will:

- Create the database **runcutweb** if it does not exist  
- Create all required tables (DataSets, Routes, Stops, Trips, StopTimings, CalendarDates, d_Date)

You should see: `PostgreSQL schema created successfully.`

## 4. Run the app

```powershell
dotnet run
```

The app will connect to the `runcutweb` database and work normally.

**Note:** If you skip step 3, the app will still create the tables on first startup when it connects to PostgreSQL (as long as the database exists). Use `create-schema` if you want to create the database and tables without starting the web server.
