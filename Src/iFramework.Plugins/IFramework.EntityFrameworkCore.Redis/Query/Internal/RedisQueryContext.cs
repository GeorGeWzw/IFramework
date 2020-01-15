using System;
using System.Collections.Generic;
using System.Text;
using IFramework.EntityFrameworkCore.Redis.Storage.Internal;
using Microsoft.EntityFrameworkCore.Query;

namespace IFramework.EntityFrameworkCore.Redis.Query.Internal
{
    public class RedisQueryContext: QueryContext
    {

        public RedisQueryContext(QueryContextDependencies dependencies, IRedisDatabase database) : base(dependencies)
        {
            DataBase = database;
        }

        public virtual IRedisDatabase DataBase { get; }
    }
}
