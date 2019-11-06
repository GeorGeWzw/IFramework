using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using IFramework.EntityFrameworkCore.Redis.DependencyInjection;
using IFramework.EntityFrameworkCore.Redis.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Microsoft.EntityFrameworkCore
{
    public class RedisOptionsExtension: IDbContextOptionsExtension
    {
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _db;
        public RedisOptionsExtension()
        {

        }

        public RedisOptionsExtension(RedisOptionsExtension copyFrom)
        {
            DatabaseName = copyFrom.DatabaseName;
            AsyncState = copyFrom.AsyncState;
            ConnectionString = copyFrom.ConnectionString;
        }

        public object AsyncState { get; set; }

        public int DatabaseName { get; set; } = -1;

        public string ConnectionString { get; set; }

        public virtual ConnectionMultiplexer ConnectionMultiplexer => _connectionMultiplexer ??= ConnectionMultiplexer.Connect(ConnectionString);

        public virtual IDatabase Dd => _db ??= ConnectionMultiplexer.GetDatabase(DatabaseName, AsyncState);

        public virtual RedisOptionsExtension Clone()
        {
            return new RedisOptionsExtension(this);
        }

        public virtual RedisOptionsExtension WithConnectionString([NotNull] string connectionString)
        {
           
            RedisOptionsExtension optionsExtension = this.Clone();
            optionsExtension.ConnectionString = connectionString;
            return optionsExtension;
        }

        public virtual RedisOptionsExtension WithConnection(
            [NotNull] ConnectionMultiplexer connectionMultiplexer)
        {
            if (connectionMultiplexer == null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }

            RedisOptionsExtension optionsExtension = this.Clone();
            optionsExtension._connectionMultiplexer = connectionMultiplexer;
            return optionsExtension;
        }

     
        public virtual void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkRedis();
        }

        public void Validate(IDbContextOptions options)
        {
           
        }

        private DbContextOptionsExtensionInfo _info;
        public DbContextOptionsExtensionInfo Info => _info ??= new RedisContextOptionsExtensionInfo(this);
    }
}
