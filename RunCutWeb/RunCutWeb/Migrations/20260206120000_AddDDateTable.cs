using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunCutWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddDDateTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "d_Date",
                columns: table => new
                {
                    DateKey = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    DayName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_d_Date", x => x.DateKey);
                });

            // Seed date dimension: 2010-01-01 through 2040-12-31 (one row per day)
            migrationBuilder.Sql(@"
;WITH n AS (
    SELECT 0 i UNION ALL SELECT i + 1 FROM n WHERE i < 11322
)
INSERT INTO d_Date (DateKey, Date, DayOfWeek, DayName)
SELECT
    CONVERT(int, FORMAT(DATEADD(day, n.i, '2010-01-01'), 'yyyyMMdd')),
    DATEADD(day, n.i, '2010-01-01'),
    DATEPART(weekday, DATEADD(day, n.i, '2010-01-01')) - 1,
    DATENAME(weekday, DATEADD(day, n.i, '2010-01-01'))
FROM n
OPTION (MAXRECURSION 11323);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "d_Date");
        }
    }
}
