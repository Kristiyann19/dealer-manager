using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DealerManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleSales_FinancialTransactions_FinancialTransactionId",
                table: "VehicleSales");

            migrationBuilder.AlterColumn<int>(
                name: "FinancialTransactionId",
                table: "VehicleSales",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ReservationId",
                table: "VehicleSales",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VehicleReservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    VehicleId = table.Column<int>(type: "integer", nullable: false),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CustomerEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    DepositAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ReservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CancellationNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DepositTransactionId = table.Column<int>(type: "integer", nullable: false),
                    RefundTransactionId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VehicleReservations", x => x.Id);
                    table.CheckConstraint("CK_VehicleReservations_PositiveDeposit", "\"DepositAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_VehicleReservations_FinancialTransactions_DepositTransactio~",
                        column: x => x.DepositTransactionId,
                        principalTable: "FinancialTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleReservations_FinancialTransactions_RefundTransaction~",
                        column: x => x.RefundTransactionId,
                        principalTable: "FinancialTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VehicleReservations_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VehicleSales_ReservationId",
                table: "VehicleSales",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReservations_DepositTransactionId",
                table: "VehicleReservations",
                column: "DepositTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReservations_RefundTransactionId",
                table: "VehicleReservations",
                column: "RefundTransactionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VehicleReservations_VehicleId",
                table: "VehicleReservations",
                column: "VehicleId",
                unique: true,
                filter: "\"Status\" = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleSales_FinancialTransactions_FinancialTransactionId",
                table: "VehicleSales",
                column: "FinancialTransactionId",
                principalTable: "FinancialTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleSales_VehicleReservations_ReservationId",
                table: "VehicleSales",
                column: "ReservationId",
                principalTable: "VehicleReservations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VehicleSales_FinancialTransactions_FinancialTransactionId",
                table: "VehicleSales");

            migrationBuilder.DropForeignKey(
                name: "FK_VehicleSales_VehicleReservations_ReservationId",
                table: "VehicleSales");

            migrationBuilder.DropTable(
                name: "VehicleReservations");

            migrationBuilder.DropIndex(
                name: "IX_VehicleSales_ReservationId",
                table: "VehicleSales");

            migrationBuilder.DropColumn(
                name: "ReservationId",
                table: "VehicleSales");

            migrationBuilder.AlterColumn<int>(
                name: "FinancialTransactionId",
                table: "VehicleSales",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_VehicleSales_FinancialTransactions_FinancialTransactionId",
                table: "VehicleSales",
                column: "FinancialTransactionId",
                principalTable: "FinancialTransactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
