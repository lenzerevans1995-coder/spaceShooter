using UnityEngine;
using UnityEngine.InputSystem;
using SpaceShooter.Combat;

namespace SpaceShooter.Inputs
{
    /// <summary>
    /// Source-agnostic twin-stick input for a ship. Reads (in priority order) on-screen
    /// joysticks, gamepad sticks, then keyboard+mouse — so the same PlayerShip works on
    /// mobile and desktop. In twin-stick, the ship fires while the aim stick is engaged.
    /// An autoDemo mode synthesizes input for testing without a device.
    /// </summary>
    public class ShipInput : MonoBehaviour
    {
        [Header("Mobile")]
        [SerializeField] VirtualJoystick _moveStick;
        [SerializeField] VirtualJoystick _aimStick;

        [Header("Desktop aim helpers")]
        [SerializeField] Camera _cam;
        [SerializeField] Transform _ship;

        [Header("Testing")]
        public bool autoDemo = false;

        public Vector2 Move { get; private set; }
        public Vector2 Aim { get; private set; }     // normalized, zero when not aiming
        public bool FireHeld { get; private set; }

        float _demoTime;

        public void Setup(Camera cam, Transform ship, VirtualJoystick moveStick, VirtualJoystick aimStick)
        {
            _cam = cam; _ship = ship; _moveStick = moveStick; _aimStick = aimStick;
        }

        void Update()
        {
            if (autoDemo) { UpdateAutoDemo(); return; }

            Vector2 move = Vector2.zero, aim = Vector2.zero;
            bool fire = false;

            var gp = Gamepad.current;
            if (gp != null)
            {
                move = gp.leftStick.ReadValue();
                aim = gp.rightStick.ReadValue();
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (k != Vector2.zero) move = k;
            }

            if (_moveStick != null && _moveStick.Active) move = _moveStick.Value;
            if (_aimStick != null && _aimStick.Active) aim = _aimStick.Value;

            // Aim has a stick: fire while aiming (twin-stick). Else fall back to mouse aim.
            if (aim.sqrMagnitude > 0.04f)
            {
                fire = true;
            }
            else
            {
                var mouse = Mouse.current;
                if (mouse != null && _cam != null && _ship != null)
                {
                    Ray ray = _cam.ScreenPointToRay(mouse.position.ReadValue());
                    Plane plane = new Plane(Vector3.up, _ship.position);
                    if (plane.Raycast(ray, out float ent))
                    {
                        Vector3 hit = ray.GetPoint(ent);
                        Vector3 d = hit - _ship.position;
                        aim = new Vector2(d.x, d.z);
                    }
                    if (mouse.leftButton.isPressed) fire = true;
                }
            }

            if (kb != null && kb.spaceKey.isPressed) fire = true;

            Move = Vector2.ClampMagnitude(move, 1f);
            Aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.zero;
            FireHeld = fire;
        }

        void UpdateAutoDemo()
        {
            _demoTime += Time.deltaTime;
            // Drift around the field in a slow circle.
            Move = new Vector2(Mathf.Cos(_demoTime * 0.6f), Mathf.Sin(_demoTime * 0.6f)) * 0.85f;

            // Aim at the nearest opposing target.
            Vector2 aim = Vector2.zero;
            if (_ship != null)
            {
                var targets = TargetRegistry.Targets;
                float best = float.MaxValue; Vector3 pos = _ship.position;
                for (int i = 0; i < targets.Count; i++)
                {
                    var t = targets[i];
                    if (!t.IsAlive || t.Faction == Faction.Player) continue;
                    Vector3 d = t.Position - pos; float s = d.x * d.x + d.z * d.z;
                    if (s < best) { best = s; aim = new Vector2(d.x, d.z); }
                }
            }
            Aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.zero;
            FireHeld = Aim != Vector2.zero;
        }
    }
}
