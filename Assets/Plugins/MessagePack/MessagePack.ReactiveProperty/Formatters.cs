// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using MessagePack.Formatters;
using UniRx;

#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.ReactivePropertyExtension
{
    public class ReactivePropertyFormatter<T> : IMessagePackFormatter<ReactiveProperty<T>>
    {
        public void Serialize(ref MessagePackWriter writer, ReactiveProperty<T> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                options.Resolver.GetFormatterWithVerify<T>().Serialize(ref writer, value.Value, options);
            }
        }

        public ReactiveProperty<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }
            else
            {
                T v = options.Resolver.GetFormatterWithVerify<T>().Deserialize(ref reader, options);

                return new ReactiveProperty<T>(v);
            }
        }
    }

    public class ReactiveCollectionFormatter<T> : CollectionFormatterBase<T, ReactiveCollection<T>>
    {
        protected override void Add(ReactiveCollection<T> collection, int index, T value, MessagePackSerializerOptions options)
        {
            collection.Add(value);
        }

        protected override ReactiveCollection<T> Create(int count, MessagePackSerializerOptions options)
        {
            return new ReactiveCollection<T>();
        }
    }

    public class UnitFormatter : IMessagePackFormatter<Unit>
    {
        public static readonly UnitFormatter Instance = new UnitFormatter();

        private UnitFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Unit value, MessagePackSerializerOptions options)
        {
            writer.WriteNil();
        }

        public Unit Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return Unit.Default;
            }
            else
            {
                throw new InvalidOperationException("Invalid Data type. Code: " + MessagePackCode.ToFormatName(reader.NextCode));
            }
        }
    }

    public class NullableUnitFormatter : IMessagePackFormatter<Unit?>
    {
        public static readonly NullableUnitFormatter Instance = new NullableUnitFormatter();

        private NullableUnitFormatter()
        {
        }

        public void Serialize(ref MessagePackWriter writer, Unit? value, MessagePackSerializerOptions options)
        {
            writer.WriteNil();
        }

        public Unit? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return Unit.Default;
            }
            else
            {
                throw new InvalidOperationException("Invalid Data type. Code: " + MessagePackCode.ToFormatName(reader.NextCode));
            }
        }
    }
}
