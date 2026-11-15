using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ConvictionCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "portfolio");

            migrationBuilder.EnsureSchema(
                name: "trading");

            migrationBuilder.CreateTable(
                name: "broker",
                schema: "portfolio",
                columns: table => new
                {
                    broker_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_broker", x => x.broker_id);
                });

            migrationBuilder.CreateTable(
                name: "owner",
                schema: "portfolio",
                columns: table => new
                {
                    owner_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    owner_kind = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_owner", x => x.owner_id);
                });

            migrationBuilder.CreateTable(
                name: "portfolio",
                schema: "portfolio",
                columns: table => new
                {
                    portfolio_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    owner_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    reporting_currency = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_portfolio", x => x.portfolio_id);
                    table.UniqueConstraint("AK_portfolio_portfolio_id_owner_id", x => new { x.portfolio_id, x.owner_id });
                    table.ForeignKey(
                        name: "FK_portfolio_owner_owner_id",
                        column: x => x.owner_id,
                        principalSchema: "portfolio",
                        principalTable: "owner",
                        principalColumn: "owner_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "broker_account",
                schema: "portfolio",
                columns: table => new
                {
                    broker_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    portfolio_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    owner_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    broker_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    display_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    external_reference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    masked_reference = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    base_currency = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, nullable: false),
                    status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_broker_account", x => x.broker_account_id);
                    table.ForeignKey(
                        name: "FK_broker_account_broker_broker_id",
                        column: x => x.broker_id,
                        principalSchema: "portfolio",
                        principalTable: "broker",
                        principalColumn: "broker_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_broker_account_portfolio_portfolio_id_owner_id",
                        columns: x => new { x.portfolio_id, x.owner_id },
                        principalSchema: "portfolio",
                        principalTable: "portfolio",
                        principalColumns: new[] { "portfolio_id", "owner_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_batch",
                schema: "trading",
                columns: table => new
                {
                    import_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    broker_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    content_hash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    parser_version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_batch", x => x.import_batch_id);
                    table.ForeignKey(
                        name: "FK_import_batch_broker_account_broker_account_id",
                        column: x => x.broker_account_id,
                        principalSchema: "portfolio",
                        principalTable: "broker_account",
                        principalColumn: "broker_account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "portfolio",
                table: "broker",
                columns: new[] { "broker_id", "code", "display_name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-4111-8111-111111111111"), "firstrade", "Firstrade" },
                    { new Guid("22222222-2222-4222-8222-222222222222"), "ibkr", "Interactive Brokers" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_broker_code",
                schema: "portfolio",
                table: "broker",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_broker_account_broker_id_external_reference",
                schema: "portfolio",
                table: "broker_account",
                columns: new[] { "broker_id", "external_reference" },
                unique: true,
                filter: "[external_reference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_broker_account_portfolio_id_owner_id",
                schema: "portfolio",
                table: "broker_account",
                columns: new[] { "portfolio_id", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "IX_import_batch_broker_account_id_content_hash",
                schema: "trading",
                table: "import_batch",
                columns: new[] { "broker_account_id", "content_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_portfolio_owner_id",
                schema: "portfolio",
                table: "portfolio",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "import_batch",
                schema: "trading");

            migrationBuilder.DropTable(
                name: "broker_account",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "broker",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "portfolio",
                schema: "portfolio");

            migrationBuilder.DropTable(
                name: "owner",
                schema: "portfolio");
        }
    }
}
