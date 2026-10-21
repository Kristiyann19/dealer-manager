using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DealerManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateAuctionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcquisitionType",
                table: "Candidates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CandidateAuctionDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CandidateId = table.Column<int>(type: "integer", nullable: false),
                    Platform = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ListingUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ObservedBid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxBid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateAuctionDetails", x => x.Id);
                    table.CheckConstraint("CK_CandidateAuctionDetails_MaxBid", "\"MaxBid\" IS NULL OR \"MaxBid\" > 0");
                    table.CheckConstraint("CK_CandidateAuctionDetails_ObservedBid", "\"ObservedBid\" IS NULL OR \"ObservedBid\" >= 0");
                    table.ForeignKey(
                        name: "FK_CandidateAuctionDetails_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateAuctionDetails_CandidateId",
                table: "CandidateAuctionDetails",
                column: "CandidateId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateAuctionDetails");

            migrationBuilder.DropColumn(
                name: "AcquisitionType",
                table: "Candidates");
        }
    }
}
