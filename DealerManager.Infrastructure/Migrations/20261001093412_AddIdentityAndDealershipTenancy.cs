using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DealerManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAndDealershipTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_CapitalAccounts_CapitalAccountId",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_Vehicles_VehicleId",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Candidates_SourceCandidateId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_SourceCandidateId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_CapitalAccountId",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_VehicleId",
                table: "FinancialTransactions");

            migrationBuilder.AddColumn<int>(
                name: "DealershipId",
                table: "Vehicles",
                type: "integer",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "DealershipId",
                table: "FinancialTransactions",
                type: "integer",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "DealershipId",
                table: "CapitalAccounts",
                type: "integer",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "DealershipId",
                table: "Candidates",
                type: "integer",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Vehicles_Id_DealershipId",
                table: "Vehicles",
                columns: new[] { "Id", "DealershipId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_FinancialTransactions_Id_DealershipId",
                table: "FinancialTransactions",
                columns: new[] { "Id", "DealershipId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_CapitalAccounts_Id_DealershipId",
                table: "CapitalAccounts",
                columns: new[] { "Id", "DealershipId" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Candidates_Id_DealershipId",
                table: "Candidates",
                columns: new[] { "Id", "DealershipId" });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dealerships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealerships", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DealershipId = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Dealerships_DealershipId",
                        column: x => x.DealershipId,
                        principalTable: "Dealerships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[] { 1, "owner-v1", "Owner", "OWNER" });

            migrationBuilder.InsertData(
                table: "Dealerships",
                columns: new[] { "Id", "CreatedAt", "Name" },
                values: new object[] { -1, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Legacy — unassigned" });

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_DealershipId",
                table: "Vehicles",
                column: "DealershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_SourceCandidateId_DealershipId",
                table: "Vehicles",
                columns: new[] { "SourceCandidateId", "DealershipId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_CapitalAccountId_DealershipId",
                table: "FinancialTransactions",
                columns: new[] { "CapitalAccountId", "DealershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_DealershipId",
                table: "FinancialTransactions",
                column: "DealershipId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_VehicleId_DealershipId",
                table: "FinancialTransactions",
                columns: new[] { "VehicleId", "DealershipId" });

            migrationBuilder.CreateIndex(
                name: "IX_CapitalAccounts_DealershipId",
                table: "CapitalAccounts",
                column: "DealershipId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_DealershipId",
                table: "Candidates",
                column: "DealershipId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DealershipId",
                table: "AspNetUsers",
                column: "DealershipId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Candidates_Dealerships_DealershipId",
                table: "Candidates",
                column: "DealershipId",
                principalTable: "Dealerships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CapitalAccounts_Dealerships_DealershipId",
                table: "CapitalAccounts",
                column: "DealershipId",
                principalTable: "Dealerships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_CapitalAccounts_CapitalAccountId_Deal~",
                table: "FinancialTransactions",
                columns: new[] { "CapitalAccountId", "DealershipId" },
                principalTable: "CapitalAccounts",
                principalColumns: new[] { "Id", "DealershipId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_Dealerships_DealershipId",
                table: "FinancialTransactions",
                column: "DealershipId",
                principalTable: "Dealerships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_Vehicles_VehicleId_DealershipId",
                table: "FinancialTransactions",
                columns: new[] { "VehicleId", "DealershipId" },
                principalTable: "Vehicles",
                principalColumns: new[] { "Id", "DealershipId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Candidates_SourceCandidateId_DealershipId",
                table: "Vehicles",
                columns: new[] { "SourceCandidateId", "DealershipId" },
                principalTable: "Candidates",
                principalColumns: new[] { "Id", "DealershipId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Dealerships_DealershipId",
                table: "Vehicles",
                column: "DealershipId",
                principalTable: "Dealerships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
            // Temporary defaults only backfill pre-authentication records into an inaccessible legacy tenant.
            // New records must supply a tenant; never default new data into the legacy tenant.
            migrationBuilder.Sql("ALTER TABLE \"Candidates\" ALTER COLUMN \"DealershipId\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Vehicles\" ALTER COLUMN \"DealershipId\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"CapitalAccounts\" ALTER COLUMN \"DealershipId\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"FinancialTransactions\" ALTER COLUMN \"DealershipId\" DROP DEFAULT;");
            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('\"AspNetRoles\"', 'Id'), 1, true);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Candidates_Dealerships_DealershipId",
                table: "Candidates");

            migrationBuilder.DropForeignKey(
                name: "FK_CapitalAccounts_Dealerships_DealershipId",
                table: "CapitalAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_CapitalAccounts_CapitalAccountId_Deal~",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_Dealerships_DealershipId",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_Vehicles_VehicleId_DealershipId",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Candidates_SourceCandidateId_DealershipId",
                table: "Vehicles");

            migrationBuilder.DropForeignKey(
                name: "FK_Vehicles_Dealerships_DealershipId",
                table: "Vehicles");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Dealerships");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Vehicles_Id_DealershipId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_DealershipId",
                table: "Vehicles");

            migrationBuilder.DropIndex(
                name: "IX_Vehicles_SourceCandidateId_DealershipId",
                table: "Vehicles");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_FinancialTransactions_Id_DealershipId",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_CapitalAccountId_DealershipId",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_DealershipId",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_VehicleId_DealershipId",
                table: "FinancialTransactions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_CapitalAccounts_Id_DealershipId",
                table: "CapitalAccounts");

            migrationBuilder.DropIndex(
                name: "IX_CapitalAccounts_DealershipId",
                table: "CapitalAccounts");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Candidates_Id_DealershipId",
                table: "Candidates");

            migrationBuilder.DropIndex(
                name: "IX_Candidates_DealershipId",
                table: "Candidates");

            migrationBuilder.DropColumn(
                name: "DealershipId",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "DealershipId",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "DealershipId",
                table: "CapitalAccounts");

            migrationBuilder.DropColumn(
                name: "DealershipId",
                table: "Candidates");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_SourceCandidateId",
                table: "Vehicles",
                column: "SourceCandidateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_CapitalAccountId",
                table: "FinancialTransactions",
                column: "CapitalAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_VehicleId",
                table: "FinancialTransactions",
                column: "VehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_CapitalAccounts_CapitalAccountId",
                table: "FinancialTransactions",
                column: "CapitalAccountId",
                principalTable: "CapitalAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_Vehicles_VehicleId",
                table: "FinancialTransactions",
                column: "VehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicles_Candidates_SourceCandidateId",
                table: "Vehicles",
                column: "SourceCandidateId",
                principalTable: "Candidates",
                principalColumn: "Id");
        }
    }
}
