using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using IFramework.EntityFrameworkCore.Redis.Infrastructure;
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
            var entityFrameworkServicesBuilder = new EntityFrameworkRedisServicesBuilder(serviceCollection);
            entityFrameworkServicesBuilder.TryAddCoreServices();
            return serviceCollection;
        }
    }
}
