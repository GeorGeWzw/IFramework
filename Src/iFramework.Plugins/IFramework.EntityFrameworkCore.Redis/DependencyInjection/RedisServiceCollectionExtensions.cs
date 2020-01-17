using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IFramework.EntityFrameworkCore.Redis.Diagnostics.Internal;
using IFramework.EntityFrameworkCore.Redis.Infrastructure;
using IFramework.EntityFrameworkCore.Redis.Metadata.Conventions;
using IFramework.EntityFrameworkCore.Redis.Query.Internal;
using IFramework.EntityFrameworkCore.Redis.Storage.Internal;
using IFramework.EntityFrameworkCore.Redis.ValueGeneration.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.DependencyInjection;

namespace IFramework.EntityFrameworkCore.Redis.DependencyInjection
{
    public static class RedisServiceCollectionExtensions
    {
        public static IServiceCollection AddEntityFrameworkRedis([NotNull] this IServiceCollection serviceCollection)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            var builder = new EntityFrameworkServicesBuilder(serviceCollection)
                          .TryAdd<LoggingDefinitions, RedisLoggingDefinitions>()
                          .TryAdd<IDatabaseProvider, DatabaseProvider<RedisDbContextOptionsExtension>>()
                          .TryAdd<IValueGeneratorSelector, RedisValueGeneratorSelector>()
                          .TryAdd<IDatabase>(p => p.GetService<IRedisDatabase>())
                          .TryAdd<IDbContextTransactionManager, RedisTransactionManager>()
                          .TryAdd<IDatabaseCreator, RedisDatabaseCreator>()
                          .TryAdd<IQueryContextFactory, RedisQueryContextFactory>()
                          .TryAdd<IShapedQueryCompilingExpressionVisitorFactory, RedisShapedQueryCompilingExpressionVisitorFactory>()
                          .TryAdd<IQueryableMethodTranslatingExpressionVisitorFactory, RedisQueryableMethodTranslatingExpressionVisitorFactory>()
                          .TryAdd<IProviderConventionSetBuilder, RedisConventionSetBuilder>()
                          .TryAdd<ITypeMappingSource, RedisTypeMappingSource>()
                          .TryAddProviderSpecificServices(b => b.TryAddScoped<IRedisDatabase, RedisDatabase>()
                                                                .TryAddScoped<IQueryProvider, RedisQueryProviderImpl>());


            builder.TryAddCoreServices();
            return serviceCollection;
        }
    }
}