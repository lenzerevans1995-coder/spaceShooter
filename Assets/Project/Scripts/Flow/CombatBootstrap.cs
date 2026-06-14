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

        [Header("Field (portrait). Z = vertical play depth; X auto-fit to screen aspect at Start.")]
        public Vector2 fieldExtents = new Vector2(13f, 26f);
        public float groundY = -1.8f;
        public float bulletCullMargin = 8f;   // bullets fly this far past the walls before dying

        [Header("Wave treadmill")]
        public float waveBaseInterval = 1.4f; // seconds between spawns at the start
        public float waveMinInterval = 0.5f;  // fastest spawn cadence once ramped up
        public float waveRampSeconds = 90f;   // base→min ramp duration
        public int waveMaxAlive = 14;         // live-enemy population cap
        public float enemyDriftSpeed = 6f;    // downward treadmill speed

        [Header("Scale (visual; hitbox is measured from mesh)")]
        public float shipScaleMultiplier = 0.5f;   // global shrink so the arena reads bigger
        [Range(0.1f, 1f)] public float playerHitboxFraction = 0.55f;  // grazing: hit radius < visual
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

        [Header("Projectile VFX (Unique Projectiles prefabs; null = glowing sphere)")]
        public GameObject playerProjectile;
        public GameObject enemyProjectile;
        public float projectileScale = 1f;

        [Header("Thruster VFX (Polygon Arsenal jet; spawned at ShipHardpoints thruster points)")]
        public GameObject thrusterVFX;
        public float thrusterScale = 1f;

        [Header("Testing")]
        public bool autoDemo = true;

        CombatField _field;
        PlayerShip _player;
        RoundController _round;
        WaveSpawner _spawner;
        GameObject _playerBullet, _enemyBullet;
        int _spawnIndex;

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
            _field.SetExtents(fieldExtents);   // provisional; width is replaced by the aspect fit below

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

            // Aspect-adaptive: keep the vertical play depth, fit the camera + derive field WIDTH so the
            // Y=0 arena fills the device screen (static tilted framing — fixed arena, no follow).
            Vector2 ext = rig.FitFieldToScreen(_field.Center, fieldExtents.y);
            _field.SetExtents(ext);
            BuildGroundFill(cam);   // floor covers the full screen (not just the play rectangle)
            // Cull bullets on a rectangle matching the field (+ margin) so edge shots fly past the wall.
            BulletManager.Instance.SetCullBox(
                new Vector2(_field.Center.x, _field.Center.z), ext + Vector2.one * bulletCullMargin);

            EnsureEventSystem();
            var canvas = BuildCanvas();
            var moveStick = BuildJoystick(canvas, "MoveStick", new Vector2(170, 150), true);
            var aimStick = BuildJoystick(canvas, "AimStick", new Vector2(-170, 150), false);
            var input = _player.GetComponent<ShipInput>();
            input.Setup(cam, _player.transform, moveStick, aimStick);
            input.autoDemo = autoDemo;

            // Wave treadmill: continuously spawn drifting enemies just above the top edge. They drift
            // down (EnemyShip drift mode), swarm via separation, and self-despawn past the bottom.
            _spawner = new GameObject("WaveSpawner").AddComponent<WaveSpawner>();
            _spawner.Configure(
                () => _field.TopSpawnPoint(-3f),   // spawn just ABOVE the top edge (negative inset)
                SpawnDriftEnemy,
                waveBaseInterval, waveMinInterval, waveMaxAlive, waveRampSeconds);

            _round = new RoundController();
            _round.Begin(_player, new List<ShipActor>());   // survival: no kill-all win; player death = lose
        }

        /// <summary>Factory for the wave treadmill: build a drifting enemy (mixed small/large) at pos.</summary>
        EnemyShip SpawnDriftEnemy(Vector3 pos)
        {
            int i = _spawnIndex++;
            bool large = (i % 4) == 3;   // ~1 in 4 is a heavy
            EnemyShip e = large
                ? BuildEnemy(PickLarge(i), largeScale, 240f, 1.1f, pos)
                : BuildEnemy(PickSmall(i), smallScale, 60f, 0.85f, pos);
            e.SetDrift(enemyDriftSpeed * (large ? 0.8f : 1f));
            return e;
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
            // Groundwork for roguelike upgrades; ship/weapon will read its stats in the next pass.
            go.AddComponent<SpaceShooter.Progression.PlayerStatsManager>();
            float radius = AddHull(go.transform, playerHull, playerYaw, playerScale * shipScaleMultiplier, new Color(0.35f, 0.95f, 1f));
            // Grazing: the hit radius is a FRACTION of the visual mesh, so near-misses don't kill.
            ship.Configure(Faction.Player, 60f, radius * playerHitboxFraction);

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
            go.AddComponent<EnemySeparation>();   // added before Setup so EnemyShip picks it up
            float radius = AddHull(go.transform, hull, enemyYaw, scale * shipScaleMultiplier, new Color(1f, 0.4f, 0.45f));
            ship.Configure(Faction.Enemy, hp, radius);
            ship.Setup(_player.transform, _field);

            var emitter = go.AddComponent<EmitterController>();
            emitter.Configure(BuildEnemyPattern(), Faction.Enemy, fireInterval, _player.transform);
            return ship;
        }

        AttackPatternSO BuildPlayerPattern()
        {
            // One projectile per shot — each authored fire point (cannon) fires a single bolt.
            var a = ScriptableObject.CreateInstance<AimedShot>();
            a.bulletPrefab = _playerBullet; a.count = 1;
            a.speed = 34f; a.lifetime = 2.2f; a.damage = 2.5f; a.radius = 0.4f;
            return a;
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

            SpawnThrusters(model);

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

        /// <summary>Attach the thruster VFX at each authored thruster point (ShipHardpoints).</summary>
        void SpawnThrusters(GameObject model)
        {
            if (thrusterVFX == null) return;
            var hp = model.GetComponentInChildren<ShipHardpoints>();
            if (hp == null) return;
            foreach (var tp in hp.ValidThrusterPoints())
            {
                var fx = Instantiate(thrusterVFX, tp);
                fx.transform.localPosition = Vector3.zero;
                // Point the flame out the BACK of the ship (engines exhaust rearward), regardless
                // of how the thruster point was rotated.
                fx.transform.rotation = Quaternion.LookRotation(-model.transform.forward, model.transform.up);
                fx.transform.localScale = Vector3.one * thrusterScale;
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
                // Use a VFX projectile prefab as the bullet VISUAL only. Strip the pack's own
                // mover script + Rigidbody + colliders so OUR Bullet drives movement — otherwise
                // they fight (frozen/erratic bullets) and pooled reuse misbehaves.
                go = Instantiate(projectilePrefab);
                go.name = name;
                go.transform.localScale = Vector3.one * projectileScale;
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) if (mb != null) DestroyImmediate(mb);
                foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) DestroyImmediate(rb);
                foreach (var c in go.GetComponentsInChildren<Collider>(true)) DestroyImmediate(c);
                // TrailRenderers streak a line from the bullet's previous pooled position to its new
                // spawn — strip them (and any LineRenderers) so pooled bullets are clean discrete bolts.
                foreach (var tr in go.GetComponentsInChildren<TrailRenderer>(true)) DestroyImmediate(tr);
                foreach (var lr in go.GetComponentsInChildren<LineRenderer>(true)) DestroyImmediate(lr);
                // Local sim so trail particles stay with the bullet (no world-space "line" streaks).
                foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true)) { var m = ps.main; m.simulationSpace = ParticleSystemSimulationSpace.Local; }
                // Camera-facing (View) alignment so the bullet is always fully visible top-down —
                // otherwise the mesh aligns to travel and goes edge-on (invisible) at some angles.
                foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true)) r.alignment = ParticleSystemRenderSpace.View;
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

        /// <summary>
        /// Build the visual ground so it covers the WHOLE screen, not just the play rectangle. A
        /// tilted camera projects a flat rectangle to a trapezoid (narrow at the top), which would
        /// leave dark gaps in the top corners. We raycast the four screen corners onto the Y=groundY
        /// plane and size the ground to span them (+ margin), so the floor fills the view at any
        /// aspect/tilt. The play field (clamp + cull) stays the inscribed rectangle; only the visual
        /// floor extends past it.
        /// </summary>
        void BuildGroundFill(Camera cam)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);  // 10x10 units at scale 1
            var col = ground.GetComponent<Collider>(); if (col != null) Destroy(col);
            ground.name = "FieldGround";

            var plane = new Plane(Vector3.up, new Vector3(0f, groundY, 0f));
            float minX = 1e9f, maxX = -1e9f, minZ = 1e9f, maxZ = -1e9f;
            var corners = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            foreach (var c in corners)
            {
                Ray r = cam.ViewportPointToRay(new Vector3(c.x, c.y, 0f));
                if (!plane.Raycast(r, out float dist)) dist = 300f;   // ray near-parallel (toward horizon)
                dist = Mathf.Min(dist, 300f);                          // clamp so the floor stays finite
                Vector3 p = r.GetPoint(dist);
                if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
                if (p.z < minZ) minZ = p.z; if (p.z > maxZ) maxZ = p.z;
            }
            float mX = (maxX - minX) * 0.08f + 4f, mZ = (maxZ - minZ) * 0.08f + 4f;
            minX -= mX; maxX += mX; minZ -= mZ; maxZ += mZ;

            ground.transform.position = new Vector3((minX + maxX) * 0.5f, groundY, (minZ + maxZ) * 0.5f);
            ground.transform.localScale = new Vector3((maxX - minX) / 10f, 1f, (maxZ - minZ) / 10f);
            var mr = ground.GetComponent<MeshRenderer>();
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
            if (_spawner != null)
                GUI.Label(new Rect(14, 40, 500, 28), $"Survived: {_spawner.Elapsed:0}s   Enemies: {_spawner.AliveCount}", style);
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
