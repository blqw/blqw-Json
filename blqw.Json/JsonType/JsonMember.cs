﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace blqw
{
    /// <summary> 可以被序列化成Json成员的对象
    /// </summary>
    public sealed class JsonMember
    {
        /// <summary> 创建Json成员对象,如果成员被指定为忽略反序列化则返回null
        /// </summary>
        /// <param name="member">对象成员信息</param>
        public JsonMember Create(MemberInfo member)
        {
            var jsonIgnore = (JsonIgnoreAttribute)Attribute.GetCustomAttribute(member, typeof(JsonIgnoreAttribute));
            if (jsonIgnore != null)
            {
                if (jsonIgnore.NonDeserialize)
                {
                    return null;
                }
                return new JsonMember(member) { NonSerialized = true };
            }
            return new JsonMember(member);
        }

        /// <summary> 构造函数
        /// </summary>
        private JsonMember(MemberInfo member)
        {
            if (member is PropertyInfo)
            {
                Member = new ObjectProperty((PropertyInfo)member);
            }
            else
            {
                Member = new ObjectProperty((FieldInfo)member);
            }
            var name = Member.Attributes.First<JsonNameAttribute>();
            JsonName = name != null ? name.Name : member.Name;
            var format = Member.Attributes.First<JsonFormatAttribute>();
            if (format != null && ExtendMethod.IsChild(typeof(IFormattable), member.ReflectedType))
            {
                MustFormat = true;
                FormatString = format.Format;
                Provider = format.Provider;
            }
        }
        /// <summary> Literacy组件的成员访问对象
        /// </summary>
        public ObjectProperty Member { get; private set; }
        /// <summary> 序列化和反序列化时的参考Json属性名称
        /// </summary>
        public string JsonName { get; private set; }
        /// <summary> 指示序列化的时候是否需要按指定格式格式化
        /// </summary>
        public bool MustFormat { get; private set; }
        /// <summary> 序列化时使用的格式化参数
        /// </summary>
        public string FormatString { get; private set; }
        /// <summary> 序列化时使用的格式化机制
        /// </summary>
        public IFormatProvider Provider { get; private set; }
        /// <summary> 指示当前成员是否忽略序列化操作
        /// </summary>
        public bool NonSerialized { get; private set; }
    }
}