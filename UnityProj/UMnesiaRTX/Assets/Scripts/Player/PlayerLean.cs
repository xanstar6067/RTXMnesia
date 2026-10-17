using RTXMnesia.Hpl;
using UnityEngine;

namespace RTXMnesia.Player
{
    /// <summary>
    /// Port of cLuxPlayerLean (LuxPlayerHelpers.cpp): while a lean key is held the head moves sideways
    /// (up to 0.5 m) and rolls (up to 15°); a small head cylinder stops it at walls.
    /// </summary>
    public class PlayerLean
    {
        const float MaxMovement = 0.5f;
        const float MaxRotation = 15f * Mathf.Deg2Rad;

        readonly AmnesiaPlayer _player;
        readonly MeshCollider _headShape;
        readonly Vector3 _headHalfExtents;

        float _dir, _dirAdd, _movement, _rotation;
        bool _pressed;

        public PlayerLean(AmnesiaPlayer player)
        {
            _player = player;
            // cLuxPlayerLean::CreateWorldEntities: radius 68% of the body, 10 cm high.
            float radius = player.Body.Size.x * 0.5f * 0.68f;
            const float height = 0.05f * 2f;
            var go = new GameObject("LeanHeadShape") { layer = HplPhysics.CharacterLayer };
            go.transform.SetParent(player.transform, false);
            _headShape = go.AddComponent<MeshCollider>();
            _headShape.sharedMesh = HplCharacterBody.CreateCylinderMesh(radius, height, 16);
            _headShape.convex = true;
            _headShape.isTrigger = true;
            _headHalfExtents = new Vector3(radius, height * 0.5f, radius) + Vector3.one * 0.01f;
        }

        public float Movement => _movement;

        public void SetLean(float mul)
        {
            _dir = Mathf.Clamp(mul, -1f, 1f);
            if (Mathf.Abs(mul) > 0f)
            {
                _dirAdd = 0f;
                _pressed = true;
            }
        }

        public void AddLean(float add)
        {
            _dirAdd = Mathf.Clamp(_dirAdd + add, -1f, 1f);
            _pressed = Mathf.Abs(_dirAdd) > 0f;
        }

        public void Update(float dt)
        {
            if (_pressed)
            {
                float dir = _dir + _dirAdd;
                _pressed = false;

                float goalPos = MaxMovement * dir;
                float goalRot = MaxRotation * -dir;

                float prevMovement = _movement;
                float moveSpeed = goalPos - _movement;
                if (Mathf.Abs(moveSpeed) < 0.1f) moveSpeed = 0.1f * dir;
                _movement += moveSpeed * dt * 3f;
                if (goalPos < 0f && _movement < goalPos) _movement = goalPos;
                if (goalPos > 0f && _movement > goalPos) _movement = goalPos;

                float prevRotation = _rotation;
                float rotSpeed = goalRot - _rotation;
                if (Mathf.Abs(rotSpeed) < 0.13f) rotSpeed = 0.13f * -dir;
                _rotation += rotSpeed * dt * 2f;
                if (goalRot < 0f && _rotation < goalRot) _rotation = goalRot;
                if (goalRot > 0f && _rotation > goalRot) _rotation = goalRot;

                // Step back while the head would be inside something.
                float reverseMov = prevMovement - _movement;
                float reverseRot = prevRotation - _rotation;
                var body = _player.Body;
                Vector3 start = body.FeetPosition + new Vector3(0f, body.Size.y + _player.CameraPosAdd.y, 0f);
                Vector3 right = _player.CameraRight;
                Vector3 pos = start + right * _movement;
                int count = 0;
                while (HplPhysics.CheckShapeWorldCollision(_headShape, pos, Quaternion.identity, _headHalfExtents, out _,
                                                         collideCharacters: false))
                {
                    _movement += reverseMov;
                    _rotation += reverseRot;
                    if ((reverseMov < 0f && _movement < 0f) || (reverseMov > 0f && _movement > 0f) || ++count > 10)
                    {
                        _movement = 0f;
                        _rotation = 0f;
                        break;
                    }
                    pos = start + right * _movement;
                }

                _player.FadeLeanRollTo(_rotation, 5f, 3f);
                _player.MoveHeadPosAdd(HeadPosAdd.Lean, new Vector3(_movement, 0f, 0f), 2f, 0.05f);
            }
            else if (_movement != 0f || _rotation != 0f)
            {
                _rotation = 0f;
                _movement = 0f;
                _dir = 0f;
                _dirAdd = 0f;
                _player.FadeLeanRollTo(0f, 4f, 2f);
                _player.MoveHeadPosAdd(HeadPosAdd.Lean, Vector3.zero, 1.3f, 0.1f);
            }
        }
    }
}
