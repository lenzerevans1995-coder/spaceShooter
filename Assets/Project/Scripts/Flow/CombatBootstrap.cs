using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using SpaceShooter.BulletHell;
using SpaceShooter.CameraSystem;
using SpaceShooter.Combat;
using SpaceShooter.Emitters;
using SpaceShooter.Inputs;
using SpaceShooter.Ships;

namespace SpaceShooter.Flow
{
    /// <summary>
    /// Phase-4 vertical slice driver. Assembles a playable round in code: bullet/beam managers,
    /// a large circular field, a player ship with twin-stick input + follow camera + touch
    /// joysticks, and a mixed wave of small and large enemy hulls using Phase-3 patterns.
    /// Hit radii are measured from each hull's mesh so the hitbox encompasses the whole ship.
    /// Placeholder HUD + win/lose; real UI is Phase 6, planet/round progression is Phase 7.
    /// </summary>
    public class CombatBootstrap : MonoBehaviour
    {
        [Header("Ship hulls (Synty prefabs)")]
        public GameObject playerHull;
        public GameObject smallEnemyHullA;
        public GameObject smallEnemyHullB;
        public GameObject largeEnemyHullA;
        public GameObject largeEnemyHullB;

        [Header("Field")]
        public float fieldRadius = 45f;
        public float groundY = -1.8f;

        [Header("Wave")]
        public int smallEnemyCount = 4;
        public int largeEnemyCount = 2;

        [Header("Scale (visual; hitbox is measured from mesh)")]
        public float playerScale = 0.42f;
        public float smallScale = 0.42f;
        public float largeScale = 0.72f;
        public float playerYaw = 0f;
        public float enemyYaw = 0f;

        [Header("Styling")]
        public bool usePotaToon = false;          // requires PotaToonFeature on the active renderer
        public Material potaToonReference;         // a real PotaToon/Toon material to clone (keywords matter!)
        public float potaToonOutline = 0.15f;
        public bool showHitboxRing = false;        // debug-only faction/hitbox disc under ships
        [Range(0f, 1f)] public float factionTint = 0.5f;

        [Header("Projectile VFX (Master Stylized Projectile prefabs; null = glowing sphere)")]
        public GameObject playerProjectile;
        public GameObject enemyProjectile;
        public float projectileScale = 1f;

        [Header("Testing")]
        public bool autoDemo = true;

        CombatField _field;
        PlayerShip _player;
        RoundController _round;
        GameObject _playerBullet, _enemyBullet;

        void Start()
        {
            if (BulletManager.Instance == null) new GameObject("BulletManager").AddComponent<BulletManager>();
            if (BeamManager.Instance == null) new GameObject("BeamManager").AddComponent<BeamManager>();

            BuildPostProcessing(); // global bloom so emissive bullets glow

            _playerBullet = BuildBullet("PlayerBullet", new Color(0.35f, 0.95f, 1f), playerProjectile);
            _enemyBullet = BuildBullet("EnemyBullet", new Color(1f, 0.45f, 0.55f), enemyProjectile);
            // Particle projectiles are heavier than primitives — prewarm modestly.
            int pwP = playerProjectile != null ? 250 : 800;
            int pwE = enemyProjectile != null ? 450 : 1600;
            BulletManager.Instance.PrewarmType(_playerBullet, pwP);
            BulletManager.Instance.PrewarmType(_enemyBullet, pwE);

            _field = new GameObject("CombatField").AddComponent<CombatField>();
            _field.SetRadius(fieldRadius);
            BuildGroundDisc(_field.Center, fieldRadius);

            _player = BuildPlayer();

            var cam = Camera.main;
            if (cam == null) cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.orthographic = false;
            cam.fieldOfView = 54f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.05f);
            cam.allowHDR = true;
            var camData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (camData == null) camData = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            var rig = cam.GetComponent<CameraRig>(); if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.SetTarget(_player.transform);

            EnsureEventSystem();
            var canvas = BuildCanvas();
            var moveStick = BuildJoystick(canvas, "MoveStick", new Vector2(170, 150), true);
            var aimStick = BuildJoystick(canvas, "AimStick", new Vector2(-170, 150), false);
            var input = _player.GetComponent<ShipInput>();
            input.Setup(cam, _player.transform, moveStick, aimStick);
            input.autoDemo = autoDemo;

            // Mixed wave: small + large hulls.
            var enemies = new List<ShipActor>();
            for (int i = 0; i < smallEnemyCount; i++)
                enemies.Add(BuildEnemy(PickSmall(i), smallScale, 60f, 0.85f, _field.RandomPoint(12f)));
            for (int i = 0; i < largeEnemyCount; i++)
                enemies.Add(BuildEnemy(PickLarge(i), largeScale, 240f, 1.1f, _field.RandomPoint(18f)));

            _round = new RoundController();
            _round.Begin(_player, enemies);
        }

        GameObject PickSmall(int i)
        {
            var a = smallEnemyHullA != null ? smallEnemyHullA : playerHull;
            var b = smallEnemyHullB != null ? smallEnemyHullB : a;
            return (i % 2 == 0) ? a : b;
        }

        GameObject PickLarge(int i)
        {
            var a = largeEnemyHullA != null ? largeEnemyHullA : smallEnemyHullA;
            var b = largeEnemyHullB != null ? largeEnemyHullB : a;
            return (i % 2 == 0) ? a : b;
        }

        // ---------------------------------------------------------------- builders

        PlayerShip BuildPlayer()
        {
            var go = new GameObject("Player");
            go.transform.position = _field.Center;
            var ship = go.AddComponent<PlayerShip>();
            float radius = AddHull(go.transform, playerHull, playerYaw, playerScale, new Color(0.35f, 0.95f, 1f));
            ship.Configure(Faction.Player, 60f, radius);

            var weapon = go.AddComponent<ShipWeapon>();
            weapon.Configure(BuildPlayerPattern(), Faction.Player, 0.14f);

            // If the hull authored muzzles (ShipHardpoints tool), fire from them.
            var hp = go.GetComponentInChildren<ShipHardpoints>();
            if (hp != null && hp.FireCount > 0) weapon.SetFirePoints(hp.firePoints.ToArray());

            var input = go.AddComponent<ShipInput>();
            ship.Setup(input, weapon, _field);
            return ship;
        }

        EnemyShip BuildEnemy(GameObject hull, float scale, float hp, float fireInterval, Vector3 pos)
        {
            var go = new GameObject("Enemy");
            go.transform.position = pos;
            var ship = go.AddComponent<EnemyShip>();
            float radius = AddHull(go.transform, hull, enemyYaw, scale, new Color(1f, 0.4f, 0.45f));
            ship.Configure(Faction.Enemy, hp, radius);
            ship.Setup(_player.transform, _field);

            var emitter = go.AddComponent<EmitterController>();
            emitter.Configure(BuildEnemyPattern(), Faction.Enemy, fireInterval, _player.transform);
            return ship;
        }

        AttackPatternSO BuildPlayerPattern()
        {
            var f = ScriptableObject.CreateInstance<FanEmitter>();
            f.bulletPrefab = _playerBullet; f.count = 3; f.arcDegrees = 9f;
            f.speed = 34f; f.lifetime = 2.2f; f.damage = 2.5f; f.radius = 0.4f;
            return f;
        }

        AttackPatternSO BuildEnemyPattern()
        {
            var f = ScriptableObject.CreateInstance<FanEmitter>();
            f.bulletPrefab = _enemyBullet; f.count = 5; f.arcDegrees = 42f;
            f.speed = 14f; f.lifetime = 5f; f.damage = 3f; f.radius = 0.4f;
            return f;
        }

        /// <summary>
        /// Instantiate a hull at <paramref name="scale"/>, measure its combined renderer bounds so
        /// the hit radius encompasses the whole mesh, add a faction disc sized to that, and return
        /// the measured XZ radius.
        /// </summary>
        float AddHull(Transform parent, GameObject hullPrefab, float yaw, float scale, Color tint)
        {
            GameObject model = hullPrefab != null ? Instantiate(hullPrefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "Model";
            model.transform.SetParent(parent, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            model.transform.localScale = Vector3.one * scale;
            foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);
            if (usePotaToon) ApplyPotaToon(model, potaToonOutline);
            else ApplyFactionTint(model, tint);

            float radius = MeasureRadius(model, parent.position);

            // Optional debug disc visualizing the measured hitbox (off by default).
            if (showHitboxRing)
            {
                var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                var dc = disc.GetComponent<Collider>(); if (dc != null) Destroy(dc);
                disc.name = "HitboxRing";
                disc.transform.SetParent(parent, false);
                disc.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                disc.transform.localScale = new Vector3(radius * 2f, 0.02f, radius * 2f);
                var mr = disc.GetComponent<MeshRenderer>();
                var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Unlit/Color");
                if (sh != null) { var m = new Material(sh); Color c = tint; c.a = 0.22f; m.color = c; mr.sharedMaterial = m; }
            }

            return radius;
        }

        /// <summary>
        /// Convert a Synty hull's materials to PotaToon/Toon (cel shading + outline), carrying the
        /// Synty albedo atlas into _MainTex, then tag the model with PotaToonCharacter so its outline
        /// pass + character shading register with the PotaToon pipeline.
        /// </summary>
        void ApplyPotaToon(GameObject model, float outlineWidth)
        {
            // Clone a REAL PotaToon material so the shader keywords (_USEMIDTONE_ON, _RECEIVELIGHTSHADOW_ON,
            // etc.) come across — a bare new Material(PotaToon/Toon) renders black/flat without them.
            Material baseMat = potaToonReference;
            if (baseMat == null)
            {
                var toon = Shader.Find("PotaToon/Toon");
                if (toon == null) { Debug.LogWarning("[CombatBootstrap] PotaToon/Toon shader not found."); return; }
                baseMat = new Material(toon);
            }

            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    var s = src[i];
                    var pm = new Material(baseMat); // inherits keywords + tuned props from the reference
                    if (s != null)
                    {
                        Texture albedo = s.HasProperty("_Albedo_Map") ? s.GetTexture("_Albedo_Map")
                                       : s.HasProperty("_Texture_Map") ? s.GetTexture("_Texture_Map")  // POLYGON_SpaceShip_Rim (Cruiser)
                                       : s.HasProperty("_BaseMap") ? s.GetTexture("_BaseMap")
                                       : s.HasProperty("_MainTex") ? s.GetTexture("_MainTex") : null;
                        if (albedo != null) pm.SetTexture("_MainTex", albedo);
                    }
                    pm.SetColor("_BaseColor", Color.white);
                    pm.SetFloat("_OutlineWidth", outlineWidth);
                    pm.SetColor("_OutlineColor", new Color(0.02f, 0.02f, 0.04f, 1f));
                    dst[i] = pm;
                }
                r.sharedMaterials = dst;
            }
            model.AddComponent<PotaToon.PotaToonCharacter>();
        }

        /// <summary>
        /// Tint a hull's materials toward its faction color so player vs enemy reads at a glance
        /// without the debug ring. Uses instanced materials so the shared Synty asset is untouched.
        /// </summary>
        void ApplyFactionTint(GameObject model, Color tint)
        {
            if (factionTint <= 0f) return;
            Color c = Color.Lerp(Color.white, tint, factionTint);
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                foreach (var m in r.materials)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                    else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                }
            }
        }

        static float MeasureRadius(GameObject model, Vector3 center)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 1f;
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            // Inscribed circle: the largest hit radius that stays INSIDE the mesh footprint
            // (never pokes outside it). Half-extents per axis, take the smaller.
            float ex = Mathf.Max(Mathf.Abs(b.max.x - center.x), Mathf.Abs(b.min.x - center.x));
            float ez = Mathf.Max(Mathf.Abs(b.max.z - center.z), Mathf.Abs(b.min.z - center.z));
            return Mathf.Max(0.4f, Mathf.Min(ex, ez));
        }

        GameObject BuildBullet(string name, Color color, GameObject projectilePrefab)
        {
            GameObject go;
            if (projectilePrefab != null)
            {
                // Use a Master Stylized Projectile VFX prefab as the bullet visual; our Bullet
                // component drives movement (the prefabs have no mover scripts).
                go = Instantiate(projectilePrefab);
                go.name = name;
                go.transform.localScale = Vector3.one * projectileScale;
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
            }
            else
            {
                // Fallback: glowing HDR sphere (also used by the stress-test scene).
                var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Unlit/Color");
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = name;
                var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
                go.transform.localScale = Vector3.one * 0.7f;
                go.GetComponent<MeshRenderer>().sharedMaterial = new Material(sh) { color = color * 3.5f };
            }

            go.AddComponent<Bullet>();
            go.SetActive(false);
            return go;
        }

        void BuildPostProcessing()
        {
            var go = new GameObject("GlobalVolume");
            var vol = go.AddComponent<UnityEngine.Rendering.Volume>();
            vol.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
            vol.sharedProfile = profile;
            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>(true);
            bloom.intensity.Override(0.9f);
            bloom.threshold.Override(1.0f);
            bloom.scatter.Override(0.6f);
        }

        void BuildGroundDisc(Vector3 center, float radius)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var col = disc.GetComponent<Collider>(); if (col != null) Destroy(col);
            disc.name = "FieldGround";
            disc.transform.position = center + new Vector3(0f, groundY, 0f);
            disc.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
            var mr = disc.GetComponent<MeshRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard");
            if (sh != null) mr.sharedMaterial = new Material(sh) { color = new Color(0.06f, 0.07f, 0.12f) };
        }

        // ---------------------------------------------------------------- UI

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem").AddComponent<EventSystem>();
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        Canvas BuildCanvas()
        {
            var go = new GameObject("TouchCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        VirtualJoystick BuildJoystick(Canvas canvas, string name, Vector2 anchoredPos, bool left)
        {
            var bgGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VirtualJoystick));
            bgGo.transform.SetParent(canvas.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(left ? 0f : 1f, 0f);
            bgRt.pivot = new Vector2(left ? 0f : 1f, 0f);
            bgRt.sizeDelta = new Vector2(260, 260);
            bgRt.anchoredPosition = anchoredPos;
            bgGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

            var hGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            hGo.transform.SetParent(bgGo.transform, false);
            var hRt = hGo.GetComponent<RectTransform>();
            hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.5f);
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.sizeDelta = new Vector2(110, 110);
            hRt.anchoredPosition = Vector2.zero;
            hGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.25f);

            var js = bgGo.GetComponent<VirtualJoystick>();
            js.Bind(bgRt, hRt, 95f);
            return js;
        }

        // ---------------------------------------------------------------- HUD / flow

        void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            style.normal.textColor = Color.white;

            float hp = _player != null ? _player.Hp : 0f;
            float maxHp = _player != null ? _player.MaxHp : 1f;
            GUI.Label(new Rect(14, 10, 500, 28), $"HP: {hp:0}/{maxHp:0}", style);
            if (_round != null)
                GUI.Label(new Rect(14, 40, 500, 28), $"Enemies: {_round.EnemiesAlive}/{_round.EnemiesTotal}", style);
            GUI.Label(new Rect(14, 70, 700, 28), autoDemo ? "AUTO-DEMO (set autoDemo=false to play)" : "Twin-stick: move / aim+fire", style);

            if (_round != null && _round.Current != RoundController.State.InProgress)
            {
                var big = new GUIStyle(GUI.skin.label) { fontSize = 44, alignment = TextAnchor.MiddleCenter };
                big.normal.textColor = _round.Current == RoundController.State.Won ? new Color(0.5f, 1f, 0.6f) : new Color(1f, 0.5f, 0.5f);
                GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 60), _round.Current == RoundController.State.Won ? "ROUND CLEAR" : "DESTROYED", big);
                var hint = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
                hint.normal.textColor = Color.white;
                GUI.Label(new Rect(0, Screen.height * 0.4f + 64, Screen.width, 40), "Press R to restart", hint);
            }
        }
    }
}
