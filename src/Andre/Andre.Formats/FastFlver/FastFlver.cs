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
    //private static ArrayPool<FlverLayout> VerticesPool = ArrayPool<FlverLayout>.Create();

    protected abstract IndexBufferHandle indexBufferAllocator(uint vbuffersize, uint ibuffersize, int vSize);
    protected abstract (nint, nint) meshResourceMapper(IndexBufferHandle geomBuffer);
    protected abstract void meshResourceUnmapper(IndexBufferHandle geomBuffer);
    protected abstract BoundingBox boundsConstructor(nint pickingVerts, int vertexCount);
    protected abstract String getTexturePath(string mtd, string type);
    protected abstract void useTexture(string path, FlverMaterial<GPUBufferHandle, SpecializationConstant> dest, FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType textureType);
    protected abstract GPUBufferHandle allocBufferHandle();
    protected abstract bool defineFlverMaterialDefault(FlverMaterial<GPUBufferHandle, SpecializationConstant> dest);
    protected abstract void defineFlverMaterialAdvanced(FlverMaterial<GPUBufferHandle, SpecializationConstant> dest, GameType type, bool blend, bool blendMask, bool hasNormal2, bool hasSpec2, bool hasShininess2);
    protected abstract BoundingBox combineBounds(BoundingBox a, BoundingBox b);
    protected abstract GPUBufferHandle allocBoneBuffer(uint boneCount);
    protected abstract void useBones();
    protected abstract SpecializationConstant generateSpecializationConstantForBone(int i, bool b);

    //Hack so less errors show
    public enum AccessLevel {
        AccessFull,
        AccessGPUOptimizedOnly,
    }
    public enum GameType {
        DemonsSouls,
        DarkSoulsPTDE,
        DarkSoulsRemastered,
        DarkSoulsIISOTFS,
        DarkSoulsIII,
    }

    //fastflver start

    public const bool CaptureMaterialLayouts = false;
    private static readonly Stack<FlverCache> FlverCaches = new();
    private static readonly object CacheLock = new();

    /// <summary>
    ///     Cache of material layouts that can be dumped
    /// </summary>
    public static Dictionary<string, FLVER2.BufferLayout> MaterialLayouts = new();

    public static object _matLayoutLock = new();
    public FLVER2 Flver;

    /// <summary>
    ///     Low level access to the flver struct. Use only in modification mode.
    /// </summary>
    public FLVER0 FlverDeS;

    public FlverMaterial<GPUBufferHandle, SpecializationConstant>[] GPUMaterials;

    public FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>[] GPUMeshes;
    public static int CacheCount { get; private set; }

    public static long CacheFootprint
    {
        get
        {
            long total = 0;
            lock (CacheLock)
            {
                foreach (FlverCache c in FlverCaches)
                {
                    total += c.MemoryUsage;
                }
            }

            return total;
        }
    }

    public BoundingBox Bounds { get; set; }

    public List<FLVER.Bone> Bones { get; private set; }
    private List<FlverBone> FBones { get; set; }
    private List<Matrix4x4> BoneTransforms { get; set; }

    public GPUBufferHandle StaticBoneBuffer { get; private set; }

    public bool _Load(Memory<byte> bytes, AccessLevel al, GameType type)
    {
        bool ret;
        if (type == GameType.DemonsSouls)
        {
            FlverDeS = FLVER0.Read(bytes);
            ret = LoadInternalDeS(al, type);
        }
        else
        {
            if (al == AccessLevel.AccessGPUOptimizedOnly && type != GameType.DarkSoulsRemastered &&
                type != GameType.DarkSoulsPTDE)
            {
                BinaryReaderEx br = new(false, bytes);
                DCX.Type ctype;
                br = SFUtil.GetDecompressedBR(br, out ctype);
                ret = LoadInternalFast(br, type);
            }
            else
            {
                FlverCache? cache = al == AccessLevel.AccessGPUOptimizedOnly ? GetCache() : null;
                Flver = FLVER2.Read(bytes, cache);
                ret = LoadInternal(al, type);
                ReleaseCache(cache);
            }
        }

        return ret;
    }

    public bool _Load(string path, AccessLevel al, GameType type)
    {
        bool ret;
        if (type == GameType.DemonsSouls)
        {
            FlverDeS = FLVER0.Read(path);
            ret = LoadInternalDeS(al, type);
        }
        else
        {
            if (al == AccessLevel.AccessGPUOptimizedOnly && type != GameType.DarkSoulsRemastered &&
                type != GameType.DarkSoulsPTDE)
            {
                using var file =
                    MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                using IMappedMemoryOwner accessor = file.CreateMemoryAccessor(0, 0, MemoryMappedFileAccess.Read);
                BinaryReaderEx br = new(false, accessor.Memory);
                DCX.Type ctype;
                br = SFUtil.GetDecompressedBR(br, out ctype);
                ret = LoadInternalFast(br, type);
            }
            else
            {
                FlverCache? cache = al == AccessLevel.AccessGPUOptimizedOnly ? GetCache() : null;
                Flver = FLVER2.Read(path, cache);
                ret = LoadInternal(al, type);
                ReleaseCache(cache);
            }
        }

        return ret;
    }

    private FlverCache GetCache()
    {
        lock (CacheLock)
        {
            if (FlverCaches.Count > 0)
            {
                return FlverCaches.Pop();
            }

            CacheCount++;
        }

        return new FlverCache();
    }

    private void ReleaseCache(FlverCache cache)
    {
        if (cache != null)
        {
            cache.ResetUsage();
            lock (CacheLock)
            {
                FlverCaches.Push(cache);
            }
        }
    }

    public static void PurgeCaches()
    {
        FlverCaches.Clear();
        //VerticesPool = ArrayPool<FlverLayout>.Create();
        //GC.Collect();
        //GC.WaitForPendingFinalizers();
        //GC.Collect();
    }

    private string TexturePathToVirtual(string texpath)
    {
        if (texpath.Contains(@"\map\"))
        {
            var splits = texpath.Split('\\');
            var mapid = splits[splits.Length - 3];
            return $@"map/tex/{mapid}/{Path.GetFileNameWithoutExtension(texpath)}";
        }
        // Chr texture reference

        if (texpath.Contains(@"\chr\"))
        {
            var splits = texpath.Split('\\');
            var chrid = splits[splits.Length - 3];
            return $@"chr/{chrid}/tex/{Path.GetFileNameWithoutExtension(texpath)}";
        }
        // Obj texture reference

        if (texpath.Contains(@"\obj\"))
        {
            var splits = texpath.Split('\\');
            var objid = splits[splits.Length - 3];
            return $@"obj/{objid}/tex/{Path.GetFileNameWithoutExtension(texpath)}";
        }
        // Asset (aet) texture references

        if (texpath.Contains(@"\aet") || texpath.StartsWith("aet"))
        {
            var splits = texpath.Split('\\');
            var aetid = splits[splits.Length - 1].Substring(0, 6);
            return $@"aet/{aetid}/{Path.GetFileNameWithoutExtension(texpath)}";
        }
        // Parts texture reference

        if (texpath.Contains(@"\parts\"))
        {
            var splits = texpath.Split('\\');
            var partsId = splits[splits.Length - 3];
            return $@"parts/{partsId}/tex/{Path.GetFileNameWithoutExtension(texpath)}";
        }

        return texpath;
    }

    private void LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType textureType, FlverMaterial<GPUBufferHandle, SpecializationConstant> dest, string type, string mpath,
        string mtd)
    {
        var path = mpath;
        if (mpath == "")
        {
            string newPath = getTexturePath(mtd, type);
            if (newPath != null)
            {
                path = newPath;
            }
        }

        if (!dest.TextureResourceFilled[(int)textureType])
        {
            useTexture(path, dest, textureType);
            dest.TextureResourceFilled[(int)textureType] = true;
        }
    }

    private void ProcessMaterialTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant> dest, string texType, string mpath, string mtd,
        GameType gameType,
        ref bool blend, ref bool hasNormal2, ref bool hasSpec2, ref bool hasShininess2, ref bool blendMask)
    {

        string paramNameCheck;
        if (texType == null)
        {
            paramNameCheck = "G_DIFFUSE";
        }
        else
        {
            paramNameCheck = texType.ToUpper();
        }

        if (paramNameCheck == "G_DIFFUSETEXTURE2" || paramNameCheck == "G_DIFFUSE2" || paramNameCheck == "G_DIFFUSE_2" ||
            paramNameCheck.Contains("ALBEDO_2"))
        {
            LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.AlbedoTextureResource2, dest, texType, mpath, mtd);
            blend = true;
        }
        else if (paramNameCheck == "G_DIFFUSETEXTURE" || paramNameCheck == "G_DIFFUSE" ||
                 paramNameCheck.Contains("ALBEDO"))
        {
            LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.AlbedoTextureResource, dest, texType, mpath, mtd);
        }
        else if (paramNameCheck == "G_BUMPMAPTEXTURE2" || paramNameCheck == "G_BUMPMAP2" || paramNameCheck == "G_BUMPMAP_2" ||
                 paramNameCheck.Contains("NORMAL_2"))
        {
            LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.NormalTextureResource2, dest, texType, mpath, mtd);
            blend = true;
            hasNormal2 = true;
        }
        else if (paramNameCheck == "G_BUMPMAPTEXTURE" || paramNameCheck == "G_BUMPMAP" ||
                 paramNameCheck.Contains("NORMAL"))
        {
            LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.NormalTextureResource, dest, texType, mpath, mtd);
        }
        else if (paramNameCheck == "G_SPECULARTEXTURE2" || paramNameCheck == "G_SPECULAR2" || paramNameCheck == "G_SPECULAR_2" ||
                 paramNameCheck.Contains("SPECULAR_2"))
        {
            if (gameType is GameType.DarkSoulsRemastered or GameType.DarkSoulsIISOTFS)
            {
                LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.ShininessTextureResource2, dest, texType, mpath, mtd);
                blend = true;
                hasShininess2 = true;
            }
            else
            {
                LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.SpecularTextureResource2, dest, texType, mpath, mtd);
                blend = true;
                hasSpec2 = true;
            }
        }
        else if (paramNameCheck == "G_SPECULARTEXTURE" || paramNameCheck == "G_SPECULAR" ||
                 paramNameCheck.Contains("SPECULAR"))
        {
            if (gameType is GameType.DarkSoulsRemastered or GameType.DarkSoulsIISOTFS)
            {
                LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.ShininessTextureResource, dest, texType, mpath, mtd);
            }
            else
            {
                LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.SpecularTextureResource, dest, texType, mpath, mtd);
            }
        }
        else if (paramNameCheck == "G_SHININESSTEXTURE2" || paramNameCheck == "G_SHININESS2" ||
                 paramNameCheck.Contains("SHININESS2"))
        {
            LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.ShininessTextureResource2, dest, texType, mpath, mtd);
            blend = true;
            hasShininess2 = true;
        }
        else if (paramNameCheck == "G_SHININESSTEXTURE" || paramNameCheck == "G_SHININESS" ||
                 paramNameCheck.Contains("SHININESS"))
        {
            LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.ShininessTextureResource, dest, texType, mpath, mtd);
        }
        else if (paramNameCheck.Contains("BLENDMASK"))
        {
            LookupTexture(FlverMaterial<GPUBufferHandle, SpecializationConstant>.TextureType.BlendmaskTextureResource, dest, texType, mpath, mtd);
            blendMask = true;
        }
    }

    private unsafe void ProcessMaterial(IFlverMaterial mat, FlverMaterial<GPUBufferHandle, SpecializationConstant> dest, GameType type)
    {
        dest.MaterialName = Path.GetFileNameWithoutExtension(mat.MTD);
        dest.MaterialBuffer = allocBufferHandle();
        dest.MaterialData = new Material();

        //FLVER0 stores layouts directly in the material
        if (type == GameType.DemonsSouls)
        {
            var desMat = (FLVER0.Material)mat;
            var foundBoneIndices = false;
            var foundBoneWeights = false;

            if (desMat.Layouts?.Count > 0)
            {
                foreach (FLVER.LayoutMember? layoutType in desMat.Layouts[0])
                {
                    switch (layoutType.Semantic)
                    {
                        case FLVER.LayoutSemantic.Normal:
                            if (layoutType.Type == FLVER.LayoutType.Byte4B ||
                                layoutType.Type == FLVER.LayoutType.Byte4E)
                            {
                                dest.SetNormalWBoneTransform(generateSpecializationConstantForBone);
                            }

                            break;
                        case FLVER.LayoutSemantic.BoneIndices:
                            foundBoneIndices = true;
                            break;
                        case FLVER.LayoutSemantic.BoneWeights:
                            foundBoneWeights = true;
                            break;
                    }
                }
            }

            //Transformation condition for DeS models
            if (foundBoneIndices && !foundBoneWeights)
            {
                dest.SetHasIndexNoWeightTransform();
            }
        }

        if (defineFlverMaterialDefault(dest))
        {
            return;
        }

        var blend = false;
        var blendMask = false;
        var hasNormal2 = false;
        var hasSpec2 = false;
        var hasShininess2 = false;

        foreach (IFlverTexture? matparam in mat.Textures)
        {
            ProcessMaterialTexture(dest, matparam.Type, matparam.Path, mat.MTD, type,
                ref blend, ref hasNormal2, ref hasSpec2, ref hasShininess2, ref blendMask);
        }

        defineFlverMaterialAdvanced(dest, type, blend, blendMask, hasNormal2, hasSpec2, hasShininess2);

        dest.UpdateMaterial();
    }

    private unsafe void ProcessMaterial(FlverMaterial<GPUBufferHandle, SpecializationConstant> dest, GameType type, BinaryReaderEx br,
        ref FlverMaterialDef mat, Span<FlverTexture> textures, bool isUTF)
    {
        var mtd = isUTF ? br.GetUTF16(mat.mtdOffset) : br.GetShiftJIS(mat.mtdOffset);
        dest.MaterialName = Path.GetFileNameWithoutExtension(mtd);
        dest.MaterialBuffer = allocBufferHandle();
        dest.MaterialData = new Material();

        if (defineFlverMaterialDefault(dest))
        {
            return;
        }

        var blend = false;
        var blendMask = false;
        var hasNormal2 = false;
        var hasSpec2 = false;
        var hasShininess2 = false;

        for (var i = mat.textureIndex; i < mat.textureIndex + mat.textureCount; i++)
        {
            var ttype = isUTF ? br.GetUTF16(textures[i].typeOffset) : br.GetShiftJIS(textures[i].typeOffset);
            var tpath = isUTF ? br.GetUTF16(textures[i].pathOffset) : br.GetShiftJIS(textures[i].pathOffset);
            ProcessMaterialTexture(dest, ttype, tpath, mtd, type,
                ref blend, ref hasNormal2, ref hasSpec2, ref hasShininess2, ref blendMask);
        }

        defineFlverMaterialAdvanced(dest, type, blend, blendMask, hasNormal2, hasSpec2, hasShininess2);

        dest.UpdateMaterial();
    }
    private unsafe void ProcessMesh(FLVER0.Mesh mesh, FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant> dest)
    {
        dest.Material = GPUMaterials[mesh.MaterialIndex];

        if (dest.Material.GetHasIndexNoWeightTransform())
        {
            //Transform based on root
            for (var v = 0; v < mesh.Vertices.Count; v++)
            {
                FLVER.Vertex vert = mesh.Vertices[v];
                var boneTransformationIndex = mesh.BoneIndices[vert.BoneIndices[0]];
                if (boneTransformationIndex > -1 && BoneTransforms.Count > boneTransformationIndex)
                {
                    Matrix4x4 boneTfm = BoneTransforms[boneTransformationIndex];

                    vert.Position = Vector3.Transform(vert.Position, boneTfm);
                    vert.Normal = Vector3.TransformNormal(vert.Normal, boneTfm);
                    mesh.Vertices[v] = vert;
                }
            }
        }

        var vSize = dest.Material.VertexSize;
        dest.PickingVertices = Marshal.AllocHGlobal(mesh.Vertices.Count * sizeof(Vector3));
        Span<Vector3> pvhandle = new(dest.PickingVertices.ToPointer(), mesh.Vertices.Count);
        var vbuffersize = (uint)mesh.Vertices.Count * vSize;

        dest.VertexCount = mesh.Vertices.Count;

        dest.MeshFacesets = new List<FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>.FlverSubmeshFaceSet>();

        var is32bit = false; //FlverDeS.Version > 0x20005 && mesh.Vertices.Count > 65535;
        Span<ushort> fs16 = null;
        Span<int> fs32 = null;

        var indices = mesh.Triangulate(FlverDeS.Header.Version).ToArray();
        var indicesTotal = indices.Length;

        dest.GeomBuffer = indexBufferAllocator(vbuffersize,
            (uint)indicesTotal * (is32bit ? 4u : 2u), (int)vSize);
        (nint meshVertices, nint meshIndices) = meshResourceMapper(dest.GeomBuffer);

        if (dest.Material.LayoutType == MeshLayoutType.LayoutSky)
        {
            FillVerticesNormalOnly(mesh, pvhandle, meshVertices);
        }
        else if (dest.Material.LayoutType == MeshLayoutType.LayoutUV2)
        {
            FillVerticesUV2(mesh, pvhandle, meshVertices);
        }
        else
        {
            FillVerticesStandard(mesh, pvhandle, meshVertices);
        }

        if (mesh.VertexIndices.Count != 0)
        {
            if (is32bit)
            {
                fs32 = new Span<int>(meshIndices.ToPointer(), indicesTotal);
            }
            else
            {
                fs16 = new Span<ushort>(meshIndices.ToPointer(), indicesTotal);
            }

            FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>.FlverSubmeshFaceSet newFaceSet = new()
            {
                BackfaceCulling = true,
                IsTriangleStrip = false,
                //IndexBuffer = factory.CreateBuffer(new BufferDescription(buffersize, BufferUsage.IndexBuffer)),
                IndexOffset = 0,
                IndexCount = indices.Length,
                Is32Bit = is32bit,
                PickingIndicesCount = indices.Length
                //PickingIndices = Marshal.AllocHGlobal(indices.Length * 4),
            };

            if (is32bit)
            {
                for (var i = 0; i < indices.Length; i++)
                {
                    if (indices[i] == 0xFFFF && indices[i] > mesh.Vertices.Count)
                    {
                        fs32[newFaceSet.IndexOffset + i] = -1;
                    }
                    else
                    {
                        fs32[newFaceSet.IndexOffset + i] = indices[i];
                    }
                }
            }
            else
            {
                for (var i = 0; i < indices.Length; i++)
                {
                    if (indices[i] == 0xFFFF && indices[i] > mesh.Vertices.Count)
                    {
                        fs16[newFaceSet.IndexOffset + i] = 0xFFFF;
                    }
                    else
                    {
                        fs16[newFaceSet.IndexOffset + i] = (ushort)indices[i];
                    }
                }
            }

            dest.MeshFacesets.Add(newFaceSet);
        }

        meshResourceUnmapper(dest.GeomBuffer);

        dest.Bounds = boundsConstructor(dest.PickingVertices, dest.VertexCount);

        if (CaptureMaterialLayouts)
        {
            lock (_matLayoutLock)
            {
                if (!MaterialLayouts.ContainsKey(dest.Material.MaterialName))
                {
                    MaterialLayouts.Add(dest.Material.MaterialName, Flver.BufferLayouts[mesh.LayoutIndex]);
                }
            }
        }
    }

    private unsafe void ProcessMesh(FLVER2.Mesh mesh, FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant> dest)
    {
        dest.Material = GPUMaterials[mesh.MaterialIndex];

        var vSize = dest.Material.VertexSize;
        dest.PickingVertices = Marshal.AllocHGlobal(mesh.VertexCount * sizeof(Vector3));
        Span<Vector3> pvhandle = new(dest.PickingVertices.ToPointer(), mesh.VertexCount);

        dest.VertexCount = mesh.VertexCount;

        dest.MeshFacesets = new List<FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>.FlverSubmeshFaceSet>();
        List<FLVER2.FaceSet>? facesets = mesh.FaceSets;

        var is32bit = Flver.Header.Version > 0x20005 && mesh.VertexCount > 65535;
        var indicesTotal = 0;
        Span<ushort> fs16 = null;
        Span<int> fs32 = null;
        foreach (FLVER2.FaceSet? faceset in facesets)
        {
            indicesTotal += faceset.Indices.Length;
        }

        var vbuffersize = (uint)mesh.VertexCount * vSize;
        dest.GeomBuffer = indexBufferAllocator(vbuffersize, (uint)indicesTotal * (is32bit ? 4u : 2u), (int)vSize);
        (nint meshVertices, nint meshIndices) = meshResourceMapper(dest.GeomBuffer);

        if (dest.Material.LayoutType == MeshLayoutType.LayoutSky)
        {
            FillVerticesNormalOnly(mesh, pvhandle, meshVertices);
        }
        else if (dest.Material.LayoutType == MeshLayoutType.LayoutUV2)
        {
            FillVerticesUV2(mesh, pvhandle, meshVertices);
        }
        else
        {
            FillVerticesStandard(mesh, pvhandle, meshVertices);
        }

        if (is32bit)
        {
            fs32 = new Span<int>(meshIndices.ToPointer(), indicesTotal);
        }
        else
        {
            fs16 = new Span<ushort>(meshIndices.ToPointer(), indicesTotal);
        }

        var idxoffset = 0;
        foreach (FLVER2.FaceSet? faceset in facesets)
        {
            if (faceset.Indices.Length == 0)
            {
                continue;
            }

            //At this point they use 32-bit faceset vertex indices
            FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>.FlverSubmeshFaceSet newFaceSet = new()
            {
                BackfaceCulling = faceset.CullBackfaces,
                IsTriangleStrip = faceset.TriangleStrip,
                IndexOffset = idxoffset,
                IndexCount = faceset.IndicesCount,
                Is32Bit = is32bit
            };


            if ((faceset.Flags & FLVER2.FaceSet.FSFlags.LodLevel1) > 0)
            {
                newFaceSet.LOD = 1;
                newFaceSet.IsMotionBlur = false;
            }
            else if ((faceset.Flags & FLVER2.FaceSet.FSFlags.LodLevel2) > 0)
            {
                newFaceSet.LOD = 2;
                newFaceSet.IsMotionBlur = false;
            }

            if ((faceset.Flags & FLVER2.FaceSet.FSFlags.MotionBlur) > 0)
            {
                newFaceSet.IsMotionBlur = true;
            }

            if (is32bit)
            {
                for (var i = 0; i < faceset.Indices.Length; i++)
                {
                    if (faceset.Indices[i] == 0xFFFF && faceset.Indices[i] > mesh.Vertices.Length)
                    {
                        fs32[newFaceSet.IndexOffset + i] = -1;
                    }
                    else
                    {
                        fs32[newFaceSet.IndexOffset + i] = faceset.Indices[i];
                    }
                }
            }
            else
            {
                for (var i = 0; i < faceset.Indices.Length; i++)
                {
                    if (faceset.Indices[i] == 0xFFFF && faceset.Indices[i] > mesh.Vertices.Length)
                    {
                        fs16[newFaceSet.IndexOffset + i] = 0xFFFF;
                    }
                    else
                    {
                        fs16[newFaceSet.IndexOffset + i] = (ushort)faceset.Indices[i];
                    }
                }
            }

            dest.MeshFacesets.Add(newFaceSet);
            idxoffset += faceset.Indices.Length;
        }

        meshResourceUnmapper(dest.GeomBuffer);

        dest.Bounds = boundsConstructor(dest.PickingVertices, dest.VertexCount);

        if (CaptureMaterialLayouts)
        {
            lock (_matLayoutLock)
            {
                if (!MaterialLayouts.ContainsKey(dest.Material.MaterialName))
                {
                    MaterialLayouts.Add(dest.Material.MaterialName,
                        Flver.BufferLayouts[mesh.VertexBuffers[0].LayoutIndex]);
                }
            }
        }

        if (mesh.Dynamic == 0)
        {
            IEnumerable<FLVER.LayoutMember> elements =
                mesh.VertexBuffers.SelectMany(b => Flver.BufferLayouts[b.LayoutIndex]);
            dest.UseNormalWBoneTransform = elements.Any(e =>
                e.Semantic == FLVER.LayoutSemantic.Normal &&
                (e.Type == FLVER.LayoutType.Byte4B || e.Type == FLVER.LayoutType.Byte4E));
            if (dest.UseNormalWBoneTransform)
            {
                dest.Material.SetNormalWBoneTransform(generateSpecializationConstantForBone);
            }
            else if (mesh.DefaultBoneIndex != -1 && mesh.DefaultBoneIndex < Bones.Count)
            {
                dest.LocalTransform = GetBoneObjectMatrix(Bones[mesh.DefaultBoneIndex], Bones);
            }
        }

        Marshal.FreeHGlobal(dest.PickingVertices);
    }

    private static Matrix4x4 GetBoneObjectMatrix(FLVER.Bone bone, List<FLVER.Bone> bones)
    {
        Matrix4x4 res = Matrix4x4.Identity;
        FLVER.Bone parentBone = bone;
        do
        {
            res *= parentBone.ComputeLocalTransform();
            if (parentBone.ParentIndex >= 0)
            {
                parentBone = bones[parentBone.ParentIndex];
            }
            else
            {
                parentBone = null;
            }
        } while (parentBone != null);

        return res;
    }

    private static Matrix4x4 GetBoneObjectMatrix(FlverBone bone, List<FlverBone> bones)
    {
        Matrix4x4 res = Matrix4x4.Identity;
        FlverBone? parentBone = bone;
        do
        {
            res *= parentBone.Value.ComputeLocalTransform();
            if (parentBone?.parentIndex >= 0)
            {
                parentBone = bones[(int)parentBone?.parentIndex];
            }
            else
            {
                parentBone = null;
            }
        } while (parentBone != null);

        return res;
    }

    private unsafe void ProcessMesh(ref FlverMesh mesh, BinaryReaderEx br, int version,
        Span<FlverVertexBuffer> buffers, Span<FlverBufferLayout> layouts,
        Span<FlverFaceset> facesets, FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant> dest)
    {
        dest.Material = GPUMaterials[mesh.materialIndex];

        Span<int> facesetIndices = stackalloc int[mesh.facesetCount];
        br.StepIn(mesh.facesetIndicesOffset);
        for (var i = 0; i < mesh.facesetCount; i++)
        {
            facesetIndices[i] = br.ReadInt32();
        }

        br.StepOut();

        Span<int> vertexBufferIndices = stackalloc int[mesh.vertexBufferCount];
        br.StepIn(mesh.vertexBufferIndicesOffset);
        for (var i = 0; i < mesh.vertexBufferCount; i++)
        {
            vertexBufferIndices[i] = br.ReadInt32();
        }

        br.StepOut();
        var vertexCount = mesh.vertexBufferCount > 0 ? buffers[vertexBufferIndices[0]].vertexCount : 0;

        var vSize = dest.Material.VertexSize;
        dest.PickingVertices = Marshal.AllocHGlobal(vertexCount * sizeof(Vector3));
        Span<Vector3> pvhandle = new(dest.PickingVertices.ToPointer(), vertexCount);

        var is32bit = version > 0x20005 && vertexCount > 65535;
        var indicesTotal = 0;
        foreach (var fsidx in facesetIndices)
        {
            indicesTotal += facesets[fsidx].indexCount;
            is32bit = is32bit || facesets[fsidx].indexSize != 16;
        }

        var vbuffersize = (uint)vertexCount * vSize;
        dest.GeomBuffer = indexBufferAllocator(vbuffersize, (uint)indicesTotal * (is32bit ? 4u : 2u), (int)vSize);
        (nint meshVertices, nint meshIndices) = meshResourceMapper(dest.GeomBuffer);

        foreach (var vbi in vertexBufferIndices)
        {
            FlverVertexBuffer vb = buffers[vbi];
            FlverBufferLayout layout = layouts[vb.layoutIndex];
            Span<FlverBufferLayoutMember> layoutmembers = stackalloc FlverBufferLayoutMember[layout.memberCount];
            br.StepIn(layout.membersOffset);
            for (var i = 0; i < layout.memberCount; i++)
            {
                layoutmembers[i] = new FlverBufferLayoutMember(br);
                if (layoutmembers[i].semantic == FLVER.LayoutSemantic.Normal &&
                    (layoutmembers[i].type == FLVER.LayoutType.Byte4B ||
                     layoutmembers[i].type == FLVER.LayoutType.Byte4E))
                {
                    dest.UseNormalWBoneTransform = true;
                }
            }

            br.StepOut();
            if (dest.Material.LayoutType == MeshLayoutType.LayoutSky)
            {
                FillVerticesNormalOnly(br, ref vb, layoutmembers, pvhandle, meshVertices);
            }
            else if (dest.Material.LayoutType == MeshLayoutType.LayoutUV2)
            {
                FillVerticesUV2(br, ref vb, layoutmembers, pvhandle, meshVertices,
                    version >= 0x2000F ? 2048 : 1024);
            }
            else
            {
                FillVerticesStandard(br, ref vb, layoutmembers, pvhandle, meshVertices,
                    version >= 0x2000F ? 2048 : 1024);
            }
        }

        dest.VertexCount = vertexCount;
        dest.MeshFacesets = new List<FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>.FlverSubmeshFaceSet>();

        Span<ushort> fs16 = null;
        Span<int> fs32 = null;
        if (is32bit)
        {
            fs32 = new Span<int>(meshIndices.ToPointer(), indicesTotal);
        }
        else
        {
            fs16 = new Span<ushort>(meshIndices.ToPointer(), indicesTotal);
        }

        var idxoffset = 0;
        foreach (var fsidx in facesetIndices)
        {
            FlverFaceset faceset = facesets[fsidx];
            if (faceset.indexCount == 0)
            {
                continue;
            }

            //At this point they use 32-bit faceset vertex indices
            FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>.FlverSubmeshFaceSet newFaceSet = new()
            {
                BackfaceCulling = faceset.cullBackfaces,
                IsTriangleStrip = faceset.triangleStrip,
                IndexOffset = idxoffset,
                IndexCount = faceset.indexCount,
                Is32Bit = is32bit,
                PickingIndicesCount = 0
            };


            if ((faceset.flags & FLVER2.FaceSet.FSFlags.LodLevel1) > 0)
            {
                newFaceSet.LOD = 1;
                newFaceSet.IsMotionBlur = false;
            }
            else if ((faceset.flags & FLVER2.FaceSet.FSFlags.LodLevel2) > 0)
            {
                newFaceSet.LOD = 2;
                newFaceSet.IsMotionBlur = false;
            }

            if ((faceset.flags & FLVER2.FaceSet.FSFlags.MotionBlur) > 0)
            {
                newFaceSet.IsMotionBlur = true;
            }

            br.StepIn(faceset.indicesOffset);
            for (var i = 0; i < faceset.indexCount; i++)
            {
                if (faceset.indexSize == 16)
                {
                    var idx = br.ReadUInt16();
                    if (is32bit)
                    {
                        fs32[newFaceSet.IndexOffset + i] = idx == 0xFFFF ? -1 : idx;
                    }
                    else
                    {
                        fs16[newFaceSet.IndexOffset + i] = idx;
                    }
                }
                else
                {
                    var idx = br.ReadInt32();
                    if (idx > vertexCount)
                    {
                        fs32[newFaceSet.IndexOffset + i] = -1;
                    }
                    else
                    {
                        fs32[newFaceSet.IndexOffset + i] = idx;
                    }
                }
            }

            br.StepOut();

            dest.MeshFacesets.Add(newFaceSet);
            idxoffset += faceset.indexCount;
        }

        meshResourceUnmapper(dest.GeomBuffer);

        dest.Bounds = boundsConstructor(dest.PickingVertices, dest.VertexCount);

        /*if (CaptureMaterialLayouts)
        {
            lock (_matLayoutLock)
            {
                if (!MaterialLayouts.ContainsKey(dest.Material.MaterialName))
                {
                    MaterialLayouts.Add(dest.Material.MaterialName, Flver.BufferLayouts[mesh.VertexBuffers[0].LayoutIndex]);
                }
            }
        }*/

        if (mesh.dynamic == 0)
        {
            if (dest.UseNormalWBoneTransform)
            {
                dest.Material.SetNormalWBoneTransform(generateSpecializationConstantForBone);
            }
            else if (mesh.defaultBoneIndex != -1 && mesh.defaultBoneIndex < FBones.Count)
            {
                dest.LocalTransform = GetBoneObjectMatrix(FBones[mesh.defaultBoneIndex], FBones);
            }
        }

        Marshal.FreeHGlobal(dest.PickingVertices);
    }

    private bool LoadInternalDeS(AccessLevel al, GameType type)
    {
        if (al == AccessLevel.AccessFull || al == AccessLevel.AccessGPUOptimizedOnly)
        {
            GPUMeshes = new FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>[FlverDeS.Meshes.Count()];
            GPUMaterials = new FlverMaterial<GPUBufferHandle, SpecializationConstant>[FlverDeS.Materials.Count()];
            Bounds = new BoundingBox();
            Bones = FlverDeS.Bones;
            BoneTransforms = new List<Matrix4x4>();
            for (var i = 0; i < Bones.Count; i++)
            {
                //BoneTransforms.Add(FlverDeS.ComputeBoneWorldMatrix(i));
                BoneTransforms.Add(Bones[i].ComputeLocalTransform());
            }

            for (var i = 0; i < FlverDeS.Materials.Count(); i++)
            {
                GPUMaterials[i] = new FlverMaterial<GPUBufferHandle, SpecializationConstant>();
                ProcessMaterial(FlverDeS.Materials[i], GPUMaterials[i], type);
            }

            for (var i = 0; i < FlverDeS.Meshes.Count(); i++)
            {
                GPUMeshes[i] = new FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>();

                FLVER0.Mesh? flverMesh = FlverDeS.Meshes[i];
                ProcessMesh(flverMesh, GPUMeshes[i]);
                if (i == 0)
                {
                    Bounds = GPUMeshes[i].Bounds;
                }
                else
                {
                    Bounds = combineBounds(Bounds, GPUMeshes[i].Bounds);
                }
            }

            BoneTransforms.Clear();
        }

        if (al == AccessLevel.AccessGPUOptimizedOnly)
        {
            Flver = null;
        }

        return true;
    }

    private bool LoadInternal(AccessLevel al, GameType type)
    {
        if (al == AccessLevel.AccessFull || al == AccessLevel.AccessGPUOptimizedOnly)
        {
            GPUMeshes = new FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>[Flver.Meshes.Count()];
            GPUMaterials = new FlverMaterial<GPUBufferHandle, SpecializationConstant>[Flver.Materials.Count()];
            Bounds = new BoundingBox();
            Bones = Flver.Bones;

            for (var i = 0; i < Flver.Materials.Count(); i++)
            {
                GPUMaterials[i] = new FlverMaterial<GPUBufferHandle, SpecializationConstant>();
                ProcessMaterial(Flver.Materials[i], GPUMaterials[i], type);
            }

            for (var i = 0; i < Flver.Meshes.Count(); i++)
            {
                GPUMeshes[i] = new FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>();
                ProcessMesh(Flver.Meshes[i], GPUMeshes[i]);
                if (i == 0)
                {
                    Bounds = GPUMeshes[i].Bounds;
                }
                else
                {
                    Bounds = combineBounds(Bounds, GPUMeshes[i].Bounds);
                }
            }

            if (GPUMeshes.Any(e => e.UseNormalWBoneTransform))
            {
                StaticBoneBuffer = allocBoneBuffer((uint)Bones.Count);
                useBones();
            }
        }

        if (al == AccessLevel.AccessGPUOptimizedOnly)
        {
            Flver = null;
        }

        return true;
    }

    // Read only flver loader designed to be very fast at reading with low memory usage
    private bool LoadInternalFast(BinaryReaderEx br, GameType type)
    {
        // Parse header
        br.BigEndian = false;
        br.AssertASCII("FLVER\0");
        br.BigEndian = br.AssertASCII(["L\0", "B\0"]) == "B\0";
        var version = br.AssertInt32([0x20005, 0x20009, 0x2000C, 0x2000D, 0x2000E, 0x2000F, 0x20010, 0x20013,
            0x20014, 0x20016, 0x2001A, 0x2001B]);
        var dataOffset = br.ReadUInt32();
        br.ReadInt32(); // Data length
        var dummyCount = br.ReadInt32();
        var materialCount = br.ReadInt32();
        var boneCount = br.ReadInt32();
        var meshCount = br.ReadInt32();
        var vertexBufferCount = br.ReadInt32();

        // Eat bounding boxes because we compute them ourself
        br.ReadVector3(); // min
        br.ReadVector3(); // max

        br.ReadInt32(); // Face count not including motion blur meshes or degenerate faces
        br.ReadInt32(); // Total face count
        int vertexIndicesSize = br.AssertByte([0, 16, 32]);
        var unicode = br.ReadBoolean();
        br.ReadBoolean(); // unknown
        br.AssertByte(0);
        br.ReadInt32(); // unknown
        var faceSetCount = br.ReadInt32();
        var bufferLayoutCount = br.ReadInt32();
        var textureCount = br.ReadInt32();
        br.ReadByte(); // unknown
        br.ReadByte(); // unknown
        br.AssertByte(0);
        br.AssertByte(0);
        br.AssertInt32(0);
        br.AssertInt32(0);
        //br.AssertInt32(0, 1, 2, 3, 4);  // unknown
        br.ReadInt32(); // unknown
        br.AssertInt32(0);
        br.AssertInt32(0);
        br.AssertInt32([0x0, 0x10]);
        br.AssertInt32(0);
        br.AssertInt32(0);

        // Don't care about dummies for now so skip them
        br.Position += dummyCount * 64; // 64 bytes per dummy

        // Materials
        Span<FlverMaterialDef> materials = stackalloc FlverMaterialDef[materialCount];
        for (var i = 0; i < materialCount; i++)
        {
            materials[i] = new FlverMaterialDef(br);
        }

        // bones
        FBones = new List<FlverBone>();
        for (var i = 0; i < boneCount; i++)
        {
            FBones.Add(new FlverBone(br));
        }

        // Meshes
        Span<FlverMesh> meshes = stackalloc FlverMesh[meshCount];
        for (var i = 0; i < meshCount; i++)
        {
            meshes[i] = new FlverMesh(br);
        }

        // Facesets
        Span<FlverFaceset> facesets = stackalloc FlverFaceset[faceSetCount];
        for (var i = 0; i < faceSetCount; i++)
        {
            facesets[i] = new FlverFaceset(br, version, vertexIndicesSize, dataOffset);
        }

        // Vertex buffers
        Span<FlverVertexBuffer> vertexbuffers = stackalloc FlverVertexBuffer[vertexBufferCount];
        for (var i = 0; i < vertexBufferCount; i++)
        {
            vertexbuffers[i] = new FlverVertexBuffer(br, dataOffset);
        }

        // Buffer layouts
        Span<FlverBufferLayout> bufferLayouts = stackalloc FlverBufferLayout[bufferLayoutCount];
        for (var i = 0; i < bufferLayoutCount; i++)
        {
            bufferLayouts[i] = new FlverBufferLayout(br);
        }

        // Textures
        Span<FlverTexture> textures = stackalloc FlverTexture[textureCount];
        for (var i = 0; i < textureCount; i++)
        {
            textures[i] = new FlverTexture(br);
        }

        // Process the materials and meshes
        GPUMeshes = new FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>[meshCount];
        GPUMaterials = new FlverMaterial<GPUBufferHandle, SpecializationConstant>[materialCount];
        Bounds = new BoundingBox();
        //Bones = Flver.Bones;

        for (var i = 0; i < materialCount; i++)
        {
            GPUMaterials[i] = new FlverMaterial<GPUBufferHandle, SpecializationConstant>();
            ProcessMaterial(GPUMaterials[i], type, br, ref materials[i], textures, unicode);
        }

        for (var i = 0; i < meshCount; i++)
        {
            GPUMeshes[i] = new FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>();
            ProcessMesh(ref meshes[i], br, version, vertexbuffers, bufferLayouts, facesets, GPUMeshes[i]);
            if (i == 0)
            {
                Bounds = GPUMeshes[i].Bounds;
            }
            else
            {
                Bounds = combineBounds(Bounds, GPUMeshes[i].Bounds);
            }
        }

        if (GPUMeshes.Any(e => e.UseNormalWBoneTransform))
        {
            StaticBoneBuffer = allocBoneBuffer((uint)FBones.Count);
            useBones();
        }

        return true;
    }

    public class FlverMaterial<GPUBufferHandle, SpecializationConstant, TextureResourceHandle, TextureResource> : IDisposable
    {
        public enum TextureType
        {
            AlbedoTextureResource = 0,
            AlbedoTextureResource2,
            NormalTextureResource,
            NormalTextureResource2,
            SpecularTextureResource,
            SpecularTextureResource2,
            ShininessTextureResource,
            ShininessTextureResource2,
            BlendmaskTextureResource,
            TextureResourceCount
        }

        public readonly bool[] TextureResourceFilled = new bool[(int)TextureType.TextureResourceCount];

        public readonly TextureResourceHandle?[] TextureResources =
            new TextureResourceHandle[(int)TextureType.TextureResourceCount];

        private bool _setHasIndexNoWeightTransform;

        private bool _setNormalWBoneTransform;

        private bool disposedValue;
        public MeshLayoutType LayoutType;
        public GPUBufferHandle MaterialBuffer;
        public Material MaterialData;
        public string MaterialName;

        public string ShaderName;
        public List<SpecializationConstant> SpecializationConstants;
        public uint VertexSize;

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool GetHasIndexNoWeightTransform()
        {
            return _setHasIndexNoWeightTransform;
        }

        public void SetHasIndexNoWeightTransform()
        {
            if (!_setHasIndexNoWeightTransform)
            {
                _setHasIndexNoWeightTransform = true;
            }
        }

        public bool GetNormalWBoneTransform()
        {
            return _setNormalWBoneTransform;
        }

        public void SetNormalWBoneTransform(Func<int, bool, SpecializationConstant> generator)
        {
            if (!_setNormalWBoneTransform)
            {
                SpecializationConstants.Add(generator(50, true));
                _setNormalWBoneTransform = true;
            }
        }

        private void SetMaterialTexture(TextureType textureType, ref ushort matTex, ushort defaultTex)
        {
           TextureResourceHandle? handle = TextureResources[(int)textureType];
            if (handle != null && handle.IsLoaded)
            {
                TextureResource? res = handle.Get();
                if (res != null && res.GPUTexture != null)
                {
                    matTex = (ushort)handle.Get().GPUTexture.TexHandle;
                }
                else
                {
                    matTex = defaultTex;
                }
            }
            else
            {
                matTex = defaultTex;
            }
        }

        public void ReleaseTextures()
        {
            for (var i = 0; i < (int)TextureType.TextureResourceCount; i++)
            {
                TextureResources[i]?.Release();
                TextureResources[i] = null;
            }
        }

        public void UpdateMaterial()
        {
            SetMaterialTexture(TextureType.AlbedoTextureResource, ref MaterialData.colorTex, 0);
            SetMaterialTexture(TextureType.AlbedoTextureResource2, ref MaterialData.colorTex2, 0);
            SetMaterialTexture(TextureType.NormalTextureResource, ref MaterialData.normalTex, 1);
            SetMaterialTexture(TextureType.NormalTextureResource2, ref MaterialData.normalTex2, 1);
            SetMaterialTexture(TextureType.SpecularTextureResource, ref MaterialData.specTex, 2);
            SetMaterialTexture(TextureType.SpecularTextureResource2, ref MaterialData.specTex2, 2);
            SetMaterialTexture(TextureType.ShininessTextureResource, ref MaterialData.shininessTex, 2);
            SetMaterialTexture(TextureType.ShininessTextureResource2, ref MaterialData.shininessTex2, 2);
            SetMaterialTexture(TextureType.BlendmaskTextureResource, ref MaterialData.blendMaskTex, 0);

            Renderer.Scene.Renderer.AddBackgroundUploadTask((d, cl) =>
            {
                Tracy.___tracy_c_zone_context ctx = Tracy.TracyCZoneN(1, @"Material upload");
                MaterialBuffer.FillBuffer(d, cl, ref MaterialData);
                Tracy.TracyCZoneEnd(ctx);
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    MaterialBuffer.Dispose();
                }

                ReleaseTextures();
                disposedValue = true;
            }
        }

        ~FlverMaterial()
        {
            Dispose(false);
        }
    }

    public class FlverSubmesh <IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant>
    {
        public Matrix4x4 LocalTransform = Matrix4x4.Identity;

        // This is native because using managed arrays causes a weird memory leak
        public IntPtr PickingVertices = IntPtr.Zero;

        public List<FlverSubmeshFaceSet> MeshFacesets { get; set; } = new();

        public IndexBufferHandle GeomBuffer { get; set; }

        public int VertexCount { get; set; }

        public BoundingBox Bounds { get; set; }

        // Use the w field in the normal as an index to a bone that has a transform
        public bool UseNormalWBoneTransform { get; set; }

        public int DefaultBoneIndex { get; set; } = -1;

        public FlverMaterial<GPUBufferHandle, SpecializationConstant> Material { get; set; }

        public struct FlverSubmeshFaceSet
        {
            public int IndexCount;
            public int IndexOffset;

            public int PickingIndicesCount;

            //public IntPtr PickingIndices;
            public bool BackfaceCulling;
            public bool IsTriangleStrip;
            public byte LOD;
            public bool IsMotionBlur;
            public bool Is32Bit;
        }
    }

    protected struct FlverMaterialDef
    {
        public uint nameOffset;
        public readonly uint mtdOffset;
        public readonly int textureCount;
        public readonly int textureIndex;
        public int flags;
        public int gxOffset;

        public FlverMaterialDef(BinaryReaderEx br)
        {
            nameOffset = br.ReadUInt32();
            mtdOffset = br.ReadUInt32();
            textureCount = br.ReadInt32();
            textureIndex = br.ReadInt32();
            flags = br.ReadInt32();
            gxOffset = br.ReadInt32();
            br.ReadInt32(); // unknown
            br.AssertInt32(0);
        }
    }

    protected struct FlverBone
    {
        public readonly Vector3 position;
        public uint nameOffset;
        public readonly Vector3 rotation;
        public readonly short parentIndex;
        public short childIndex;
        public readonly Vector3 scale;
        public short nextSiblingIndex;
        public short previousSiblingIndex;
        public Vector3 boundingBoxMin;
        public Vector3 boundingBoxMax;

        public Matrix4x4 ComputeLocalTransform()
        {
            return Matrix4x4.CreateScale(scale)
                   * Matrix4x4.CreateRotationX(rotation.X)
                   * Matrix4x4.CreateRotationZ(rotation.Z)
                   * Matrix4x4.CreateRotationY(rotation.Y)
                   * Matrix4x4.CreateTranslation(position);
        }

        public FlverBone(BinaryReaderEx br)
        {
            position = br.ReadVector3();
            nameOffset = br.ReadUInt32();
            rotation = br.ReadVector3();
            parentIndex = br.ReadInt16();
            childIndex = br.ReadInt16();
            scale = br.ReadVector3();
            nextSiblingIndex = br.ReadInt16();
            previousSiblingIndex = br.ReadInt16();
            boundingBoxMin = br.ReadVector3();
            br.ReadInt32(); // unknown
            boundingBoxMax = br.ReadVector3();
            br.Position += 0x34;
        }
    }

    protected struct FlverMesh
    {
        public readonly int dynamic;
        public readonly int materialIndex;
        public readonly int defaultBoneIndex;
        public int boneCount;
        public readonly int facesetCount;
        public readonly uint facesetIndicesOffset;
        public readonly int vertexBufferCount;
        public readonly uint vertexBufferIndicesOffset;

        public FlverMesh(BinaryReaderEx br)
        {
            dynamic = br.AssertInt32([0, 1]);
            materialIndex = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            defaultBoneIndex = br.ReadInt32();
            boneCount = br.ReadInt32();
            br.ReadInt32(); // bb offset
            br.ReadInt32(); // bone offset
            facesetCount = br.ReadInt32();
            facesetIndicesOffset = br.ReadUInt32();
            vertexBufferCount = br.AssertInt32([0, 1, 2, 3]);
            vertexBufferIndicesOffset = br.ReadUInt32();
        }
    }

    protected struct FlverFaceset
    {
        public readonly FLVER2.FaceSet.FSFlags flags;
        public readonly bool triangleStrip;
        public readonly bool cullBackfaces;
        public readonly int indexCount;
        public readonly uint indicesOffset;
        public readonly int indexSize;

        public FlverFaceset(BinaryReaderEx br, int version, int headerIndexSize, uint dataOffset)
        {
            flags = (FLVER2.FaceSet.FSFlags)br.ReadUInt32();
            triangleStrip = br.ReadBoolean();
            cullBackfaces = br.ReadBoolean();
            br.ReadByte(); // unk
            br.ReadByte(); // unk
            indexCount = br.ReadInt32();
            indicesOffset = br.ReadUInt32() + dataOffset;
            indexSize = 0;
            if (version > 0x20005)
            {
                br.ReadInt32(); // Indices length
                br.AssertInt32(0);
                indexSize = br.AssertInt32([0, 16, 32]);
                br.AssertInt32(0);
            }

            if (indexSize == 0)
            {
                indexSize = headerIndexSize;
            }
        }
    }

    protected struct FlverVertexBuffer
    {
        public int bufferIndex;
        public readonly int layoutIndex;
        public int vertexSize;
        public readonly int vertexCount;
        public readonly uint bufferOffset;

        public FlverVertexBuffer(BinaryReaderEx br, uint dataOffset)
        {
            bufferIndex = br.ReadInt32();
            layoutIndex = br.ReadInt32();
            vertexSize = br.ReadInt32();
            vertexCount = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            br.ReadInt32(); // Buffer length
            bufferOffset = br.ReadUInt32() + dataOffset;
        }
    }

    protected struct FlverBufferLayoutMember
    {
        public readonly int unk00;
        public readonly FLVER.LayoutType type;
        public readonly FLVER.LayoutSemantic semantic;
        public readonly int index;

        public FlverBufferLayoutMember(BinaryReaderEx br)
        {
            unk00 = br.ReadInt32(); // unk
            br.ReadInt32(); // struct offset
            type = br.ReadEnum32<FLVER.LayoutType>();
            semantic = br.ReadEnum32<FLVER.LayoutSemantic>();
            index = br.ReadInt32();
        }
    }

    protected struct FlverBufferLayout
    {
        public readonly int memberCount;
        public readonly uint membersOffset;

        public FlverBufferLayout(BinaryReaderEx br)
        {
            memberCount = br.ReadInt32();
            br.AssertInt32(0);
            br.AssertInt32(0);
            membersOffset = br.ReadUInt32();
        }
    }

    protected struct FlverTexture
    {
        public readonly uint pathOffset;
        public readonly uint typeOffset;
        public Vector2 scale;

        public FlverTexture(BinaryReaderEx br)
        {
            pathOffset = br.ReadUInt32();
            typeOffset = br.ReadUInt32();
            scale = br.ReadVector2();

            // unks
            br.ReadByte();
            br.ReadBoolean();
            br.AssertByte(0);
            br.AssertByte(0);
            br.ReadSingle();
            br.ReadSingle();
            br.ReadSingle();
        }
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
            }

            if (GPUMaterials != null)
            {
                foreach (FlverMaterial<GPUBufferHandle, SpecializationConstant> m in GPUMaterials)
                {
                    m.Dispose();
                }
            }

            if (GPUMeshes != null)
            {
                foreach (FlverSubmesh<IndexBufferHandle, BoundingBox, GPUBufferHandle, SpecializationConstant> m in GPUMeshes)
                {
                    m.GeomBuffer.Dispose();
                    //Marshal.FreeHGlobal(m.PickingVertices);
                }
            }

            if (StaticBoneBuffer != null)
            {
                StaticBoneBuffer.Dispose();
            }

            disposedValue = true;
        }
    }

    ~FastFlver()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        GC.SuppressFinalize(this);
    }

    #endregion
}
