using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace IFramework.EntityFrameworkCore.Redis.Metadata.Conventions
{
    public class RedisConventionSetBuilder: ProviderConventionSetBuilder
    {
        public RedisConventionSetBuilder(ProviderConventionSetBuilderDependencies dependencies) : base(dependencies)
        {
        }
    }
}
