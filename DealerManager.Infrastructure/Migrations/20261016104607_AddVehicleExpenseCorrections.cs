using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DealerManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleExpenseCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReversed",
                table: "VehicleExpenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReplacementExpenseId",
                table: "VehicleExpenses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReason",
                table: "VehicleExpenses",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversalTransactionId",
                table: "VehicleExpenses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReversedAt",
                table: "VehicleExpenses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReversedByUserId",
                table: "VehicleExpenses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleExpenses_ReplacementExpenseId",
                table: "VehicleExpenses",
                column: "ReplacementExpenseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleExpenses_ReversalTransactionId",
                table: "VehicleExpenses",
                column: "ReversalTransactionId",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_VehicleExpenses_ReversalAudit",
                table: "VehicleExpenses",
                sql: "(NOT \"IsReversed\" AND \"ReversedAt\" IS NULL AND \"ReversalTransactionId\" IS NULL AND \"ReplacementExpenseId\" IS NULL) OR (\"IsReversed\" AND \"ReversedAt\" IS NOT NULL AND \"ReversalTransactionId\" IS NOT NULL AND \"ReversalReason\" IS NOT NULL AND length(trim(\"ReversalReason\")) > 0)");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleExpenses_FinancialTransactions_ReversalTransactionId",
                table: "VehicleExpenses",
                column: "ReversalTransactionId",
                principalTable: "FinancialTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleExpenses_VehicleExpenses_ReplacementExpenseId",
                table: "VehicleExpenses",
                column: "ReplacementExpenseId",
                principalTable: "VehicleExpenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleExpenses_FinancialTransactions_ReversalTransactionId",
                table: "VehicleExpenses");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleExpenses_VehicleExpenses_ReplacementExpenseId",
                table: "VehicleExpenses");

            migrationBuilder.DropIndex(
                name: "IX_VehicleExpenses_ReplacementExpenseId",
                table: "VehicleExpenses");

            migrationBuilder.DropIndex(
                name: "IX_VehicleExpenses_ReversalTransactionId",
                table: "VehicleExpenses");

            migrationBuilder.DropCheckConstraint(
                name: "CK_VehicleExpenses_ReversalAudit",
                table: "VehicleExpenses");

            migrationBuilder.DropColumn(
                name: "IsReversed",
                table: "VehicleExpenses");

            migrationBuilder.DropColumn(
                name: "ReplacementExpenseId",
                table: "VehicleExpenses");

            migrationBuilder.DropColumn(
                name: "ReversalReason",
                table: "VehicleExpenses");

            migrationBuilder.DropColumn(
                name: "ReversalTransactionId",
                table: "VehicleExpenses");

            migrationBuilder.DropColumn(
                name: "ReversedAt",
                table: "VehicleExpenses");

            migrationBuilder.DropColumn(
                name: "ReversedByUserId",
                table: "VehicleExpenses");
        }
    }
}
