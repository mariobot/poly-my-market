using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PolyMyMarket.Context.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Markets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolutionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResolvedOutcome = table.Column<bool>(type: "bit", nullable: true),
                    InitialLiquidity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YesPool = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NoPool = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Markets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    Shares = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    OrderType = table.Column<int>(type: "int", nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MarketId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    YesShares = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NoShares = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AveragePriceYes = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AveragePriceNo = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalInvestedYes = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalInvestedNo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Positions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Markets",
                columns: new[] { "Id", "Category", "CreatedDate", "Description", "EndDate", "InitialLiquidity", "NoPool", "ResolutionDate", "ResolvedOutcome", "Status", "Title", "YesPool" },
                values: new object[,]
                {
                    { 1, "Cryptocurrency", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This market resolves to Yes if Bitcoin (BTC) reaches or exceeds $100,000 USD at any point before December 31, 2025 23:59:59 UTC. Otherwise resolves to No.", new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), 1000m, 500m, null, null, 0, "Will Bitcoin reach $100,000 by end of 2025?", 500m },
                    { 2, "Technology", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This market resolves to Yes if a credible AI system is widely recognized as passing the Turing Test by December 31, 2025. The determination will be based on mainstream media and academic consensus.", new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), 1000m, 500m, null, null, 0, "Will AI pass the Turing Test in 2025?", 500m },
                    { 3, "Economics", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This market resolves to Yes if the United States enters a recession (defined as two consecutive quarters of negative GDP growth) at any point during 2025.", new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), 1000m, 500m, null, null, 0, "Will there be a recession in 2025?", 500m },
                    { 4, "Space", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This market resolves to Yes if SpaceX successfully lands human astronauts on the surface of Mars before December 31, 2030.", new DateTime(2030, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), 1000m, 500m, null, null, 0, "Will SpaceX land humans on Mars by 2030?", 500m },
                    { 5, "Technology", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "This market resolves to Yes if Google, IBM, Microsoft, or another major tech company announces a significant quantum computing breakthrough in 2025 that is covered by mainstream tech media.", new DateTime(2025, 12, 31, 23, 59, 59, 0, DateTimeKind.Utc), 1000m, 500m, null, null, 0, "Will a major tech company announce a quantum computer breakthrough in 2025?", 500m }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Balance", "CreatedDate", "Email", "Name" },
                values: new object[] { 1, 10000m, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "demo@polymarket.com", "Demo User" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_MarketId",
                table: "Orders",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Timestamp",
                table: "Orders",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_MarketId",
                table: "Positions",
                column: "MarketId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UserId_MarketId",
                table: "Positions",
                columns: new[] { "UserId", "MarketId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "Markets");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
