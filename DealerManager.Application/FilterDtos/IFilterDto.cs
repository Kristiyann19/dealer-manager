namespace DealerManager.Application.FilterDtos
{
    public interface IFilterDto<TEntity>
    {
        int Limit { get; set; }
        int Offset { get; set; }
        bool GetAllData { get; set; }
        string TextFilter { get; set; }

        bool? IsActive { get; set; }

        IQueryable<TEntity> DefaultFilters(IQueryable<TEntity> query);
        IQueryable<TEntity> WhereBuilder(IQueryable<TEntity> query);
    }
}
