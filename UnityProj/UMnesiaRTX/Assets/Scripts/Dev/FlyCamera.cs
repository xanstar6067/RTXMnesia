using UnityEngine;
using UnityEngine.InputSystem;

namespace RTXMnesia.Dev
{
    /// <summary>
    /// Free camera for looking around imported levels until the player exists.
    /// Hold right mouse button to look, WASD to move, E/Q up/down, Shift faster, mouse wheel changes speed.
    /// </summary>
    public class FlyCamera : MonoBehaviour
    {
        public float speed = 2.5f;
        public float fastMultiplier = 4f;
        public float lookSensitivity = 0.12f;

        float _yaw, _pitch;

        void OnEnable()
        {
            Vector3 euler = transform.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        }

        void Update()
        {
            if (ScriptApi.HplMap.Current != null && ScriptApi.HplMap.Current.IsMenuPaused) return;
            if (ScriptApi.HplMap.Current != null && ScriptApi.HplMap.Current.player != null && ScriptApi.HplMap.Current.player.UiInputBlocked) return;
            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
                    _yaw += delta.x;
                    _pitch = Mathf.Clamp(_pitch - delta.y, -89f, 89f);
                    transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
                }
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll != 0f) speed = Mathf.Clamp(speed * (scroll > 0f ? 1.2f : 1f / 1.2f), 0.2f, 50f);
            }

            if (keyboard == null) return;
            Vector3 move = Vector3.zero;
            if (keyboard.wKey.isPressed) move += Vector3.forward;
            if (keyboard.sKey.isPressed) move += Vector3.back;
            if (keyboard.aKey.isPressed) move += Vector3.left;
            if (keyboard.dKey.isPressed) move += Vector3.right;
            if (keyboard.eKey.isPressed) move += Vector3.up;
            if (keyboard.qKey.isPressed) move += Vector3.down;
            float velocity = speed * (keyboard.leftShiftKey.isPressed ? fastMultiplier : 1f);
            transform.Translate(move * (velocity * Time.unscaledDeltaTime), Space.Self);
        }
    }
}
