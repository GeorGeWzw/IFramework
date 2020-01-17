using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

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
            var entityType = shapedQueryExpression.Type;
            return Expression.Call(EntityQueryMethodInfo.MakeGenericMethod(entityType),
                                   QueryCompilationContext.QueryContextParameter,
                                   Expression.Constant(shapedQueryExpression),
                                   Expression.Constant(_queryProvider));
        }

        private static IEnumerable<TEntity> EntityQuery<TEntity>(QueryContext queryContext,
                                                                Expression expression,
                                                                IQueryProvider queryProvider)
            where TEntity : class
        {
            return ((RedisQueryContext) queryContext).DataBase.Query<TEntity>(expression, queryProvider);
        }

        private class QueryingEnumerable<T> : IAsyncEnumerable<T>, IEnumerable<T>
        {
            private readonly QueryContext _queryContext;
            private readonly IEnumerable<ValueBuffer> _innerEnumerable;
            private readonly Func<QueryContext, ValueBuffer, T> _shaper;
            private readonly Type _contextType;
            private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;

            public QueryingEnumerable(
                QueryContext queryContext,
                IEnumerable<ValueBuffer> innerEnumerable,
                Func<QueryContext, ValueBuffer, T> shaper,
                Type contextType,
                IDiagnosticsLogger<DbLoggerCategory.Query> logger)
            {
                _queryContext = queryContext;
                _innerEnumerable = innerEnumerable;
                _shaper = shaper;
                _contextType = contextType;
                _logger = logger;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
                => new AsyncEnumerator(this, cancellationToken);

            public IEnumerator<T> GetEnumerator() => new Enumerator(this);

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private IEnumerator<ValueBuffer> _enumerator;
                private readonly QueryContext _queryContext;
                private readonly IEnumerable<ValueBuffer> _innerEnumerable;
                private readonly Func<QueryContext, ValueBuffer, T> _shaper;
                private readonly Type _contextType;
                private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;

                public Enumerator(QueryingEnumerable<T> queryingEnumerable)
                {
                    _queryContext = queryingEnumerable._queryContext;
                    _innerEnumerable = queryingEnumerable._innerEnumerable;
                    _shaper = queryingEnumerable._shaper;
                    _contextType = queryingEnumerable._contextType;
                    _logger = queryingEnumerable._logger;
                }

                public T Current { get; private set; }

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                    _enumerator?.Dispose();
                    _enumerator = null;
                }

                public bool MoveNext()
                {
                    try
                    {
                        using (_queryContext.ConcurrencyDetector.EnterCriticalSection())
                        {
                            if (_enumerator == null)
                            {
                                _enumerator = _innerEnumerable.GetEnumerator();
                            }

                            var hasNext = _enumerator.MoveNext();

                            Current = hasNext
                                ? _shaper(_queryContext, _enumerator.Current)
                                : default;

                            return hasNext;
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger.QueryIterationFailed(_contextType, exception);

                        throw;
                    }
                }

                public void Reset() => throw new NotImplementedException();
            }

            private sealed class AsyncEnumerator : IAsyncEnumerator<T>
            {
                private IEnumerator<ValueBuffer> _enumerator;
                private readonly QueryContext _queryContext;
                private readonly IEnumerable<ValueBuffer> _innerEnumerable;
                private readonly Func<QueryContext, ValueBuffer, T> _shaper;
                private readonly Type _contextType;
                private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;
                private readonly CancellationToken _cancellationToken;

                public AsyncEnumerator(
                    QueryingEnumerable<T> asyncQueryingEnumerable,
                    CancellationToken cancellationToken)
                {
                    _queryContext = asyncQueryingEnumerable._queryContext;
                    _innerEnumerable = asyncQueryingEnumerable._innerEnumerable;
                    _shaper = asyncQueryingEnumerable._shaper;
                    _contextType = asyncQueryingEnumerable._contextType;
                    _logger = asyncQueryingEnumerable._logger;
                    _cancellationToken = cancellationToken;
                }

                public T Current { get; private set; }

                public ValueTask<bool> MoveNextAsync()
                {
                    try
                    {
                        using (_queryContext.ConcurrencyDetector.EnterCriticalSection())
                        {
                            _cancellationToken.ThrowIfCancellationRequested();

                            if (_enumerator == null)
                            {
                                _enumerator = _innerEnumerable.GetEnumerator();
                            }

                            var hasNext = _enumerator.MoveNext();

                            Current = hasNext
                                ? _shaper(_queryContext, _enumerator.Current)
                                : default;

                            return new ValueTask<bool>(hasNext);
                        }
                    }
                    catch (Exception exception)
                    {
                        _logger.QueryIterationFailed(_contextType, exception);

                        throw;
                    }
                }

                public ValueTask DisposeAsync()
                {
                    var enumerator = _enumerator;
                    _enumerator = null;

                    return enumerator.DisposeAsyncIfAvailable();
                }
            }
        }
    }
}