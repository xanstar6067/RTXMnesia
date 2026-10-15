using RTXMnesia.Dynamics;
using RTXMnesia.Importer;
using RTXMnesia.World;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RTXMnesia.EditorDev
{
    /// <summary>
    /// Joint checks on a synthetic scene (no game data): a hinge (HPL2 angle sign vs HingeJoint, limits [0°, 90°],
    /// closing to [0°, 2°]) and a slider (limits [0, 0.5]). Results in Logs/AmnesiaImport.log.
    /// Batch: -executeMethod RTXMnesia.EditorDev.PhysicsTests.RunBatch
    /// </summary>
    public static class PhysicsTests
    {
        public static void RunBatch()
        {
            var log = new ImportLog();
            var mode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Hinge(log);
                Slider(log);
            }
            finally
            {
                Physics.simulationMode = mode;
            }
            log.Save("Physics tests");
        }

        static (GameObject entity, AmnesiaBody body) Entity(string name, Vector3 position, Vector3 boxSize)
        {
            var entity = new GameObject(name);
            entity.transform.position = position;
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(entity.transform, false);
            var body = bodyGo.AddComponent<AmnesiaBody>();
            body.mass = 2f;
            body.hasGravity = false;
            var shape = new GameObject("Shape");
            shape.transform.SetParent(bodyGo.transform, false);
            shape.AddComponent<BoxCollider>().size = boxSize;
            return (entity, body);
        }

        static void Run(HplPhysicsWorld world, int steps, System.Action perStep)
        {
            for (int i = 0; i < steps; i++)
            {
                perStep?.Invoke();
                world.Step(1f / 60f);
            }
        }

        static void Hinge(ImportLog log)
        {
            var (entity, body) = Entity("Door", new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 0.1f));
            var jointGo = new GameObject("Joint");
            jointGo.transform.SetParent(entity.transform, false);
            var data = jointGo.AddComponent<AmnesiaJoint>();
            data.type = HplJointType.Hinge;
            data.jointName = "Door_Hinge";
            data.childBody = body;
            data.pivot = new Vector3(-0.5f, 0f, 0f);
            data.pinDir = Vector3.up;
            data.minAngle = 0f;
            data.maxAngle = 90f * Mathf.Deg2Rad;

            var world = new HplPhysicsWorld();
            world.AddEntity(entity.transform, null, false);
            var joint = world.Joints[0];
            var pb = joint.Child;
            var hinge = (HingeJoint)joint.Joint;

            // A small free turn first: the sign relation of the two angles.
            hinge.useLimits = false;
            pb.Rigidbody.angularVelocity = Vector3.up * 0.5f;
            Run(world, 10, null);
            log.Info($"Hinge sign: HPL2 angle {joint.GetAngle() * Mathf.Rad2Deg:0.00}°, HingeJoint.angle {hinge.angle:0.00}° " +
                     $"(rotation +Y: AngleAxis sense) → {(Mathf.Sign(joint.GetAngle()) == Mathf.Sign(hinge.angle) ? "same sense (as ApplyLimits assumes)" : "OPPOSITE: fix ApplyLimits")}");

            // Back to 0 and limited.
            pb.SetPose(body.transform.parent.TransformPoint(Vector3.zero), Quaternion.identity);
            pb.Rigidbody.angularVelocity = Vector3.zero;
            joint.SetMaxAngle(1f);
            joint.SetMaxAngle(90f * Mathf.Deg2Rad);
            Vector3 pin = joint.PinDir;
            Run(world, 120, () => pb.AddTorque(pin * 20f));
            log.Info($"Hinge torque +pin 2 s: angle {joint.GetAngle() * Mathf.Rad2Deg:0.0}° (expected ~90, max limit)");
            Run(world, 180, () => pb.AddTorque(-pin * 20f));
            log.Info($"Hinge torque -pin 3 s: angle {joint.GetAngle() * Mathf.Rad2Deg:0.0}° (expected ~0, min limit)");
            joint.SetMaxAngle(2f * Mathf.Deg2Rad);
            Run(world, 120, () => pb.AddTorque(pin * 20f));
            log.Info($"Hinge closed [0°, 2°], torque +pin 2 s: angle {joint.GetAngle() * Mathf.Rad2Deg:0.0}° (expected ≤ 2)");

            // Soft limit past ±180°: [-360°, 120°] behaves as max 120°.
            joint.SetMinAngle(-2f * Mathf.PI);
            joint.SetMaxAngle(120f * Mathf.Deg2Rad);
            Run(world, 180, () => pb.AddTorque(pin * 20f));
            log.Info($"Hinge soft limit 120°, torque +pin 3 s: angle {joint.GetAngle() * Mathf.Rad2Deg:0.0}° (expected ~120)");
            Object.DestroyImmediate(entity);
        }

        static void Slider(ImportLog log)
        {
            var (entity, body) = Entity("Drawer", new Vector3(5f, 1f, 0f), new Vector3(0.5f, 0.2f, 0.5f));
            var jointGo = new GameObject("Joint");
            jointGo.transform.SetParent(entity.transform, false);
            var data = jointGo.AddComponent<AmnesiaJoint>();
            data.type = HplJointType.Slider;
            data.jointName = "Drawer_Slider";
            data.childBody = body;
            data.pinDir = Vector3.forward;
            data.minDistance = 0f;
            data.maxDistance = 0.5f;

            var world = new HplPhysicsWorld();
            world.AddEntity(entity.transform, null, false);
            var joint = world.Joints[0];
            var pb = joint.Child;
            Vector3 pin = joint.PinDir;
            Run(world, 120, () => pb.AddForce(pin * 10f));
            log.Info($"Slider force +pin 2 s: distance {joint.GetDistance():0.000} (expected ~0.5)");
            Run(world, 180, () => pb.AddForce(-pin * 10f));
            log.Info($"Slider force -pin 3 s: distance {joint.GetDistance():0.000} (expected ~0)");
            Object.DestroyImmediate(entity);
        }
    }
}
