// SPDX-License-Identifier: Apache-2.0

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Unity.RerunSDK.Samples
{
    public class RerunInteractiveCubeController : MonoBehaviour
    {
        [SerializeField] private float _rotateSpeed = 3f;
        [SerializeField] private float _panSpeed = 0.01f;
        [SerializeField] private float _scaleSpeed = 0.5f;
        [SerializeField] private float _minScale = 0.2f;
        [SerializeField] private float _maxScale = 5f;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _initialScale;

        private void Awake()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _initialScale = transform.localScale;
        }

        private void Update()
        {
            var right = Vector3.right;
            var up = Vector3.up;
            var cam = Camera.main;
            if (cam != null)
            {
                right = cam.transform.right;
                up = cam.transform.up;
            }

            if (TryReadMouseDelta(0, out var rotateDelta))
            {
                transform.Rotate(up, -rotateDelta.x * _rotateSpeed * 0.1f, Space.World);
                transform.Rotate(right, rotateDelta.y * _rotateSpeed * 0.1f, Space.World);
            }

            if (TryReadMouseDelta(1, out var panDelta))
            {
                transform.position += right * panDelta.x * _panSpeed + up * panDelta.y * _panSpeed;
            }

            var scroll = ReadScrollDelta();
            if (Mathf.Abs(scroll) > 0.001f)
            {
                var scale = Mathf.Clamp(transform.localScale.x + scroll * _scaleSpeed, _minScale, _maxScale);
                transform.localScale = Vector3.one * scale;
            }

            if (ReadResetPressed())
                ResetPose();
        }

        public void ResetPose()
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;
            transform.localScale = _initialScale;
        }

        private static bool TryReadMouseDelta(int button, out Vector2 delta)
        {
            delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null && IsButtonPressed(mouse, button))
            {
                delta = mouse.delta.ReadValue();
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(button))
            {
                delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
                return true;
            }
#endif

            return false;
        }

#if ENABLE_INPUT_SYSTEM
        private static bool IsButtonPressed(Mouse mouse, int button)
        {
            return button switch
            {
                0 => mouse.leftButton.isPressed,
                1 => mouse.rightButton.isPressed,
                2 => mouse.middleButton.isPressed,
                _ => false
            };
        }
#endif

        private static float ReadScrollDelta()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                var scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.001f)
                    return scroll;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta.y;
#else
            return 0f;
#endif
        }

        private static bool ReadResetPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.R);
#else
            return false;
#endif
        }
    }
}
