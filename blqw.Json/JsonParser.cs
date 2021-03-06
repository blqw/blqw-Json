﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;

namespace blqw.Serializable
{
    /// <summary>
    /// 用于将Json字符串转换为C#对象
    /// </summary>
    public sealed class JsonParser
    {
        private static readonly JsonType _JsonTypeArrayList = JsonType.Get<JsonList>();
        private static readonly JsonType _JsonTypeDictionary = JsonType.Get<JsonDictionary>();
        private static readonly JsonType _JsonTypeObject = JsonType.Get<object>();

        private readonly JsonType _arrayType;
        private readonly IFormatterConverter _converter;
        private readonly JsonType _keyValueType;

        /// <summary>
        /// 初始化json解析器
        /// </summary>
        public JsonParser()
        {
            _arrayType = _JsonTypeArrayList;
            _keyValueType = _JsonTypeDictionary;
        }
        
        /// <summary>
        /// 初始化json解析器
        /// </summary>
        /// <param name="arrayType">自定义数组类型</param>
        /// <param name="keyValueType">自定义键值对类型</param>
        /// <param name="converter">自定义转型方法</param>
        public JsonParser(Type arrayType, Type keyValueType, IFormatterConverter converter)
        {
            _arrayType = arrayType == null ? _JsonTypeArrayList : JsonType.Get(arrayType);
            _keyValueType = keyValueType == null ? _JsonTypeDictionary : JsonType.Get(keyValueType);
            _converter = converter;
        }


        private static void AreNull(object value, string argName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(argName);
            }
        }

        /// <summary>
        /// 将json字符串转换为指定对象
        /// </summary>
        public object ToObject(Type type, string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                return null;
            }
            object obj = null;
            if (type == null)
            {
                FillObject(ref obj, null, jsonString);
                return obj;
            }
            if (type.IsArray)
            {
                obj = new ArrayList();
                FillObject(ref obj, type, jsonString);
                return ((ArrayList)obj).ToArray(type.GetElementType());
            }
            FillObject(ref obj, type, jsonString);
            return obj;
        }

        /// <summary>
        /// 将json字符串中的数据填充到指定对象,obj为null抛出异常
        /// </summary>
        public void FillObject(object obj, string jsonString)
        {
            AreNull(obj, "obj");
            if (!string.IsNullOrEmpty(jsonString))
            {
                FillObject(ref obj, obj.GetType(), jsonString);
            }
        }


        /* 以下方法私有 */

        /// <summary>
        /// 将json字符串中的数据填充到指定对象
        /// </summary>
        /// <param name="obj"> 将数据填充到对象,如果对象为null且json不是空字符串则创建对象并返回 </param>
        /// <param name="type"> obj的对象类型,该参数不能为null,且必须obj的类型或父类或接口 </param>
        /// <param name="jsonString"> json字符串 </param>
        private void FillObject(ref object obj, Type type, string jsonString)
        {
            unsafe
            {
                try
                {
                    fixed (char* p = jsonString)
                    {
                        using (var reader = new UnsafeJsonReader(p, jsonString))
                        {
                            //如果是由空白和回车组成的字符串,直接返回
                            if (reader.IsEnd()) return;
                            FillObject(ref obj, type, reader);
                            if (reader.IsEnd()) return;
                        }
                    }
                }
                catch (JsonParseException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new JsonParseException("无法解析json : 详见内部异常", jsonString, ex);
                }
                throw new JsonParseException("Json字符串未正确结束", jsonString);
            }
        }

        /// <summary>
        /// 解释 json 字符串,并填充到obj对象中,如果obj为null则新建对象
        /// </summary>
        private void FillObject(ref object obj, Type type, UnsafeJsonReader reader)
        {
            reader.CheckEnd();
            JsonType jsonType;
            if (type == null || type == typeof(object))
            {
                switch (reader.Current)
                {
                    case '{':
                        jsonType = _keyValueType;
                        break;
                    case '[': // 中括号的json仅支持反序列化成IList的对象
                        jsonType = _arrayType;
                        break;
                    default:
                        jsonType = _JsonTypeObject;
                        break;
                }
            }
            else
            {
                jsonType = JsonType.Get(type);
            }


            //如果obj == null创建新对象
            if (obj == null)
            {
                if (jsonType.IsAnonymous)
                {
                    obj = FormatterServices.GetUninitializedObject(jsonType.Type);
                }
                else if (jsonType.IsMataType == false)
                {
                    obj = Activator.CreateInstance(jsonType.Type);
                }
            }


            //判断起始字符
            switch (reader.Current)
            {
                case '{':
                    reader.MoveNext();
                    if (jsonType.IsDictionary)
                    {
                        FillDictionary(ref obj, jsonType, reader);
                    }
                    else
                    {
                        FillProperty(obj, jsonType, reader);
                    }
                    reader.SkipChar('}', true);
                    break;
                case '[': // 中括号的json仅支持反序列化成IList的对象
                    reader.MoveNext();
                    if (jsonType.Type.IsArray)
                    {
                        if (obj is ArrayList)
                        {
                            FillList(obj, jsonType, reader);
                        }
                        else
                        {
                            var arr = obj as Array;
                            if (arr == null)
                            {
                                throw new JsonParseException(jsonType.DisplayText + " 无法从集合型的Json反序列化", reader.RawJson);
                            }
                            FillArray(arr, jsonType, reader);
                        }
                    }
                    else if (jsonType.IsList)
                    {
                        FillList(obj, jsonType, reader);
                    }
                    else
                    {
                        throw new JsonParseException(jsonType.DisplayText + " 无法从集合型的Json反序列化", reader.RawJson);
                    }
                    reader.SkipChar(']', true);
                    break;
                default:
                    obj = ReadValue(reader, jsonType);
                    break;
            }
        }

        /// <summary>
        /// 填充一般对象的属性
        /// </summary>
        /// <param name="obj"> </param>
        /// <param name="jsonType"> </param>
        /// <param name="reader"> </param>
        private void FillProperty(object obj, JsonType jsonType, UnsafeJsonReader reader)
        {
            reader.CheckEnd();
            if (reader.Current == '}')
            {
                return;
            }
            do
            {
                var key = ReadKey(reader); //获取Key
                var member = jsonType[key, true]; //得到对象属性
                if (member != null && member.CanWrite)
                {
                    try
                    {
                        var val = ReadValue(reader, member.JsonType); //得到值
                        member.SetValue(obj, val); //赋值
                    }
                    catch (JsonParseException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new JsonParseException(member.DisplayText + " 赋值失败", reader.RawJson, ex);
                    }
                }
                else
                {
                    SkipValue(reader); //跳过Json中的值
                }
            } while (reader.SkipChar(',', false));
        }

        private void FillArray(Array arr, JsonType jsonType, UnsafeJsonReader reader)
        {
            reader.CheckEnd();
            if (reader.Current == ']' || arr.Length == 0)
            {
                return;
            }

            var length = arr.Length;
            var eleType = jsonType.ElementType;
            for (var i = 0; i < length; i++)
            {
                var val = ReadValue(reader, eleType); //得到值
                arr.SetValue(val, i);
                if (reader.SkipChar(',', false) == false)
                {
                    return;
                }
            }
        }

        /// <summary>
        /// 填充 IDictionary 或者 IDictionary&lt;,&gt;
        /// </summary>
        /// <param name="obj">
        /// IDictionar或IDictionary&lt;,&gt;实例
        /// </param>
        /// <param name="jsonType"> </param>
        /// <param name="reader"> </param>
        private void FillDictionary(ref object obj, JsonType jsonType, UnsafeJsonReader reader)
        {
            reader.CheckEnd();
            if (reader.Current == '}')
            {
                return;
            }
            if (jsonType.AddKeyValue == null)
            {
                throw new JsonParseException($"{jsonType.DisplayText} 无法写入数据,有可能是只读的或找不到Add(key,value)方法",
                    reader.RawJson);
            }
            
            var eleType = jsonType.ElementType;
            var keyType = jsonType.KeyType;
            if (keyType.TypeCode == TypeCode.String || keyType.IsObject)
            {
                var key = ReadKey(reader); //获取Key
                if((obj as IDictionary)?[key] is Type newEleType)
                {
                    eleType = JsonType.Get(newEleType);
                    ((IDictionary) obj).Remove(key);
                }
                var val = ReadValue(reader, eleType); //得到值
                if (key != null && key.Length > 0 && val is string && key[0] == '$' && key == "$Type$")
                {
                    var type = Type.GetType((string)val, false, false);
                    if (type != null)
                    {
                        jsonType = JsonType.Get(type);
                        obj = Activator.CreateInstance(type);
                        if (reader.SkipChar(',', false))
                        {
                            FillProperty(obj, jsonType, reader);
                        }
                        return;
                    }
                }
                jsonType.AddKeyValue(obj, key, val);

                while (reader.SkipChar(',', false))
                {
                    key = ReadKey(reader); //获取Key
                    if ((obj as IDictionary)?[key] is Type newEleType1)
                    {
                        eleType = JsonType.Get(newEleType1);
                        ((IDictionary)obj).Remove(key);
                    }
                    else
                    {
                        eleType = jsonType.ElementType;
                    }
                    val = ReadValue(reader, eleType); //得到值
                    jsonType.AddKeyValue(obj, key, val);
                }
            }
            else
            {
                do
                {
                    var keyStr = ReadKey(reader); //获取Key
                    var key = keyType.Convert(keyStr, jsonType.Type);
                    var val = ReadValue(reader, eleType); //得到值
                    jsonType.AddKeyValue(obj, key, val);
                } while (reader.SkipChar(',', false));
            }
        }

        /// <summary>
        /// 填充 IList 或者 IList &lt;&gt;
        /// </summary>
        /// <param name="obj"> </param>
        /// <param name="jsonType"> </param>
        /// <param name="reader"> </param>
        private void FillList(object obj, JsonType jsonType, UnsafeJsonReader reader)
        {
            reader.CheckEnd();
            if (reader.Current == ']')
            {
                return;
            }
            if (jsonType.AddValue == null)
            {
                throw new JsonParseException($"{jsonType.DisplayText} 无法写入数据,有可能是只读的或找不到Add(value)方法", reader.RawJson);
            }
            var eleType = jsonType.ElementType;
            do
            {
                var val = ReadValue(reader, eleType); //得到值
                //((dynamic)obj).Add((dynamic)val);
                jsonType.AddValue(obj, val); //赋值
            } while (reader.SkipChar(',', false));
        }

        /// <summary>
        /// 跳过一个键
        /// </summary>
        /// <param name="reader"> </param>
        private static void SkipKey(UnsafeJsonReader reader)
        {
            reader.CheckEnd();
            if (reader.Current == '"' || reader.Current == '\'')
            {
                reader.SkipString();
            }
            else
            {
                reader.SkipWord();
            }
            reader.SkipChar(':', true);
        }

        /// <summary>
        /// 获取一个键
        /// </summary>
        /// <param name="reader"> </param>
        /// <returns> </returns>
        private static string ReadKey(UnsafeJsonReader reader)
        {
            reader.CheckEnd();
            string key;
            if (reader.Current == '"' || reader.Current == '\'')
            {
                key = reader.ReadString();
            }
            else
            {
                key = reader.ReadWord();
            }
            reader.SkipChar(':', true);
            return key;
        }


        /// <summary>
        /// 跳过一个值
        /// </summary>
        /// <param name="reader"> </param>
        private static void SkipValue(UnsafeJsonReader reader)
        {
            reader.CheckEnd();
            switch (reader.Current)
            {
                case '[':
                    reader.MoveNext();
                    reader.CheckEnd();
                    if (reader.Current != ']')
                    {
                        do
                        {
                            SkipValue(reader);
                        } while (reader.SkipChar(',', false));
                    }
                    reader.SkipChar(']', true);
                    break;
                case '{':
                    reader.MoveNext();
                    reader.CheckEnd();
                    if (reader.Current != '}')
                    {
                        do
                        {
                            SkipKey(reader);
                            SkipValue(reader);
                        } while (reader.SkipChar(',', false));
                    }
                    reader.SkipChar('}', true);
                    break;
                case '"':
                case '\'':
                    reader.SkipString();
                    break;
                default:
                    reader.ReadConsts(false);
                    break;
            }
        }

        /// <summary>
        /// 读取一个值对象
        /// </summary>
        /// <param name="reader"> </param>
        /// <param name="jsonType"> </param>
        /// <returns> </returns>
        private object ReadValue(UnsafeJsonReader reader, JsonType jsonType)
        {
            reader.CheckEnd();
            switch (reader.Current)
            {
                case '[':
                    reader.MoveNext();
                    var array = ReadList(reader, jsonType);
                    reader.SkipChar(']', true);
                    return array;
                case '{':
                    reader.MoveNext();
                    var obj = ReadObject(reader, jsonType);
                    reader.SkipChar('}', true);
                    return obj;
                case '"':
                case '\'':
                    return ParseString(reader, jsonType);
                default:
                    var val = reader.ReadConsts(false);
                    if (_converter != null)
                    {
                        return _converter.Convert(val, jsonType.TypeCode);
                    }
                    return jsonType.Convert(val, jsonType.Type);
            }
        }

        /// <summary>
        /// 将字符串解析为指定类型
        /// </summary>
        /// <param name="reader"> </param>
        /// <param name="jsonType"> </param>
        /// <returns> </returns>
        private object ParseString(UnsafeJsonReader reader, JsonType jsonType)
        {
            //数字
            if (jsonType.TypeCode == TypeCode.Boolean)
            {
                //枚举
                if (jsonType.Type.IsEnum)
                {
                    return jsonType.Convert(reader.ReadString(), jsonType.Type);
                }
                var quot = reader.Current;
                reader.MoveNext();
                var val = jsonType.Convert(reader.ReadConsts(true), jsonType.Type);
                reader.SkipChar(quot, true);
                return val;
            }
            if (jsonType.TypeCode == TypeCode.DateTime)
            {
                return reader.ReadDateTime();
            }
            if (_converter != null)
            {
                return _converter.Convert(reader.ReadString(), jsonType.TypeCode);
            }
            return jsonType.Convert(reader.ReadString(), jsonType.Type);
        }


        /// <summary>
        /// 读取数组
        /// </summary>
        /// <param name="reader"> </param>
        /// <param name="jsonType"> </param>
        /// <returns> </returns>
        private object ReadList(UnsafeJsonReader reader, JsonType jsonType)
        {
            if (jsonType.Type.IsArray)
            {
                var list = new ArrayList(); //Array类型中的AddValue方法调用的是ArrayList
                FillList(list, jsonType, reader);
                return list.ToArray(jsonType.ElementType.Type);
            }
            if (jsonType.IsObject)
            {
                var list = Activator.CreateInstance(_arrayType.Type);
                FillList(list, _arrayType, reader);
                return list;
            }
            else
            {
                var list = Activator.CreateInstance(jsonType.Type);
                FillList(list, jsonType, reader);
                return list;
            }
        }

        /// <summary>
        /// 读取对象
        /// </summary>
        /// <param name="reader"> </param>
        /// <param name="jsonType"> </param>
        /// <returns> </returns>
        private object ReadObject(UnsafeJsonReader reader, JsonType jsonType)
        {
            if (jsonType.IsObject)
            {
                var obj = Activator.CreateInstance(_keyValueType.Type);
                FillDictionary(ref obj, _keyValueType, reader);
                return obj;
            }
            if (jsonType.IsDictionary)
            {
                var obj = Activator.CreateInstance(jsonType.Type);
                FillDictionary(ref obj, jsonType, reader);
                return obj;
            }
            if (!jsonType.IsMataType)
            {
                var obj = Activator.CreateInstance(jsonType.Type);
                FillProperty(obj, jsonType, reader);
                return obj;
            }
            return ReadValue(reader, jsonType);
        }
    }
}