using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Unity.Collections;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Binary storage of generated relief meshes. A text .asset (the project serializes as ForceText) writes the vertex
    /// buffer as hex, twice its size; these files hold the compact buffer deflated. Positions stay Float32 (joints are
    /// millimetres, ray tracing builds from them), normals and tangents SNorm16, colours UNorm8, UVs without unused zw.
    /// </summary>
    public static class HplReliefMeshFile
    {
        public const string Extension = ".hplrelief";
        const int Magic = 0x4D52_4C48; // "HLRM"
        const int FormatVersion = 1;

        struct Attribute
        {
            public VertexAttribute attribute;
            public VertexAttributeFormat format;
            public int dimension;
        }

        public static void Write(Mesh mesh, string absolutePath) => File.WriteAllBytes(absolutePath, Encode(mesh));

        public static byte[] Encode(Mesh mesh)
        {
            int count = mesh.vertexCount;
            var positions = mesh.vertices; var normals = mesh.normals; var tangents = mesh.tangents; var colors = mesh.colors32;
            var uvs = new List<Vector4>[4];
            var attributes = new List<Attribute> { new() { attribute = VertexAttribute.Position, format = VertexAttributeFormat.Float32, dimension = 3 } };
            if (normals.Length == count) attributes.Add(new() { attribute = VertexAttribute.Normal, format = VertexAttributeFormat.SNorm16, dimension = 4 });
            if (tangents.Length == count) attributes.Add(new() { attribute = VertexAttribute.Tangent, format = VertexAttributeFormat.SNorm16, dimension = 4 });
            if (colors.Length == count) attributes.Add(new() { attribute = VertexAttribute.Color, format = VertexAttributeFormat.UNorm8, dimension = 4 });
            for (int channel = 0; channel < uvs.Length; channel++)
            {
                int dimension = mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0 + channel);
                if (dimension == 0) continue;
                uvs[channel] = new List<Vector4>();
                mesh.GetUVs(channel, uvs[channel]);
                // Unity stores the dimension the builder set (4); the HPL sources only fill xy.
                if (dimension > 2 && uvs[channel].TrueForAll(v => v.z == 0 && v.w == 0)) dimension = 2;
                attributes.Add(new() { attribute = VertexAttribute.TexCoord0 + channel, format = VertexAttributeFormat.Float32, dimension = dimension });
            }

            using var raw = new MemoryStream();
            using (var writer = new BinaryWriter(raw, System.Text.Encoding.UTF8, true))
            {
                writer.Write(Magic); writer.Write(FormatVersion);
                writer.Write(count); writer.Write(attributes.Count);
                foreach (var a in attributes) { writer.Write((int)a.attribute); writer.Write((int)a.format); writer.Write(a.dimension); }
                for (int v = 0; v < count; v++)
                    foreach (var a in attributes)
                        switch (a.attribute)
                        {
                            case VertexAttribute.Position: writer.Write(positions[v].x); writer.Write(positions[v].y); writer.Write(positions[v].z); break;
                            case VertexAttribute.Normal: SNorm16(writer, normals[v]); SNorm16(writer, 0); break;
                            case VertexAttribute.Tangent: SNorm16(writer, tangents[v]); SNorm16(writer, tangents[v].w); break;
                            case VertexAttribute.Color: writer.Write(colors[v].r); writer.Write(colors[v].g); writer.Write(colors[v].b); writer.Write(colors[v].a); break;
                            default:
                                var uv = uvs[a.attribute - VertexAttribute.TexCoord0][v];
                                for (int k = 0; k < a.dimension; k++) writer.Write(uv[k]);
                                break;
                        }
                bool wide = count > 65535;
                writer.Write(wide); writer.Write(mesh.subMeshCount);
                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    var indices = mesh.GetIndices(sub);
                    writer.Write((int)mesh.GetTopology(sub)); writer.Write(indices.Length);
                    foreach (int index in indices) { if (wide) writer.Write(index); else writer.Write((ushort)index); }
                }
                var bounds = mesh.bounds;
                for (int k = 0; k < 3; k++) writer.Write(bounds.center[k]);
                for (int k = 0; k < 3; k++) writer.Write(bounds.extents[k]);
            }
            using var packed = new MemoryStream();
            using (var deflate = new DeflateStream(packed, System.IO.Compression.CompressionLevel.Optimal, true)) raw.WriteTo(deflate);
            return packed.ToArray();
        }

        static void SNorm16(BinaryWriter writer, Vector3 v) { SNorm16(writer, v.x); SNorm16(writer, v.y); SNorm16(writer, v.z); }
        static void SNorm16(BinaryWriter writer, float value) => writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(value, -1, 1) * 32767));

        public static Mesh Decode(byte[] bytes, string name)
        {
            using var raw = new MemoryStream();
            using (var deflate = new DeflateStream(new MemoryStream(bytes), CompressionMode.Decompress)) deflate.CopyTo(raw);
            raw.Position = 0;
            using var reader = new BinaryReader(raw);
            if (reader.ReadInt32() != Magic || reader.ReadInt32() != FormatVersion) throw new InvalidDataException("Not a relief mesh: " + name);
            int count = reader.ReadInt32();
            var descriptors = new VertexAttributeDescriptor[reader.ReadInt32()];
            int stride = 0;
            for (int i = 0; i < descriptors.Length; i++)
            {
                descriptors[i] = new VertexAttributeDescriptor((VertexAttribute)reader.ReadInt32(), (VertexAttributeFormat)reader.ReadInt32(), reader.ReadInt32());
                stride += descriptors[i].dimension * (descriptors[i].format switch
                {
                    VertexAttributeFormat.Float32 => 4, VertexAttributeFormat.SNorm16 => 2, VertexAttributeFormat.UNorm8 => 1,
                    _ => throw new InvalidDataException("Relief vertex format: " + descriptors[i].format)
                });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertexBufferParams(count, descriptors);
            using (var vertices = new NativeArray<byte>(reader.ReadBytes(count * stride), Allocator.Temp))
                mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length, 0, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            bool wide = reader.ReadBoolean();
            int subMeshes = reader.ReadInt32();
            var topology = new MeshTopology[subMeshes];
            var lists = new int[subMeshes][];
            for (int sub = 0; sub < subMeshes; sub++)
            {
                topology[sub] = (MeshTopology)reader.ReadInt32();
                lists[sub] = new int[reader.ReadInt32()];
                for (int i = 0; i < lists[sub].Length; i++) lists[sub][i] = wide ? reader.ReadInt32() : reader.ReadUInt16();
            }
            mesh.indexFormat = wide ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.subMeshCount = subMeshes;
            for (int sub = 0; sub < subMeshes; sub++) mesh.SetIndices(lists[sub], topology[sub], sub);
            var center = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            var extents = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            mesh.bounds = new Bounds(center, extents * 2);
            return mesh;
        }
    }

    /// <summary>Resources.Load("HplReliefMeshes/&lt;id&gt;_detail") finds the imported Mesh as before.</summary>
    [ScriptedImporter(1, "hplrelief")]
    public sealed class HplReliefMeshImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var mesh = HplReliefMeshFile.Decode(File.ReadAllBytes(ctx.assetPath), Path.GetFileNameWithoutExtension(ctx.assetPath));
            ctx.AddObjectToAsset("mesh", mesh);
            ctx.SetMainObject(mesh);
        }
    }
}
