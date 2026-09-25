using DealerManager.Application.FilterDtos;
using DealerManager.Application.FilterDtos.Candidate;
using DealerManager.Application.IRepository;
using DealerManager.Domain.Entities;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Infrastructure.Repository;
using DealerManager.Infrastructure.Service.Candidate;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using CandidateEntity = DealerManager.Domain.Entities.Candidate;

namespace DealerManager.Tests.Candidate;

internal sealed class FixedTimeProvider : TimeProvider
{
    public DateTimeOffset Now { get; set; } = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => Now;
}

internal sealed class CandidateTestStore : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    public DealerManagerDbContext Context { get; }
    public FixedTimeProvider Clock { get; } = new();
    public CandidateService Service { get; }
    public UnitOfWork UnitOfWork { get; }

    public CandidateTestStore(Func<IUnitOfWork, IUnitOfWork>? decorateUnitOfWork = null)
    {
        connection.Open();
        Context = new DealerManagerDbContext(new DbContextOptionsBuilder<DealerManagerDbContext>()
            .UseSqlite(connection).Options);
        Context.Database.EnsureCreated();
        UnitOfWork = new UnitOfWork(Context);
        Service = new CandidateService(
            new BaseRepository<CandidateEntity, CandidateFilterDto, DealerManagerDbContext>(Context),
            new BaseRepository<CandidateEstimate, FilterDto<CandidateEstimate>, DealerManagerDbContext>(Context),
            decorateUnitOfWork?.Invoke(UnitOfWork) ?? UnitOfWork, new CandidateFinancialCalculator(), Clock);
    }

    public void Dispose()
    {
        Context.Dispose();
        connection.Dispose();
    }
}
