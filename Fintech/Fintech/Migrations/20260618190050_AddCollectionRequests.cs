using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fintech.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_customers_customer_code",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "customer_code",
                table: "customers");

            migrationBuilder.CreateTable(
                name: "collection_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_number = table.Column<string>(type: "text", nullable: false),
                    installment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    loan_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<long>(type: "bigint", nullable: false),
                    payment_mode = table.Column<string>(type: "text", nullable: false),
                    utr_ref = table.Column<string>(type: "text", nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    previous_due_amount = table.Column<long>(type: "bigint", nullable: false),
                    new_due_amount = table.Column<long>(type: "bigint", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_collection_requests_installments_installment_id",
                        column: x => x.installment_id,
                        principalTable: "installments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_collection_requests_loan_cases_loan_case_id",
                        column: x => x.loan_case_id,
                        principalTable: "loan_cases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_collection_requests_users_approved_by_id",
                        column: x => x.approved_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_collection_requests_users_requested_by_id",
                        column: x => x.requested_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_collection_requests_approved_by_id",
                table: "collection_requests",
                column: "approved_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_requests_installment_id",
                table: "collection_requests",
                column: "installment_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_requests_loan_case_id",
                table: "collection_requests",
                column: "loan_case_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_requests_request_number",
                table: "collection_requests",
                column: "request_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_collection_requests_requested_by_id",
                table: "collection_requests",
                column: "requested_by_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "collection_requests");

            migrationBuilder.AddColumn<string>(
                name: "customer_code",
                table: "customers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_customers_customer_code",
                table: "customers",
                column: "customer_code",
                unique: true);
        }
    }
}
