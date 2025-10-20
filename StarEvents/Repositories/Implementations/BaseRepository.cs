using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using StarEvents.Data;
using StarEvents.Repositories.Interfaces;

namespace StarEvents.Repositories.Implementations
{
    /// <summary>
    /// Generic repository for EF6 providing common async CRUD and query operations.
    /// Derived repositories may override IncludeNavigationProperties to eager-load navigation props.
    /// Implements IRepository<T>.
    /// </summary>
    public abstract class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;

        protected BaseRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        // Override this in derived classes to include navigation properties.
        protected virtual IQueryable<T> IncludeNavigationProperties(IQueryable<T> query) => query;

        public virtual async Task<T> GetByIdAsync(int id)
        {
            // Derived repos can override if they need to include navigation props
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            IQueryable<T> query = _dbSet.AsQueryable();
            query = IncludeNavigationProperties(query);
            return await query.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            IQueryable<T> query = _dbSet.Where(predicate);
            query = IncludeNavigationProperties(query);
            return await query.ToListAsync();
        }

        public virtual async Task AddAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _dbSet.Add(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task UpdateAsync(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public virtual async Task DeleteAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity == null) return;
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            var entity = await _dbSet.FindAsync(id);
            return entity != null;
        }

        public virtual async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}