using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using NetCoreKit.Domain;

namespace NetCoreKit.Infrastructure.EfCore.Extensions
{
    public static class RepositoryWithIdExtensions
    {
        public static async Task<TEntity> GetByIdAsync<TDbContext, TEntity, TId>(
            this IQueryRepositoryWithId<TEntity, TId> repo,
            Guid id,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = true)
            where TDbContext : DbContext
            where TEntity : class, IAggregateRootWithId<TId>
        {
            var queryable = repo.Queryable();

            if (disableTracking) queryable = queryable.AsNoTracking();

            if (include != null) queryable = include.Invoke(queryable);

            return await queryable.SingleOrDefaultAsync(e => e.Id.Equals(id));
        }

        public static async Task<TEntity> FindOneAsync<TDbContext, TEntity, TId>(
            this IQueryRepositoryWithId<TEntity, TId> repo,
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = true)
            where TDbContext : DbContext
            where TEntity : class, IAggregateRootWithId<TId>
        {
            var queryable = repo.Queryable();

            if (include != null) queryable = include.Invoke(queryable);

            if (disableTracking) queryable = queryable.AsNoTracking();

            return await queryable.FirstOrDefaultAsync(filter);
        }

        public static async Task<IReadOnlyList<TEntity>> ListAsync<TDbContext, TEntity, TId>(
            this IQueryRepositoryWithId<TEntity, TId> repo,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = true)
            where TDbContext : DbContext
            where TEntity : class, IAggregateRootWithId<TId>
        {
            var queryable = repo.Queryable();

            if (include != null) queryable = include.Invoke(queryable);

            if (disableTracking) queryable = queryable.AsNoTracking();

            return await queryable.ToListAsync();
        }

        public static async Task<PaginatedItem<TResponse>> QueryAsync<TDbContext, TEntity, TId, TResponse>(
            this IQueryRepositoryWithId<TEntity, TId> repo,
            Criterion criterion,
            Expression<Func<TEntity, TResponse>> selector,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = true)
            where TDbContext : DbContext
            where TEntity : class, IAggregateRootWithId<TId>
        {
            return await GetDataAsync<TDbContext, TEntity, TId, TResponse>(repo, criterion, selector, null, include, disableTracking);
        }

        public static async Task<PaginatedItem<TResponse>> FindAllAsync<TDbContext, TEntity, TId, TResponse>(
            this IQueryRepositoryWithId<TEntity, TId> repo,
            Criterion criterion,
            Expression<Func<TEntity, TResponse>> selector,
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = true)
            where TDbContext : DbContext
            where TEntity : class, IAggregateRootWithId<TId>
        {
            return await GetDataAsync<TDbContext, TEntity, TId, TResponse>(repo, criterion, selector, filter, include, disableTracking);
        }

        private static async Task<PaginatedItem<TResponse>> GetDataAsync<TDbContext, TEntity, TId, TResponse>(
            IQueryRepositoryWithId<TEntity, TId> repo,
            Criterion criterion,
            Expression<Func<TEntity, TResponse>> selector,
            Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>> include = null,
            bool disableTracking = true)
            where TDbContext : DbContext
            where TEntity : class, IAggregateRootWithId<TId>
        {
            var queryable = repo.Queryable();
            if (disableTracking) queryable = queryable.AsNoTracking();

            if (include != null) queryable = include.Invoke(queryable);

            if (filter != null) queryable = queryable.Where(filter);

            if (!string.IsNullOrWhiteSpace(criterion.SortBy))
            {
                var isDesc = string.Equals(criterion.SortOrder, "desc", StringComparison.OrdinalIgnoreCase)
                    ? true
                    : false;
                queryable = queryable.OrderByPropertyName<TEntity, TId>(criterion.SortBy, isDesc);
            }

            var results = await queryable
                .Skip(criterion.CurrentPage * criterion.PageSize)
                .Take(criterion.PageSize)
                .Select(selector)
                .ToListAsync();

            var totalRecord = await queryable.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalRecord / criterion.PageSize);

            if (criterion.CurrentPage > totalPages)
            {
                // criterion.SetCurrentPage(totalPages);
            }

            return new PaginatedItem<TResponse>(totalRecord, totalPages, results);
        }
    }
}