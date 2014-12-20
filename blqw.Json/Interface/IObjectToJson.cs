﻿using System;
using System.Collections.Generic;
using System.Text;

namespace blqw
{
    /// <summary> 允许对象控制自己的序列化行为
    /// </summary>
    public interface IObjectToJson
    {
        /// <summary> 获取当前对象用于序列化为Json字符串的新对象
        /// </summary>
        object GetJsonObject();
    }
}
