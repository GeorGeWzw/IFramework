using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace IFramework.EntityFrameworkCore.Redis.Query.Internal
{
    public class RedisQueryExpression:Expression, IPrintableExpression
    {
        private IEntityType _entityType;

        public RedisQueryExpression(IEntityType entityType)
        {
            _entityType = entityType;
        }

        public void Print(ExpressionPrinter expressionPrinter)
        {
            expressionPrinter.AppendLine(nameof(RedisQueryExpression) + ": entityTypeName: " + _entityType.Name);
        }
    }
}
