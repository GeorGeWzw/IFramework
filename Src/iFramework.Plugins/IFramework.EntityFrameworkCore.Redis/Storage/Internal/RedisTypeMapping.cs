using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace IFramework.EntityFrameworkCore.Redis.Storage.Internal
{
    public class RedisTypeMapping : CoreTypeMapping
    {
        public RedisTypeMapping([NotNull] Type clrType,
                                ValueComparer comparer = null,
                                ValueComparer keyComparer = null,
                                ValueComparer structuralComparer = null)
            : base(new CoreTypeMappingParameters(clrType,
                                                 null,
                                                 comparer,
                                                 keyComparer,
                                                 structuralComparer))
        {
        }

        private RedisTypeMapping(CoreTypeMappingParameters parameters) : base(parameters)
        {
        }

        public override CoreTypeMapping Clone(ValueConverter converter)
        {
            return new RedisTypeMapping(Parameters.WithComposedConverter(converter));
        }
    }
}