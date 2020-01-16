using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace IFramework.EntityFrameworkCore.Redis.Query.Internal
{
    public class RedisQueryableMethodTranslatingExpressionVisitorFactory:IQueryableMethodTranslatingExpressionVisitorFactory
    {
        private readonly QueryableMethodTranslatingExpressionVisitorDependencies _dependencies;

        public RedisQueryableMethodTranslatingExpressionVisitorFactory(
            [NotNull] QueryableMethodTranslatingExpressionVisitorDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public virtual QueryableMethodTranslatingExpressionVisitor Create(IModel model)
            => new RedisQueryableMethodTranslatingExpressionVisitor(_dependencies, model);
    }
}
