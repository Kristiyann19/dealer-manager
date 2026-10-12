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
            services.AddScoped<DealerManager.Application.IService.Dashboard.IDashboardService, DealerManager.Infrastructure.Service.Dashboard.DashboardService>();
            services.AddScoped<IPurchaseCandidateService, PurchaseCandidateService>();
            services.AddScoped<DealerManager.Application.IService.Vehicle.IVehicleService, DealerManager.Infrastructure.Service.Vehicle.VehicleService>();
            services.AddScoped<DealerManager.Application.IService.Vehicle.IVehicleFilesService, DealerManager.Infrastructure.Service.Vehicle.VehicleFilesService>();
            services.AddSingleton<DealerManager.Application.IService.Storage.IFileStorage>(provider => {
                var environment = provider.GetRequiredService<IWebHostEnvironment>();
                var configured = provider.GetRequiredService<IConfiguration>()["FileStorage:RootPath"] ?? "DealerManagerStorage";
                var root = Path.GetFullPath(configured, environment.ContentRootPath);
                var webRoot = Path.GetFullPath(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"));
                if (root.Equals(webRoot, StringComparison.OrdinalIgnoreCase) || root.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Private file storage must be outside wwwroot.");
                return new DealerManager.Infrastructure.Storage.LocalFileStorage(root);
            });
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton<ICandidateFinancialCalculator,
                CandidateFinancialCalculator>();
            services.AddScoped<ICandidateService,
                CandidateService>();
            services.AddScoped<ICapitalAccountService,
                CapitalAccountService>();
        }

        public static void ConfigureAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(configuration => { },
                typeof(DealerManager.Infrastructure.AutoMapperProfiles.CandidateProfile).Assembly);
        }
    }
}
