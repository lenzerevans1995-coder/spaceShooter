using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SpaceShooter.BulletHell;
using SpaceShooter.Combat;

namespace SpaceShooter.Emitters
{
    /// <summary>
    /// Drives a timeline of attack patterns. Each step fires its pattern (projectile OR beam)
    /// on a cadence for a duration or a fixed number of volleys; steps run in order and the
    /// whole timeline can loop. This is the foundation for multi-phase bosses: chain
    /// "ring → aimed burst → sweeping beam → scatter" entirely from data.
    /// </summary>
    public class EmitterController : MonoBehaviour
    {
        [System.Serializable]
        public class AttackStep
        {
            public AttackPatternSO pattern;
            public float startDelay = 0f;
            public float fireInterval = 0.2f;
            public float duration = 2f;
            [Tooltip("If > 0, fire exactly this many volleys and ignore duration.")]
            public int volleys = 0;
        }

        [SerializeField] List<AttackStep> _timeline = new();
        [SerializeField] Faction _faction = Faction.Enemy;
        [SerializeField] Transform _aimTarget;
        [SerializeField] bool _loop = true;
        [SerializeField] bool _autoStart = true;

        int _shotIndex;
        Transform _tf;
        Coroutine _run;

        void Awake() { _tf = transform; }

        void Start()
        {
            // Guard against double-start: Configure() may already have launched the timeline.
            if (_autoStart && _timeline.Count > 0 && _run == null) StartTimeline();
        }

        /// <summary>Runtime one-pattern setup (used by the stress test and simple spawners).</summary>
        public void Configure(AttackPatternSO pattern, Faction faction, float fireInterval, Transform aimTarget)
        {
            _timeline = new List<AttackStep>
            {
                new AttackStep { pattern = pattern, fireInterval = fireInterval, duration = Mathf.Infinity }
            };
            _faction = faction;
            _aimTarget = aimTarget;
            _loop = true;
            if (isActiveAndEnabled) StartTimeline();
        }

        public void SetAimTarget(Transform t) => _aimTarget = t;
        public void SetFaction(Faction faction) => _faction = faction;

        /// <summary>Assign a multi-step timeline directly (the real authoring path; designers also set this in the inspector).</summary>
        public void SetTimeline(List<AttackStep> steps, bool loop = true)
        {
            _timeline = steps;
            _loop = loop;
        }

        public void StartTimeline()
        {
            if (_run != null) StopCoroutine(_run);
            _run = StartCoroutine(RunTimeline());
        }

        public void StopTimeline()
        {
            if (_run != null) { StopCoroutine(_run); _run = null; }
        }

        IEnumerator RunTimeline()
        {
            do
            {
                for (int s = 0; s < _timeline.Count; s++)
                {
                    AttackStep step = _timeline[s];
                    if (step.pattern == null) continue;
                    if (step.startDelay > 0f) yield return new WaitForSeconds(step.startDelay);

                    float interval = Mathf.Max(0.0001f, step.fireInterval);
                    float elapsed = 0f, timer = interval; // fire immediately on entry
                    int fired = 0;

                    while (true)
                    {
                        float dt = Time.deltaTime;
                        timer += dt;
                        elapsed += dt;

                        while (timer >= interval)
                        {
                            timer -= interval;
                            Fire(step.pattern);
                            fired++;
                            if (step.volleys > 0 && fired >= step.volleys) break;
                        }

                        if (step.volleys > 0) { if (fired >= step.volleys) break; }
                        else if (elapsed >= step.duration) break;

                        yield return null;
                    }
                }
            } while (_loop);

            _run = null;
        }

        void Fire(AttackPatternSO pattern)
        {
            Vector3 origin = _tf.position;
            Vector3 aim = _aimTarget != null ? (_aimTarget.position - origin) : Vector3.right;
            aim.y = 0f;
            if (aim.sqrMagnitude < 1e-4f) aim = Vector3.right;
            aim.Normalize();

            var ctx = new AttackContext(BulletManager.Instance, BeamManager.Instance,
                                        origin, aim, _faction, _shotIndex++);
            pattern.Emit(in ctx);
        }
    }
}
