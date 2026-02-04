using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;
using HVTravel.Domain.Models;

namespace HVTravel.Domain.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(string id);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task UpdateAsync(string id, T entity);
        Task DeleteAsync(string id);
        Task<PaginatedResult<T>> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>> filter = null);
    }
}
