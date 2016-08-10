﻿using System;
using System.Collections;
using System.IO;

namespace blqw.Serializable
{
    public class JsonWriterArgs
    {
        //循环引用对象缓存区
        private readonly IList _loopObject;
        private string _dateTimeFormatString;

        public JsonWriterArgs(TextWriter writer, JsonBuilderSettings settings)
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
            SerializableType = (settings & JsonBuilderSettings.SerializableType) != 0;
            FormatAllMember = (settings & JsonBuilderSettings.FormatAllMember) != 0;
            FilterSpecialCharacter = (settings & JsonBuilderSettings.FilterSpecialCharacter) != 0;
            CastUnicode = (settings & JsonBuilderSettings.CastUnicode) != 0;
            GuidToUpper = (settings & JsonBuilderSettings.GuidToUpper) != 0;
            GuidFormatString = (settings & JsonBuilderSettings.GuidHasHyphens) != 0 ? "D" : "N";
            Writer = writer;
            Depth = 0;
            if (CheckLoopRef)
            {
                _loopObject = new ArrayList(32);
            }
        }

        /// <summary>
        /// 将布尔值转为数字值 true = 1 ,false = 0
        /// </summary>
        public bool BooleanToNumber { get; }

        /// <summary>
        /// 是否将大于255的字符转为Unicode编码
        /// </summary>
        public bool CastUnicode { get; }

        /// <summary>
        /// 检查循环引用,发现循环应用时输出 undefined
        /// </summary>
        public bool CheckLoopRef { get; }

        /// <summary>
        /// 将枚举转为对应的数字值
        /// </summary>
        public bool EnumToNumber { get; }

        /// <summary>
        /// 过滤特殊字符
        /// </summary>
        public bool FilterSpecialCharacter { get; }

        /// <summary>
        /// 输出所有属性/字段
        /// </summary>
        public bool FormatAllMember { get; }

        /// <summary>
        /// 格式化 DateTime 对象中的日期
        /// </summary>
        public bool FormatDate { get; }

        /// <summary>
        /// 格式化 DateTime 对象中的时间
        /// </summary>
        public bool FormatTime { get; }

        /// <summary>
        /// 格式化 DateTime 对象中的时间时忽略(00:00:00.000000) ,存在FormatTime时才生效
        /// </summary>
        public bool IgnoreEmptyTime { get; }

        /// <summary>
        /// 忽略值是null的属性
        /// </summary>
        public bool IgnoreNullMember { get; }

        /// <summary>
        /// 使用双引号包装布尔的值
        /// </summary>
        public bool QuotWrapBoolean { get; }

        /// <summary>
        /// 使用双引号包装数字的值
        /// </summary>
        public bool QuotWrapNumber { get; }

        /// <summary>
        /// 同时序列化字段
        /// </summary>
        public bool SerializableField { get; }

        /// <summary>
        /// 输出类型信息
        /// </summary>
        public bool SerializableType { get; }

        /// <summary>
        /// 时间格式字符串
        /// </summary>
        public string TimeFormatString { get; set; }
        /// <summary>
        /// 日期格式字符串
        /// </summary>
        public string DateFormatString { get; set; }

        /// <summary>
        /// 日期和时间格式字符串
        /// </summary>
        public string DateTimeFormatString
        {
            get
            {
                if (_dateTimeFormatString == null && TimeFormatString != null && DateFormatString != null)
                {
                    return $"{DateFormatString} {TimeFormatString}";
                }
                return _dateTimeFormatString;
            }
            set { _dateTimeFormatString = value; }
        }

        /// <summary>
        /// Guid格式化字符串
        /// </summary>
        public string GuidFormatString { get; }

        /// <summary>
        /// Guid需要转为大写
        /// </summary>
        public bool GuidToUpper { get; }

        public TextWriter Writer { get; }

        public int Depth { get; private set; }

        public void WriteCheckLoop(object value, IJsonWriter writer)
        {
            if (value == null || value is DBNull)
            {
                JsonWriterContainer.NullWriter.Write(null, this);
                return;
            }
            if (writer == null)
            {
                writer = JsonWriterContainer.Get(value.GetType());
            }
            if (CheckLoopRef)
            {
                if (value is ValueType)
                {
                    writer.Write(value, this);
                }
                else if (_loopObject.Contains(value) == false)
                {
                    var index = _loopObject.Add(value);
                    writer.Write(value, this);
                    _loopObject.RemoveAt(index);
                }
                else
                {
                    Writer?.Write("undefined");
                }
            }
            else if (Depth > 64)
            {
                throw new NotSupportedException("对象过于复杂或存在循环引用");
            }
            else
            {
                Depth++;
                writer.Write(value, this);
                Depth--;
            }
        }
    }
}