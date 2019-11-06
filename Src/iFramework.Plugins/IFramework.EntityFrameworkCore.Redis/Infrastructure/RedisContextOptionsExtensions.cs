using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StackExchange.Redis;

namespace Microsoft.EntityFrameworkCore
{
    public static class RedisContextOptionsExtensions
    {
        public static DbContextOptionsBuilder UseRedis([NotNull] this DbContextOptionsBuilder optionsBuilder,
                                                       [NotNull] string connectionString,
                                                       Action<RedisContextOptionsBuilder> redisOptionsAction = null)
        {
            if (optionsBuilder == null)
            {
                throw new ArgumentNullException(nameof(optionsBuilder));
            }

            if (connectionString == null)
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            RedisOptionsExtension extension = GetOrCreateExtension(optionsBuilder).WithConnectionString(connectionString);
            ((IDbContextOptionsBuilderInfrastructure) optionsBuilder).AddOrUpdateExtension(extension);

            redisOptionsAction?.Invoke(new RedisContextOptionsBuilder(optionsBuilder));

            return optionsBuilder;
        }


        public static DbContextOptionsBuilder UseRedis([NotNull] this DbContextOptionsBuilder optionsBuilder,
                                                       [NotNull] ConnectionMultiplexer connection,
                                                       Action<RedisContextOptionsBuilder> redisOptionsAction = null)
        {
            RedisOptionsExtension extension = GetOrCreateExtension(optionsBuilder).WithConnection(connection);
            ((IDbContextOptionsBuilderInfrastructure) optionsBuilder).AddOrUpdateExtension(extension);

            redisOptionsAction?.Invoke(new RedisContextOptionsBuilder(optionsBuilder));

            return optionsBuilder;
        }


        private static RedisOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
        {
            return optionsBuilder.Options.FindExtension<RedisOptionsExtension>() ?? new RedisOptionsExtension();
        }
    }
}