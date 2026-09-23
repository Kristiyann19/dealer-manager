using DealerManager.Application.FilterDtos;
using DealerManager.Domain.Common;
using DealerManager.Domain.Enums.BaseRepositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DealerManager.Application.IRepository
{
    public interface IBaseRepository<TEntity, TFilterDto, TDbContext>
       where TEntity : IEntity
       where TFilterDto : IFilterDto<TEntity>
       where TDbContext : DbContext
    {
        Task<TEntity> GetById(int id, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null);
        Task<TEntity> GetByProperties(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null);

        Task<List<TEntity>> GetList(TFilterDto filerDto, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null);
        Task<List<TEntity>> GetListByProperties(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null);

        Task<(List<TEntity>, int)> GetAll(TFilterDto filerDto, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null);

        Task<List<TEntity>> GetAllWithoutCount(TFilterDto filerDto, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null);
        Task<int> GetCount(TFilterDto filter, CancellationToken cancellationToken);

        IQueryable<TEntity> GetQuery(TFilterDto filerDto, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null);
        IQueryable<TEntity> GetQueryByProperties(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null);
        IQueryable<int> GetIdQuery(TFilterDto filerDto);

        Task<bool> AnyEntity(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken);

        Task Create(TEntity entity);
        Task CreateRange(List<TEntity> entities);
        void Update(TEntity entity, TEntity modificationEntity);
        void UpdateFromDto<TDto>(TEntity entity, TDto modificationDto);
        void Delete(TEntity entity);

        void SetEntryModified(TEntity entity);
        void SetEntriesModified(List<TEntity> entities);

        Func<IQueryable<TEntity>, IQueryable<TEntity>> ConstructInclude(IncludeType includeType = IncludeType.None);
    }
}
