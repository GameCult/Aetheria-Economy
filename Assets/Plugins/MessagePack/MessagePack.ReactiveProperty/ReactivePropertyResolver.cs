﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using MessagePack.Formatters;
using UniRx;

namespace MessagePack.ReactivePropertyExtension
{
    public class ReactivePropertyResolver : IFormatterResolver
    {
        public static readonly ReactivePropertyResolver Instance = new ReactivePropertyResolver();

        private ReactivePropertyResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>)ReactivePropertyResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }

    internal static class ReactivePropertyResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, Type> FormatterMap = new Dictionary<Type, Type>()
        {
              { typeof(ReactiveProperty<>), typeof(ReactivePropertyFormatter<>) },
              { typeof(ReactiveCollection<>), typeof(ReactiveCollectionFormatter<>) },
        };

        internal static object GetFormatter(Type t)
        {
            if (t == typeof(Unit))
            {
                return UnitFormatter.Instance;
            }
            else if (t == typeof(Unit?))
            {
                return NullableUnitFormatter.Instance;
            }

            TypeInfo ti = t.GetTypeInfo();

            if (ti.IsGenericType)
            {
                Type genericType = ti.GetGenericTypeDefinition();
                Type formatterType;
                if (FormatterMap.TryGetValue(genericType, out formatterType))
                {
                    return CreateInstance(formatterType, ti.GenericTypeArguments);
                }
            }

            return null;
        }

        private static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
        }
    }
}
