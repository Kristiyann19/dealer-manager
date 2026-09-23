namespace DealerManager.Application.FilterDtos
{
    public class FilterDto<TEntity> : IFilterDto<TEntity>
        where TEntity : class
    {
        public int Limit { get; set; } = 30;
        public int Offset { get; set; }
        public bool GetAllData { get; set; } = false;

        public string TextFilter { get; set; }

        public bool? IsActive { get; set; }

        public virtual IQueryable<TEntity> DefaultFilters(IQueryable<TEntity> query)
        {
            return query;
        }

        public virtual IQueryable<TEntity> WhereBuilder(IQueryable<TEntity> query)
        {
            query = DefaultFilters(query);

            return query;
        }
    }
}
