using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
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
            RedisOptionsExtension extension = CloneExtension();
            extension.DatabaseName = databaseName;
            ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(extension);
            return this;
        }

       
        protected virtual RedisOptionsExtension CloneExtension()
            => new RedisOptionsExtension(OptionsBuilder.Options.GetExtension<RedisOptionsExtension>());
    }
}
