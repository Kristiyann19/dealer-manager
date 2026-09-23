using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Candidate;
using DealerManager.Infrastructure.Repository;
using DealerManager.Infrastructure.Service.Candidate;

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
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<ICandidateFinancialCalculator,
                CandidateFinancialCalculator>();
            services.AddScoped<ICandidateService,
                CandidateService>();
        }

        public static void ConfigureAutoMapper(this IServiceCollection services, ILoggerFactory loggerFactory)
        {

        }
    }
}
