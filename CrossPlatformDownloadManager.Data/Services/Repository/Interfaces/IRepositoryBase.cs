using System.Linq.Expressions;
using CrossPlatformDownloadManager.Data.Models;

namespace CrossPlatformDownloadManager.Data.Services.Repository.Interfaces;

public interface IRepositoryBase<T> where T : DbModelBase
{
    Task AddAsync(T? entity);

    Task AddRangeAsync(IEnumerable<T>? entities);

    Task<T?> GetAsync(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params string[] includeProperties);

    Task<TR?> GetAsync<TR>(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<T, TR>? select = null,
        params string[] includeProperties);

    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        bool distinct = false,
        params string[] includeProperties);

    Task<List<TR>> GetAllAsync<TR>(Expression<Func<T, bool>>? where = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        Func<T, TR>? select = null,
        bool distinct = false,
        params string[] includeProperties);

    Task DeleteAsync(T? entity);

    Task DeleteAllAsync(IEnumerable<T>? entities);

    Task UpdateAsync(T? entity);

    Task UpdateAllAsync(IEnumerable<T>? entities);

    Task<int> GetCountAsync(Expression<Func<T, bool>>? where = null,
        bool distinct = false,
        params string[] includeProperties);

    Task<TResult> GetMaxAsync<TResult>(Expression<Func<T, TResult>> selector,
        Expression<Func<T, bool>>? where = null,
        bool distinct = false,
        params string[] includeProperties);
}