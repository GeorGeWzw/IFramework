using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IFramework.EntityFrameworkCore.Redis.Query.Internal
{
    public class RedisShapedQueryCompilingExpressionVisitorFactory: IShapedQueryCompilingExpressionVisitorFactory
    {
        private readonly ShapedQueryCompilingExpressionVisitorDependencies _dependencies;
        private readonly IQueryProvider _queryProvider;
        public RedisShapedQueryCompilingExpressionVisitorFactory(ShapedQueryCompilingExpressionVisitorDependencies dependencies, IQueryProvider queryProvider)
        {
            _dependencies = dependencies;
            _queryProvider = queryProvider;
        }

        public virtual ShapedQueryCompilingExpressionVisitor Create(QueryCompilationContext queryCompilationContext)
            => new RedisShapedQueryCompilingExpressionVisitor(_dependencies, queryCompilationContext, _queryProvider);
    }
}
