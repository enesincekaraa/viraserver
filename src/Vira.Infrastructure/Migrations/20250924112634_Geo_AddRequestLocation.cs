using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Vira.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Geo_AddRequestLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Point>(
                 name: "Location",
                 table: "requests",
                 type: "geography (Point,4326)",
                 nullable: true);
            migrationBuilder.CreateIndex(
                 name: "IX_requests_Location",
                 table: "requests",
                 column: "Location")
                 .Annotation("Npgsql:IndexMethod", "GIST");

            // 3) Mevcut kayıtları doldur (sütun adları PASCAL CASE ve tırnaklı!)
            migrationBuilder.Sql(@"
UPDATE ""requests""
SET ""Location"" = ST_SetSRID(ST_MakePoint(""Longitude"", ""Latitude""), 4326)::geography
WHERE ""Location"" IS NULL;
");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_requests_Location",
                table: "requests");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "requests");
        }

    }
}
