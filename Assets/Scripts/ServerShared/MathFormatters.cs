using System;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public class Float2Formatter : MessagePack.Formatters.IMessagePackFormatter<float2>
{
    public void Serialize(ref MessagePackWriter writer, float2 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(2);
        writer.Write(value.x);
        writer.Write(value.y);
    }

    public float2 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.IsNil)
        {
            throw new InvalidOperationException("typecode is null, struct not supported");
        }

        var length = reader.ReadArrayHeader();
        var x = default(float);
        var y = default(float);
        for (int i = 0; i < length; i++)
        {
            var key = i;
            switch (key)
            {
                case 0:
                    x = reader.ReadSingle();
                    break;
                case 1:
                    y = reader.ReadSingle();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        var result = float2(x, y);
        return result;
    }
}

public class Float3Formatter : MessagePack.Formatters.IMessagePackFormatter<float3>
{
    public void Serialize(ref MessagePackWriter writer, float3 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(3);
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
    }

    public float3 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.IsNil)
        {
            throw new InvalidOperationException("typecode is null, struct not supported");
        }

        var length = reader.ReadArrayHeader();
        var x = default(float);
        var y = default(float);
        var z = default(float);
        for (int i = 0; i < length; i++)
        {
            var key = i;
            switch (key)
            {
                case 0:
                    x = reader.ReadSingle();
                    break;
                case 1:
                    y = reader.ReadSingle();
                    break;
                case 2:
                    z = reader.ReadSingle();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        var result = float3(x, y, z);
        return result;
    }
}

public class Float4Formatter : MessagePack.Formatters.IMessagePackFormatter<float4>
{
    public void Serialize(ref MessagePackWriter writer, float4 value, MessagePackSerializerOptions options)
    {
        writer.WriteArrayHeader(4);
        writer.Write(value.x);
        writer.Write(value.y);
        writer.Write(value.z);
        writer.Write(value.w);
    }

    public float4 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.IsNil)
        {
            throw new InvalidOperationException("typecode is null, struct not supported");
        }

        var length = reader.ReadArrayHeader();
        var x = default(float);
        var y = default(float);
        var z = default(float);
        var w = default(float);
        for (int i = 0; i < length; i++)
        {
            var key = i;
            switch (key)
            {
                case 0:
                    x = reader.ReadSingle();
                    break;
                case 1:
                    y = reader.ReadSingle();
                    break;
                case 2:
                    z = reader.ReadSingle();
                    break;
                case 3:
                    w = reader.ReadSingle();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        var result = float4(x, y, z, w);
        return result;
    }
}