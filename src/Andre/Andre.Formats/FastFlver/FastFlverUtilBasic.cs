#nullable enable
using DotNext.IO.MemoryMappedFiles;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static SoulsFormats.FLVER;

namespace Andre.Formats;

public abstract partial class FastFlver<IndexBufferHandle, BoundingBox, GPUBufferHandle, Material, SpecializationConstant> : IDisposable where BoundingBox : new() where Material : new()
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillVertexColor(byte* dest, ref FLVER.Vertex v)
    {
        dest[0] = (byte)(v.Colors[0].R * 255);
        dest[1] = (byte)(v.Colors[0].G * 255);
        dest[2] = (byte)(v.Colors[0].B * 255);
        dest[3] = (byte)(v.Colors[0].A * 255);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillVertexColorDefault(byte* dest)
    {
        dest[0] = 255;
        dest[1] = 255;
        dest[2] = 255;
        dest[3] = 255;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillVertexColor(byte* dest, BinaryReaderEx br, FLVER.LayoutType type)
    {
        if (type == LayoutType.Float4)
        {
            dest[0] = (byte)(br.ReadSingle() * 255);
            dest[1] = (byte)(br.ReadSingle() * 255);
            dest[2] = (byte)(br.ReadSingle() * 255);
            dest[3] = (byte)(br.ReadSingle() * 255);
        }
        else if (type == LayoutType.Byte4A)
        {
            // Definitely RGBA in DeS
            dest[0] = br.ReadByte();
            dest[1] = br.ReadByte();
            dest[2] = br.ReadByte();
            dest[3] = br.ReadByte();
        }
        else if (type == LayoutType.Byte4C)
        {
            // Definitely RGBA in DS1
            dest[0] = br.ReadByte();
            dest[1] = br.ReadByte();
            dest[2] = br.ReadByte();
            dest[3] = br.ReadByte();
        }
        else
            throw new NotImplementedException($"Read not implemented for {type} color.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillNormalSNorm8(sbyte* dest, ref FLVER.Vertex v)
    {
        Vector3 n = Vector3.Normalize(new Vector3(v.Normal.X, v.Normal.Y, v.Normal.Z));
        dest[0] = (sbyte)(n.X * 127.0f);
        dest[1] = (sbyte)(n.Y * 127.0f);
        dest[2] = (sbyte)(n.Z * 127.0f);
        dest[3] = (sbyte)v.NormalW;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillNormalSNorm8(sbyte* dest, BinaryReaderEx br, FLVER.LayoutType type, Vector3* n)
    {
        var nw = 0;
        if (type == FLVER.LayoutType.Float3)
        {
            *n = br.ReadVector3();
        }
        else if (type == FLVER.LayoutType.Float4)
        {
            *n = br.ReadVector3();
            var w = br.ReadSingle();
            nw = (int)w;
            if (w != nw)
            {
                throw new InvalidDataException($"Float4 Normal W was not a whole number: {w}");
            }
        }
        else if (type == FLVER.LayoutType.Byte4A)
        {
            *n = FLVER.Vertex.ReadByteNormXYZ(br);
            nw = br.ReadByte();
        }
        else if (type == FLVER.LayoutType.Byte4B)
        {
            *n = FLVER.Vertex.ReadByteNormXYZ(br);
            nw = br.ReadByte();
        }
        else if (type == FLVER.LayoutType.Short2toFloat2)
        {
            nw = br.ReadByte();
            *n = FLVER.Vertex.ReadSByteNormZYX(br);
        }
        else if (type == FLVER.LayoutType.Byte4C)
        {
            *n = FLVER.Vertex.ReadByteNormXYZ(br);
            nw = br.ReadByte();
        }
        else if (type == FLVER.LayoutType.Short4toFloat4A)
        {
            *n = FLVER.Vertex.ReadShortNormXYZ(br);
            nw = br.ReadInt16();
        }
        else if (type == FLVER.LayoutType.Short4toFloat4B)
        {
            //Normal = ReadUShortNormXYZ(br);
            *n = FLVER.Vertex.ReadFloat16NormXYZ(br);
            nw = br.ReadInt16();
        }
        else if (type == FLVER.LayoutType.Byte4E)
        {
            *n = FLVER.Vertex.ReadByteNormXYZ(br);
            nw = br.ReadByte();
        }
        else if (type == FLVER.LayoutType.ShortBoneIndices)
        {
            *n = FLVER.Vertex.ReadShortNormXYZ(br);
            nw = br.ReadInt16();
        }
        else
        {
            throw new NotImplementedException($"Read not implemented for {type} normal.");
        }

        dest[0] = (sbyte)(n->X * 127.0f);
        dest[1] = (sbyte)(n->Y * 127.0f);
        dest[2] = (sbyte)(n->Z * 127.0f);
        dest[3] = (sbyte)nw;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillUVShort(short* dest, ref FLVER.Vertex v, byte index)
    {
        Vector3 uv = v.GetUV(index);
        dest[0] = (short)(uv.X * 2048.0f);
        dest[1] = (short)(uv.Y * 2048.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillUVShort(short* dest, BinaryReaderEx br, FLVER.LayoutType type, float uvFactor,
        bool allowv2, out bool hasv2)
    {
        Vector3 v;
        Vector3 v2;
        hasv2 = false;
        if (type == FLVER.LayoutType.Float2)
        {
            v = new Vector3(br.ReadVector2(), 0);
        }
        else if (type == FLVER.LayoutType.Float3)
        {
            v = br.ReadVector3();
        }
        else if (type == FLVER.LayoutType.Float4)
        {
            v = new Vector3(br.ReadVector2(), 0);
            v2 = new Vector3(br.ReadVector2(), 0);
            hasv2 = allowv2;
        }
        else if (type == FLVER.LayoutType.Byte4A)
        {
            v = new Vector3(br.ReadInt16(), br.ReadInt16(), 0) / uvFactor;
        }
        else if (type == FLVER.LayoutType.Byte4B)
        {
            v = new Vector3(br.ReadInt16(), br.ReadInt16(), 0) / uvFactor;
        }
        else if (type == FLVER.LayoutType.Short2toFloat2)
        {
            v = new Vector3(br.ReadInt16(), br.ReadInt16(), 0) / uvFactor;
        }
        else if (type == FLVER.LayoutType.Byte4C)
        {
            v = new Vector3(br.ReadInt16(), br.ReadInt16(), 0) / uvFactor;
        }
        else if (type == FLVER.LayoutType.UV)
        {
            v = new Vector3(br.ReadInt16(), br.ReadInt16(), 0) / uvFactor;
        }
        else if (type == FLVER.LayoutType.UVPair)
        {
            v = new Vector3(br.ReadInt16(), br.ReadInt16(), 0) / uvFactor;
            v2 = new Vector3(br.ReadInt16(), br.ReadInt16(), 0) / uvFactor;
            hasv2 = allowv2;
        }
        else if (type == FLVER.LayoutType.Short4toFloat4B)
        {
            //AddUV(new Vector3(br.ReadInt16(), br.ReadInt16(), br.ReadInt16()) / uvFactor);
            v = FLVER.Vertex.ReadFloat16NormXYZ(br);
            br.AssertInt16(0);
        }
        else
        {
            throw new NotImplementedException($"Read not implemented for {type} UV.");
        }

        dest[0] = (short)(v.X * 2048.0f);
        dest[1] = (short)(v.Y * 2048.0f);
        if (hasv2)
        {
            dest[3] = (short)(v.X * 2048.0f);
            dest[4] = (short)(v.Y * 2048.0f);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillUVShortZero(short* dest)
    {
        dest[0] = 0;
        dest[1] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void FillUVFloat(ref Vector2 dest, ref FLVER.Vertex v, byte index)
    {
        Vector3 uv = v.GetUV(index);
        dest.X = uv.X;
        dest.Y = uv.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillBinormalBitangentSNorm8(sbyte* destBinorm, sbyte* destBitan, ref FLVER.Vertex v,
        byte index)
    {
        Vector4 tan = v.GetTangent(index);
        Vector3 t = Vector3.Normalize(new Vector3(tan.X, tan.Y, tan.Z));
        destBitan[0] = (sbyte)(t.X * 127.0f);
        destBitan[1] = (sbyte)(t.Y * 127.0f);
        destBitan[2] = (sbyte)(t.Z * 127.0f);
        destBitan[3] = (sbyte)(tan.W * 127.0f);

        Vector3 bn = Vector3.Cross(Vector3.Normalize(v.Normal), Vector3.Normalize(new Vector3(t.X, t.Y, t.Z))) *
                     tan.W;
        destBinorm[0] = (sbyte)(bn.X * 127.0f);
        destBinorm[1] = (sbyte)(bn.Y * 127.0f);
        destBinorm[2] = (sbyte)(bn.Z * 127.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillBinormalBitangentSNorm8(sbyte* destBinorm, sbyte* destBitan, Vector3* n,
        BinaryReaderEx br, FLVER.LayoutType type)
    {
        Vector4 tan;
        if (type == FLVER.LayoutType.Float4)
        {
            tan = br.ReadVector4();
        }
        else if (type == FLVER.LayoutType.Byte4A)
        {
            tan = FLVER.Vertex.ReadByteNormXYZW(br);
        }
        else if (type == FLVER.LayoutType.Byte4B)
        {
            tan = FLVER.Vertex.ReadByteNormXYZW(br);
        }
        else if (type == FLVER.LayoutType.Byte4C)
        {
            tan = FLVER.Vertex.ReadByteNormXYZW(br);
        }
        else if (type == FLVER.LayoutType.Short4toFloat4A)
        {
            tan = FLVER.Vertex.ReadByteNormXYZW(br);
        }
        else if (type == FLVER.LayoutType.Byte4E)
        {
            tan = FLVER.Vertex.ReadByteNormXYZW(br);
        }
        else
        {
            throw new NotImplementedException($"Read not implemented for {type} tangent.");
        }

        Vector3 t = Vector3.Normalize(new Vector3(tan.X, tan.Y, tan.Z));
        destBitan[0] = (sbyte)(t.X * 127.0f);
        destBitan[1] = (sbyte)(t.Y * 127.0f);
        destBitan[2] = (sbyte)(t.Z * 127.0f);
        destBitan[3] = (sbyte)(tan.W * 127.0f);

        Vector3 bn = Vector3.Cross(Vector3.Normalize(*n), Vector3.Normalize(new Vector3(t.X, t.Y, t.Z))) * tan.W;
        destBinorm[0] = (sbyte)(bn.X * 127.0f);
        destBinorm[1] = (sbyte)(bn.Y * 127.0f);
        destBinorm[2] = (sbyte)(bn.Z * 127.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillBinormalBitangentSNorm8Zero(sbyte* destBinorm, sbyte* destBitan)
    {
        destBitan[0] = 0;
        destBitan[1] = 0;
        destBitan[2] = 0;
        destBitan[3] = 127;

        destBinorm[0] = 0;
        destBinorm[1] = 0;
        destBinorm[2] = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillColorUNorm(byte* dest, ref FLVER.Vertex v)
    {
    }
}
