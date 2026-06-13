using UnityEngine;
using SpaceShooter.BulletHell;
using SpaceShooter.Emitters;
using SpaceShooter.Combat;

namespace SpaceShooter.Test
{
    /// <summary>
    /// Phase-2 performance proof. Spawns a grid of rotating ring emitters and reports
    /// live bullet count + smoothed FPS via OnGUI. Goal: hold 60 fps with 1000+
    /// simultaneous bullets. If no bullet prefab is assigned, it builds a simple unlit
    /// sphere at runtime so the test runs WITHOUT the (not-yet-imported) projectile pack.
    /// </summary>
    public class BulletHellStressTest : MonoBehaviour
    {
        [Header("Optional — auto-built if left empty")]
        [SerializeField] RingEmitter _ringPattern;
        [SerializeField] GameObject _bulletPrefab;

        [Header("Stress settings")]
        [SerializeField] int _emitterCount = 4;
        [SerializeField] float _emitterSpacing = 9f;
        [SerializeField] float _fireInterval = 0.12f;
        [SerializeField] int _ringBulletCount = 24;
        [SerializeField] float _bulletSpeed = 8f;
        [SerializeField] float _bulletLifetime = 4f;
        [SerializeField] int _prewarm = 3500;

        float _fpsSmooth;
        BulletManager _bullets;

        void Start()
        {
            _bullets = BulletManager.Instance;
            if (_bullets == null)
                _bullets = new GameObject("BulletManager").AddComponent<BulletManager>();

            if (_bulletPrefab == null) _bulletPrefab = BuildDefaultBullet();

            if (_ringPattern == null)
            {
                _ringPattern = ScriptableObject.CreateInstance<RingEmitter>();
                _ringPattern.bulletCount = _ringBulletCount;
                _ringPattern.spinPerShot = 13f;
                _ringPattern.speed = _bulletSpeed;
                _ringPattern.lifetime = _bulletLifetime;
                _ringPattern.radius = 0.25f;
            }
            if (_ringPattern.bulletPrefab == null) _ringPattern.bulletPrefab = _bulletPrefab;

            _bullets.PrewarmType(_bulletPrefab, _prewarm);

            // Lay emitters out in a square grid centered on the origin.
            int side = Mathf.CeilToInt(Mathf.Sqrt(_emitterCount));
            int made = 0;
            for (int x = 0; x < side && made < _emitterCount; x++)
            for (int z = 0; z < side && made < _emitterCount; z++, made++)
            {
                var go = new GameObject($"Emitter_{made}");
                float px = (x - (side - 1) * 0.5f) * _emitterSpacing;
                float pz = (z - (side - 1) * 0.5f) * _emitterSpacing;
                go.transform.position = new Vector3(px, 0f, pz);
                go.AddComponent<EmitterController>()
                  .Configure(_ringPattern, Faction.Enemy, _fireInterval, null);
            }
        }

        void Update()
        {
            float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _fpsSmooth = _fpsSmooth <= 0f ? fps : Mathf.Lerp(_fpsSmooth, fps, 0.1f);
        }

        GameObject BuildDefaultBullet()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "DefaultBullet";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.transform.localScale = Vector3.one * 0.5f;

            var mr = go.GetComponent<MeshRenderer>();
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh != null)
            {
                var mat = new Material(sh) { color = new Color(0.25f, 0.9f, 1f) };
                mr.sharedMaterial = mat;
            }

            go.AddComponent<Bullet>();
            go.SetActive(false);
            return go;
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 22 };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(14, 10, 600, 30), $"Bullets live: {(_bullets != null ? _bullets.ActiveCount : 0)}", style);
            GUI.Label(new Rect(14, 42, 600, 30), $"FPS: {_fpsSmooth:0}", style);
            GUI.Label(new Rect(14, 74, 600, 30), "Target: 1000+ bullets @ 60 fps", style);
        }
    }
}
