using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Candidate;
using DealerManager.Infrastructure.Repository;
using DealerManager.Infrastructure.Service.Candidate;
using DealerManager.Application.IService.Finance;
using DealerManager.Infrastructure.Service.Finance;

namespace DealerManager.WebAPI.Extensions
{
    public static class InternalServicesExtensions
    {
        public static void ConfigureRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(IBaseRepository<,,>),
                typeof(BaseRepository<,,>));
            services.AddScoped<IUnitOfWork,
                UnitOfWork>();
        }

        public static void ConfigureServices(this IServiceCollection services)
        {
            services.AddScoped<IPurchaseCandidateService, PurchaseCandidateService>();
            services.AddScoped<DealerManager.Application.IService.Vehicle.IVehicleService, DealerManager.Infrastructure.Service.Vehicle.VehicleService>();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<ICandidateFinancialCalculator,
                CandidateFinancialCalculator>();
            services.AddScoped<ICandidateService,
                CandidateService>();
            services.AddScoped<ICapitalAccountService,
                CapitalAccountService>();
        }

        public static void ConfigureAutoMapper(this IServiceCollection services, ILoggerFactory loggerFactory)
        {

        }
    }
}
