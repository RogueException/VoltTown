using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace VoltTown.Data
{
    public static class DbExtensions
    {
        public static bool AddIfNotExists<TEntity>(this DbSet<TEntity> dbSet, TEntity entity, Expression<Func<TEntity, bool>> predicate)
            where TEntity : class, new()
        {
            if (!dbSet.Any(predicate))
            {
                dbSet.Add(entity);
                return true;
            }
            return false;
        }
    }
}
