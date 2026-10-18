using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// Reader of the HPL2 mesh cache (MeshLoaderMSH.cpp, MSH v7, no skeleton). The shipped game loads the .msh next to
    /// a .dae whenever it exists (Main ForceCacheLoadingAndSkipSaving, true by default: LuxConfigHandler.cpp), so the
    /// caches are what the player sees. Vectors and matrices in HPL2 space. Never modifies the game cache.
    /// </summary>
    public static class HplMsh
    {
        public sealed class SubMesh
        {
            public string Name;
            /// <summary>The sub mesh matrix (HPL2 space, the node scale baked into the vertices).</summary>
            public Matrix4x4 Local;
            public Vector3[] Positions, Normals;
            public int[] Indices;
        }

        static string ReadString(BinaryReader r)
        {
            var bytes = new List<byte>();
            for (byte b; (b = r.ReadByte()) != 0;) bytes.Add(b);
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        static Matrix4x4 ReadMatrix(BinaryReader r)
        {
            var m = new Matrix4x4();
            for (int row = 0; row < 4; row++)
                for (int col = 0; col < 4; col++) m[row, col] = r.ReadSingle();
            return m;
        }

        static void ReadNode(BinaryReader r, Matrix4x4 parent, Dictionary<string, Matrix4x4> nodes)
        {
            string name = ReadString(r);
            var world = parent * ReadMatrix(r);
            if (!nodes.ContainsKey(name)) nodes[name] = world;
            r.ReadInt32(); // custom flags
            int children = r.ReadInt32();
            for (int i = 0; i < children; i++) ReadNode(r, world, nodes);
        }

        public static List<SubMesh> Read(string path, out Dictionary<string, Matrix4x4> nodes)
        {
            using var r = new BinaryReader(File.OpenRead(path));
            if (r.ReadInt32() != 0x76034569 || r.ReadInt32() != 7)
                throw new InvalidDataException("Expected MSH v7");
            int count = r.ReadInt32();
            r.ReadInt32(); // animation count
            if (r.ReadBoolean()) throw new NotSupportedException("Skeleton audit is not implemented");
            nodes = new Dictionary<string, Matrix4x4>();
            int roots = r.ReadInt32();
            for (int i = 0; i < roots; i++) ReadNode(r, Matrix4x4.identity, nodes);
            var meshes = new List<SubMesh>();
            for (int i = 0; i < count; i++)
            {
                var mesh = new SubMesh { Name = ReadString(r) };
                ReadString(r); // material
                var local = ReadMatrix(r);
                mesh.Local = local;
                if (!nodes.ContainsKey(mesh.Name)) nodes[mesh.Name] = local;
                r.ReadBytes(12); // model scale
                r.ReadBoolean();
                int colliders = r.ReadInt32();
                r.ReadBytes(colliders * (2 + 64 + 12 + 1));
                int pairs = r.ReadInt32();
                r.ReadBytes(pairs * 12);
                int vertices = r.ReadInt32(), arrays = r.ReadInt32();
                for (int a = 0; a < arrays; a++)
                {
                    int type = r.ReadInt16(), format = r.ReadInt16();
                    r.ReadInt32();
                    int elements = r.ReadInt32();
                    if ((type == 0 || type == 1) && format == 0 && elements >= 3)
                    {
                        var data = new Vector3[vertices];
                        for (int v = 0; v < vertices; v++)
                        {
                            data[v] = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
                            r.ReadBytes((elements - 3) * 4);
                        }
                        if (type == 1) mesh.Positions = data; else mesh.Normals = data;
                    }
                    else r.ReadBytes(vertices * elements * (format == 2 ? 1 : 4));
                }
                mesh.Indices = new int[r.ReadInt32()];
                for (int j = 0; j < mesh.Indices.Length; j++) mesh.Indices[j] = r.ReadInt32();
                meshes.Add(mesh);
            }
            return meshes;
        }

    }
}
