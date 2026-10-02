using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
namespace DealerManager.Tests.Identity;

public sealed class PostgresTenancyFactAttribute : FactAttribute
{
    public PostgresTenancyFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEALER_TENANCY_POSTGRES")))
            Skip = "Set DEALER_TENANCY_POSTGRES to verify migrations in an isolated rollback-only PostgreSQL schema.";
    }
}
public class TenancyMigrationTests
{
    [PostgresTenancyFact]
    public async Task MigrationPreservesLegacyDataSeedsOwnerAndEnforcesTenantForeignKeys()
    {
        await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("DEALER_TENANCY_POSTGRES"));
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        // Generated identifier, never supplied by a client. CREATE SCHEMA and all DDL/data roll back together.
        var schema = "tenant_verification_" + Guid.NewGuid().ToString("N");
        async Task Execute(string sql) { await using var command = new NpgsqlCommand(sql, connection, transaction); await command.ExecuteNonQueryAsync(); }
        async Task<long> Scalar(string sql) { await using var command = new NpgsqlCommand(sql, connection, transaction); return Convert.ToInt64(await command.ExecuteScalarAsync()); }
        try
        {
            await Execute($"CREATE SCHEMA \"{schema}\"; SET LOCAL search_path TO \"{schema}\";");
            await using var db = new DealerManagerDbContext(new DbContextOptionsBuilder<DealerManagerDbContext>().UseNpgsql(connection).Options);
            var migrator = db.GetService<IMigrator>();
            var migrations = db.Database.GetMigrations().ToArray();
            var index = Array.FindIndex(migrations, m => m.EndsWith("_AddIdentityAndDealershipTenancy"));
            var previous = migrations[index - 1]; var current = migrations[index];
            await Execute(migrator.GenerateScript(null, previous, MigrationsSqlGenerationOptions.NoTransactions));
            await Execute("""
                INSERT INTO "Candidates" ("Id", "Status", "Make", "Model", "Year", "ExpectedSellingPrice", "CreatedAt") VALUES (1, 3, 'Legacy', 'Car', 2020, 9000, CURRENT_TIMESTAMP);
                INSERT INTO "Vehicles" ("Id", "SourceCandidateId", "Status", "Make", "Model", "Year", "PurchaseDate", "CreatedAt") VALUES (1, 1, 0, 'Legacy', 'Car', 2020, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
                INSERT INTO "CapitalAccounts" ("Id", "Name", "Currency", "IsActive") VALUES (1, 'Legacy account', 'EUR', TRUE);
                INSERT INTO "FinancialTransactions" ("Id", "CapitalAccountId", "Type", "Direction", "Amount", "VehicleId", "Description", "OccurredAt", "CreatedAt") VALUES (1, 1, 1, 1, 6000, 1, 'Legacy purchase', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
                """);
            await Execute(migrator.GenerateScript(previous, current, MigrationsSqlGenerationOptions.NoTransactions));
            foreach (var table in new[] { "Candidates", "Vehicles", "CapitalAccounts", "FinancialTransactions" })
                Assert.Equal(-1, await Scalar($"SELECT \"DealershipId\" FROM \"{table}\" WHERE \"Id\"=1"));
            Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM \"AspNetRoles\" WHERE \"Name\"='Owner'"));
            Assert.Equal(0, await Scalar("SELECT COUNT(*) FROM \"AspNetUsers\""));
            Assert.Equal(0, await Scalar($"SELECT COUNT(*) FROM information_schema.columns WHERE table_schema='{schema}' AND column_name='DealershipId' AND column_default IS NOT NULL"));
            await Execute("""
                INSERT INTO "Dealerships" ("Id", "Name", "CreatedAt") VALUES (10, 'A', CURRENT_TIMESTAMP), (20, 'B', CURRENT_TIMESTAMP);
                INSERT INTO "CapitalAccounts" ("Id", "Name", "Currency", "IsActive", "DealershipId") VALUES (10, 'A', 'EUR', TRUE, 10);
                SAVEPOINT cross_tenant_check;
                """);
            var error = await Assert.ThrowsAsync<PostgresException>(() => Execute("""
                INSERT INTO "FinancialTransactions" ("Id", "CapitalAccountId", "DealershipId", "Type", "Direction", "Amount", "Description", "OccurredAt", "CreatedAt") VALUES (10, 10, 20, 0, 0, 100, 'Forbidden', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
                """));
            Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, error.SqlState);
            await Execute("ROLLBACK TO SAVEPOINT cross_tenant_check;");
            // Subsequent additive dossier migration must preserve legacy vehicles and their financial history.
            var dossier = migrations.Single(m => m.EndsWith("_AddVehicleDossier"));
            await Execute(migrator.GenerateScript(current, dossier, MigrationsSqlGenerationOptions.NoTransactions));
            Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM \"Vehicles\" WHERE \"Id\"=1 AND \"RegistrationNumber\" IS NULL AND \"FirstRegistration\" IS NULL AND \"PowerHp\" IS NULL"));
            Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM \"FinancialTransactions\" WHERE \"Id\"=1"));
            await Execute("""
                UPDATE "Vehicles" SET "FuelType"=' Дизел ', "Transmission"='Automatic', "DriveType"='RWD', "EuroStandard"='Euro 6', "BodyType"='Custom body', "Notes"='Existing note' WHERE "Id"=1;
                """);
            var enums = migrations.Single(m => m.EndsWith("_VehicleDossierEnums"));
            await Execute(migrator.GenerateScript(dossier, enums, MigrationsSqlGenerationOptions.NoTransactions));
            Assert.Equal(1, await Scalar("SELECT COUNT(*) FROM \"Vehicles\" WHERE \"Id\"=1 AND \"FuelType\"=1 AND \"Transmission\"=1 AND \"DriveType\"=1 AND \"EuroStandard\"=5 AND \"BodyType\"=99 AND \"Notes\" LIKE 'Existing note%' AND \"Notes\" LIKE '%Custom body%'"));
        }
        finally { await transaction.RollbackAsync(); }
    }
}
