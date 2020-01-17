using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using IFramework.EntityFrameworkCore.Redis.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StackExchange.Redis;

namespace IFramework.EntityFrameworkCore.Redis.Query.Internal
{
    public class RedisQueryProviderImpl: IQueryProvider
    {
        private readonly IDatabase _database;

        public RedisQueryProviderImpl(RedisDbContextOptionsExtension dbContextOptions)
        {
            _database = dbContextOptions.Database;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type sequenceElementType = expression.Type.GetSequenceElementType();
            try
            {
                return (IQueryable) Activator.CreateInstance(typeof (RedisQueryableImpl<>).MakeGenericType(sequenceElementType), 
                                                             (object) expression);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new RedisQueryableImpl<TElement>(_database, expression, this);
        }

        public object Execute(Expression expression)
        {
            LambdaExpression lambdaExpression = Expression.Lambda(expression, Array.Empty<ParameterExpression>());
            try
            {
                return lambdaExpression.Compile().DynamicInvoke((object[]) null);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression);
        }
    }
}
