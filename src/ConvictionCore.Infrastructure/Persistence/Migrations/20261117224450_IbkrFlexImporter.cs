using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ConvictionCore.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IbkrFlexImporter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_import_batch_broker_account_id_content_hash",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "content_hash",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.AddColumn<int>(
                name: "corrected_count",
                schema: "trading",
                table: "import_batch",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "counts_json",
                schema: "trading",
                table: "import_batch",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_code",
                schema: "trading",
                table: "import_batch",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                schema: "trading",
                table: "import_batch",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "finished_at",
                schema: "trading",
                table: "import_batch",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "inserted_count",
                schema: "trading",
                table: "import_batch",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "requested_from",
                schema: "trading",
                table: "import_batch",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "requested_to",
                schema: "trading",
                table: "import_batch",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "source",
                schema: "trading",
                table: "import_batch",
                type: "varchar(32)",
                unicode: false,
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "started_at",
                schema: "trading",
                table: "import_batch",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "trigger",
                schema: "trading",
                table: "import_batch",
                type: "varchar(16)",
                unicode: false,
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "unchanged_count",
                schema: "trading",
                table: "import_batch",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "warnings_json",
                schema: "trading",
                table: "import_batch",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ibkr_raw_statement",
                schema: "trading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    import_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    broker_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    query_kind = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    query_id = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    to_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reference_code = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: true),
                    when_generated_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    when_generated_ibkr_et = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: true),
                    fetched_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    byte_length = table.Column<int>(type: "int", nullable: false),
                    sha256 = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    payload_encoding = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    payload = table.Column<byte[]>(type: "varbinary(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibkr_raw_statement", x => x.id);
                    table.ForeignKey(
                        name: "FK_ibkr_raw_statement_broker_account_broker_account_id",
                        column: x => x.broker_account_id,
                        principalSchema: "portfolio",
                        principalTable: "broker_account",
                        principalColumn: "broker_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_raw_statement_import_batch_import_batch_id",
                        column: x => x.import_batch_id,
                        principalSchema: "trading",
                        principalTable: "import_batch",
                        principalColumn: "import_batch_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ibkr_account_info",
                schema: "trading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    currency = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    account_type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    customer_type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    date_opened = table.Column<DateOnly>(type: "date", nullable: true),
                    date_funded = table.Column<DateOnly>(type: "date", nullable: true),
                    row_hash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    first_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_changed_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    broker_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibkr_account_info", x => x.id);
                    table.ForeignKey(
                        name: "FK_ibkr_account_info_broker_account_broker_account_id",
                        column: x => x.broker_account_id,
                        principalSchema: "portfolio",
                        principalTable: "broker_account",
                        principalColumn: "broker_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_account_info_ibkr_raw_statement_first_raw_statement_id",
                        column: x => x.first_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_account_info_ibkr_raw_statement_last_changed_raw_statement_id",
                        column: x => x.last_changed_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ibkr_corporate_action",
                schema: "trading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: true),
                    action_id = table.Column<long>(type: "bigint", nullable: true),
                    conid = table.Column<long>(type: "bigint", nullable: false),
                    type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    report_date = table.Column<DateOnly>(type: "date", nullable: true),
                    date_time_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    date_time_ibkr_et = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    asset_category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    action_description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    proceeds = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    value = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    fifo_pnl_realized = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    row_hash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    first_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_changed_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    broker_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibkr_corporate_action", x => x.id);
                    table.ForeignKey(
                        name: "FK_ibkr_corporate_action_broker_account_broker_account_id",
                        column: x => x.broker_account_id,
                        principalSchema: "portfolio",
                        principalTable: "broker_account",
                        principalColumn: "broker_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_corporate_action_ibkr_raw_statement_first_raw_statement_id",
                        column: x => x.first_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_corporate_action_ibkr_raw_statement_last_changed_raw_statement_id",
                        column: x => x.last_changed_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ibkr_open_position_lot",
                schema: "trading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    report_date = table.Column<DateOnly>(type: "date", nullable: false),
                    conid = table.Column<long>(type: "bigint", nullable: false),
                    originating_transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    originating_order_id = table.Column<long>(type: "bigint", nullable: true),
                    asset_category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    underlying_conid = table.Column<long>(type: "bigint", nullable: true),
                    put_call = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    strike = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    multiplier = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    fx_rate_to_base = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    position = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    side = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    mark_price = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    position_value = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    open_price = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    cost_basis_price = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    cost_basis_money = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    fifo_pnl_unrealized = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    open_date_time_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    open_date_time_ibkr_et = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    holding_period_date_time_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    holding_period_date_time_ibkr_et = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    row_hash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    first_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_changed_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    broker_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibkr_open_position_lot", x => x.id);
                    table.ForeignKey(
                        name: "FK_ibkr_open_position_lot_broker_account_broker_account_id",
                        column: x => x.broker_account_id,
                        principalSchema: "portfolio",
                        principalTable: "broker_account",
                        principalColumn: "broker_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_open_position_lot_ibkr_raw_statement_first_raw_statement_id",
                        column: x => x.first_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_open_position_lot_ibkr_raw_statement_last_changed_raw_statement_id",
                        column: x => x.last_changed_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ibkr_option_eae",
                schema: "trading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    trade_id = table.Column<long>(type: "bigint", nullable: false),
                    conid = table.Column<long>(type: "bigint", nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: true),
                    asset_category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    underlying_conid = table.Column<long>(type: "bigint", nullable: true),
                    underlying_symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    put_call = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    strike = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    multiplier = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    trade_price = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    mark_price = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    proceeds = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    commisions_and_tax = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    cost_basis = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    realized_pnl = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    mtm_pnl = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    fx_pnl = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    row_hash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    first_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_changed_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    broker_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibkr_option_eae", x => x.id);
                    table.ForeignKey(
                        name: "FK_ibkr_option_eae_broker_account_broker_account_id",
                        column: x => x.broker_account_id,
                        principalSchema: "portfolio",
                        principalTable: "broker_account",
                        principalColumn: "broker_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_option_eae_ibkr_raw_statement_first_raw_statement_id",
                        column: x => x.first_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_option_eae_ibkr_raw_statement_last_changed_raw_statement_id",
                        column: x => x.last_changed_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ibkr_security",
                schema: "trading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    conid = table.Column<long>(type: "bigint", nullable: false),
                    asset_category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    sub_category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    underlying_conid = table.Column<long>(type: "bigint", nullable: true),
                    underlying_symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    underlying_category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    multiplier = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    strike = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    put_call = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    listing_exchange = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    isin = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    cusip = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    figi = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    settlement_policy_method = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    row_hash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    first_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_changed_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibkr_security", x => x.id);
                    table.ForeignKey(
                        name: "FK_ibkr_security_ibkr_raw_statement_first_raw_statement_id",
                        column: x => x.first_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_security_ibkr_raw_statement_last_changed_raw_statement_id",
                        column: x => x.last_changed_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ibkr_trade_execution",
                schema: "trading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    trade_id = table.Column<long>(type: "bigint", nullable: true),
                    ib_exec_id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ib_order_id = table.Column<long>(type: "bigint", nullable: true),
                    orig_trade_id = table.Column<long>(type: "bigint", nullable: true),
                    orig_transaction_id = table.Column<long>(type: "bigint", nullable: true),
                    related_transaction_id = table.Column<long>(type: "bigint", nullable: true),
                    transaction_type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    asset_category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    conid = table.Column<long>(type: "bigint", nullable: false),
                    underlying_conid = table.Column<long>(type: "bigint", nullable: true),
                    symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    underlying_symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    put_call = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    strike = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    expiry = table.Column<DateOnly>(type: "date", nullable: true),
                    multiplier = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    fx_rate_to_base = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    date_time_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    date_time_ibkr_et = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    trade_date = table.Column<DateOnly>(type: "date", nullable: true),
                    settle_date_target = table.Column<DateOnly>(type: "date", nullable: true),
                    buy_sell = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    trade_price = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    proceeds = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    ib_commission = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    ib_commission_currency = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    taxes = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    net_cash = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    cost = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    fifo_pnl_realized = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    mtm_pnl = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    open_close_indicator = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    open_date_time_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    open_date_time_ibkr_et = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    holding_period_date_time_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    holding_period_date_time_ibkr_et = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    level_of_detail = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    row_hash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    first_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_changed_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    broker_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibkr_trade_execution", x => x.id);
                    table.UniqueConstraint("ux_ibkr_trade_execution_natural_key", x => new { x.broker_account_id, x.transaction_id });
                    table.ForeignKey(
                        name: "FK_ibkr_trade_execution_broker_account_broker_account_id",
                        column: x => x.broker_account_id,
                        principalSchema: "portfolio",
                        principalTable: "broker_account",
                        principalColumn: "broker_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_trade_execution_ibkr_raw_statement_first_raw_statement_id",
                        column: x => x.first_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_trade_execution_ibkr_raw_statement_last_changed_raw_statement_id",
                        column: x => x.last_changed_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ibkr_closed_lot",
                schema: "trading",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    close_transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    open_transaction_id = table.Column<long>(type: "bigint", nullable: false),
                    conid = table.Column<long>(type: "bigint", nullable: false),
                    asset_category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    symbol = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    currency = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    multiplier = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    quantity = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    trade_price = table.Column<decimal>(type: "decimal(28,10)", precision: 28, scale: 10, nullable: true),
                    cost = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    fifo_pnl_realized = table.Column<decimal>(type: "decimal(28,16)", precision: 28, scale: 16, nullable: true),
                    date_time_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    date_time_ibkr_et = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    trade_date = table.Column<DateOnly>(type: "date", nullable: true),
                    open_date_time_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    open_date_time_ibkr_et = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    holding_period_date_time_utc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    holding_period_date_time_ibkr_et = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    buy_sell = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    notes = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    row_hash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    first_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    last_changed_raw_statement_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    broker_account_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ibkr_closed_lot", x => x.id);
                    table.ForeignKey(
                        name: "FK_ibkr_closed_lot_broker_account_broker_account_id",
                        column: x => x.broker_account_id,
                        principalSchema: "portfolio",
                        principalTable: "broker_account",
                        principalColumn: "broker_account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_closed_lot_ibkr_raw_statement_first_raw_statement_id",
                        column: x => x.first_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_closed_lot_ibkr_raw_statement_last_changed_raw_statement_id",
                        column: x => x.last_changed_raw_statement_id,
                        principalSchema: "trading",
                        principalTable: "ibkr_raw_statement",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ibkr_closed_lot_ibkr_trade_execution_broker_account_id_close_transaction_id",
                        columns: x => new { x.broker_account_id, x.close_transaction_id },
                        principalSchema: "trading",
                        principalTable: "ibkr_trade_execution",
                        principalColumns: new[] { "broker_account_id", "transaction_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ux_import_batch_one_running_per_account",
                schema: "trading",
                table: "import_batch",
                column: "broker_account_id",
                unique: true,
                filter: "[status] = 'Running'");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_account_info_first_raw_statement_id",
                schema: "trading",
                table: "ibkr_account_info",
                column: "first_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_account_info_last_changed_raw_statement_id",
                schema: "trading",
                table: "ibkr_account_info",
                column: "last_changed_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "ux_ibkr_account_info_broker_account",
                schema: "trading",
                table: "ibkr_account_info",
                column: "broker_account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_closed_lot_first_raw_statement_id",
                schema: "trading",
                table: "ibkr_closed_lot",
                column: "first_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_closed_lot_last_changed_raw_statement_id",
                schema: "trading",
                table: "ibkr_closed_lot",
                column: "last_changed_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "ux_ibkr_closed_lot_natural_key",
                schema: "trading",
                table: "ibkr_closed_lot",
                columns: new[] { "broker_account_id", "close_transaction_id", "open_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_corporate_action_first_raw_statement_id",
                schema: "trading",
                table: "ibkr_corporate_action",
                column: "first_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_corporate_action_last_changed_raw_statement_id",
                schema: "trading",
                table: "ibkr_corporate_action",
                column: "last_changed_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "ux_ibkr_corporate_action_action_conid",
                schema: "trading",
                table: "ibkr_corporate_action",
                columns: new[] { "broker_account_id", "action_id", "conid" },
                unique: true,
                filter: "[transaction_id] IS NULL");

            migrationBuilder.CreateIndex(
                name: "ux_ibkr_corporate_action_transaction",
                schema: "trading",
                table: "ibkr_corporate_action",
                columns: new[] { "broker_account_id", "transaction_id" },
                unique: true,
                filter: "[transaction_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_open_position_lot_first_raw_statement_id",
                schema: "trading",
                table: "ibkr_open_position_lot",
                column: "first_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_open_position_lot_last_changed_raw_statement_id",
                schema: "trading",
                table: "ibkr_open_position_lot",
                column: "last_changed_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "ux_ibkr_open_position_lot_natural_key",
                schema: "trading",
                table: "ibkr_open_position_lot",
                columns: new[] { "broker_account_id", "report_date", "conid", "originating_transaction_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_option_eae_first_raw_statement_id",
                schema: "trading",
                table: "ibkr_option_eae",
                column: "first_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_option_eae_last_changed_raw_statement_id",
                schema: "trading",
                table: "ibkr_option_eae",
                column: "last_changed_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "ux_ibkr_option_eae_natural_key",
                schema: "trading",
                table: "ibkr_option_eae",
                columns: new[] { "broker_account_id", "trade_id", "conid", "transaction_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_raw_statement_broker_account_id_fetched_at",
                schema: "trading",
                table: "ibkr_raw_statement",
                columns: new[] { "broker_account_id", "fetched_at" });

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_raw_statement_import_batch_id",
                schema: "trading",
                table: "ibkr_raw_statement",
                column: "import_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_raw_statement_sha256",
                schema: "trading",
                table: "ibkr_raw_statement",
                column: "sha256");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_security_first_raw_statement_id",
                schema: "trading",
                table: "ibkr_security",
                column: "first_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_security_last_changed_raw_statement_id",
                schema: "trading",
                table: "ibkr_security",
                column: "last_changed_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "ux_ibkr_security_conid",
                schema: "trading",
                table: "ibkr_security",
                column: "conid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_trade_execution_first_raw_statement_id",
                schema: "trading",
                table: "ibkr_trade_execution",
                column: "first_raw_statement_id");

            migrationBuilder.CreateIndex(
                name: "IX_ibkr_trade_execution_last_changed_raw_statement_id",
                schema: "trading",
                table: "ibkr_trade_execution",
                column: "last_changed_raw_statement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ibkr_account_info",
                schema: "trading");

            migrationBuilder.DropTable(
                name: "ibkr_closed_lot",
                schema: "trading");

            migrationBuilder.DropTable(
                name: "ibkr_corporate_action",
                schema: "trading");

            migrationBuilder.DropTable(
                name: "ibkr_open_position_lot",
                schema: "trading");

            migrationBuilder.DropTable(
                name: "ibkr_option_eae",
                schema: "trading");

            migrationBuilder.DropTable(
                name: "ibkr_security",
                schema: "trading");

            migrationBuilder.DropTable(
                name: "ibkr_trade_execution",
                schema: "trading");

            migrationBuilder.DropTable(
                name: "ibkr_raw_statement",
                schema: "trading");

            migrationBuilder.DropIndex(
                name: "ux_import_batch_one_running_per_account",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "corrected_count",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "counts_json",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "error_code",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "error_message",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "finished_at",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "inserted_count",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "requested_from",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "requested_to",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "source",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "started_at",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "trigger",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "unchanged_count",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.DropColumn(
                name: "warnings_json",
                schema: "trading",
                table: "import_batch");

            migrationBuilder.AddColumn<string>(
                name: "content_hash",
                schema: "trading",
                table: "import_batch",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_import_batch_broker_account_id_content_hash",
                schema: "trading",
                table: "import_batch",
                columns: new[] { "broker_account_id", "content_hash" },
                unique: true);
        }
    }
}
