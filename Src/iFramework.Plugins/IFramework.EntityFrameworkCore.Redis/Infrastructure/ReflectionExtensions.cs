using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace IFramework.EntityFrameworkCore.Redis.Infrastructure
{
    internal static class ReflectionExtensions
    {
        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            type = type.GetTypeInfo().BaseType;

            while (type != null)
            {
                yield return type;

                type = type.GetTypeInfo().BaseType;
            }
        }

        public static object GetDefaultValue(this Type type)
        {
            return type.GetTypeInfo().IsValueType ? Activator.CreateInstance(type) : null;
        }

        public static bool ImplementsInterface(this Type type, Type iface)
        {
            if (type == iface)
            {
                return true;
            }

            TypeInfo typeInfo = type.GetTypeInfo();
            return typeInfo.IsGenericType && type.GetGenericTypeDefinition() == iface ||
                   typeInfo.GetInterfaces().Any(i => i.ImplementsInterface(iface));
        }

        public static bool IsNullable(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == (object) typeof(Nullable<>);
        }

        public static bool IsNullableEnum(this Type type)
        {
            return type.IsNullable() && type.GetNullableUnderlyingType().GetTypeInfo().IsEnum;
        }

        public static bool IsNumeric(this Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(decimal);
        }

        public static bool IsConvertibleToEnum(this Type type)
        {
            return type == (object) typeof(sbyte) || type == (object) typeof(short) || type == (object) typeof(int) || type == (object) typeof(long) || type == (object) typeof(byte) || type == (object) typeof(ushort) || type == (object) typeof(uint) || type == (object) typeof(ulong) || type == (object) typeof(Enum) || type == (object) typeof(string);
        }

        public static Type GetNullableUnderlyingType(this Type type)
        {
            if (!type.IsNullable())
            {
                throw new ArgumentException("Type must be nullable.", nameof(type));
            }

            return type.GetTypeInfo().GetGenericArguments()[0];
        }

        public static Type GetSequenceElementType(this Type type)
        {
            Type ienumerable = type.FindIEnumerable();
            return (object) ienumerable == null ? type : ienumerable.GetTypeInfo().GetGenericArguments()[0];
        }

        public static Type FindIEnumerable(this Type seqType)
        {
            if ((object) seqType == null || seqType == (object) typeof(string))
            {
                return null;
            }

            TypeInfo typeInfo = seqType.GetTypeInfo();
            if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == (object) typeof(IEnumerable<>))
            {
                return seqType;
            }

            if (typeInfo.IsArray)
            {
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            }

            if (typeInfo.IsGenericType)
            {
                foreach (Type genericArgument in seqType.GetTypeInfo().GetGenericArguments())
                {
                    Type type = typeof(IEnumerable<>).MakeGenericType(genericArgument);
                    if (type.GetTypeInfo().IsAssignableFrom(seqType))
                    {
                        return type;
                    }
                }
            }

            Type[] interfaces = typeInfo.GetInterfaces();
            if (interfaces != null && interfaces.Length != 0)
            {
                foreach (Type seqType1 in interfaces)
                {
                    Type ienumerable = seqType1.FindIEnumerable();
                    if ((object) ienumerable != null)
                    {
                        return ienumerable;
                    }
                }
            }

            return (object) typeInfo.BaseType != null && (object) typeInfo.BaseType != (object) typeof(object) ? typeInfo.BaseType.FindIEnumerable() : null;
        }
    }
}