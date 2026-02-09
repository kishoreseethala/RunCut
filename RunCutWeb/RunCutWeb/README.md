# RunCut Web Application

A web application for importing and managing GTFS (General Transit Feed Specification) data files.

## Architecture

This application follows **Clean Architecture** principles with the following layers:

- **Domain Layer** (`Domain/`): Contains entity models (DataSet, Route, Stop, StopTiming, Trip, Calendar)
- **Application Layer** (`Application/`): Contains interfaces and DTOs for business logic
- **Infrastructure Layer** (`Infrastructure/`): Contains Entity Framework DbContext, data access, and CSV parsing services
- **Presentation Layer** (`Controllers/`, `Views/`): Contains MVC controllers and views

## Features

- **Data Importer Tab**: Upload and import GTFS CSV files (Routes, Stops, Stop Timings, Trips, Calendar)
- **Tabbed Interface**: Modern UI with Bootstrap tabs for different functionalities
- **Entity Framework Core**: Model-first approach with SQL Server database

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or SQL Server Express)
- Visual Studio 2022 or Visual Studio Code

## Setup Instructions

### 1. Install NuGet Packages

The following packages are already configured in `RunCutWeb.csproj`:
- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.0)
- Microsoft.EntityFrameworkCore.Tools (8.0.0)
- CsvHelper (30.0.1)

### 2. Configure Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RunCutWebDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### 3. Create Database Migration

Open a terminal in the project root and run:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run the Application

```bash
dotnet run
```

Or press F5 in Visual Studio.

The application will be available at `https://localhost:5001` or `http://localhost:5000`.

## Usage

### Importing GTFS Data

1. Navigate to the **Data Importer** tab (default tab)
2. Enter a **Dataset Name** (required)
3. Upload one or more CSV files:
   - Routes.csv
   - Stops.csv
   - Stop Timings.csv
   - Trips.csv
   - Calendar.csv
4. Click **Import Data**
5. View the import statistics and results

## Project Structure

```
RunCutWeb/
├── Controllers/          # MVC Controllers
│   ├── HomeController.cs
│   └── DataImporterController.cs
├── Domain/              # Domain Layer
│   └── Entities/        # Entity models
├── Application/         # Application Layer
│   ├── Interfaces/      # Service interfaces
│   └── DTOs/           # Data Transfer Objects
├── Infrastructure/     # Infrastructure Layer
│   ├── Data/           # DbContext
│   └── Services/       # Service implementations
├── Views/              # Razor Views
│   ├── Home/
│   └── Shared/
└── wwwroot/            # Static files
```

## Database Schema

The application uses Entity Framework Core with the following main entities:

- **DataSet**: Container for imported GTFS data
- **Route**: Transit routes
- **Stop**: Transit stops/stations
- **StopTiming**: Stop times for trips
- **Trip**: Transit trips
- **Calendar**: Service calendar

## Notes

- All CSV files should follow the GTFS specification format
- The application validates file types (must be .csv)
- Import statistics are displayed after successful import
- Multiple datasets can be imported and stored separately
