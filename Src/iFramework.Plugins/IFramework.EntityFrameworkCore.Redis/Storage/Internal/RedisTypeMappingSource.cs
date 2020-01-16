using System;
using System.Diagnostics;
using System.Linq;
using IFramework.EntityFrameworkCore.Redis.Infrastructure;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace IFramework.EntityFrameworkCore.Redis.Storage.Internal
{
    public class RedisTypeMappingSource : TypeMappingSource
    {
        public RedisTypeMappingSource(TypeMappingSourceDependencies dependencies) : base(dependencies)
        {
        }


        protected override CoreTypeMapping FindMapping(in TypeMappingInfo mappingInfo)
        {
            var clrType = mappingInfo.ClrType;
            Debug.Assert(clrType != null);

            if (clrType.IsValueType
                || clrType == typeof(string))
            {
                return new RedisTypeMapping(clrType);
            }

            if (clrType == typeof(byte[]))
            {
                return new RedisTypeMapping(clrType, structuralComparer: new ArrayStructuralComparer<byte>());
            }

            if (clrType.FullName == "NetTopologySuite.Geometries.Geometry"
                || clrType.GetBaseTypes().Any(t => t.FullName == "NetTopologySuite.Geometries.Geometry"))
            {
                var comparer = (ValueComparer) Activator.CreateInstance(typeof(GeometryValueComparer<>).MakeGenericType(clrType));

                return new RedisTypeMapping(clrType,
                                            comparer,
                                            comparer,
                                            comparer);
            }

            return base.FindMapping(mappingInfo);
        }
    }
}