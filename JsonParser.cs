﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace blqw
{
    /// <summary> 用于将Json字符串转换为C#对象
    /// </summary>
    public class JsonParser
    {
        #region 构造函数
        public JsonParser()
        {
        }
        #endregion

        /// <summary> 将json字符串转换为指定对象
        /// </summary>
        public Object ToObject(Type type, string json)
        {
            if (json == null)
            {
                return null;
            }
            if (type == null)
            {
                object obj = null;
                FillObject(ref obj, null, json);
                return obj;
            }
            if (type.IsArray)
            {
                object arr = new ArrayList();
                var eleType = type.GetElementType();
                FillObject(ref arr, Literacy.Cache(eleType, false), json);
                return ((ArrayList)arr).ToArray(eleType);
            }
            else
            {
                var lit = Literacy.Cache(type, true);
                var obj = lit.NewObject();
                FillObject(ref obj, lit, json);
                return obj;
            }
        }

        /// <summary> 将json字符串中的数据填充到指定对象
        /// </summary>
        public void FillObject(object obj, string json)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (json != null)
            {
                FillObject(ref obj, Literacy.Cache(obj.GetType(), true), json);
            }
        }

        private void FillObject(ref object obj, Literacy lit, string json)
        {
            if (json == null || json.Length == 0)
            {
                return;
            }

            unsafe
            {
                fixed (char* p = json)
                {
                    UnsafeJsonReader reader = new UnsafeJsonReader(p, json.Length);

                    if (reader.IsEnd())
                    {
                        return;
                    }

                    if (reader.Current == '{')
                    {
                        reader.MoveNext();
                        if (obj == null)
                        {
                            obj = new Dictionary<string, object>();
                            lit = Literacy.Cache(typeof(Dictionary<string, object>), false);
                        }
                        FillObject(obj, lit, reader);
                        if (reader.Current != '}')
                        {
                            ThrowMissingCharException('}');
                        }
                    }
                    else if (reader.Current == '[')
                    {
                        reader.MoveNext();

                        if (obj == null)
                        {
                            obj = new ArrayList();
                            FillList((IList)obj, typeof(object), reader);
                        }
                        else if (obj is ArrayList)
                        {
                            FillList((IList)obj, lit.Type, reader);
                        }
                        else
                        {
                            var st = GenericCollection.GetList(lit.Type);
                            if (st == null)
                            {
                                ThrowNoIList(lit.Type);
                            }
                            FillList((IList)obj, st.ElementType, reader);
                        }
                        
                        if (reader.Current != ']')
                        {
                            ThrowMissingCharException(']');
                        }
                    }
                    else
                    {
                        ThrowException("起始字符:" + reader.Current);
                    }
                    reader.MoveNext();
                    if (reader.IsEnd())
                    {
                        reader.Dispose();
                    }
                    else
                    {
                        ThrowException("错误的结束字符:" + reader.Current);
                    }
                }
            }
        }

        private void FillObject(object obj, Literacy lit, UnsafeJsonReader reader)
        {
            if (reader.Current == '}') return;
            if (obj is IDictionary)
            {
                var st = GenericCollection.GetDict(obj.GetType());
                FillDictionary((IDictionary)obj, st.KeyType, st.ElementType, reader);
            }
            else
            {
                while (true)
                {
                    var key = ReadKey(reader);      //获取Key
                    var prop = lit.Property[key];   //得到对象属性
                    if (prop == null || prop.CanWrite == false)//如果属性不存在或不可写
                    {
                        SkipValue(reader);          //跳过Json中的值
                    }
                    else
                    {
                        object val = ReadValue(reader, prop.MemberType);//得到值
                        prop.TrySetValue(obj, val); //赋值
                    }
                    if (reader.SkipChar(',') == false)
                    {
                        return;
                    }
                }
            }
        }

        private void FillDictionary(IDictionary dict, Type keyType, Type elementType, UnsafeJsonReader reader)
        {
            if (reader.Current == '}') return;
            if (keyType == typeof(string) || keyType == typeof(object))
            {
                while (true)
                {
                    string key = ReadKey(reader);      //获取Key
                    object val = ReadValue(reader, elementType);//得到值
                    dict[key] = val;
                    if (reader.SkipChar(',') == false)//跳过,号
                    {
                        return;                     //失败,终止方法
                    }
                }
            }
            else
            {
                while (true)
                {
                    string skey = ReadKey(reader);      //获取Key
                    object key = Convert.ChangeType(skey, keyType);
                    object val = ReadValue(reader, elementType);//得到值
                    dict[key] = val;
                    if (reader.SkipChar(',') == false)//跳过,号
                    {
                        return;                     //失败,终止方法
                    }
                }
            }
        }

        private void FillList(IList list, Type elementType, UnsafeJsonReader reader)
        {
            if (reader.Current == ']') return;
            while (true)
            {
                object val = ReadValue(reader, elementType);//得到值
                list.Add(val);                  //赋值
                if (reader.SkipChar(',') == false)//跳过,号
                {
                    return;                     //失败,终止方法
                }
            }
        }

        /// <summary> 跳过一个值
        /// </summary>
        /// <param name="reader"></param>
        private void SkipValue(UnsafeJsonReader reader)
        {
            switch (reader.Current)
            {
                case '[':
                    reader.MoveNext();
                    if (reader.SkipChar(']'))
                    {
                        return;
                    }
                    do
                    {
                        SkipValue(reader);
                    } while (reader.SkipChar(','));
                    if (reader.Current != ']')
                    {
                        ThrowException("缺少闭合的 ]");
                    }
                    reader.MoveNext();
                    break;
                case '{':
                    reader.MoveNext();
                    if (reader.SkipChar('}'))
                    {
                        return;
                    }
                    do
                    {
                        ReadKey(reader);
                        SkipValue(reader);
                    } while (reader.SkipChar(','));
                    if (reader.Current != '}')
                    {
                        ThrowException("缺少闭合的 }");
                    }
                    reader.MoveNext();
                    break;
                case '"':
                case '\'':
                    reader.SkipString();
                    break;
                default:
                    reader.ReadConsts();
                    break;
            }
        }

        /// <summary> 跳过一个键
        /// </summary>
        /// <param name="reader"></param>
        private void SkipKey(UnsafeJsonReader reader)
        {
            if (reader.Current == '"' || reader.Current == '\'')
            {
                reader.SkipString();
            }
            else
            {
                reader.SkipWord();
            }
            reader.SkipChar(':');
        }

        /// <summary> 获取一个键
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private string ReadKey(UnsafeJsonReader reader)
        {
            string key;
            if (reader.Current == '"' || reader.Current == '\'')
            {
                key = reader.ReadString();
            }
            else
            {
                key = reader.ReadWord();
            }
            if (reader.SkipChar(':') == false)//跳过:号
            {
                ThrowMissingCharException(':'); //失败,终止方法
            }
            return key;
        }

        /// <summary> 读取一个值对象
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private object ReadValue(UnsafeJsonReader reader, Type type)
        {
            if (reader.IsEnd())
            {
                ThrowException("字符串意外结束!");
            }
            var c = reader.Current;
            switch (c)
            {
                case '[':
                    reader.MoveNext();
                    var array = ReadArray(reader, type);
                    if (reader.Current != ']')
                    {
                        ThrowException("缺少闭合的 ]");
                    }
                    reader.MoveNext();
                    return array;
                case '{':
                    reader.MoveNext();
                    var obj = ReadObject(reader, type);
                    if (reader.Current != '}')
                    {
                        ThrowException("缺少闭合的 }");
                    }
                    reader.MoveNext();
                    return obj;
                case '"':
                case '\'':
                    {
                        if (type == typeof(DateTime))
                        {
                            return reader.ReadDateTime();
                        }
                        return ParseString(reader, type);
                    }
                default:
                    {
                        object val = reader.ReadConsts();
                        if (val == null)
                        {
                            return null;
                        }
                        if (type.IsInstanceOfType(val) == false)
                        {
                            val = Convert.ChangeType(val, type);
                        }
                        return val;
                    }
            }
        }

        /// <summary> 将字符串解析为指定类型
        /// </summary>
        /// <param name="str"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private object ParseString(UnsafeJsonReader reader, Type type)
        {
            char quot;
            string str;
            var typecode = Type.GetTypeCode(type);
            if ((typecode >= TypeCode.SByte && typecode <= TypeCode.Decimal) || typecode == TypeCode.Boolean)
            {
                if (type.IsSubclassOf(typeof(Enum)))
                {
                    return Enum.Parse(type, reader.ReadString());
                }
                quot = reader.Current;
                var val = Convert.ChangeType(reader.ReadConsts(), type);
                if (reader.SkipChar(quot) == false)
                {
                    ThrowMissingCharException(quot);
                }
                return val;
            }
            switch (typecode)
            {
                case TypeCode.DateTime:
                    return reader.ReadDateTime();
                case TypeCode.Object:
                    str = reader.ReadString();
                    if (type == typeof(Guid))
                    {
                        if (str.Length > 30)//这个是从fastJson学来的 =^_^=
                        {
                            return new Guid(str);
                        }
                        else
                        {
                            return new Guid(Convert.FromBase64String(str));
                        }
                    }
                    else if (type == typeof(Object))
                    {
                        return str;
                    }
                    throw new Exception();
                case TypeCode.Char:
                    return Char.Parse(reader.ReadString());
                case TypeCode.String:
                    return reader.ReadString();
                case TypeCode.DBNull:
                    str = reader.ReadString();
                    if (str.Length == 0 ||
                        str == "null" ||
                        str == "undefined" ||
                        string.IsNullOrEmpty(str))
                    {
                        return DBNull.Value;
                    }
                    throw new Exception();
                default:
                    return Convert.ChangeType(reader.ReadString(), type);
            }
        }

        /// <summary> 读取数组
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private IList ReadArray(UnsafeJsonReader reader, Type type)
        {
            if (type.IsArray)
            {
                var eletype = type.GetElementType();
                ArrayList list = new ArrayList();
                FillList(list, Nullable.GetUnderlyingType(eletype) ?? eletype, reader);
                return list.ToArray(eletype);
            }
            else if (type == typeof(object))
            {
                ArrayList list = new ArrayList();
                FillList(list, typeof(object), reader);
                return list.ToArray(typeof(object));
            }
            else
            {
                var st = GenericCollection.GetList(type);
                if (st == null)
                {
                    ThrowCastException("[]", type);
                }
                else if (st.Init == null)
                {
                    ThrowNoConstructor(type);
                }
                var list = (IList)st.Init();
                FillList(list, st.ElementType, reader);
                return list;
            }
        }

        /// <summary> 读取对象
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private object ReadObject(UnsafeJsonReader reader, Type type)
        {
            object obj;
            if (type.GetInterface("System.Collections.IDictionary") == typeof(IDictionary))
            {
                var st = GenericCollection.GetDict(type);
                if (st.Init == null)
                {
                    ThrowNoConstructor(type);
                }
                obj = st.Init();
                FillDictionary((IDictionary)obj, st.KeyType, st.ElementType, reader);
                return obj;
            }
            else if (type == typeof(object))
            {
                obj = new Dictionary<string, object>();
                FillDictionary((IDictionary)obj, typeof(string), typeof(object), reader);
                return obj;
            }
            else
            {
                var lit = Literacy.Cache(type, true);
                obj = lit.NewObject();
                FillObject(obj, lit, reader);
                return obj;
            }
        }

        private void ThrowMissingCharException(char c)
        {
            throw new Exception("缺少必要符号:" + c);
        }

        private void ThrowException(string word)
        {
            throw new Exception("无法解析:" + word);
        }

        private void ThrowNoIList(Type type)
        {
            throw new InvalidCastException(type.FullName + " 没有实现IList接口,无法接收数组的值");
        }

        private void ThrowCastException(string str, Type type)
        {
            throw new InvalidCastException(string.Concat("无法将 ", str, " 转为 ", type, " 类型"));
        }

        private void ThrowNoConstructor(Type type)
        {
            throw new Exception(type.FullName + " 类型缺少无参的构造函数");
        }

        class GenericCollection
        {
            static Dictionary<Type, GenericCollection> Cache = new Dictionary<Type, GenericCollection>();
            public LiteracyNewObject Init;
            public Type ElementType;
            public Type KeyType;

            public static GenericCollection GetList(Type type)
            {
                GenericCollection list;
                if (Cache.TryGetValue(type, out list))
                {
                    return list;
                }
                if (type.GetInterface("System.Collections.IList") != typeof(IList))
                {
                    return null;
                }
                lock (Cache)
                {
                    if (Cache.TryGetValue(type, out list))
                    {
                        return list;
                    }
                    list = new GenericCollection();
                    list.Init = Literacy.CreateNewObject(type);
                    if (type.IsGenericType)
                    {
                        list.ElementType = type.GetGenericArguments()[0];
                        list.ElementType = Nullable.GetUnderlyingType(list.ElementType) ?? list.ElementType;
                    }
                    else
                    {
                        list.ElementType = typeof(object);
                    }
                    Cache.Add(type, list);
                    return list;
                }
            }

            public static GenericCollection GetDict(Type type)
            {
                GenericCollection dict;
                if (Cache.TryGetValue(type, out dict))
                {
                    return dict;
                }
                lock (Cache)
                {
                    if (Cache.TryGetValue(type, out dict))
                    {
                        return dict;
                    }
                    dict = new GenericCollection();
                    dict.Init = Literacy.CreateNewObject(type);
                    if (type.IsGenericType)
                    {
                        var ga = type.GetGenericArguments();
                        if (ga.Length > 1)
                        {
                            dict.KeyType = type.GetGenericArguments()[0];
                            dict.ElementType = type.GetGenericArguments()[1];
                            dict.ElementType = Nullable.GetUnderlyingType(dict.ElementType) ?? dict.ElementType;
                        }
                        else
                        {
                            dict.ElementType = typeof(object);
                        }
                    }
                    else
                    {
                        dict.ElementType = typeof(object);
                    }
                    Cache.Add(type, dict);
                    return dict;
                }
            }
        }
    }
}
