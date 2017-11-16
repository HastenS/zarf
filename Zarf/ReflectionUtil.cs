﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Zarf
{
    /// <summary>
    /// 反射工具
    /// </summary>
    public static class ReflectionUtil
    {
        /// <summary>
        /// Enumerable.Where
        /// </summary>
        public static MethodInfo EnumerableWhereMethod { get; }

        /// <summary>
        /// Enumerable.ToList
        /// </summary>
        public static MethodInfo EnumerableToListMethod { get; }

        public static MethodInfo EnumerableDistinct { get; }

        public static MethodInfo[] AllQueryableMethods { get; }

        public static MethodInfo[] AllEnumerableMethods { get; }

        public static readonly Type CharType = typeof(char);

        public static readonly Type CharNullableType = typeof(char?);

        public static readonly Type ByteType = typeof(byte);

        public static readonly Type ByteNullableType = typeof(byte?);

        public static readonly Type StringType = typeof(string);

        public static readonly Type IntType = typeof(int);

        public static readonly Type IntNullableType = typeof(int?);

        public static readonly Type UIntType = typeof(uint);

        public static readonly Type UIntNullableType = typeof(uint?);

        public static readonly Type ShortType = typeof(short);

        public static readonly Type ShortNullableType = typeof(short?);

        public static readonly Type UShortType = typeof(ushort);

        public static readonly Type UShortNullableType = typeof(ushort?);

        public static readonly Type LongType = typeof(long);

        public static readonly Type LongNullbaleType = typeof(long?);

        public static readonly Type ULongType = typeof(ulong);

        public static readonly Type ULongNullableType = typeof(ulong?);

        public static readonly Type DecimalType = typeof(decimal);

        public static readonly Type DecimalNullableType = typeof(decimal?);

        public static readonly Type FloatType = typeof(float);

        public static readonly Type FloatNullableType = typeof(float?);

        public static readonly Type DoubleType = typeof(double);

        public static readonly Type DoubleNullableType = typeof(double?);

        public static readonly Type DateTimeType = typeof(DateTime);

        public static readonly Type DateTimeNullableType = typeof(DateTime?);

        public static readonly Type GuidType = typeof(Guid);

        public static readonly Type GuidNullableType = typeof(Guid?);

        public static readonly Type BooleanType = typeof(bool);

        public static readonly Type BooleanNullableType = typeof(bool?);

        /// <summary>
        /// 简单类型
        /// </summary>
        public static HashSet<Type> SimpleTypes = new HashSet<Type>()
        {
            CharType,
            CharNullableType,
            ByteType,
            ByteNullableType,
            StringType,
            IntType,
            IntNullableType,
            UIntType,
            UIntNullableType,
            ShortType,
            ShortNullableType,
            UShortType,
            UShortNullableType,
            LongType,
            LongNullbaleType,
            ULongType,
            ULongNullableType,
            DecimalType,
            DecimalNullableType,
            FloatType,
            FloatNullableType,
            DoubleType,
            DoubleNullableType,
            DateTimeType,
            DateTimeNullableType,
            GuidType,
            GuidNullableType,
            BooleanType,
            BooleanNullableType
        };

        static ReflectionUtil()
        {
            AllQueryableMethods = typeof(Queryable).GetMethods();
            AllEnumerableMethods = typeof(Enumerable).GetMethods();

            EnumerableWhereMethod = typeof(ReflectionUtil).GetMethod(nameof(Where));
            EnumerableToListMethod = AllEnumerableMethods.FirstOrDefault(item => item.Name == "ToList");
        }

        public static IEnumerable<TEntity> Where<TOEntity, TEntity>(this IEnumerable<TEntity> entities, TOEntity oEntity, Func<TOEntity, TEntity, bool> filter)
        {
            foreach (var entity in entities)
            {
                if (filter(oEntity, entity))
                {
                    yield return entity;
                }
            }
        }
    }
}
