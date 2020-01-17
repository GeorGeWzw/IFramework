using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using IFramework.EntityFrameworkCore.Redis.Infrastructure;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore
{
    public class RedisContextOptionsBuilder
    {
        protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

        public RedisContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
        {
            OptionsBuilder = optionsBuilder;
        }

        public RedisContextOptionsBuilder UseDatabase(int databaseName)
        {
            RedisDbContextOptionsExtension extension = CloneExtension();
            extension.DatabaseName = databaseName;
            ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);
            return this;
        }

       
        protected virtual RedisDbContextOptionsExtension CloneExtension()
            => new RedisDbContextOptionsExtension(OptionsBuilder.Options.GetExtension<RedisDbContextOptionsExtension>());
    }
}
