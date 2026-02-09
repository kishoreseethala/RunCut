using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunCutWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DataSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Calendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataSetId = table.Column<int>(type: "int", nullable: false),
                    ServiceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Monday = table.Column<int>(type: "int", nullable: false),
                    Tuesday = table.Column<int>(type: "int", nullable: false),
                    Wednesday = table.Column<int>(type: "int", nullable: false),
                    Thursday = table.Column<int>(type: "int", nullable: false),
                    Friday = table.Column<int>(type: "int", nullable: false),
                    Saturday = table.Column<int>(type: "int", nullable: false),
                    Sunday = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Calendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Calendars_DataSets_DataSetId",
                        column: x => x.DataSetId,
                        principalTable: "DataSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataSetId = table.Column<int>(type: "int", nullable: false),
                    RouteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AgencyId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteLongName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteType = table.Column<int>(type: "int", nullable: true),
                    RouteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RouteTextColor = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_DataSets_DataSetId",
                        column: x => x.DataSetId,
                        principalTable: "DataSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataSetId = table.Column<int>(type: "int", nullable: false),
                    StopId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StopCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopLat = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StopLon = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ZoneId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocationType = table.Column<int>(type: "int", nullable: true),
                    ParentStation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopTimeZone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WheelchairBoarding = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stops_DataSets_DataSetId",
                        column: x => x.DataSetId,
                        principalTable: "DataSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataSetId = table.Column<int>(type: "int", nullable: false),
                    RouteId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TripId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TripHeadsign = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TripShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DirectionId = table.Column<int>(type: "int", nullable: true),
                    BlockId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShapeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WheelchairAccessible = table.Column<int>(type: "int", nullable: true),
                    BikesAllowed = table.Column<int>(type: "int", nullable: true),
                    RouteId1 = table.Column<int>(type: "int", nullable: true),
                    CalendarId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Trips_Calendars_CalendarId",
                        column: x => x.CalendarId,
                        principalTable: "Calendars",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Trips_DataSets_DataSetId",
                        column: x => x.DataSetId,
                        principalTable: "DataSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Trips_Routes_RouteId1",
                        column: x => x.RouteId1,
                        principalTable: "Routes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StopTimings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataSetId = table.Column<int>(type: "int", nullable: false),
                    TripId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ArrivalTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartureTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StopSequence = table.Column<int>(type: "int", nullable: true),
                    StopHeadsign = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PickupType = table.Column<int>(type: "int", nullable: true),
                    DropOffType = table.Column<int>(type: "int", nullable: true),
                    ShapeDistTraveled = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Timepoint = table.Column<int>(type: "int", nullable: true),
                    StopId1 = table.Column<int>(type: "int", nullable: true),
                    TripId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StopTimings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StopTimings_DataSets_DataSetId",
                        column: x => x.DataSetId,
                        principalTable: "DataSets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StopTimings_Stops_StopId1",
                        column: x => x.StopId1,
                        principalTable: "Stops",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StopTimings_Trips_TripId1",
                        column: x => x.TripId1,
                        principalTable: "Trips",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Calendars_DataSetId_ServiceId",
                table: "Calendars",
                columns: new[] { "DataSetId", "ServiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_Routes_DataSetId_RouteId",
                table: "Routes",
                columns: new[] { "DataSetId", "RouteId" });

            migrationBuilder.CreateIndex(
                name: "IX_Stops_DataSetId_StopId",
                table: "Stops",
                columns: new[] { "DataSetId", "StopId" });

            migrationBuilder.CreateIndex(
                name: "IX_StopTimings_DataSetId_TripId_StopSequence",
                table: "StopTimings",
                columns: new[] { "DataSetId", "TripId", "StopSequence" });

            migrationBuilder.CreateIndex(
                name: "IX_StopTimings_StopId1",
                table: "StopTimings",
                column: "StopId1");

            migrationBuilder.CreateIndex(
                name: "IX_StopTimings_TripId1",
                table: "StopTimings",
                column: "TripId1");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_CalendarId",
                table: "Trips",
                column: "CalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_DataSetId_TripId",
                table: "Trips",
                columns: new[] { "DataSetId", "TripId" });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_RouteId1",
                table: "Trips",
                column: "RouteId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StopTimings");

            migrationBuilder.DropTable(
                name: "Stops");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "Calendars");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "DataSets");
        }
    }
}
