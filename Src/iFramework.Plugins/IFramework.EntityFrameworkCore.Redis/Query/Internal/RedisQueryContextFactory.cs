using System;
using System.Collections.Generic;
using System.Text;
using IFramework.EntityFrameworkCore.Redis.Storage.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace IFramework.EntityFrameworkCore.Redis.Query.Internal
{
    public class RedisQueryContextFactory: IQueryContextFactory
    {
        private readonly IRedisDatabase _database;
        private readonly QueryContextDependencies _dependencies;
        public RedisQueryContextFactory(IRedisDatabase database, QueryContextDependencies dependencies)
        {
            _database = database;
            _dependencies = dependencies;
        }

        public QueryContext Create() => new RedisQueryContext(_dependencies, _database);
    }
}
