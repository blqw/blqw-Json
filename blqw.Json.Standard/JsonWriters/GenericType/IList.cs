﻿using System;
using System.Collections.Generic;


namespace blqw.JsonServices.JsonWriters
{
    internal class IListTWriter : IGenericJsonWriter
    {
        public Type Type => typeof(IList<>);

        public IJsonWriter MakeGenericType(Type genericType)
        {
            foreach (var item in genericType.GetInterfaces())
            {
                if (item.IsGenericType
                    && item.IsGenericTypeDefinition == false
                    && item.GetGenericTypeDefinition() == Type)
                {
                    var t = typeof(InnerWriter<>).MakeGenericType(item.GetGenericArguments());
                    return (IJsonWriter)Activator.CreateInstance(t);
                }
            }
            if (genericType.IsInterface)
            {
                if (genericType.IsGenericType
                    && genericType.IsGenericTypeDefinition == false
                    && genericType.GetGenericTypeDefinition() == Type)
                {
                    var t = typeof(InnerWriter<>).MakeGenericType(genericType.GetGenericArguments());
                    return (IJsonWriter)Activator.CreateInstance(t);
                }
            }
            throw new NotImplementedException();
        }

        public void Write(object obj, JsonWriterSettings args)
        {
            throw new NotImplementedException();
        }

        private class InnerWriter<T> : IJsonWriter
        {
            public Type Type { get; } = typeof(IList<T>);

            public void Write(object obj, JsonWriterSettings args)
            {
                if (obj == null || obj is DBNull)
                {
                    args.WriteNull();
                    return;
                }
                var writer = TypeService.IsSealed<T>() ? args.Selector.Get<T>() : null;
                var list = (IList<T>)obj;
                if (list.Count == 0)
                {
                    args.BeginArray();
                    args.EndArray();
                    return;
                }
                args.BeginArray();
                args.WriteCheckLoop(list[0], writer);

                for (int i = 1, length = list.Count; i < length; i++)
                {
                    args.Common();
                    args.WriteCheckLoop(list[i], writer);
                }

                args.EndArray();
            }

        }
    }
}