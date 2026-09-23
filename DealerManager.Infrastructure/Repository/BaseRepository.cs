using DealerManager.Application.FilterDtos;
using DealerManager.Application.IRepository;
using DealerManager.Domain.Common;
using DealerManager.Domain.Enums.BaseRepositories;
using DealerManager.Infrastructure.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DealerManager.Infrastructure.Repository
{
    public class BaseRepository<TEntity, TFilter, TDbContext> : IBaseRepository<TEntity, TFilter, TDbContext>
        where TEntity : Entity
        where TFilter : FilterDto<TEntity>, new()
        where TDbContext : DbContext
    {
        protected readonly TDbContext context;

        public BaseRepository(
            TDbContext context
            )
        {
            this.context = context;
        }

        public virtual async Task<TEntity> GetById(int id, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null)
        {
            var query = context.Set<TEntity>().AsNoTracking();

            if (includesFunc != null)
            {
                query = includesFunc(query);
            }

            return await query.SingleOrDefaultAsync(e => e.Id == id, cancellationToken);
        }

        public virtual async Task<TEntity> GetByProperties(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null)
        {
            var query = context.Set<TEntity>().AsNoTracking();

            if (includesFunc != null)
            {
                query = includesFunc(query);
            }

            return await query.SingleOrDefaultAsync(predicate, cancellationToken);
        }

        public virtual async Task<List<TEntity>> GetList(TFilter filter, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        {
            if (filter == null)
            {
                filter = new TFilter();
            }

            var query = GetQuery(filter, includesFunc, orderBy);

            var result = filter.GetAllData ? await query.ToListAsync(cancellationToken) : await query.Skip(filter.Offset).Take(filter.Limit).ToListAsync(cancellationToken);

            return result;
        }

        public virtual async Task<List<TEntity>> GetListByProperties(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        {
            var query = context.Set<TEntity>().AsNoTracking();

            if (includesFunc != null)
            {
                query = includesFunc(query);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return await query.Where(predicate).ToListAsync(cancellationToken);
        }

        public virtual async Task<(List<TEntity>, int)> GetAll(TFilter filter, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        {
            if (filter == null)
            {
                filter = new TFilter();
            }

            var query = GetQuery(filter, includesFunc, orderBy);

            var result = filter.GetAllData ? await query.ToListAsync(cancellationToken) : await query.Skip(filter.Offset).Take(filter.Limit).ToListAsync(cancellationToken);

            return (result, await query.CountAsync(cancellationToken));
        }

        public virtual async Task<List<TEntity>> GetAllWithoutCount(TFilter filter, CancellationToken cancellationToken, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null)
        {
            if (filter == null)
            {
                filter = new TFilter();
            }

            var queryIds = GetIdQuery(filter);

            var loadIds = await queryIds
                .Take(filter.Limit)
                .ToListAsync(cancellationToken);

            var query = context.Set<TEntity>()
                .AsNoTracking()
                .Where(e => loadIds.Contains(e.Id));

            if (includesFunc != null)
            {
                query = includesFunc(query);
            }

            return await query.OrderByDescending(e => e.Id).ToListAsync(cancellationToken);
        }

        public virtual async Task<int> GetCount(TFilter filter, CancellationToken cancellationToken)
        {
            var queryIds = GetIdQuery(filter);

            return await queryIds.CountAsync(cancellationToken);
        }

        public virtual IQueryable<TEntity> GetQuery(TFilter filter, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        {
            var query = context.Set<TEntity>().AsNoTracking();

            if (includesFunc != null)
            {
                query = includesFunc(query);
            }

            query = filter.WhereBuilder(query);

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return query;
        }

        public virtual IQueryable<TEntity> GetQueryByProperties(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> includesFunc = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null)
        {
            var query = context.Set<TEntity>().AsNoTracking();

            if (includesFunc != null)
            {
                query = includesFunc(query);
            }

            query = query.Where(predicate);

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            return query;
        }

        public virtual IQueryable<int> GetIdQuery(TFilter filter)
        {
            var query = context.Set<TEntity>()
                .AsNoTracking()
                .OrderByDescending(e => e.Id)
                .AsQueryable();

            var queryIds = filter.WhereBuilder(query).Select(e => e.Id);

            return queryIds;
        }

        public virtual async Task<bool> AnyEntity(Expression<Func<TEntity, bool>> predicate, CancellationToken token)
        {
            return await context.Set<TEntity>().AsNoTracking().AnyAsync(predicate, token);
        }

        public virtual async Task Create(TEntity entity)
        {
            EntityHelper.ClearSkipProperties(entity);
            await context.Set<TEntity>().AddAsync(entity);
        }

        public virtual async Task CreateRange(List<TEntity> entities)
        {
            foreach (TEntity entity in entities)
            {
                EntityHelper.ClearSkipProperties(entity);
            }

            await context.Set<TEntity>().AddRangeAsync(entities);
        }

        public virtual void Update(TEntity entity, TEntity modificationEntity)
        {
            EntityHelper.Update(entity, modificationEntity, context);
        }

        public virtual void UpdateFromDto<TDto>(TEntity entity, TDto modificationDto)
        {
            EntityHelper.UpdateFromDto(entity, modificationDto, context);
        }

        public virtual void Delete(TEntity entity)
        {
            EntityHelper.ClearSkipProperties(entity);
            EntityHelper.Remove(entity, context);
        }

        public virtual void SetEntryModified(TEntity entity)
        {
            context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void SetEntriesModified(List<TEntity> entities)
        {
            foreach (TEntity entity in entities)
            {
                context.Entry(entity).State = EntityState.Modified;
            }
        }

        public virtual Func<IQueryable<TEntity>, IQueryable<TEntity>> ConstructInclude(IncludeType includeType = IncludeType.None)
        {
            return e => e;
        }
    }
}
