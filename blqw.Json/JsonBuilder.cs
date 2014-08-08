﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace blqw
{
    /// <summary> 用于将C#转换为Json字符串
    /// </summary>
    public class JsonBuilder
    {
        //循环引用对象缓存区
        private IList _loopObject;

        //private Dictionary<int,

        #region private

        private void AppendConvertible(IConvertible obj)
        {
            var @enum = obj as Enum;
            if (@enum != null)
            {
                AppendEnum(@enum);
                return;
            }
            switch (obj.GetTypeCode())
            {
                case TypeCode.Boolean: AppendBoolean(obj.ToBoolean(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.Byte: AppendByte(obj.ToByte(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.Char: AppendChar(obj.ToChar(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.DateTime: AppendDateTime(obj.ToDateTime(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.Decimal: AppendDecimal(obj.ToDecimal(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.Double: AppendDouble(obj.ToDouble(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.Int16: AppendInt16(obj.ToInt16(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.Int32: AppendInt32(obj.ToInt32(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.Int64: AppendInt64(obj.ToInt64(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.SByte: AppendSByte(obj.ToSByte(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.Single: AppendSingle(obj.ToSingle(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.UInt16: AppendUInt16(obj.ToUInt16(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.UInt32: AppendUInt32(obj.ToUInt32(CultureInfo.InvariantCulture));
                    break;
                case TypeCode.UInt64: AppendUInt64(obj.ToUInt64(CultureInfo.InvariantCulture));
                    break;
                default:
                    AppendCheckLoopRef(obj);
                    break;
            }
        }

        private void AppendCheckLoopRef(object obj)
        {
            if (obj is ValueType)
            {
                if (obj is IConvertible) AppendOther(obj);
                else if (obj is IDictionary) AppendJson((IDictionary)obj);
                else if (obj is IDataReader) AppendDataSet((IDataReader)obj);
                else if (obj is IEnumerable) AppendArray((IEnumerable)obj);
                else AppendOther(obj);
            }
            else if (CheckLoopRef == false)
            {
                _depth++;
                if (_depth > 128)
                {
                    throw new NotSupportedException("对象过于复杂或存在循环引用");
                }
                if (obj is IConvertible) AppendOther(obj);
                else if (obj is IDictionary) AppendJson((IDictionary)obj);
                else if (obj is IDataReader) AppendDataSet((IDataReader)obj);
                else if (obj is IEnumerable) AppendArray((IEnumerable)obj);
                else if (obj is DataSet) AppendDataSet((DataSet)obj);
                else if (obj is DataTable) AppendDataTable((DataTable)obj);
                else if (obj is DataView) AppendDataView((DataView)obj);
                else AppendOther(obj);
                _depth--;
            }
            else if (_loopObject.Contains(obj) == false)
            {
                var index = _loopObject.Add(obj);
                if (obj is IConvertible) AppendOther(obj);
                else if (obj is IDictionary) AppendJson((IDictionary)obj);
                else if (obj is IDataReader) AppendDataSet((IDataReader)obj);
                else if (obj is IEnumerable) AppendArray((IEnumerable)obj);
                else if (obj is DataSet) AppendDataSet((DataSet)obj);
                else if (obj is DataTable) AppendDataTable((DataTable)obj);
                else if (obj is DataView) AppendDataView((DataView)obj);
                else AppendOther(obj);
                _loopObject.RemoveAt(index);
            }
            else
            {
                Buffer.Append("undefined");
            }
        }

        private static IEnumerable GetDataReaderNames(IDataRecord reader)
        {
            int c = reader.FieldCount;
            for (int i = 0; i < c; i++)
            {
                yield return reader.GetName(i);
            }
        }

        private static IEnumerable GetDataReaderValues(IDataReader reader)
        {
            int c = reader.FieldCount;
            while (reader.Read())
            {
                object[] values = new object[c];
                reader.GetValues(values);
                yield return values;
            }
        }

        public static string EscapeString(string str)
        {
            var size = str.Length * 2;
            if (size > ushort.MaxValue)
            {
                size = ushort.MaxValue;
            }
            QuickStringWriter buffer = null;
            try
            {
                unsafe
                {
                    var length = str.Length;
                    fixed (char* fp = str)
                    {
                        char* p = fp;
                        char* end = fp + length;
                        char* flag = fp;
                        while (p < end)
                        {
                            char c = *p;
                            switch (c)
                            {
                                case '\\':
                                case '"':
                                    if (buffer == null) buffer = new QuickStringWriter((ushort)size).Append('"');
                                    buffer.Append(flag, 0, (int)(p - flag));
                                    buffer.Append('\\');
                                    flag = p;
                                    break;
                                case '\n':
                                    if (buffer == null) buffer = new QuickStringWriter((ushort)size);
                                    buffer.Append(flag, 0, (int)(p - flag));
                                    buffer.Append('\\');
                                    buffer.Append('n');
                                    flag = p + 1;
                                    break;
                                case '\r':
                                    if (buffer == null) buffer = new QuickStringWriter((ushort)size);
                                    buffer.Append(flag, 0, (int)(p - flag));
                                    buffer.Append('\\');
                                    buffer.Append('r');
                                    flag = p + 1;
                                    break;
                                case '\t':
                                    if (buffer == null) buffer = new QuickStringWriter((ushort)size);
                                    buffer.Append(flag, 0, (int)(p - flag));
                                    buffer.Append('\\');
                                    buffer.Append('t');
                                    flag = p + 1;
                                    break;
                                case '\f':
                                    if (buffer == null) buffer = new QuickStringWriter((ushort)size);
                                    buffer.Append(flag, 0, (int)(p - flag));
                                    buffer.Append('\\');
                                    buffer.Append('f');
                                    flag = p + 1;
                                    break;
                                case '\0':
                                    if (buffer == null) buffer = new QuickStringWriter((ushort)size);
                                    buffer.Append(flag, 0, (int)(p - flag));
                                    buffer.Append('\\');
                                    buffer.Append('0');
                                    flag = p + 1;
                                    break;
                                case '\a':
                                    if (buffer == null) buffer = new QuickStringWriter((ushort)size);
                                    buffer.Append(flag, 0, (int)(p - flag));
                                    buffer.Append('\\');
                                    buffer.Append('a');
                                    flag = p + 1;
                                    break;
                                case '\b':
                                    if (buffer == null) buffer = new QuickStringWriter((ushort)size);
                                    buffer.Append(flag, 0, (int)(p - flag));
                                    buffer.Append('\\');
                                    buffer.Append('b');
                                    flag = p + 1;
                                    break;
                                case '\v':
                                    if (buffer == null) buffer = new QuickStringWriter((ushort)size);
                                    buffer.Append(flag, 0, (int)(p - flag));
                                    buffer.Append('\\');
                                    buffer.Append('v');
                                    flag = p + 1;
                                    break;
                                default:
                                    break;
                            }
                            p++;
                        }
                        if (flag == fp)
                        {
                            if (buffer == null)
                            {
                                return str;
                            }
                            buffer.Append(fp, 0, length);
                        }
                        else if (p > flag)
                        {
                            if (buffer == null) buffer = new QuickStringWriter((ushort)size).Append('"');
                            buffer.Append(flag, 0, (int)(p - flag));
                        }
                    }
                }
                return buffer.ToString();
            }
            finally
            {
                if (buffer != null)
                {
                    buffer.Dispose();
                }
            }


        }

        private int _depth;

        #endregion

        protected QuickStringWriter Buffer;//字符缓冲区

        public JsonBuilder()
            : this(JsonBuilderSettings.Default)
        {

        }

        public JsonBuilder(JsonBuilderSettings settings)
        {
            FormatDate = (settings & JsonBuilderSettings.FormatDate) != 0;
            FormatTime = (settings & JsonBuilderSettings.FormatTime) != 0;
            SerializableField = (settings & JsonBuilderSettings.SerializableField) != 0;
            QuotWrapNumber = (settings & JsonBuilderSettings.QuotWrapNumber) != 0;
            BooleanToNumber = (settings & JsonBuilderSettings.BooleanToNumber) != 0;
            EnumToNumber = (settings & JsonBuilderSettings.EnumToNumber) != 0;
            CheckLoopRef = (settings & JsonBuilderSettings.CheckLoopRef) != 0;
            IgnoreEmptyTime = (settings & JsonBuilderSettings.IgnoreEmptyTime) != 0;
            QuotWrapBoolean = (settings & JsonBuilderSettings.QuotWrapBoolean) != 0;
            IgnoreNullMember = (settings & JsonBuilderSettings.IgnoreNullMember) != 0;
        }

        #region settings

        /// <summary> 格式化 DateTime 对象中的日期
        /// </summary>
        public bool FormatDate;
        /// <summary> 格式化 DateTime 对象中的时间
        /// </summary>
        public bool FormatTime;
        /// <summary> 同时序列化字段
        /// </summary>
        public bool SerializableField;
        /// <summary> 使用双引号包装数字的值
        /// </summary>
        public bool QuotWrapNumber;
        /// <summary> 将布尔值转为数字值 true = 1 ,false = 0
        /// </summary>
        public bool BooleanToNumber;
        /// <summary> 将枚举转为对应的数字值
        /// </summary>
        public bool EnumToNumber;
        /// <summary> 检查循环引用,发现循环应用时输出 undefined
        /// </summary>
        public bool CheckLoopRef;
        /// <summary> 格式化 DateTime 对象中的时间时忽略(00:00:00.000000) ,存在FormatTime时才生效
        /// </summary>
        public bool IgnoreEmptyTime;
        /// <summary> 使用双引号包装布尔的值
        /// </summary>
        public bool QuotWrapBoolean;
        /// <summary> 忽略值是null的属性
        /// </summary>
        public bool IgnoreNullMember;
        #endregion

        /// <summary> 将对象转换为Json字符串
        /// </summary>
        public string ToJsonString(object obj)
        {
            using (Buffer = new QuickStringWriter(4096))
            {
                if (CheckLoopRef)
                {
                    _loopObject = new ArrayList(64);
                }
                AppendObject(obj);
                var json = Buffer.ToString();
                _loopObject = null;
                return json;
            }
        }
        /// <summary> 将 任意对象 转换Json字符串写入Buffer
        /// </summary>
        /// <param name="obj">任意对象</param>
        protected void AppendObject(object obj)
        {
            if (obj == null || obj is DBNull)
            {
                Buffer.Append("null");
            }
            else
            {
                var s = obj as string;
                if (s != null)
                {
                    AppendString(s);
                }
                else
                {
                    var conv = obj as IConvertible;
                    if (conv != null)
                    {
                        AppendConvertible(conv);
                    }
                    else if (obj is Guid)
                    {
                        AppendGuid((Guid)obj);
                    }
                    else
                    {
                        AppendCheckLoopRef(obj);
                    }
                }
            }
        }

        /// <summary> 非安全方式向Buffer追加一个字符(该方法不会验证字符的有效性)
        /// </summary>
        /// <param name="value">向Buffer追加的字符</param>
        protected virtual void UnsafeAppend(Char value)
        {
            Buffer.Append(value);
        }
        /// <summary> 非安全方式向Buffer追加一个字符串(该方法不会验证字符串的有效性)
        /// </summary>
        /// <param name="value">向Buffer追加的字符串</param>
        protected virtual void UnsafeAppend(string value)
        {
            Buffer.Append(value);
        }
        /// <summary> 将未知对象按属性名和值转换为Json中的键值字符串写入Buffer
        /// </summary>
        /// <param name="obj">非null的位置对象</param>
        protected virtual void AppendOther(object obj)
        {
            Type t = obj.GetType();
            Buffer.Append('{');
            string fix = "";
            foreach (var p in t.GetProperties())
            {
                if (p.CanRead)
                {
                    object value = p.GetValue(obj, null);
                    if (value != null || !IgnoreNullMember)
                    {
                        Buffer.Append(fix);
                        AppendKey(p.Name, false);
                        AppendObject(value);
                        fix = ",";
                    }
                }
            }
            Buffer.Append('}');
        }
        /// <summary> 追加Key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="escape">key中是否有(引号,回车,制表符等)特殊字符,需要转义</param>
        protected virtual void AppendKey(string key, bool escape)
        {
            if (escape)
            {
                AppendString(key);
            }
            else
            {
                Buffer.Append('"');
                Buffer.Append(key);
                Buffer.Append('"');
            }
            Buffer.Append(':');
        }
        /// <summary> 将 Byte 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Byte 对象</param>
        protected virtual void AppendByte(Byte value) { AppendNumber(value); }
        /// <summary> 将 Decimal 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Decimal 对象</param>
        protected virtual void AppendDecimal(Decimal value) { AppendNumber(value); }
        /// <summary> 将 Int16 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Int16 对象</param>
        protected virtual void AppendInt16(Int16 value) { AppendNumber(value); }
        /// <summary> 将 Int32 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Int32 对象</param>
        protected virtual void AppendInt32(Int32 value) { AppendNumber(value); }
        /// <summary> 将 Int64 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Int64 对象</param>
        protected virtual void AppendInt64(Int64 value) { AppendNumber(value); }
        /// <summary> 将 SByte 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">SByte 对象</param>
        protected virtual void AppendSByte(SByte value) { AppendNumber(value); }
        /// <summary> 将 UInt16 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">UInt16 对象</param>
        protected virtual void AppendUInt16(UInt16 value) { AppendNumber(value); }
        /// <summary> 将 UInt32 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">UInt32 对象</param>
        protected virtual void AppendUInt32(UInt32 value) { AppendNumber(value); }
        /// <summary> 将 UInt64 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">UInt64 对象</param>
        protected virtual void AppendUInt64(UInt64 value) { AppendNumber(value); }
        /// <summary> 将 Double 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Double 对象</param>
        protected virtual void AppendDouble(Double value) { AppendNumber(value); }
        /// <summary> 将 Single 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Single 对象</param>
        protected virtual void AppendSingle(Single value) { AppendNumber(value); }
        /// <summary> 将 Boolean 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Boolean 对象</param>
        protected virtual void AppendBoolean(Boolean value)
        {
            if (BooleanToNumber)
            {
                AppendNumber(value ? 1 : 0);
            }
            else if (QuotWrapBoolean)
            {
                Buffer.Append('"');
                Buffer.Append(value);
                Buffer.Append('"');
            }
            else
            {
                Buffer.Append(value);
            }
        }
        /// <summary> 将 Char 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Char 对象</param>
        protected virtual void AppendChar(Char value)
        {
            Buffer.Append('"');
            //如果是特殊字符,将转义之后写入
            switch (value)
            {
                case '\\':
                    Buffer.Append('\\');
                    Buffer.Append('\\');
                    break;
                case '"':
                    Buffer.Append('\\');
                    Buffer.Append('"');
                    break;
                case '\n':
                    Buffer.Append('\\');
                    Buffer.Append('n');
                    break;
                case '\r':
                    Buffer.Append('\\');
                    Buffer.Append('r');
                    break;
                case '\t':
                    Buffer.Append('\\');
                    Buffer.Append('t');
                    break;
                case '\f':
                    Buffer.Append('\\');
                    Buffer.Append('f');
                    break;
                case '\0':
                    Buffer.Append('\\');
                    Buffer.Append('0');
                    break;
                case '\a':
                    Buffer.Append('\\');
                    Buffer.Append('a');
                    break;
                case '\b':
                    Buffer.Append('\\');
                    Buffer.Append('b');
                    break;
                case '\v':
                    Buffer.Append('\\');
                    Buffer.Append('v');
                    break;
                default:
                    Buffer.Append(value);
                    break;
            }
            Buffer.Append('"');
        }
        /// <summary> 将 可格式化 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="val">可格式化对象</param>
        /// <param name="format">格式化参数</param>
        /// <param name="provider">格式化机制</param>
        protected virtual void AppendFormattable(IFormattable formattable, string format, IFormatProvider provider)
        {
            if (formattable is DateTime)
            {
                if (string.Equals(format, "yyyy-MM-dd HH:mm:ss", StringComparison.Ordinal))
                {
                    Buffer.Append('"');
                    Buffer.Append((DateTime)formattable, true, true, false);
                    Buffer.Append('"');
                }
                else if (string.Equals(format, "yyyy-MM-dd HH:mm:ss.fff", StringComparison.Ordinal))
                {
                    Buffer.Append('"');
                    Buffer.Append((DateTime)formattable, true, true, true);
                    Buffer.Append('"');
                }
                else if (string.Equals(format, "HH:mm:ss", StringComparison.Ordinal))
                {
                    Buffer.Append('"');
                    Buffer.Append((DateTime)formattable, false, true, false);
                    Buffer.Append('"');
                }
                else if (string.Equals(format, "HH:mm:ss.fff", StringComparison.Ordinal))
                {
                    Buffer.Append('"');
                    Buffer.Append((DateTime)formattable, false, true, true);
                    Buffer.Append('"');
                }
                else if (string.Equals(format, "yyyy-MM-dd", StringComparison.Ordinal))
                {
                    Buffer.Append('"');
                    Buffer.Append((DateTime)formattable, false, false, false);
                    Buffer.Append('"');
                }
                else if (string.Equals(format, "fff", StringComparison.Ordinal))
                {
                    Buffer.Append('"');
                    Buffer.Append((DateTime)formattable, false, false, true);
                    Buffer.Append('"');
                }
                else
                {
                    AppendString(formattable.ToString(format, provider));
                }
            }
            else if (formattable is Guid && (format == null || format.Length == 1))
            {
                Buffer.Append('"');
                Buffer.Append((Guid)formattable, format[0]);
                Buffer.Append('"');
            }
            else
            {
                AppendString(formattable.ToString(format, provider));
            }
        }
        /// <summary> 将 String 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Char 对象</param>
        protected virtual void AppendString(String value)
        {
            Buffer.Append('"');
            unsafe
            {
                var length = value.Length;
                fixed (char* fp = value)
                {
                    char* p = fp;
                    char* end = fp + length;
                    char* flag = fp;
                    while (p < end)
                    {
                        char c = *p;
                        switch (c)
                        {
                            case '\\':
                            case '"':
                                Buffer.Append(flag, 0, (int)(p - flag));
                                Buffer.Append('\\');
                                flag = p;
                                break;
                            case '\n':
                                Buffer.Append(flag, 0, (int)(p - flag));
                                Buffer.Append('\\');
                                Buffer.Append('n');
                                flag = p + 1;
                                break;
                            case '\r':
                                Buffer.Append(flag, 0, (int)(p - flag));
                                Buffer.Append('\\');
                                Buffer.Append('r');
                                flag = p + 1;
                                break;
                            case '\t':
                                Buffer.Append(flag, 0, (int)(p - flag));
                                Buffer.Append('\\');
                                Buffer.Append('t');
                                flag = p + 1;
                                break;
                            case '\f':
                                Buffer.Append(flag, 0, (int)(p - flag));
                                Buffer.Append('\\');
                                Buffer.Append('f');
                                flag = p + 1;
                                break;
                            case '\0':
                                Buffer.Append(flag, 0, (int)(p - flag));
                                Buffer.Append('\\');
                                Buffer.Append('0');
                                flag = p + 1;
                                break;
                            case '\a':
                                Buffer.Append(flag, 0, (int)(p - flag));
                                Buffer.Append('\\');
                                Buffer.Append('a');
                                flag = p + 1;
                                break;
                            case '\b':
                                Buffer.Append(flag, 0, (int)(p - flag));
                                Buffer.Append('\\');
                                Buffer.Append('b');
                                flag = p + 1;
                                break;
                            case '\v':
                                Buffer.Append(flag, 0, (int)(p - flag));
                                Buffer.Append('\\');
                                Buffer.Append('v');
                                flag = p + 1;
                                break;
                            default:
                                break;
                        }
                        p++;
                    }
                    if (flag == fp)
                    {
                        Buffer.Append(fp, 0, length);
                    }
                    else if (p > flag)
                    {
                        Buffer.Append(flag, 0, (int)(p - flag));
                    }
                }
            }

            Buffer.Append('"');
        }
        /// <summary> 将 DateTime 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">DateTime 对象</param>
        protected virtual void AppendDateTime(DateTime value)
        {
            Buffer.Append('"');
            if (FormatTime && IgnoreEmptyTime)
            {
                Buffer.Append(value, FormatDate, value.Millisecond > 0 || value.Hour > 0 || value.Minute > 0 || value.Second > 0, false);
            }
            else
            {
                Buffer.Append(value, FormatDate, FormatTime, false);
            }
            Buffer.Append('"');
        }
        /// <summary> 将 Guid 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">Guid 对象</param>
        protected virtual void AppendGuid(Guid value)
        {
            Buffer.Append('"').Append(value).Append('"');
        }
        /// <summary> 将 枚举 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="value">枚举 对象</param>
        protected virtual void AppendEnum(Enum value)
        {
            if (EnumToNumber)
            {
                AppendNumber(value);
            }
            else
            {
                Buffer.Append('"').Append(value.ToString()).Append('"');
            }
        }
        /// <summary> 将 数字 类型对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="number">数字对象</param>
        protected virtual void AppendNumber(IConvertible number)
        {
            switch (number.GetTypeCode())
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    if (QuotWrapNumber)
                    {
                        Buffer.Append('"');
                        Buffer.Append(number.ToString(CultureInfo.InvariantCulture));
                        Buffer.Append('"');
                    }
                    else
                    {
                        Buffer.Append(number.ToString(CultureInfo.InvariantCulture));
                    }
                    break;
                case TypeCode.Int16:
                    if (QuotWrapNumber)
                    {
                        Buffer.Append('"');
                        Buffer.Append(number.ToInt16(CultureInfo.InvariantCulture));
                        Buffer.Append('"');
                    }
                    else
                    {
                        Buffer.Append(number.ToInt16(CultureInfo.InvariantCulture));
                    }
                    break;
                case TypeCode.Int32:
                case TypeCode.Int64:
                    if (QuotWrapNumber)
                    {
                        Buffer.Append('"');
                        Buffer.Append(number.ToInt64(CultureInfo.InvariantCulture));
                        Buffer.Append('"');
                    }
                    else
                    {
                        Buffer.Append(number.ToInt64(CultureInfo.InvariantCulture));
                    }
                    break;
                case TypeCode.SByte:
                    if (QuotWrapNumber)
                    {
                        Buffer.Append('"');
                        Buffer.Append(number.ToSByte(CultureInfo.InvariantCulture));
                        Buffer.Append('"');
                    }
                    else
                    {
                        Buffer.Append(number.ToSByte(CultureInfo.InvariantCulture));
                    }
                    break;
                case TypeCode.Byte:
                    if (QuotWrapNumber)
                    {
                        Buffer.Append('"');
                        Buffer.Append(number.ToByte(CultureInfo.InvariantCulture));
                        Buffer.Append('"');
                    }
                    else
                    {
                        Buffer.Append(number.ToByte(CultureInfo.InvariantCulture));
                    }
                    break;
                case TypeCode.UInt16:
                    if (QuotWrapNumber)
                    {
                        Buffer.Append('"');
                        Buffer.Append(number.ToUInt16(CultureInfo.InvariantCulture));
                        Buffer.Append('"');
                    }
                    else
                    {
                        Buffer.Append(number.ToUInt16(CultureInfo.InvariantCulture));
                    }
                    break;
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    if (QuotWrapNumber)
                    {
                        Buffer.Append('"');
                        Buffer.Append(number.ToUInt64(CultureInfo.InvariantCulture));
                        Buffer.Append('"');
                    }
                    else
                    {
                        Buffer.Append(number.ToUInt64(CultureInfo.InvariantCulture));
                    }
                    break;
                default:
                    break;
            }
        }
        /// <summary> 将 数组 对象转换Json中的数组字符串写入Buffer
        /// </summary>
        /// <param name="array">数组对象</param>
        protected virtual void AppendArray(IEnumerable array)
        {
            Buffer.Append('[');
            var ee = array.GetEnumerator();
            if (ee.MoveNext())
            {
                AppendObject(ee.Current);
                while (ee.MoveNext())
                {
                    Buffer.Append(',');
                    AppendObject(ee.Current);
                }
            }
            Buffer.Append(']');
        }
        /// <summary> 将 键值对 对象转换Json中的键值字符串写入Buffer
        /// </summary>
        /// <param name="dict">键值对 对象</param>
        protected virtual void AppendJson(IDictionary dict)
        {
            AppendJson(dict.Keys, dict.Values);
        }
        /// <summary> 将 键枚举 和 值枚举 转换Json中的键值字符串写入Buffer
        /// </summary>
        /// <param name="keys">键枚举</param>
        /// <param name="values">值枚举</param>
        protected virtual void AppendJson(IEnumerable keys, IEnumerable values)
        {
            Buffer.Append('{');
            var ke = keys.GetEnumerator();
            var ve = values.GetEnumerator();
            if (ke.MoveNext() && ve.MoveNext())
            {
                AppendKey(ke.Current + "", true);
                AppendObject(ve.Current);
                while (ke.MoveNext() && ve.MoveNext())
                {
                    Buffer.Append(',');
                    AppendKey(ke.Current + "", true);
                    AppendObject(ve.Current);
                }
            }
            Buffer.Append('}');
        }
        /// <summary> 将 对象枚举 和 值转换委托 转换Json中的数组字符串写入Buffer
        /// </summary>
        /// <param name="enumer">对象枚举</param>
        /// <param name="getVal">值转换委托</param>
        protected virtual void AppendArray(IEnumerable enumer, Converter<object, object> getVal)
        {
            Buffer.Append('[');
            var ee = enumer.GetEnumerator();
            if (ee.MoveNext())
            {
                AppendObject(getVal(ee.Current));
                while (ee.MoveNext())
                {
                    Buffer.Append(',');
                    AppendObject(getVal(ee.Current));
                }
            }
            Buffer.Append(']');
        }
        /// <summary> 将 对象枚举 和 键/值转换委托 转换Json中的键值对象字符串写入Buffer
        /// </summary>
        /// <param name="enumer">对象枚举</param>
        /// <param name="getKey">键转换委托</param>
        /// <param name="getVal">值转换委托</param>
        /// <param name="escapekey">是否需要对Key进行转义</param>
        protected virtual void AppendJson(IEnumerable enumer, Converter<object, string> getKey, Converter<object, object> getVal, bool escapekey)
        {
            Buffer.Append('{');

            var ee = enumer.GetEnumerator();
            if (ee.MoveNext())
            {
                AppendKey(getKey(ee.Current), escapekey);
                AppendObject(getVal(ee.Current));
                while (ee.MoveNext())
                {
                    Buffer.Append(':');
                    AppendKey(getKey(ee.Current), true);
                    AppendObject(getVal(ee.Current));
                }
            }
            Buffer.Append('}');
        }
        /// <summary> 将 DataSet 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="dataset">DataSet 对象</param>
        protected virtual void AppendDataSet(DataSet dataset)
        {
            Buffer.Append('{');
            var ee = dataset.Tables.GetEnumerator();
            if (ee.MoveNext())
            {
                DataTable table = (DataTable)ee.Current;
                AppendKey(table.TableName, true);
                AppendDataTable(table);
                while (ee.MoveNext())
                {
                    Buffer.Append(':');
                    table = (DataTable)ee.Current;
                    AppendKey(table.TableName, true);
                    AppendDataTable(table);
                }
            }
            Buffer.Append('}');
        }
        /// <summary> 将 DataTable 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="table">DataTable 对象</param>
        protected virtual void AppendDataTable(DataTable table)
        {
            Buffer.Append('[');

            var ee = table.Rows.GetEnumerator();
            if (ee.MoveNext())
            {
                var names = new string[table.Columns.Count];
                var columns = table.Columns;
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = EscapeString(columns[i].ColumnName);
                }
                AppendJson(names, ((DataRow)ee.Current).ItemArray);
                while (ee.MoveNext())
                {
                    Buffer.Append(',');
                    AppendJson(names, ((DataRow)ee.Current).ItemArray);
                }
            }

            Buffer.Append(']');
        }
        /// <summary> 将 DataView 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="tableView">DataView 对象</param>
        protected virtual void AppendDataView(DataView tableView)
        {
            Buffer.Append('[');
            var table = tableView.Table ?? tableView.ToTable();
            var ee = tableView.GetEnumerator();
            if (ee.MoveNext())
            {
                var names = new string[table.Columns.Count];
                var columns = table.Columns;
                for (int i = 0; i < names.Length; i++)
                {
                    names[i] = EscapeString(columns[i].ColumnName);
                }
                AppendJson(names, ((DataRowView)ee.Current).Row.ItemArray);
                while (ee.MoveNext())
                {
                    Buffer.Append(',');
                    AppendJson(names, ((DataRowView)ee.Current).Row.ItemArray);
                }
            }

            Buffer.Append(']');
        }
        /// <summary> 将 IDataReader 对象转换Json字符串写入Buffer
        /// </summary>
        /// <param name="reader">IDataReader 对象</param>
        protected virtual void AppendDataSet(IDataReader reader)
        {
            Buffer.Append("{\"columns\":");
            AppendArray(GetDataReaderNames(reader));
            Buffer.Append(",\"rows\":");
            AppendArray(GetDataReaderValues(reader));
            Buffer.Append('}');
        }
    }
}
