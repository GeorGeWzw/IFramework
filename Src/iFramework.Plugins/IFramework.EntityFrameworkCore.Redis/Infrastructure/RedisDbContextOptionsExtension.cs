using System;
using System.Diagnostics.CodeAnalysis;
using IFramework.EntityFrameworkCore.Redis.DependencyInjection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace IFramework.EntityFrameworkCore.Redis.Infrastructure
{
    public class RedisDbContextOptionsExtension: IDbContextOptionsExtension
    {
        private ConnectionMultiplexer _connectionMultiplexer;
        private IDatabase _db;
        public RedisDbContextOptionsExtension()
        {

        }

        public RedisDbContextOptionsExtension(RedisDbContextOptionsExtension copyFrom)
        {
            DatabaseName = copyFrom.DatabaseName;
            AsyncState = copyFrom.AsyncState;
            ConnectionString = copyFrom.ConnectionString;
        }

        public object AsyncState { get; set; }

        public int DatabaseName { get; set; } = -1;

        public string ConnectionString { get; set; }

        public virtual ConnectionMultiplexer ConnectionMultiplexer => _connectionMultiplexer ??= ConnectionMultiplexer.Connect(ConnectionString);

        public virtual IDatabase Database => _db ??= ConnectionMultiplexer.GetDatabase(DatabaseName, AsyncState);

        public virtual RedisDbContextOptionsExtension Clone()
        {
            return new RedisDbContextOptionsExtension(this);
        }

        public virtual RedisDbContextOptionsExtension WithConnectionString([NotNull] string connectionString)
        {
           
            RedisDbContextOptionsExtension dbContextOptionsExtension = this.Clone();
            dbContextOptionsExtension.ConnectionString = connectionString;
            return dbContextOptionsExtension;
        }

        public virtual RedisDbContextOptionsExtension WithConnection(
            [NotNull] ConnectionMultiplexer connectionMultiplexer)
        {
            if (connectionMultiplexer == null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }

            RedisDbContextOptionsExtension dbContextOptionsExtension = this.Clone();
            dbContextOptionsExtension._connectionMultiplexer = connectionMultiplexer;
            return dbContextOptionsExtension;
        }

     
        public virtual void ApplyServices(IServiceCollection services)
        {
            services.AddEntityFrameworkRedis()
                    .AddSingleton(this);
        }

        public void Validate(IDbContextOptions options)
        {
           
        }

        private DbContextOptionsExtensionInfo _info;
        public DbContextOptionsExtensionInfo Info => _info ??= new RedisContextOptionsExtensionInfo(this);
    }
}
