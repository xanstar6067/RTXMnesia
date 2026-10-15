using RTXMnesia.Dynamics;
using RTXMnesia.Hpl;
using RTXMnesia.ScriptApi;
using RTXMnesia.World;
using UnityEngine;

namespace RTXMnesia.Props
{
    /// <summary>
    /// cLuxProp_MoveObject (LuxProp_MoveObject.cpp): a static body moved by script between a closed state (its load
    /// pose) and an open one: linear (along a local axis by the body's size × OpenAmount) or angular (about an axis,
    /// optionally around an offset area). State 0..1, MoveToState through the prop's static movement. Computed in
    /// HPL2 space.
    /// </summary>
    public sealed class PropMoveObject : HplProp
    {
        bool _angular;
        float _openAmount, _openAcc, _openSpeed, _closeAcc, _closeSpeed;
        int _moveAxis;
        bool _autoMove, _autoMoveReachedGoal, _useAngularLocalOffset;
        float _autoMoveStateGoal, _autoMoveAcc, _autoMoveSpeed, _autoMoveSlowdownDist;
        string _angularOffsetArea = "";
        Vector3 _angularOffsetPos;
        Matrix4x4 _closedTransform, _openTransform;

        public PropMoveObject(AmnesiaEntity entity) : base(entity, HplEntityType.Prop) { }

        static int StringToAxis(string s)
        {
            switch ((s ?? "").ToLowerInvariant())
            {
                case "x": return 0;
                case "y": return 1;
                case "z": return 2;
            }
            HplLog.Error($"Axis '{s}' does not exist!");
            return 0;
        }

        protected override void LoadVariables()
        {
            string type = VarString("MoveObjectType", "Linear").ToLowerInvariant();
            if (type != "linear" && type != "angular") HplLog.Error($"MoveObjectType '{type}' does not exist!");
            _angular = type == "angular";
            _openAmount = VarFloat("OpenAmount", 1f);
            _moveAxis = StringToAxis(VarString("MoveAxis", "y"));
            _openAcc = VarFloat("OpenAcc", 1f);
            _openSpeed = VarFloat("OpenSpeed", 0.8f);
            _closeAcc = VarFloat("CloseAcc", 9f);
            _closeSpeed = VarFloat("CloseSpeed", 5f);
            _autoMove = VarBool("AutoMove", false);
            _autoMoveStateGoal = VarFloat("AutoMoveStateGoal", 0f);
            _autoMoveAcc = VarFloat("AutoMoveAcc", 9f);
            _autoMoveSpeed = VarFloat("AutoMoveSpeed", 4f);
            _autoMoveSlowdownDist = VarFloat("AutoMoveSlowdownDist", 0.1f);
        }

        protected override void LoadInstanceVariables() => _angularOffsetArea = InstanceString("AngularOffsetArea", "");

        protected override void OnSetupAfterLoad()
        {
            var body = MainBody;
            if (body == null) return;
            _closedTransform = body.HplMatrix;
            if (!_angular)
            {
                // The local size of the body's bounding volume along the axis (HPL2 space axes).
                Vector3 size = LocalSize(body);
                Vector3 mul = _moveAxis == 0 ? Vector3.right : _moveAxis == 1 ? Vector3.up : Vector3.forward;
                Vector3 add = Vector3.Scale(size, mul) * _openAmount;
                _openTransform = HplMatrix.SetTranslation(HplMatrix.GetRotation(_closedTransform), HplMatrix.GetTranslation(_closedTransform) + add);
            }
            else CalculateOpenRotateMatrix();
        }

        /// <summary>Bounding box size of the body's colliders in its own frame (HPL2 axes, so x is unsigned anyway).</summary>
        static Vector3 LocalSize(HplPhysicsBody body)
        {
            var inverse = Matrix4x4.TRS(body.Position, body.Rotation, Vector3.one).inverse;
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (var c in body.Colliders)
            {
                if (c == null) continue;
                var b = c.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x, (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    var p = inverse.MultiplyPoint3x4(corner);
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }
            return min.x == float.PositiveInfinity ? Vector3.zero : max - min;
        }

        /// <summary>cLuxProp_MoveObject::CalculateOpenRotateMatrix: the closed rotation turned by OpenAmount × 90° about the local axis.</summary>
        void CalculateOpenRotateMatrix()
        {
            Vector3 angles = Vector3.zero;
            angles[_moveAxis] = Mathf.PI / 2f * _openAmount;
            Matrix4x4 rotation = HplMatrix.RotateXYZ(angles);
            _openTransform = HplMatrix.SetTranslation(HplMatrix.GetRotation(_closedTransform) * rotation, HplMatrix.GetTranslation(_closedTransform));
        }

        public override bool CanInteract(HplPhysicsBody body) => InteractCallback != "";
        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos) => InteractCallback != "";

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) =>
            InteractCallback != "" ? FocusCrosshair.Grab : FocusCrosshair.Default;

        protected override void UpdatePropSpecific(float timeStep)
        {
            if (_angularOffsetArea != "")
            {
                if (Map.GetEntityByName(_angularOffsetArea) is HplArea area) SetAngularOffsetPos(HplSpace.Position(area.GameObject.transform.position));
                else HplLog.Error($"Angular offset area '{_angularOffsetArea}' does not exist!");
                _angularOffsetArea = "";
            }
            UpdateAutoMove();
        }

        public void MoveToState(float state, float acc, float maxSpeed, float slowdownDist, bool resetSpeed)
        {
            if (!_angular)
            {
                Vector3 wanted = HplMatrix.GetTranslation(_closedTransform) * (1f - state) + HplMatrix.GetTranslation(_openTransform) * state;
                MoveLinearTo(wanted, acc, maxSpeed, slowdownDist, resetSpeed);
            }
            else
            {
                Matrix4x4 wanted = HplMatrix.Slerp(state, _closedTransform, _openTransform);
                Vector3 localOffset = HplMatrix.Mul(_closedTransform.inverse, _angularOffsetPos);
                if (_useAngularLocalOffset) MoveAngularTo(wanted, acc, maxSpeed, slowdownDist, resetSpeed, true, _angularOffsetPos, localOffset);
                else MoveAngularTo(wanted, acc, maxSpeed, slowdownDist, resetSpeed, false, Vector3.zero, Vector3.zero);
            }
        }

        /// <summary>SetMoveObjectState: closing uses the close speeds, opening the open ones.</summary>
        public void MoveToState(float state)
        {
            if (state < GetMoveState()) MoveToState(state, _closeAcc, _closeSpeed, 0.3f, true);
            else MoveToState(state, _openAcc, _openSpeed, 0.3f, true);
        }

        public void SetAngularOffsetPos(Vector3 hplWorldPos)
        {
            _angularOffsetPos = hplWorldPos;
            _useAngularLocalOffset = true;
            if (_angular) CalculateOpenRotateMatrix();
        }

        /// <summary>cLuxProp_MoveObject::GetMoveState: the first non-zero component of delta / full delta.</summary>
        public float GetMoveState()
        {
            Vector3 delta, t;
            if (!_angular)
            {
                Vector3 current = MainBody.HplPosition;
                Vector3 closed = HplMatrix.GetTranslation(_closedTransform);
                Vector3 open = HplMatrix.GetTranslation(_openTransform);
                delta = current - closed;
                Vector3 full = open - closed;
                t = new Vector3(delta.x / full.x, delta.y / full.y, delta.z / full.z);
            }
            else
            {
                delta = HplMatrix.EulerAngleDistance(_closedTransform, MainBody.HplMatrix);
                Vector3 full = HplMatrix.EulerAngleDistance(_closedTransform, _openTransform);
                t = new Vector3(delta.x / full.x, delta.y / full.y, delta.z / full.z);
            }
            if (delta.x != 0f) return t.x;
            if (delta.y != 0f) return t.y;
            if (delta.z != 0f) return t.z;
            return 0f;
        }

        public override void OnConnectionStateChange(HplEntity entity, int state)
        {
            if (state > 0) MoveToState(1f);
            if (state < 0) MoveToState(0f);
        }

        void UpdateAutoMove()
        {
            if (!_autoMove || IsMoving || _autoMoveReachedGoal) return;
            MoveToState(_autoMoveStateGoal, _autoMoveAcc, _autoMoveSpeed, _autoMoveSlowdownDist, true);
            _autoMoveReachedGoal = true;
        }
    }

    /// <summary>
    /// Prop types without their own behavior yet (Chest, Button, OilBarrel, EmotionStone, NPC, MultiSlider, Photocell,
    /// critters) and enemies / commentary icons until step 6: physics bodies, effects, callbacks of the base prop.
    /// </summary>
    public sealed class PropGeneric : HplProp
    {
        public PropGeneric(AmnesiaEntity entity, HplEntityType type) : base(entity, type) { }

        public override bool CanInteract(HplPhysicsBody body) => InteractCallback != "";
        public override bool OnInteract(HplPhysicsBody body, Vector3 hplFocusPos) => InteractCallback != "";

        public override FocusCrosshair GetFocusCrosshair(HplPhysicsBody body, Vector3 hplFocusPos) =>
            InteractCallback != "" ? (CustomFocusCrossHair == FocusCrosshair.Default ? FocusCrosshair.Grab : CustomFocusCrossHair) : FocusCrosshair.Default;
    }
}
