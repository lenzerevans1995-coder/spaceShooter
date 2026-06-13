using System.Collections.Generic;
using UnityEngine;

namespace SpaceShooter.BulletHell
{
    /// <summary>
    /// Per-prefab object pool for bullets, keyed by the source prefab GameObject.
    /// Pre-warmed so no Instantiate happens during combat (the source of frame hitches
    /// in bullet-hell games).
    /// </summary>
    public class BulletPool
    {
        readonly Dictionary<GameObject, Stack<Bullet>> _pools = new();
        readonly Transform _container;

        public BulletPool(Transform container) { _container = container; }

        /// <summary>Create <paramref name="count"/> inactive instances of a prefab ahead of time.</summary>
        public void Prewarm(GameObject prefab, int count)
        {
            var stack = GetStack(prefab);
            for (int i = 0; i < count; i++)
                stack.Push(CreateInstance(prefab));
        }

        /// <summary>Get a ready bullet, instantiating only if the pool has run dry.</summary>
        public Bullet Get(GameObject prefab)
        {
            var stack = GetStack(prefab);
            Bullet b = stack.Count > 0 ? stack.Pop() : CreateInstance(prefab);
            b.gameObject.SetActive(true);
            return b;
        }

        /// <summary>Deactivate and return a bullet to the pool it came from.</summary>
        public void Return(Bullet b)
        {
            b.gameObject.SetActive(false);
            GetStack(b.PrefabKey).Push(b);
        }

        Stack<Bullet> GetStack(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<Bullet>(64);
                _pools[prefab] = stack;
            }
            return stack;
        }

        Bullet CreateInstance(GameObject prefab)
        {
            GameObject go = Object.Instantiate(prefab, _container);
            go.SetActive(false);
            Bullet b = go.GetComponent<Bullet>();
            if (b == null) b = go.AddComponent<Bullet>();
            b.PrefabKey = prefab; // so Return() knows which pool to recycle into
            return b;
        }
    }
}
