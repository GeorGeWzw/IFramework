using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace IFramework.EntityFrameworkCore.Redis.ValueGeneration.Internal
{
    public class RedisValueGeneratorSelector: ValueGeneratorSelector
    {
        public RedisValueGeneratorSelector(ValueGeneratorSelectorDependencies dependencies) : base(dependencies)
        {
        }
    }
}
