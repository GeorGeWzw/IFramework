using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using StackExchange.Redis;

namespace IFramework.EntityFrameworkCore.Redis.Query.Internal
{
    public class RedisQueryableImpl<TOutput> : IOrderedQueryable<TOutput>
    {
        private readonly IDatabase _database;

        public RedisQueryableImpl(IDatabase database, Expression expression, IQueryProvider provider)
        {
            _database = database;
            Expression = expression;
            Provider = provider;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Type ElementType => typeof(TOutput);
        public Expression Expression { get; }
        public IQueryProvider Provider { get; }

        public IEnumerator<TOutput> GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}