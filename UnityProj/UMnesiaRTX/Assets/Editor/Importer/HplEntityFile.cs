using System.Collections.Generic;
using System.Xml;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Importer
{
    /// <summary>
    /// The parts of an HPL2 .ent file the level import needs (EntityLoader_Object.cpp).
    /// All transforms are relative to the entity and in HPL2 space.
    /// </summary>
    public class HplEntityFile
    {
        public class SubMesh
        {
            public string Name;
            public int Id;
            public Vector3 Position, Rotation, Scale;
            public string Material;
        }

        public class Shape
        {
            public int Id;
            public string Type; // box, sphere, cylinder, capsule
            public Vector3 Size; // "Scale": box size, sphere radii, cylinder/capsule (radius, height)
            public Vector3 RelativePosition, RelativeRotation; // relative to the body
        }

        public class Body
        {
            public int Id;
            public string Name;
            public Vector3 Position, Rotation;
            public float Mass;
            public string Material;
            public bool CollideCharacter, CollideNonCharacter, HasGravity, BlocksSound, PushedByCharacterGravity, CanAttachCharacter;
            public float LinearDamping, AngularDamping;
            public readonly List<int> ShapeIds = new();
            public readonly List<int> ChildIds = new();
        }

        public string Path;
        public string MeshFile;
        public bool HasBones;
        public string EntityType = "", EntitySubType = "";
        public List<HplVariable> Variables = new();
        public readonly List<SubMesh> SubMeshes = new();
        public readonly List<Shape> Shapes = new();
        public readonly List<Body> Bodies = new();
        /// <summary>Lights, billboards, particle systems and sounds that come with the entity.</summary>
        public readonly List<XmlElement> WorldEntities = new();

        public static HplEntityFile Load(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            var root = doc.DocumentElement;
            var ent = new HplEntityFile { Path = path };

            var vars = root.Child("UserDefinedVariables");
            if (vars != null)
            {
                ent.EntityType = vars.Str("EntityType");
                ent.EntitySubType = vars.Str("EntitySubType");
                ent.Variables = HplXml.Variables(vars);
            }

            var model = root.Child("ModelData");
            if (model == null) return ent;

            var mesh = model.Child("Mesh");
            if (mesh != null)
            {
                ent.MeshFile = mesh.Str("Filename");
                foreach (var e in mesh.Children())
                {
                    if (e.Name != "SubMesh") continue;
                    ent.SubMeshes.Add(new SubMesh
                    {
                        Name = e.Str("Name"),
                        Id = e.Int("ID", e.Int("SubMeshID", -1)),
                        Position = e.Vec3("WorldPos"),
                        Rotation = e.Vec3("Rotation"),
                        Scale = e.Vec3("Scale", Vector3.one),
                        Material = e.Str("Material"),
                    });
                }
            }

            ent.HasBones = model.Child("Bones")?.HasChildNodes == true;

            foreach (var e in model.Child("Entities").Children())
                ent.WorldEntities.Add(e);

            foreach (var e in model.Child("Shapes").Children())
            {
                ent.Shapes.Add(new Shape
                {
                    Id = e.Int("ID"),
                    Type = e.Str("ShapeType").ToLowerInvariant(),
                    Size = e.Vec3("Scale", Vector3.one),
                    RelativePosition = e.Vec3("RelativeTranslation"),
                    RelativeRotation = e.Vec3("RelativeRotation"),
                });
            }

            foreach (var e in model.Child("Bodies").Children())
            {
                var body = new Body
                {
                    Id = e.Int("ID"),
                    Name = e.Str("Name"),
                    Position = e.Vec3("WorldPos"),
                    Rotation = e.Vec3("Rotation"),
                    Mass = e.Float("Mass", 1f),
                    Material = e.Str("Material"),
                    CollideCharacter = e.Bool("CollideCharacter", true),
                    CollideNonCharacter = e.Bool("CollideNonCharacter", true),
                    HasGravity = e.Bool("HasGravity", true),
                    BlocksSound = e.Bool("BlocksSound"),
                    PushedByCharacterGravity = e.Bool("PushedByCharacterGravity"),
                    CanAttachCharacter = e.Bool("CanAttachCharacter"),
                    LinearDamping = e.Float("LinearDamping"),
                    AngularDamping = e.Float("AngularDamping"),
                };
                foreach (var child in e.Children())
                {
                    if (child.Name == "Shape") body.ShapeIds.Add(child.Int("ID"));
                    else if (child.Name == "Children")
                        foreach (var c in child.Children()) body.ChildIds.Add(c.Int("ID"));
                }
                ent.Bodies.Add(body);
            }
            return ent;
        }
    }
}
