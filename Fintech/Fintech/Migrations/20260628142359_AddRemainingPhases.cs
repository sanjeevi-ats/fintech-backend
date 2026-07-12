using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fintech.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingPhases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "remarks",
                table: "receipts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "approved_at",
                table: "loan_cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_id",
                table: "loan_cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "document_urls",
                table: "loan_cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "loan_cases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "submitted_at",
                table: "loan_cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "submitted_by_id",
                table: "loan_cases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_code",
                table: "customers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cash_flow_forecasts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    forecast_period = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    projected_cash_flow = table.Column<long>(type: "bigint", nullable: false),
                    confidence_level = table.Column<int>(type: "integer", nullable: false),
                    assumptions = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_flow_forecasts", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_flow_forecasts_accounting_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cash_flow_forecasts_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cash_flow_statements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operating_cash_flow = table.Column<long>(type: "bigint", nullable: false),
                    investing_cash_flow = table.Column<long>(type: "bigint", nullable: false),
                    financing_cash_flow = table.Column<long>(type: "bigint", nullable: false),
                    net_cash_flow = table.Column<long>(type: "bigint", nullable: false),
                    beginning_balance = table.Column<long>(type: "bigint", nullable: false),
                    ending_balance = table.Column<long>(type: "bigint", nullable: false),
                    statement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_flow_statements", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_flow_statements_accounting_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_cash_flow_statements_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "company_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    logo_base64 = table.Column<string>(type: "text", nullable: true),
                    logo_mime_type = table.Column<string>(type: "text", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "text", nullable: true),
                    state = table.Column<string>(type: "text", nullable: true),
                    pincode = table.Column<string>(type: "text", nullable: true),
                    phone = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "text", nullable: true),
                    website = table.Column<string>(type: "text", nullable: true),
                    gst_number = table.Column<string>(type: "text", nullable: true),
                    pan_number = table.Column<string>(type: "text", nullable: true),
                    cin_number = table.Column<string>(type: "text", nullable: true),
                    tagline = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "interest_calculations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    daily_rate = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false),
                    accrual_amount = table.Column<long>(type: "bigint", nullable: false),
                    calculation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    interest_type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interest_calculations", x => x.id);
                    table.ForeignKey(
                        name: "fk_interest_calculations_accounting_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_interest_calculations_loan_cases_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loan_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "interest_postings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    posted_amount = table.Column<long>(type: "bigint", nullable: false),
                    posted_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interest_postings", x => x.id);
                    table.ForeignKey(
                        name: "fk_interest_postings_accounting_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_interest_postings_journal_entries_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_interest_postings_loan_cases_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loan_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "interest_waivers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    waiver_amount = table.Column<long>(type: "bigint", nullable: false),
                    waiver_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    approved_by = table.Column<string>(type: "text", nullable: true),
                    approval_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reversal_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interest_waivers", x => x.id);
                    table.ForeignKey(
                        name: "fk_interest_waivers_journal_entries_reversal_journal_entry_id",
                        column: x => x.reversal_journal_entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_interest_waivers_loan_cases_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loan_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "profit_loss_statements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: true),
                    total_revenue = table.Column<long>(type: "bigint", nullable: false),
                    total_expenses = table.Column<long>(type: "bigint", nullable: false),
                    gross_profit = table.Column<long>(type: "bigint", nullable: false),
                    net_profit = table.Column<long>(type: "bigint", nullable: false),
                    profit_margin = table.Column<decimal>(type: "numeric", nullable: false),
                    statement_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profit_loss_statements", x => x.id);
                    table.ForeignKey(
                        name: "fk_profit_loss_statements_accounting_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_profit_loss_statements_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cash_flow_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cash_flow_statement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    item_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    reference_code = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cash_flow_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_cash_flow_items_cash_flow_statements_cash_flow_statement_id",
                        column: x => x.cash_flow_statement_id,
                        principalTable: "cash_flow_statements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expense_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profit_loss_statement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    reference_code = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expense_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_expense_lines_profit_loss_statements_profit_loss_statement_",
                        column: x => x.profit_loss_statement_id,
                        principalTable: "profit_loss_statements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "revenue_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    profit_loss_statement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    reference_code = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_revenue_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_revenue_lines_profit_loss_statements_profit_loss_statement_",
                        column: x => x.profit_loss_statement_id,
                        principalTable: "profit_loss_statements",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cash_flow_forecasts_branch_id",
                table: "cash_flow_forecasts",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_flow_forecasts_forecast_period",
                table: "cash_flow_forecasts",
                column: "forecast_period");

            migrationBuilder.CreateIndex(
                name: "ix_cash_flow_forecasts_period_id",
                table: "cash_flow_forecasts",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_flow_items_cash_flow_statement_id",
                table: "cash_flow_items",
                column: "cash_flow_statement_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_flow_items_category",
                table: "cash_flow_items",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_cash_flow_items_item_type",
                table: "cash_flow_items",
                column: "item_type");

            migrationBuilder.CreateIndex(
                name: "ix_cash_flow_statements_branch_id",
                table: "cash_flow_statements",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_flow_statements_period_id",
                table: "cash_flow_statements",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_cash_flow_statements_statement_date",
                table: "cash_flow_statements",
                column: "statement_date");

            migrationBuilder.CreateIndex(
                name: "ix_expense_lines_category",
                table: "expense_lines",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_expense_lines_profit_loss_statement_id",
                table: "expense_lines",
                column: "profit_loss_statement_id");

            migrationBuilder.CreateIndex(
                name: "ix_interest_calculations_calculation_date",
                table: "interest_calculations",
                column: "calculation_date");

            migrationBuilder.CreateIndex(
                name: "ix_interest_calculations_loan_id",
                table: "interest_calculations",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_interest_calculations_period_id",
                table: "interest_calculations",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_interest_postings_journal_entry_id",
                table: "interest_postings",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_interest_postings_loan_id",
                table: "interest_postings",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_interest_postings_period_id",
                table: "interest_postings",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_interest_postings_posted_date",
                table: "interest_postings",
                column: "posted_date");

            migrationBuilder.CreateIndex(
                name: "ix_interest_waivers_loan_id",
                table: "interest_waivers",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_interest_waivers_reversal_journal_entry_id",
                table: "interest_waivers",
                column: "reversal_journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_interest_waivers_waiver_date",
                table: "interest_waivers",
                column: "waiver_date");

            migrationBuilder.CreateIndex(
                name: "ix_profit_loss_statements_branch_id",
                table: "profit_loss_statements",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_profit_loss_statements_period_id",
                table: "profit_loss_statements",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_profit_loss_statements_statement_date",
                table: "profit_loss_statements",
                column: "statement_date");

            migrationBuilder.CreateIndex(
                name: "ix_revenue_lines_category",
                table: "revenue_lines",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_revenue_lines_profit_loss_statement_id",
                table: "revenue_lines",
                column: "profit_loss_statement_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cash_flow_forecasts");

            migrationBuilder.DropTable(
                name: "cash_flow_items");

            migrationBuilder.DropTable(
                name: "company_settings");

            migrationBuilder.DropTable(
                name: "expense_lines");

            migrationBuilder.DropTable(
                name: "interest_calculations");

            migrationBuilder.DropTable(
                name: "interest_postings");

            migrationBuilder.DropTable(
                name: "interest_waivers");

            migrationBuilder.DropTable(
                name: "revenue_lines");

            migrationBuilder.DropTable(
                name: "cash_flow_statements");

            migrationBuilder.DropTable(
                name: "profit_loss_statements");

            migrationBuilder.DropColumn(
                name: "remarks",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "approved_at",
                table: "loan_cases");

            migrationBuilder.DropColumn(
                name: "approved_by_id",
                table: "loan_cases");

            migrationBuilder.DropColumn(
                name: "document_urls",
                table: "loan_cases");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "loan_cases");

            migrationBuilder.DropColumn(
                name: "submitted_at",
                table: "loan_cases");

            migrationBuilder.DropColumn(
                name: "submitted_by_id",
                table: "loan_cases");

            migrationBuilder.DropColumn(
                name: "customer_code",
                table: "customers");
        }
    }
}
