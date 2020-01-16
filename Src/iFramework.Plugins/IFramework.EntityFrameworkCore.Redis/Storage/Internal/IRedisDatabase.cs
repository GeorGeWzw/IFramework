using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage;

namespace IFramework.EntityFrameworkCore.Redis.Storage.Internal
{
    public interface IRedisDatabase:IDatabase
    {
        IQueryable<TEntity> Query<TEntity>(Expression expression, IQueryProvider queryProvider);

        StackExchange.Redis.IDatabase Database { get; }
    }
}
