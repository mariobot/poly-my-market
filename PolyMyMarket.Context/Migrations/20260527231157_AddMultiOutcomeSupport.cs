using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PolyMyMarket.Context.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiOutcomeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Outcome",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "MarketOutcomeId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarketType",
                table: "Markets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MarketOutcomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    LiquidityPool = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsWinner = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketOutcomes_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutcomePositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    MarketOutcomeId = table.Column<int>(type: "int", nullable: false),
                    Shares = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalInvested = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomePositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutcomePositions_MarketOutcomes_MarketOutcomeId",
                        column: x => x.MarketOutcomeId,
                        principalTable: "MarketOutcomes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutcomePositions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 1,
                column: "MarketType",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 2,
                column: "MarketType",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 3,
                column: "MarketType",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 4,
                column: "MarketType",
                value: 0);

            migrationBuilder.UpdateData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 5,
                column: "MarketType",
                value: 0);

            migrationBuilder.InsertData(
                table: "Markets",
                columns: new[] { "Id", "Category", "CreatedDate", "Description", "EndDate", "InitialLiquidity", "MarketType", "NoPool", "ResolutionDate", "ResolvedOutcome", "Status", "Title", "YesPool" },
                values: new object[] { 7, "Politics", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This market will resolve to the candidate who wins the 2028 United States Presidential Election.", new DateTime(2028, 11, 5, 23, 59, 59, 0, DateTimeKind.Utc), 2000m, 1, 0m, null, null, 0, "Who will win the 2028 US Presidential Election?", 0m });

            migrationBuilder.InsertData(
                table: "MarketOutcomes",
                columns: new[] { "Id", "Description", "DisplayOrder", "IsWinner", "LiquidityPool", "MarketId", "Name" },
                values: new object[,]
                {
                    { 1, "The Democratic Party nominee wins", 1, false, 500m, 7, "Democratic Candidate" },
                    { 2, "The Republican Party nominee wins", 2, false, 500m, 7, "Republican Candidate" },
                    { 3, "An independent or third-party candidate wins", 3, false, 500m, 7, "Independent Candidate" },
                    { 4, "Any other outcome", 4, false, 500m, 7, "Other" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_MarketOutcomeId",
                table: "Orders",
                column: "MarketOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketOutcomes_MarketId",
                table: "MarketOutcomes",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketOutcomes_MarketId_DisplayOrder",
                table: "MarketOutcomes",
                columns: new[] { "MarketId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_OutcomePositions_MarketOutcomeId",
                table: "OutcomePositions",
                column: "MarketOutcomeId");

            migrationBuilder.CreateIndex(
                name: "IX_OutcomePositions_UserId_MarketOutcomeId",
                table: "OutcomePositions",
                columns: new[] { "UserId", "MarketOutcomeId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_MarketOutcomes_MarketOutcomeId",
                table: "Orders",
                column: "MarketOutcomeId",
                principalTable: "MarketOutcomes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_MarketOutcomes_MarketOutcomeId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "OutcomePositions");

            migrationBuilder.DropTable(
                name: "MarketOutcomes");

            migrationBuilder.DropIndex(
                name: "IX_Orders_MarketOutcomeId",
                table: "Orders");

            migrationBuilder.DeleteData(
                table: "Markets",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DropColumn(
                name: "MarketOutcomeId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "MarketType",
                table: "Markets");

            migrationBuilder.AlterColumn<int>(
                name: "Outcome",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
