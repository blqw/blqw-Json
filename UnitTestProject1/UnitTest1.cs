﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Collections;

namespace UnitTestProject1
{
    #region 测试用例
    public class User
    {
        public static User TestUser()
        {//这里我尽量构造一个看上去很复杂的对象,并且这个对象几乎涵盖了所有常用的类型
            User user = new User();
            user.UID = Guid.NewGuid();
            user.Birthday = new DateTime(1986, 10, 29, 18, 00, 00);
            user.IsDeleted = false;
            user.Name = "blqw";
            user.Sex = UserSex.Male;
            user.LoginHistory = new List<DateTime>();
            user.LoginHistory.Add(DateTime.Today.Add(new TimeSpan(8, 00, 00)));
            user.LoginHistory.Add(DateTime.Today.Add(new TimeSpan(10, 10, 10)));
            user.LoginHistory.Add(DateTime.Today.Add(new TimeSpan(12, 33, 56)));
            user.LoginHistory.Add(DateTime.Today.Add(new TimeSpan(17, 25, 18)));
            user.LoginHistory.Add(DateTime.Today.Add(new TimeSpan(23, 06, 59)));
            user.Info = new UserInfo();
            user.Info.Address = "广东省\n广州市";
            user.Info.ZipCode = 510000;
            user.Info.Phone = new Dictionary<string, string>();
            user.Info.Phone.Add("手机", "18688888888");
            user.Info.Phone.Add("电话", "82580000");
            user.Info.Phone.Add("短号", "10086");
            user.Info.Phone.Add("QQ", "21979018");
            user.Double = Double.NegativeInfinity;
            // user.Self = user; //这里是用来测试循环引用的解析情况
            return user;
        }

        public User Self { get; set; }
        //User self
        /// <summary> 唯一ID
        /// </summary>
        public Guid UID { get; set; }
        /// <summary> 用户名称
        /// </summary>
        public string Name { get; set; }
        /// <summary> 生日
        /// </summary>
        public DateTime Birthday { get; set; }
        /// <summary> 性别
        /// </summary>
        public UserSex Sex { get; set; }
        /// <summary> 是否删除标记
        /// </summary>
        public bool IsDeleted { get; set; }
        /// <summary> 最近登录记录
        /// </summary>
        public List<DateTime> LoginHistory { get; set; }
        /// <summary> 联系信息
        /// </summary>
        public UserInfo Info { get; set; }
        public Double Double { get; set; }
    }
    /// <summary> 用户性别
    /// </summary>
    public enum UserSex
    {
        /// <summary> 男
        /// </summary>
        Male,
        /// <summary> 女
        /// </summary>
        Female
    }
    /// <summary> 用户信息
    /// </summary>
    public class UserInfo
    {
        /// <summary> 地址
        /// </summary>
        public string Address { get; set; }
        /// <summary> 联系方式
        /// </summary>
        public Dictionary<string, string> Phone { get; set; }
        /// <summary> 邮政编码
        /// </summary>
        public int ZipCode { get; set; }
    } 
    #endregion

    public class Object1
    {
        public bool Result { get; set; }
        public string Message { get; set; }
        public Object1Item[] data { get; set; }
        public int total { get; set; }
    }

    public class Object1Item
    {
        public long vptNum { get; set; }
        public int state { get; set; }
        public DateTime tdate { get; set; }
        public string ntype { get; set; }
        public int abnormal { get; set; }
    }
    public class Object2
    {
        public uint ORGCODE { get; set; }
        public string NAME { get; set; }
        public uint VALUE { get; set; }
        public string VTYPE { get; set; }
    }
    

    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var user = User.TestUser();
            var userJson = blqw.Json.ToJsonString(user);
            Test1<Object1>(File.ReadAllText("json1.txt"));
            Test1<Object2[]>(File.ReadAllText("json2.txt"));
            Test1<List<Object2>>(File.ReadAllText("json2.txt"));
            Test1<User>(userJson);
            Test2<Object1>(File.ReadAllText("json1.txt"));
            Test2<Object2[]>(File.ReadAllText("json2.txt"));
            Test2<List<Object2>>(File.ReadAllText("json2.txt"));
            Test2<User>(userJson);
        }

        public void Test1<T>(string jsonString)
        {
            var obj1 = blqw.Json.ToObject<T>(jsonString);
            var obj2 = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(jsonString);
            AssertEquals(obj1, obj2);
        }

        public void Test2<T>(string jsonString)
        {
            var obj1 = blqw.Json.ToObject<T>(jsonString);
            var jsonString1 = blqw.Json.ToJsonString(obj1);
            var obj2 = blqw.Json.ToObject<T>(jsonString1);
            AssertEquals(obj1, obj2);
            var jsonString2 = blqw.Json.ToJsonString(obj2);
            Assert.AreEqual(jsonString1, jsonString2);
        }


        #region MyRegion
        /// <summary> 检查一个类型是否为可空值类型
        /// </summary>
        public static bool IsNullable(Type t)
        {
            return (t.IsValueType && t.IsGenericType && !t.IsGenericTypeDefinition && object.ReferenceEquals(t.GetGenericTypeDefinition(), typeof(Nullable<>)));
        }

        public static bool IsPrimitive(Type type)
        {
            if (Type.GetTypeCode(type) == TypeCode.Object)
            {
                if (type == typeof(Guid))
                {
                    return true;
                }
                else if (IsNullable(type))
                {
                    return IsPrimitive(type.GetGenericArguments()[0]);
                }
            }
            else
            {
                if (type.IsPrimitive)
                {
                    return true;
                }
                else if (type == typeof(DateTime))
                {
                    return true;
                }
                else if (type == typeof(string))
                {
                    return true;
                }
            }
            return false;
        }

        public static void AssertEquals<T>(T t1, T t2, string title = null)
        {
            if (t1 == null || (t1.GetType().IsValueType && t1.GetHashCode() == 0))
            {
                return;
            }
            if (t1 == null || t2 == null)
            {
                if (t1 != null || t2 != null)
                {
                    throw new Exception(string.Format("{0} 值不同 ,值1 {1}, 值2 {2}", title, (object)t1 ?? "NULL", (object)t2 ?? "NULL"));
                }
                return;
            }

            if (IsPrimitive(t1.GetType()))
            {
                if (!object.Equals(t1, t2))
                {
                    throw new Exception(string.Format("{0} 值不同 ,值1 {1}, 值2 {2}", title, (object)t1 ?? "NULL", (object)t2 ?? "NULL"));
                }
            }
            else if (t1 is IEnumerable)
            {
                var e1 = ((IEnumerable)t1).GetEnumerator();
                var e2 = ((IEnumerable)t2).GetEnumerator();

                while (e1.MoveNext())
                {
                    if (e2.MoveNext() == false)
                    {
                        throw new Exception(string.Format("{0} 个数不同1", title));
                    }
                    AssertEquals(e1.Current, e2.Current, title);
                }
                if (e2.MoveNext())
                {
                    throw new Exception(string.Format("{0} 个数不同2", title));
                }
            }
            else
            {
                var lit = blqw.Literacy.Cache(t1.GetType(), false);

                foreach (var p in lit.Property)
                {
                    if (p.CanRead)
                    {
                        var val1 = p.GetValue(t1);
                        var val2 = p.GetValue(t2);
                        AssertEquals(val1, val2, "属性" + p.Name);
                    }
                }
            }
        } 
        #endregion
    }


}