﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Dynamic;
using System.Linq;
using System.Runtime.Serialization;
using blqw.IOC;
using blqw.Serializable.JsonWriters;

namespace blqw.Serializable
{
    /// <summary>
    /// <see cref="IJsonWriter" /> 的容器
    /// </summary>
    public static class JsonWriterContainer
    {
        /// <summary>
        /// <see cref="JsonWriterWrapper" /> 的缓存
        /// </summary>
        private static TypeCache<JsonWriterWrapper> _Items;

        /// <summary>
        /// typeof(Type)
        /// </summary>
        private static readonly Type _ObjectType = typeof(object);


        /// <summary>
        /// 通过IOC加载的所有 <see cref="IJsonWriter" /> 的集合
        /// </summary>
        [ImportMany]
#pragma warning disable 649
        // ReSharper disable once CollectionNeverUpdated.Local
        private static List<IJsonWriter> _Writers;
#pragma warning restore 649

        private static JsonWriterWrapper _NullWapper;
        private static JsonWriterWrapper _VersionWapper;
        private static JsonWriterWrapper _UriWapper;
        private static JsonWriterWrapper _UInt64Wapper;
        private static JsonWriterWrapper _UInt32Wapper;
        private static JsonWriterWrapper _UInt16Wapper;
        private static JsonWriterWrapper _TypeWapper;
        private static JsonWriterWrapper _TimeSpanWapper;
        private static JsonWriterWrapper _StringWapper;
        private static JsonWriterWrapper _SingleWapper;
        private static JsonWriterWrapper _SByteWapper;
        private static JsonWriterWrapper _Int64Wapper;
        private static JsonWriterWrapper _Int32Wapper;
        private static JsonWriterWrapper _Int16Wapper;
        private static JsonWriterWrapper _ConvertibleWapper;
        private static JsonWriterWrapper _GuidWapper;
        private static JsonWriterWrapper _EnumWapper;
        private static JsonWriterWrapper _DoubleWapper;
        private static JsonWriterWrapper _DateTimeWapper;
        private static JsonWriterWrapper _DecimalWapper;
        private static JsonWriterWrapper _CharWapper;
        private static JsonWriterWrapper _ByteWapper;
        private static JsonWriterWrapper _BooleanWapper;

        private static readonly Dictionary<Type, int> _Prioritys = new Dictionary<Type, int>
        {
            [typeof(IObjectReference)] = 400,
            [typeof(IFormatProvider)] = 300,
            [typeof(IDictionary<,>)] = 200,
            [typeof(IDictionary)] = 199,
            [typeof(IEnumerable<>)] = 99,
            [typeof(IEnumerable)] = 98,
            [typeof(IEnumerator<>)] = 97,
            [typeof(IEnumerator)] = 96,
            [typeof(DynamicObject)] = 95
        };

        static JsonWriterContainer()
        {
            Reload();
        }


        /// <summary>
        /// <see cref="bool" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter BooleanWriter => (_BooleanWapper ?? (_BooleanWapper = _Items.Get<bool>())).Writer;

        /// <summary>
        /// <see cref="byte" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter ByteWriter => (_ByteWapper ?? (_ByteWapper = _Items.Get<byte>())).Writer;

        /// <summary>
        /// <see cref="char" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter CharWriter => (_CharWapper ?? (_CharWapper = _Items.Get<char>())).Writer;

        /// <summary>
        /// <see cref="DateTime" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter DateTimeWriter
            => (_DateTimeWapper ?? (_DateTimeWapper = _Items.Get<DateTime>())).Writer;

        /// <summary>
        /// <see cref="decimal" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter DecimalWriter => (_DecimalWapper ?? (_DecimalWapper = _Items.Get<decimal>())).Writer;

        /// <summary>
        /// <see cref="double" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter DoubleWriter => (_DoubleWapper ?? (_DoubleWapper = _Items.Get<double>())).Writer;

        /// <summary>
        /// <see cref="Enum" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter EnumWriter => (_EnumWapper ?? (_EnumWapper = _Items.Get<Enum>())).Writer;

        /// <summary>
        /// <see cref="Guid" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter GuidWriter => (_GuidWapper ?? (_GuidWapper = _Items.Get<Guid>())).Writer;

        /// <summary>
        /// <see cref="IConvertible" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter ConvertibleWriter
            => (_ConvertibleWapper ?? (_ConvertibleWapper = _Items.Get<IConvertible>())).Writer;

        /// <summary>
        /// <see cref="short" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter Int16Writer => (_Int16Wapper ?? (_Int16Wapper = _Items.Get<short>())).Writer;

        /// <summary>
        /// <see cref="int" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter Int32Writer => (_Int32Wapper ?? (_Int32Wapper = _Items.Get<int>())).Writer;

        /// <summary>
        /// <see cref="long" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter Int64Writer => (_Int64Wapper ?? (_Int64Wapper = _Items.Get<long>())).Writer;

        /// <summary>
        /// <see cref="sbyte" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter SByteWriter => (_SByteWapper ?? (_SByteWapper = _Items.Get<sbyte>())).Writer;

        /// <summary>
        /// <see cref="float" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter SingleWriter => (_SingleWapper ?? (_SingleWapper = _Items.Get<float>())).Writer;

        /// <summary>
        /// <see cref="string" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter StringWriter => (_StringWapper ?? (_StringWapper = _Items.Get<string>())).Writer;

        /// <summary>
        /// <see cref="TimeSpan" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter TimeSpanWriter
            => (_TimeSpanWapper ?? (_TimeSpanWapper = _Items.Get<TimeSpan>())).Writer;

        /// <summary>
        /// <see cref="Type" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter TypeWriter => (_TypeWapper ?? (_TypeWapper = _Items.Get<Type>())).Writer;

        /// <summary>
        /// <see cref="ushort" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter UInt16Writer => (_UInt16Wapper ?? (_UInt16Wapper = _Items.Get<ushort>())).Writer;

        /// <summary>
        /// <see cref="uint" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter UInt32Writer => (_UInt32Wapper ?? (_UInt32Wapper = _Items.Get<uint>())).Writer;

        /// <summary>
        /// <see cref="ulong" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter UInt64Writer => (_UInt64Wapper ?? (_UInt64Wapper = _Items.Get<ulong>())).Writer;

        /// <summary>
        /// <see cref="Uri" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter UriWriter => (_UriWapper ?? (_UriWapper = _Items.Get<Uri>())).Writer;

        /// <summary>
        /// <see cref="Version" /> 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter VersionWriter => (_VersionWapper ?? (_VersionWapper = _Items.Get<Version>())).Writer;

        /// <summary>
        /// null 类型的 <see cref="IJsonWriter" />
        /// </summary>
        public static IJsonWriter NullWriter => (_NullWapper ?? (_NullWapper = _Items.Get(typeof(void)))).Writer;

        /// <summary>
        /// 获取所有 <see cref="IJsonWriter" />
        /// </summary>
        /// <returns> </returns>
        public static IEnumerable<IJsonWriter> GetAll => _Writers?.AsReadOnly();

        /// <summary>
        /// 重新加载所有 <see cref="IJsonWriter" />
        /// </summary>
        public static void Reload()
        {
            MEF.Import(typeof(JsonWriterContainer));
            _Items = new TypeCache<JsonWriterWrapper>();
            foreach (var w in _Writers)
            {
                _Items.Set(w.Type, new JsonWriterWrapper(w));
            }
        }

        /// <summary>
        /// 获取一个匹配度最高的 <see cref="IJsonWriter" />
        /// </summary>
        /// <param name="type"> 用于匹配的 <see cref="Type" /> </param>
        /// <returns> </returns>
        public static IJsonWriter Get(Type type)
        {
            return GetWrap(type)?.Writer;
        }

        public static void Write(object value, JsonWriterArgs args)
        {
            if (value == null || value is DBNull)
            {
                NullWriter.Write(null, args);
            }
            else
            {
                Get(value.GetType()).Write(value, args);
            }
        }

        /// <summary>
        /// 获取容器中 <see cref="IJsonWriter" /> 的个数
        /// </summary>
        public static int GetWriterCount()
        {
            return _Writers?.Count ?? 0;
        }

        /// <summary>
        /// 设置 <see cref="IJsonWriter" />,如果已经存在则替换
        /// </summary>
        /// <param name="writer"> <see cref="IJsonWriter" /> 对象 </param>
        public static void Set(IJsonWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            var w = _Items.GetOrCreate(writer.Type, t => new JsonWriterWrapper(writer));
            if (w.Writer.Type == writer.Type)
            {
                w.Writer = writer;
            }
            else
            {
                _Items.Set(writer.Type, new JsonWriterWrapper(writer));
            }
        }

        /// <summary>
        /// 获取匹配 <paramref name="type" /> 的 <see cref="JsonWriterWrapper" />
        /// </summary>
        /// <param name="type"> 用于匹配的 <see cref="Type" /> </param>
        /// <returns> </returns>
        internal static JsonWriterWrapper GetWrap(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return _Items.GetOrCreate(type, Select);
        }

        /// <summary>
        /// 从 <see cref="IJsonWriter" /> 集合 <seealso cref="_Items" /> 中选择一个匹配
        /// <paramref name="type" /> 的 <see cref="JsonWriterWrapper" />
        /// </summary>
        /// <param name="type"> 用于匹配的 <see cref="Type" /> </param>
        /// <returns> </returns>
        private static JsonWriterWrapper Select(Type type)
        {
            var ee = All(type);
            if (ee.MoveNext())
            {
                var wrapper = ee.Current;
                var wirters = wrapper.Writer as IMultiJsonWirters;
                if (wirters == null)
                {
                    return wrapper;
                }

                while (ee.MoveNext())
                {
                    wirters.Add(ee.Current);
                }
                return wrapper;
            }
            //匹配object定义
            return new JsonWriterWrapper(_Items.Get(_ObjectType), type);
        }

        private static IEnumerator<JsonWriterWrapper> All(Type type)
        {
            //精确匹配当前类 或 泛型定义
            var wrap = _Items.Get(type);
            if (wrap != null)
            {
                yield return wrap;
            }
            else
            {
                wrap = SelectByGenericDefinition(type, type);
                if (wrap != null)
                {
                    yield return new JsonWriterWrapper(wrap, type);
                }
            }
            //匹配父类和接口
            var baseTypes = GetBaseType(type).Union(type.GetInterfaces()).OrderByDescending(GetPriority);

            foreach (var interfaceType in baseTypes)
            {
                wrap = _Items.Get(interfaceType);
                if (wrap != null)
                {
                    yield return new JsonWriterWrapper(wrap, type);
                }
                else //匹配接口泛型定义
                {
                    wrap = SelectByGenericDefinition(interfaceType, type);
                    if (wrap != null)
                    {
                        yield return new JsonWriterWrapper(wrap, type);
                    }
                }
            }

            //匹配object定义
            yield return new JsonWriterWrapper(_Items.Get(_ObjectType), type);
        }

        /// <summary>
        /// 枚举所有父类
        /// </summary>
        /// <param name="type"> </param>
        /// <returns> </returns>
        private static IEnumerable<Type> GetBaseType(Type type)
        {
            var baseType = type.BaseType ?? typeof(object);
            while (baseType != typeof(object))
            {
                yield return baseType;
                baseType = baseType.BaseType ?? typeof(object);
            }
        }

        /// <summary>
        /// 获取类型的优先级
        /// </summary>
        /// <param name="type"> </param>
        /// <returns> </returns>
        private static int GetPriority(Type type)
        {
            int i;
            if (type.IsGenericType)
            {
                type = type.GetGenericTypeDefinition() ?? type;
            }
            return _Prioritys.TryGetValue(type, out i) ? i : 100;
        }

        /// <summary>
        /// 获取与 <paramref name="type" /> 的泛型定义类型匹配的 <see cref="JsonWriterWrapper" />,如果
        /// <paramref name="genericType" /> 不是泛型,返回 null
        /// </summary>
        /// <param name="genericType"> 用于匹配的 <see cref="Type" /> </param>
        /// <param name="type"> 实际的 <see cref="Type" /> </param>
        /// <returns> </returns>
        private static JsonWriterWrapper SelectByGenericDefinition(Type genericType, Type type)
        {
            if (genericType.IsGenericType && genericType.IsGenericTypeDefinition == false)
            {
                var wrap = _Items.Get(genericType.GetGenericTypeDefinition());
                if (wrap != null)
                {
                    return new JsonWriterWrapper(wrap, type);
                }
            }
            return null;
        }
    }
}