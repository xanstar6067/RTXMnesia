using UnityEngine;

namespace RTXMnesia.World
{
    public enum HplJointType { Hinge, Ball, Slider, Screw }

    /// <summary>
    /// A joint of an entity (.ent &lt;JointHinge|JointBall|JointSlider|JoinScrew&gt;, EntityLoader_Object.cpp CreateJoint and
    /// SetJointProperties). Data only, in Unity space relative to the entity: the runtime joint is created by the map
    /// (RTXMnesia.Dynamics.HplJoint). Angles in radians, distances in meters, as the HPL2 setters take them.
    /// </summary>
    public class AmnesiaJoint : MonoBehaviour
    {
        public HplJointType type;
        [Tooltip("HPL2 joint name: <entity>_<joint name>.")]
        public string jointName;
        public int hplId = -1;
        [Tooltip("Null: the joint is attached to the world (ConnectedParentBodyID <= 0).")]
        public AmnesiaBody parentBody;
        public AmnesiaBody childBody;
        [Tooltip("Pivot in entity space (Unity).")]
        public Vector3 pivot;
        [Tooltip("Pin direction in entity space (Unity, mirrored like any direction).")]
        public Vector3 pinDir = Vector3.up;

        [Header("Limits")]
        public float minAngle, maxAngle;
        public float maxConeAngle, maxTwistAngle;
        public float minDistance, maxDistance;
        public bool stickyMinLimit, stickyMaxLimit;
        public bool limitAutoSleep;
        public float limitAutoSleepDist = 0.02f;
        public int limitAutoSleepNumSteps = 10;
        public bool collideBodies = true;

        [Header("Breaking")]
        public bool breakable;
        public float breakForce = 1000f;
        public string breakSound = "";

        [Header("Sounds (step 6)")]
        public string moveSound = "";
        public bool moveSpeedAngular;
        public float minMoveSpeed = 0.5f, minMoveFreq = 0.9f, minMoveVolume = 0.3f, minMoveFreqSpeed = 0.9f;
        public float maxMoveFreq = 1.1f, maxMoveVolume = 1f, maxMoveFreqSpeed = 1.1f;
        public float middleMoveSpeed = 1f, middleMoveVolume = 1f;
        public string maxLimitSound = "", minLimitSound = "";
        public float maxLimitMaxSpeed = 10f, maxLimitMinSpeed = 20f, minLimitMaxSpeed = 10f, minLimitMinSpeed = 20f;
    }
}
