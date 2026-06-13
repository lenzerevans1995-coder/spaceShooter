using System.Collections.Generic;
using UnityEngine;
using SpaceShooter.BulletHell;
using SpaceShooter.Emitters;
using SpaceShooter.Combat;

namespace SpaceShooter.Test
{
    /// <summary>
    /// Phase-3 review harness. Stands up the bullet + beam managers, a default bullet, a
    /// single enemy emitter cycling through every pattern (Ring → Spiral → Fan → Aimed →
    /// Wall → Scatter → sweeping Beam), and a moving dummy "player" target so aimed/homing
    /// patterns and beam damage are all visible. OnGUI shows the active pattern + live counts.
    /// </summary>
    public class PatternShowcase : MonoBehaviour
    {
        BulletManager _bullets;
        BeamManager _beams;
        GameObject _bulletPrefab;
        TestDummyTarget _player;
        EmitterController _emitter;
        readonly List<string> _stepNames = new();
        float _fpsSmooth, _cycle;

        void Start()
        {
            _bullets = new GameObject("BulletManager").AddComponent<BulletManager>();
            _beams = new GameObject("BeamManager").AddComponent<BeamManager>();
            _bulletPrefab = BuildDefaultBullet(new Color(0.3f, 0.9f, 1f));
            _bullets.PrewarmType(_bulletPrefab, 2000);

            // Moving dummy target ("player") that the emitter aims at.
            _player = BuildDummy("Player(dummy)", Faction.Player, new Color(0.3f, 1f, 0.4f), new Vector3(0f, 0f, -14f));

            // Enemy emitter at origin running a full pattern tour.
            var emitterGo = new GameObject("EnemyEmitter");
            emitterGo.transform.position = new Vector3(0f, 0f, 6f);
            _emitter = emitterGo.AddComponent<EmitterController>();

            var steps = new List<EmitterController.AttackStep>
            {
                Step("Ring",    MakeRing(),    0.12f, 3.5f),
                Step("Spiral",  MakeSpiral(),  0.04f, 3.5f),
                Step("Fan",     MakeFan(),     0.18f, 3.5f),
                Step("Aimed",   MakeAimed(),   0.10f, 3.5f),
                Step("Wall",    MakeWall(),    0.55f, 3.5f),
                Step("Scatter", MakeScatter(), 0.20f, 3.5f),
                Step("Beam (sweeping)", MakeBeam(), 2.6f, 5.2f),
            };
            foreach (var s in steps) _stepNames.Add(s.pattern.name);
            ConfigureTimeline(steps, Faction.Enemy, _player.transform);
        }

        EmitterController.AttackStep Step(string label, AttackPatternSO p, float interval, float duration)
        {
            p.name = label;
            return new EmitterController.AttackStep { pattern = p, fireInterval = interval, duration = duration };
        }

        // Each pattern shares the default bullet prefab.
        RingEmitter MakeRing()
        {
            var r = ScriptableObject.CreateInstance<RingEmitter>();
            r.bulletPrefab = _bulletPrefab; r.bulletCount = 28; r.spinPerShot = 9f; r.speed = 9f; r.lifetime = 4f;
            return r;
        }
        SpiralEmitter MakeSpiral()
        {
            var s = ScriptableObject.CreateInstance<SpiralEmitter>();
            s.bulletPrefab = _bulletPrefab; s.arms = 4; s.spinPerShot = 17f; s.speed = 10f; s.lifetime = 4f;
            return s;
        }
        FanEmitter MakeFan()
        {
            var f = ScriptableObject.CreateInstance<FanEmitter>();
            f.bulletPrefab = _bulletPrefab; f.count = 9; f.arcDegrees = 70f; f.speed = 12f; f.lifetime = 4f;
            return f;
        }
        AimedShot MakeAimed()
        {
            var a = ScriptableObject.CreateInstance<AimedShot>();
            a.bulletPrefab = _bulletPrefab; a.count = 3; a.spreadDegrees = 8f; a.speed = 16f; a.lifetime = 4f;
            return a;
        }
        WallEmitter MakeWall()
        {
            var w = ScriptableObject.CreateInstance<WallEmitter>();
            w.bulletPrefab = _bulletPrefab; w.count = 12; w.width = 26f; w.gapIndex = 6; w.speed = 7f; w.lifetime = 5f;
            return w;
        }
        ScatterEmitter MakeScatter()
        {
            var s = ScriptableObject.CreateInstance<ScatterEmitter>();
            s.bulletPrefab = _bulletPrefab; s.count = 18; s.arcDegrees = 360f; s.minSpeed = 5f; s.maxSpeed = 13f; s.lifetime = 4f;
            return s;
        }
        BeamPatternSO MakeBeam()
        {
            var b = ScriptableObject.CreateInstance<BeamPatternSO>();
            b.length = 60f; b.width = 1.6f; b.aimAtTarget = true; b.sweepDegrees = 80f;
            b.telegraphTime = 0.9f; b.activeTime = 1.4f; b.fadeTime = 0.3f; b.damagePerSecond = 12f;
            b.color = new Color(1f, 0.3f, 0.4f);
            return b;
        }

        void ConfigureTimeline(List<EmitterController.AttackStep> steps, Faction faction, Transform aim)
        {
            _emitter.SetFaction(faction);
            _emitter.SetAimTarget(aim);
            _emitter.SetTimeline(steps, true);
            _emitter.StartTimeline();
        }

        TestDummyTarget BuildDummy(string name, Faction faction, Color color, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 1.6f;
            var col = visual.GetComponent<Collider>(); if (col != null) Destroy(col);
            var mr = visual.GetComponent<MeshRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Standard");
            if (sh != null) mr.sharedMaterial = new Material(sh) { color = color };
            var t = go.AddComponent<TestDummyTarget>();
            t.Init(faction, 100000f, 1.2f); // big HP so it survives the whole tour
            return t;
        }

        GameObject BuildDefaultBullet(Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "DefaultBullet";
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.transform.localScale = Vector3.one * 0.5f;
            var mr = go.GetComponent<MeshRenderer>();
            var sh = Shader.Find("Universal Render Pipeline/Unlit"); if (sh == null) sh = Shader.Find("Unlit/Color");
            if (sh != null) mr.sharedMaterial = new Material(sh) { color = color };
            go.AddComponent<Bullet>();
            go.SetActive(false);
            return go;
        }

        void Update()
        {
            // Sweep the dummy player left-right so aimed/homing/beam visibly track it.
            _cycle += Time.deltaTime;
            if (_player != null)
                _player.transform.position = new Vector3(Mathf.Sin(_cycle * 0.7f) * 12f, 0f, -14f);

            float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _fpsSmooth = _fpsSmooth <= 0f ? fps : Mathf.Lerp(_fpsSmooth, fps, 0.1f);
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label) { fontSize = 20 };
            style.normal.textColor = Color.white;
            GUI.Label(new Rect(14, 10, 700, 28), $"Bullets: {(_bullets ? _bullets.ActiveCount : 0)}    FPS: {_fpsSmooth:0}", style);
            GUI.Label(new Rect(14, 40, 700, 28), "Pattern tour: Ring -> Spiral -> Fan -> Aimed -> Wall -> Scatter -> Beam (loops)", style);
        }
    }
}
