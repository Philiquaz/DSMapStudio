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
    private void FillVertex(ref Vector3 dest, ref FLVER.Vertex v)
    {
        dest = v.Position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe void FillVertex(Vector3* dest, BinaryReaderEx br, FLVER.LayoutType type)
    {
        if (type == FLVER.LayoutType.Float3)
        {
            *dest = br.ReadVector3();
        }
        else if (type == FLVER.LayoutType.Float4)
        {
            *dest = br.ReadVector3();
            br.AssertSingle(0);
        }
        else
        {
            throw new NotImplementedException($"Read not implemented for {type} vertex.");
        }

        // Sanity check position to find bugs
        //if (dest.X > 10000.0f || dest.Y > 10000.0f || dest.Z > 10000.0f)
        //{
        //    Debugger.Break();
        //}
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EatVertex(BinaryReaderEx br, FLVER.LayoutType type)
    {
        switch (type)
        {
            case FLVER.LayoutType.Byte4A:
            case FLVER.LayoutType.Byte4B:
            case FLVER.LayoutType.Short2toFloat2:
            case FLVER.LayoutType.Byte4C:
            case FLVER.LayoutType.UV:
            case FLVER.LayoutType.Byte4E:
            case FLVER.LayoutType.Unknown:
                br.ReadUInt32();
                break;

            case FLVER.LayoutType.Float2:
            case FLVER.LayoutType.UVPair:
            case FLVER.LayoutType.ShortBoneIndices:
            case FLVER.LayoutType.Short4toFloat4A:
            case FLVER.LayoutType.Short4toFloat4B:
                br.ReadUInt64();
                break;

            case FLVER.LayoutType.Float3:
                br.ReadUInt32();
                br.ReadUInt64();
                break;

            case FLVER.LayoutType.Float4:
                br.ReadUInt64();
                br.ReadUInt64();
                break;

            default:
                throw new NotImplementedException($"No size defined for buffer layout type: {type}");
        }
    }

    private unsafe void FillVerticesNormalOnly(BinaryReaderEx br, ref FlverVertexBuffer buffer,
        Span<FlverBufferLayoutMember> layouts, Span<Vector3> pickingVerts, IntPtr vertBuffer)
    {
        Span<FlverLayoutSky> verts = new(vertBuffer.ToPointer(), buffer.vertexCount);
        br.StepIn(buffer.bufferOffset);
        for (var i = 0; i < buffer.vertexCount; i++)
        {
            Vector3 n = Vector3.Zero;
            fixed (FlverLayoutSky* v = &verts[i])
            {
                var posfilled = false;
                foreach (FlverBufferLayoutMember l in layouts)
                {
                    // ER meme
                    if (l.unk00 == -2147483647)
                    {
                        continue;
                    }

                    if (l.semantic == FLVER.LayoutSemantic.Position)
                    {
                        FillVertex(&(*v).Position, br, l.type);
                        posfilled = true;
                    }
                    else if (l.semantic == FLVER.LayoutSemantic.Normal)
                    {
                        FillNormalSNorm8((*v).Normal, br, l.type, &n);
                    }
                    else
                    {
                        EatVertex(br, l.type);
                    }
                }

                if (!posfilled)
                {
                    (*v).Position = new Vector3(0, 0, 0);
                }

                pickingVerts[i] = (*v).Position;
            }
        }

        br.StepOut();
    }

    private unsafe void FillVerticesNormalOnly(FLVER2.Mesh mesh, Span<Vector3> pickingVerts, IntPtr vertBuffer)
    {
        Span<FlverLayoutSky> verts = new(vertBuffer.ToPointer(), mesh.VertexCount);
        for (var i = 0; i < mesh.VertexCount; i++)
        {
            FLVER.Vertex vert = mesh.Vertices[i];

            verts[i] = new FlverLayoutSky();
            pickingVerts[i] = new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z);
            fixed (FlverLayoutSky* v = &verts[i])
            {
                FillVertex(ref (*v).Position, ref vert);
                FillNormalSNorm8((*v).Normal, ref vert);
            }
        }
    }

    private unsafe void FillVerticesNormalOnly(FLVER0.Mesh mesh, Span<Vector3> pickingVerts, IntPtr vertBuffer)
    {
        Span<FlverLayoutSky> verts = new(vertBuffer.ToPointer(), mesh.Vertices.Count);
        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            FLVER.Vertex vert = mesh.Vertices[i];

            verts[i] = new FlverLayoutSky();
            pickingVerts[i] = new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z);
            fixed (FlverLayoutSky* v = &verts[i])
            {
                FillVertex(ref (*v).Position, ref vert);
                FillNormalSNorm8((*v).Normal, ref vert);
            }
        }
    }

    private unsafe void FillVerticesStandard(BinaryReaderEx br, ref FlverVertexBuffer buffer,
        Span<FlverBufferLayoutMember> layouts, Span<Vector3> pickingVerts, IntPtr vertBuffer, float uvFactor)
    {
        br.StepIn(buffer.bufferOffset);
        var pverts = (FlverLayout*)vertBuffer;

        for (var i = 0; i < buffer.vertexCount; i++)
        {
            FlverLayout* v = &pverts[i];
            Vector3 n = Vector3.UnitX;
            FillUVShortZero((*v).Uv1);
            FillBinormalBitangentSNorm8Zero((*v).Binormal, (*v).Bitangent);
            var posfilled = false;
            var colorFilled = false;
            foreach (FlverBufferLayoutMember l in layouts)
            {
                // ER meme
                if (l.unk00 == -2147483647)
                {
                    continue;
                }

                if (l.semantic == FLVER.LayoutSemantic.Position)
                {
                    FillVertex(&(*v).Position, br, l.type);
                    posfilled = true;
                }
                else if (l.semantic == FLVER.LayoutSemantic.Normal)
                {
                    FillNormalSNorm8((*v).Normal, br, l.type, &n);
                }
                else if (l.semantic == FLVER.LayoutSemantic.UV && l.index == 0)
                {
                    bool hasv2;
                    FillUVShort((*v).Uv1, br, l.type, uvFactor, false, out hasv2);
                }
                else if (l.semantic == FLVER.LayoutSemantic.Tangent && l.index == 0)
                {
                    FillBinormalBitangentSNorm8((*v).Binormal, (*v).Bitangent, &n, br, l.type);
                }
                else if (l.semantic == FLVER.LayoutSemantic.VertexColor && l.index == 0)
                {
                    FillVertexColor((*v).Color, br, l.type);
                    colorFilled = true;
                }
                else
                {
                    EatVertex(br, l.type);
                }
            }

            if (!posfilled)
            {
                (*v).Position = new Vector3(0, 0, 0);
            }
            if (!colorFilled)
            {
                FillVertexColorDefault((*v).Color);
            }

            pickingVerts[i] = (*v).Position;
        }

        br.StepOut();
    }

    private unsafe void FillVerticesStandard(FLVER2.Mesh mesh, Span<Vector3> pickingVerts, IntPtr vertBuffer)
    {
        Span<FlverLayout> verts = new(vertBuffer.ToPointer(), mesh.VertexCount);
        fixed (FlverLayout* pverts = verts)
        {
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                FlverLayout* v = &pverts[i];
                FLVER.Vertex vert = mesh.Vertices[i];

                verts[i] = new FlverLayout();
                pickingVerts[i] = new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z);
                FillVertex(ref (*v).Position, ref vert);
                FillNormalSNorm8((*v).Normal, ref vert);
                if (vert.UVCount > 0)
                {
                    FillUVShort((*v).Uv1, ref vert, 0);
                }
                else
                {
                    FillUVShortZero((*v).Uv1);
                }

                if (vert.TangentCount > 0)
                {
                    FillBinormalBitangentSNorm8((*v).Binormal, (*v).Bitangent, ref vert, 0);
                }
                else
                {
                    FillBinormalBitangentSNorm8Zero((*v).Binormal, (*v).Bitangent);
                }

                if (vert.Colors?.Count > 0)
                {
                    FillVertexColor((*v).Color, ref vert);
                }
                else
                {
                    FillVertexColorDefault((*v).Color);
                }
            }
        }
    }

    private unsafe void FillVerticesStandard(FLVER0.Mesh mesh, Span<Vector3> pickingVerts, IntPtr vertBuffer)
    {
        Span<FlverLayout> verts = new(vertBuffer.ToPointer(), mesh.Vertices.Count);
        fixed (FlverLayout* pverts = verts)
        {
            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                FlverLayout* v = &pverts[i];
                FLVER.Vertex vert = mesh.Vertices[i];

                verts[i] = new FlverLayout();
                pickingVerts[i] = new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z);
                FillVertex(ref (*v).Position, ref vert);
                FillNormalSNorm8((*v).Normal, ref vert);
                if (vert.UVCount > 0)
                {
                    FillUVShort((*v).Uv1, ref vert, 0);
                }
                else
                {
                    FillUVShortZero((*v).Uv1);
                }

                if (vert.TangentCount > 0)
                {
                    FillBinormalBitangentSNorm8((*v).Binormal, (*v).Bitangent, ref vert, 0);
                }
                else
                {
                    FillBinormalBitangentSNorm8Zero((*v).Binormal, (*v).Bitangent);
                }

                if (vert.Colors?.Count > 0)
                {
                    FillVertexColor((*v).Color, ref vert);
                }
                else
                {
                    FillVertexColorDefault((*v).Color);
                }
            }
        }
    }

    private unsafe void FillVerticesUV2(BinaryReaderEx br, ref FlverVertexBuffer buffer,
        Span<FlverBufferLayoutMember> layouts, Span<Vector3> pickingVerts, IntPtr vertBuffer, float uvFactor)
    {
        Span<FlverLayoutUV2> verts = new(vertBuffer.ToPointer(), buffer.vertexCount);
        br.StepIn(buffer.bufferOffset);
        fixed (FlverLayoutUV2* pverts = verts)
        {
            for (var i = 0; i < buffer.vertexCount; i++)
            {
                FlverLayoutUV2* v = &pverts[i];
                Vector3 n = Vector3.UnitX;
                FillBinormalBitangentSNorm8Zero((*v).Binormal, (*v).Bitangent);
                var uvsfilled = 0;
                var colorFilled = false;
                foreach (FlverBufferLayoutMember l in layouts)
                {
                    // ER meme
                    if (l.unk00 == -2147483647)
                    {
                        continue;
                    }

                    if (l.semantic == FLVER.LayoutSemantic.Position)
                    {
                        FillVertex(&(*v).Position, br, l.type);
                    }
                    else if (l.semantic == FLVER.LayoutSemantic.Normal)
                    {
                        FillNormalSNorm8((*v).Normal, br, l.type, &n);
                    }
                    else if (l.semantic == FLVER.LayoutSemantic.UV && uvsfilled < 2)
                    {
                        bool hasv2;
                        FillUVShort(uvsfilled > 0 ? (*v).Uv2 : (*v).Uv1, br, l.type, uvFactor, false, out hasv2);
                        uvsfilled += hasv2 ? 2 : 1;
                    }
                    else if (l.semantic == FLVER.LayoutSemantic.Tangent && l.index == 0)
                    {
                        FillBinormalBitangentSNorm8((*v).Binormal, (*v).Bitangent, &n, br, l.type);
                    }
                    else if (l.semantic == FLVER.LayoutSemantic.VertexColor && l.index == 0)
                    {
                        FillVertexColor((*v).Color, br, l.type);
                        colorFilled = true;
                    }
                    else
                    {
                        EatVertex(br, l.type);
                    }
                }

                pickingVerts[i] = (*v).Position;
                if (!colorFilled)
                {
                    FillVertexColorDefault((*v).Color);
                }
            }
        }

        br.StepOut();
    }

    private unsafe void FillVerticesUV2(FLVER2.Mesh mesh, Span<Vector3> pickingVerts, IntPtr vertBuffer)
    {
        Span<FlverLayoutUV2> verts = new(vertBuffer.ToPointer(), mesh.VertexCount);
        fixed (FlverLayoutUV2* pverts = verts)
        {
            for (var i = 0; i < mesh.VertexCount; i++)
            {
                FLVER.Vertex vert = mesh.Vertices[i];

                verts[i] = new FlverLayoutUV2();
                pickingVerts[i] = new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z);
                FlverLayoutUV2* v = &pverts[i];
                FillVertex(ref (*v).Position, ref vert);
                FillNormalSNorm8((*v).Normal, ref vert);
                FillUVShort((*v).Uv1, ref vert, 0);
                FillUVShort((*v).Uv2, ref vert, 1);
                if (vert.TangentCount > 0)
                {
                    FillBinormalBitangentSNorm8((*v).Binormal, (*v).Bitangent, ref vert, 0);
                }
                else
                {
                    FillBinormalBitangentSNorm8Zero((*v).Binormal, (*v).Bitangent);
                }
                if (vert.Colors?.Count > 0)
                {
                    FillVertexColor((*v).Color, ref vert);
                }
                else
                {
                    FillVertexColorDefault((*v).Color);
                }
            }
        }
    }

    private unsafe void FillVerticesUV2(FLVER0.Mesh mesh, Span<Vector3> pickingVerts, IntPtr vertBuffer)
    {
        Span<FlverLayoutUV2> verts = new(vertBuffer.ToPointer(), mesh.Vertices.Count);
        fixed (FlverLayoutUV2* pverts = verts)
        {
            for (var i = 0; i < mesh.Vertices.Count; i++)
            {
                FLVER.Vertex vert = mesh.Vertices[i];

                verts[i] = new FlverLayoutUV2();
                pickingVerts[i] = new Vector3(vert.Position.X, vert.Position.Y, vert.Position.Z);
                FlverLayoutUV2* v = &pverts[i];
                FillVertex(ref (*v).Position, ref vert);
                FillNormalSNorm8((*v).Normal, ref vert);
                FillUVShort((*v).Uv1, ref vert, 0);
                FillUVShort((*v).Uv2, ref vert, 1);
                if (vert.TangentCount > 0)
                {
                    FillBinormalBitangentSNorm8((*v).Binormal, (*v).Bitangent, ref vert, 0);
                }
                else
                {
                    FillBinormalBitangentSNorm8Zero((*v).Binormal, (*v).Bitangent);
                }

                if (vert.Colors?.Count > 0)
                {
                    FillVertexColor((*v).Color, ref vert);
                }
                else
                {
                    FillVertexColorDefault((*v).Color);
                }
            }
        }
    }
}
