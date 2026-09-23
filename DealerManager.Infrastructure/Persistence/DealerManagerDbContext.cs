using DealerManager.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace DealerManager.Infrastructure.Persistence
{
    public class DealerManagerDbContext : DbContext
    {
        #region Candidate
        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<CandidateEstimate> CandidateEstimates { get; set; }
        public DbSet<CandidateEstimateItem> CandidateEstimateItems { get; set; }
        public DbSet<CandidatePhoto> CandidatePhotos { get; set; }
        #endregion

        #region Finance
        public DbSet<CapitalAccount> CapitalAccounts { get; set; }
        public DbSet<FinancialTransaction> FinancialTransactions { get; set; }
        #endregion

        #region Vehicle
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<VehicleCostPlanItem> VehicleCostPlanItems { get; set; }
        public DbSet<VehicleExpense> VehicleExpenses { get; set; }
        public DbSet<VehicleSale> VehicleSales { get; set; }
        public DbSet<VehicleStatusHistory> VehicleStatusHistories { get; set; }
        #endregion

        public DealerManagerDbContext(DbContextOptions<DealerManagerDbContext> options)
            : base(options)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ApplyConfigurations(modelBuilder);
            DisableCascadeDelete(modelBuilder);
        }

        protected void ApplyConfigurations(ModelBuilder modelBuilder)
        {
            var typesToRegister = Assembly.GetExecutingAssembly().GetTypes()
                   .Where(t => t.GetInterfaces().Any(gi =>
                       gi.IsGenericType
                       && gi.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)))
                   .ToList();

            foreach (var type in typesToRegister)
            {
                dynamic configurationInstance = Activator.CreateInstance(type);
                modelBuilder.ApplyConfiguration(configurationInstance);
            }
        }

        protected void DisableCascadeDelete(ModelBuilder modelBuilder)
        {
            modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => !fk.IsOwnership
                    && fk.DeleteBehavior == DeleteBehavior.Cascade)
                .ToList()
                .ForEach(e => e.DeleteBehavior = DeleteBehavior.Restrict);
        }
    }
}
