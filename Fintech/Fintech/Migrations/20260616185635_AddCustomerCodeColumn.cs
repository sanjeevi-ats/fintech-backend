using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fintech.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerCodeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_journal_lines_accounts_account_id",
                table: "journal_lines");

            migrationBuilder.DropIndex(
                name: "ix_journal_lines_account_id",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "public_id",
                table: "loan_cases");

            migrationBuilder.DropColumn(
                name: "account_id",
                table: "journal_lines");

            migrationBuilder.RenameColumn(
                name: "processing_fees",
                table: "loan_cases",
                newName: "file_charges_amount");

            migrationBuilder.RenameColumn(
                name: "principal",
                table: "loan_cases",
                newName: "finance_amount");

            migrationBuilder.RenameColumn(
                name: "type",
                table: "journal_lines",
                newName: "entry_type");

            migrationBuilder.RenameColumn(
                name: "date",
                table: "journal_entries",
                newName: "entry_date");

            migrationBuilder.RenameColumn(
                name: "no",
                table: "installments",
                newName: "installment_no");

            migrationBuilder.AlterColumn<string>(
                name: "role",
                table: "users",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "refresh_token",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "totp_enabled",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "user_code",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "loan_case_id",
                table: "receipts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "receipt_code",
                table: "receipts",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "loan_cases",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "loan_code",
                table: "loan_cases",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "entry_type",
                table: "journal_lines",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "account_name",
                table: "journal_lines",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "journal_line_code",
                table: "journal_lines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "journal_entry_code",
                table: "journal_entries",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "installments",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "installment_code",
                table: "installments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "customer_code",
                table: "customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "customers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "branch_code",
                table: "branches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "city",
                table: "branches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "branches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "settings_json",
                table: "branches",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "audit_log_code",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "account_code",
                table: "accounts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "code_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    next_sequence_number = table.Column<int>(type: "integer", nullable: false),
                    code_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_code_sequences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "day_ends",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_closed = table.Column<bool>(type: "boolean", nullable: false),
                    journals_locked = table.Column<int>(type: "integer", nullable: false),
                    total_collected = table.Column<long>(type: "bigint", nullable: false),
                    discrepancy = table.Column<long>(type: "bigint", nullable: false),
                    discrepancy_resolved = table.Column<bool>(type: "boolean", nullable: false),
                    day_end_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_day_ends", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "loan_products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    interest_rate = table.Column<double>(type: "double precision", nullable: false),
                    default_tenure_months = table.Column<int>(type: "integer", nullable: false),
                    repayment_frequency = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_loan_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "partners",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    equity_pct = table.Column<double>(type: "double precision", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    partner_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partners", x => x.id);
                    table.ForeignKey(
                        name: "fk_partners_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "capital_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    partner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    capital_account_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_capital_accounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_capital_accounts_partners_partner_id",
                        column: x => x.partner_id,
                        principalTable: "partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "profit_distributions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<string>(type: "text", nullable: false),
                    payout_amount = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    partner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    profit_distribution_code = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_profit_distributions", x => x.id);
                    table.ForeignKey(
                        name: "fk_profit_distributions_partners_partner_id",
                        column: x => x.partner_id,
                        principalTable: "partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_users_user_code",
                table: "users",
                column: "user_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_receipts_loan_case_id",
                table: "receipts",
                column: "loan_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipts_receipt_code",
                table: "receipts",
                column: "receipt_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_loan_cases_loan_code",
                table: "loan_cases",
                column: "loan_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_journal_line_code",
                table: "journal_lines",
                column: "journal_line_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_journal_entry_code",
                table: "journal_entries",
                column: "journal_entry_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_installments_installment_code",
                table: "installments",
                column: "installment_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_customer_code",
                table: "customers",
                column: "customer_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branches_branch_code",
                table: "branches",
                column: "branch_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_audit_log_code",
                table: "audit_logs",
                column: "audit_log_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accounts_account_code",
                table: "accounts",
                column: "account_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_capital_accounts_capital_account_code",
                table: "capital_accounts",
                column: "capital_account_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_capital_accounts_partner_id",
                table: "capital_accounts",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_code_sequences_entity_name_branch_id",
                table: "code_sequences",
                columns: new[] { "entity_name", "branch_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_day_ends_day_end_code",
                table: "day_ends",
                column: "day_end_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_partners_partner_code",
                table: "partners",
                column: "partner_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_partners_user_id",
                table: "partners",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_profit_distributions_partner_id",
                table: "profit_distributions",
                column: "partner_id");

            migrationBuilder.CreateIndex(
                name: "ix_profit_distributions_profit_distribution_code",
                table: "profit_distributions",
                column: "profit_distribution_code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_receipts_loan_cases_loan_case_id",
                table: "receipts",
                column: "loan_case_id",
                principalTable: "loan_cases",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_receipts_loan_cases_loan_case_id",
                table: "receipts");

            migrationBuilder.DropTable(
                name: "capital_accounts");

            migrationBuilder.DropTable(
                name: "code_sequences");

            migrationBuilder.DropTable(
                name: "day_ends");

            migrationBuilder.DropTable(
                name: "loan_products");

            migrationBuilder.DropTable(
                name: "profit_distributions");

            migrationBuilder.DropTable(
                name: "partners");

            migrationBuilder.DropIndex(
                name: "ix_users_user_code",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_receipts_loan_case_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "ix_receipts_receipt_code",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "ix_loan_cases_loan_code",
                table: "loan_cases");

            migrationBuilder.DropIndex(
                name: "ix_journal_lines_journal_line_code",
                table: "journal_lines");

            migrationBuilder.DropIndex(
                name: "ix_journal_entries_journal_entry_code",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "ix_installments_installment_code",
                table: "installments");

            migrationBuilder.DropIndex(
                name: "ix_customers_customer_code",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "ix_branches_branch_code",
                table: "branches");

            migrationBuilder.DropIndex(
                name: "ix_audit_logs_audit_log_code",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_accounts_account_code",
                table: "accounts");

            migrationBuilder.DropColumn(
                name: "refresh_token",
                table: "users");

            migrationBuilder.DropColumn(
                name: "totp_enabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "user_code",
                table: "users");

            migrationBuilder.DropColumn(
                name: "loan_case_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "receipt_code",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "loan_code",
                table: "loan_cases");

            migrationBuilder.DropColumn(
                name: "account_name",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "journal_line_code",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "journal_entry_code",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "installment_code",
                table: "installments");

            migrationBuilder.DropColumn(
                name: "customer_code",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "branch_code",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "city",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "settings_json",
                table: "branches");

            migrationBuilder.DropColumn(
                name: "audit_log_code",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "account_code",
                table: "accounts");

            migrationBuilder.RenameColumn(
                name: "finance_amount",
                table: "loan_cases",
                newName: "principal");

            migrationBuilder.RenameColumn(
                name: "file_charges_amount",
                table: "loan_cases",
                newName: "processing_fees");

            migrationBuilder.RenameColumn(
                name: "entry_type",
                table: "journal_lines",
                newName: "type");

            migrationBuilder.RenameColumn(
                name: "entry_date",
                table: "journal_entries",
                newName: "date");

            migrationBuilder.RenameColumn(
                name: "installment_no",
                table: "installments",
                newName: "no");

            migrationBuilder.AlterColumn<int>(
                name: "role",
                table: "users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "loan_cases",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "public_id",
                table: "loan_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "type",
                table: "journal_lines",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "account_id",
                table: "journal_lines",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "installments",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "ix_journal_lines_account_id",
                table: "journal_lines",
                column: "account_id");

            migrationBuilder.AddForeignKey(
                name: "fk_journal_lines_accounts_account_id",
                table: "journal_lines",
                column: "account_id",
                principalTable: "accounts",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
