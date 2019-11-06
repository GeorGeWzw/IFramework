using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace IFramework.EntityFrameworkCore.Redis.Infrastructure
{
    public class EntityFrameworkRedisServicesBuilder : EntityFrameworkServicesBuilder
    {
        private static readonly IDictionary<Type, ServiceCharacteristics> RelationalServices
            = new Dictionary<Type, ServiceCharacteristics> {
                {typeof(StackExchange.Redis.IDatabase), new ServiceCharacteristics(ServiceLifetime.Singleton)},
                {typeof(ConnectionMultiplexer), new ServiceCharacteristics(ServiceLifetime.Singleton)}
            };

        public EntityFrameworkRedisServicesBuilder(IServiceCollection serviceCollection) : base(serviceCollection)
        {
        }

        protected override ServiceCharacteristics GetServiceCharacteristics(Type serviceType)
            => RelationalServices.TryGetValue(serviceType, out ServiceCharacteristics characteristics)
                   ? characteristics
                   : base.GetServiceCharacteristics(serviceType);

          public override EntityFrameworkServicesBuilder TryAddCoreServices()
        {
            TryAdd<IDatabaseProvider, DatabaseProvider<RedisOptionsExtension>>();
            TryAdd<ITypeMappingSource>(serviceProvider => serviceProvider.GetRequiredService<TypeMappingSource>());
            TryAddProviderSpecificServices(serviceCollectionMap =>
            {
             
                serviceCollectionMap.TryAddScoped(serviceProvider =>
                {
                    RedisOptionsExtension extension = serviceProvider
                        .GetRequiredService<IDbContextOptions>()
                        .FindExtension<RedisOptionsExtension>();
                    return extension?.ConnectionMultiplexer;
                });
            });
            return base.TryAddCoreServices();
        }
    }
}