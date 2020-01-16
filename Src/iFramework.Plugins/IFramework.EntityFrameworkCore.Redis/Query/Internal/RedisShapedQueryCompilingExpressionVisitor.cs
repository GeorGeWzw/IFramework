using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;

namespace IFramework.EntityFrameworkCore.Redis.Query.Internal
{
    public class RedisShapedQueryCompilingExpressionVisitor : ShapedQueryCompilingExpressionVisitor
    {
        private readonly IQueryProvider _queryProvider;
        private static readonly MethodInfo EntityQueryMethodInfo
            = typeof(RedisShapedQueryCompilingExpressionVisitor).GetTypeInfo()
                                                                .GetDeclaredMethod(nameof(EntityQuery));

        private readonly Type _contextType;
        private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;

        public RedisShapedQueryCompilingExpressionVisitor(ShapedQueryCompilingExpressionVisitorDependencies dependencies,
                                                          QueryCompilationContext queryCompilationContext,
                                                          IQueryProvider queryProvider) : base(dependencies, queryCompilationContext)
        {
            _queryProvider = queryProvider;
            _contextType = queryCompilationContext.ContextType;
            _logger = queryCompilationContext.Logger;
        }

        protected override Expression VisitShapedQueryExpression(ShapedQueryExpression shapedQueryExpression)
        {
            var redisQueryExpression = shapedQueryExpression.QueryExpression;
            var entityType = ((LambdaExpression) redisQueryExpression).ReturnType;
            var innerEnumerable = Visit(redisQueryExpression);
            return Expression.Call(EntityQueryMethodInfo.MakeGenericMethod(entityType),
                                   QueryCompilationContext.QueryContextParameter,
                                   innerEnumerable ?? throw new InvalidOperationException(),
                                   Expression.Constant(_queryProvider));
        }

        private static IQueryable<TEntity> EntityQuery<TEntity>(QueryContext queryContext,
                                                                Expression expression,
                                                                IQueryProvider queryProvider)
            where TEntity : class
        {
            return ((RedisQueryContext) queryContext).DataBase.Query<TEntity>(expression, queryProvider);
        }
    }
}