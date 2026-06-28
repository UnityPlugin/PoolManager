#define POOL_LOG

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityPlugin
{
    public class PoolManager : Singleton<PoolManager>
    {
        Dictionary<GameObject, Queue<GameObject>> _pools = new();
        Dictionary<GameObject, List<GameObject>> _inUses = new();
        Dictionary<GameObject, GameObject> _objToPrefab = new();

        Dictionary<AsyncOperation, GameObject> _cacheOp = new();

        public void InitPool(GameObject prefab, int poolSize = 0)
        {
            if (prefab == null) return;

            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new();
                _pools[prefab] = pool;
            }

            if (!_inUses.TryGetValue(prefab, out var inUse))
            {
                inUse = new();
                _inUses[prefab] = inUse;
            }

            var currentSize = pool.Count + inUse.Count;
            if (poolSize > currentSize)
            {
                var op = InstantiateAsync(prefab, poolSize - currentSize);
                op.completed += OnInitCache;
                _cacheOp[op] = prefab;
            }
        }

        void OnInitCache(AsyncOperation operation)
        {
            operation.completed -= OnInitCache;

            var op = operation as AsyncInstantiateOperation<GameObject>;
            if (op != null && op.Result.Length > 0 && _cacheOp.TryGetValue(op, out var prefab))
            {
                GetPool(prefab, out var pool, out _);
                if (pool != null)
                {
                    var result = op.Result;
                    for (var i = 0; i < result.Length; i++)
                    {
                        pool.Enqueue(result[i]);
                    }
                }
            }
        }

        public GameObject Spawn(GameObject prefab, Transform parent = null)
        {
            GetPool(prefab, out var pool, out var inUse);
            if (pool == null) return null;

            if (pool.TryDequeue(out var result))
            {
                result.transform.SetParent(parent);
            }
            else
            {
                result = Instantiate(prefab, parent);
            }

            _objToPrefab[result] = prefab;
            if (inUse != null) inUse.Add(result);

            return result;
        }

        public void Recycle(GameObject instance)
        {
            var prefab = GetPrefab(instance);
#if POOL_LOG
            Debug.LogWarning($"[PoolManager] No prefab for {instance}", instance);
#endif
            GetPool(prefab, out var pool, out var inUse);

            if (inUse != null)
            {
                inUse.Remove(instance);
            }

            if (pool == null)
            {
                if (instance)
                {
#if POOL_LOG
                    Debug.LogWarning($"[PoolManager] Destroy instead recycle {instance}", instance);
#endif
                    Destroy(gameObject);
                }
                return;
            }

            pool.Enqueue(instance);
        }

        public void DestroyPool(GameObject prefab)
        {
            GetPool(prefab, out var pool, out var inUse);

            if (pool != null)
            {
                while (pool.TryDequeue(out var instance))
                {
                    _objToPrefab.Remove(instance);
                    if (instance) Destroy(instance);
                }
                pool.Clear();
                _pools.Remove(prefab);
            }

            if (inUse != null)
            {
                foreach (var instance in inUse)
                {
                    _objToPrefab.Remove(instance);
                    if (instance) Destroy(instance);
                }
                inUse.Clear();
                _inUses.Remove(prefab);
            }
        }

        void GetPool(GameObject prefab, out Queue<GameObject> pool, out List<GameObject> inUse)
        {
            pool = null;
            inUse = null;

            if (prefab == null)
            {
#if POOL_LOG
                Debug.LogWarning($"[PoolManager] GetPool failed by null object");
#endif
                return;
            }

            _pools.TryGetValue(prefab, out pool);
            _inUses.TryGetValue(prefab, out inUse);

#if POOL_LOG
            if (pool == null || inUse == null)
            {
                Debug.LogWarning($"[PoolManager] Pool is not init for {prefab}", prefab);
            }
#endif
        }

        GameObject GetPrefab(GameObject instance)
        {
            if (instance == null) return null;
            if (!_objToPrefab.TryGetValue(instance, out var prefab))
            {
                return null;
            }
            return prefab;
        }

#if POOL_LOG
        public static void LOG()
        {

        }
#endif
    }
}
