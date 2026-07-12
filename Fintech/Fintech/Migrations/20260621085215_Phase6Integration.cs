using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fintech.Migrations
{
    /// <inheritdoc />
    public partial class Phase6Integration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "transaction_type",
                table: "capital_accounts",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "branch_id",
                table: "capital_accounts",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "capital_accounts",
                newName: "opening_balance");

            migrationBuilder.AddColumn<string>(
                name: "account_code",
                table: "journal_lines",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "capital_transaction_id",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "journal_entries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by",
                table: "journal_entries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "reversal_of_entry_id",
                table: "journal_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "capital_account_code",
                table: "capital_accounts",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "capital_accounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "current_balance",
                table: "capital_accounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_modified_at",
                table: "capital_accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "last_modified_by",
                table: "capital_accounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ownership_percentage",
                table: "capital_accounts",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "account_mappings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capital_account_code = table.Column<string>(type: "text", nullable: false),
                    capital_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gl_account_code = table.Column<string>(type: "text", nullable: false),
                    gl_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_account_mappings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accounting_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_code = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    closed_by = table.Column<string>(type: "text", nullable: true),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounting_periods", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "capital_account_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    capital_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false),
                    ownership_percentage = table.Column<decimal>(type: "numeric", nullable: false),
                    total_capital_at_date = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_capital_account_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_capital_account_histories_capital_accounts_capital_account_",
                        column: x => x.capital_account_id,
                        principalTable: "capital_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "capital_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_code = table.Column<string>(type: "text", nullable: false),
                    capital_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_type = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    reference_number = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    last_modified_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_capital_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_capital_transactions_capital_accounts_capital_account_id",
                        column: x => x.capital_account_id,
                        principalTable: "capital_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_capital_transactions_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_capital_transactions_users_created_by",
                        column: x => x.created_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ledger_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gl_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gl_account_code = table.Column<string>(type: "text", nullable: false),
                    account_name = table.Column<string>(type: "text", nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false),
                    total_debits = table.Column<long>(type: "bigint", nullable: false),
                    total_credits = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_balances", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ledger_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gl_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gl_account_code = table.Column<string>(type: "text", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    balance = table.Column<long>(type: "bigint", nullable: false),
                    debits = table.Column<long>(type: "bigint", nullable: false),
                    credits = table.Column<long>(type: "bigint", nullable: false),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ledger_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accrual_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reversal_journal_entry_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accrual_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accrual_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_accrual_entries_accounting_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_accrual_entries_journal_entries_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_accrual_entries_journal_entries_reversal_journal_entry_id",
                        column: x => x.reversal_journal_entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_accrual_entries_loan_cases_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loan_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "period_reversals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reversed_by = table.Column<string>(type: "text", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    deleted_accruals = table.Column<int>(type: "integer", nullable: true),
                    deleted_provisions = table.Column<int>(type: "integer", nullable: true),
                    deleted_journal_entries = table.Column<int>(type: "integer", nullable: true),
                    reversed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_period_reversals", x => x.id);
                    table.ForeignKey(
                        name: "fk_period_reversals_accounting_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "provision_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    previous_amount = table.Column<long>(type: "bigint", nullable: false),
                    journal_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    calculation_method = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provision_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_provision_entries_accounting_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "accounting_periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_provision_entries_journal_entries_journal_entry_id",
                        column: x => x.journal_entry_id,
                        principalTable: "journal_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_provision_entries_loan_cases_loan_id",
                        column: x => x.loan_id,
                        principalTable: "loan_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_journal_entries_capital_transaction_id",
                table: "journal_entries",
                column: "capital_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_mappings_branch_id_capital_account_code",
                table: "account_mappings",
                columns: new[] { "branch_id", "capital_account_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_account_mappings_gl_account_code",
                table: "account_mappings",
                column: "gl_account_code");

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_branch_id_status",
                table: "accounting_periods",
                columns: new[] { "branch_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_accounting_periods_period_code",
                table: "accounting_periods",
                column: "period_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_accrual_entries_journal_entry_id",
                table: "accrual_entries",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_accrual_entries_loan_id",
                table: "accrual_entries",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_accrual_entries_period_id",
                table: "accrual_entries",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_accrual_entries_reversal_journal_entry_id",
                table: "accrual_entries",
                column: "reversal_journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_accrual_entries_type",
                table: "accrual_entries",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_capital_account_histories_capital_account_id_effective_date",
                table: "capital_account_histories",
                columns: new[] { "capital_account_id", "effective_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_capital_transactions_approved_by",
                table: "capital_transactions",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "ix_capital_transactions_capital_account_id",
                table: "capital_transactions",
                column: "capital_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_capital_transactions_created_by",
                table: "capital_transactions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_capital_transactions_transaction_code",
                table: "capital_transactions",
                column: "transaction_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_balances_branch_id_gl_account_code",
                table: "ledger_balances",
                columns: new[] { "branch_id", "gl_account_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ledger_histories_branch_id_gl_account_code_date",
                table: "ledger_histories",
                columns: new[] { "branch_id", "gl_account_code", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_period_reversals_period_id",
                table: "period_reversals",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_provision_entries_journal_entry_id",
                table: "provision_entries",
                column: "journal_entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_provision_entries_loan_id",
                table: "provision_entries",
                column: "loan_id");

            migrationBuilder.CreateIndex(
                name: "ix_provision_entries_period_id",
                table: "provision_entries",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_provision_entries_status",
                table: "provision_entries",
                column: "status");

            migrationBuilder.AddForeignKey(
                name: "fk_journal_entries_capital_transactions_capital_transaction_id",
                table: "journal_entries",
                column: "capital_transaction_id",
                principalTable: "capital_transactions",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_journal_entries_capital_transactions_capital_transaction_id",
                table: "journal_entries");

            migrationBuilder.DropTable(
                name: "account_mappings");

            migrationBuilder.DropTable(
                name: "accrual_entries");

            migrationBuilder.DropTable(
                name: "capital_account_histories");

            migrationBuilder.DropTable(
                name: "capital_transactions");

            migrationBuilder.DropTable(
                name: "ledger_balances");

            migrationBuilder.DropTable(
                name: "ledger_histories");

            migrationBuilder.DropTable(
                name: "period_reversals");

            migrationBuilder.DropTable(
                name: "provision_entries");

            migrationBuilder.DropTable(
                name: "accounting_periods");

            migrationBuilder.DropIndex(
                name: "ix_journal_entries_capital_transaction_id",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "account_code",
                table: "journal_lines");

            migrationBuilder.DropColumn(
                name: "capital_transaction_id",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "created_by",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "reversal_of_entry_id",
                table: "journal_entries");

            migrationBuilder.DropColumn(
                name: "currency",
                table: "capital_accounts");

            migrationBuilder.DropColumn(
                name: "current_balance",
                table: "capital_accounts");

            migrationBuilder.DropColumn(
                name: "last_modified_at",
                table: "capital_accounts");

            migrationBuilder.DropColumn(
                name: "last_modified_by",
                table: "capital_accounts");

            migrationBuilder.DropColumn(
                name: "ownership_percentage",
                table: "capital_accounts");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "capital_accounts",
                newName: "transaction_type");

            migrationBuilder.RenameColumn(
                name: "opening_balance",
                table: "capital_accounts",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "capital_accounts",
                newName: "branch_id");

            migrationBuilder.AlterColumn<string>(
                name: "capital_account_code",
                table: "capital_accounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
