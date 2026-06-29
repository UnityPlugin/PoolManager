#define POOL_DEBUG
using System.Collections.Generic;
using UnityEngine;

namespace UnityPlugin
{
    public class PoolManager : Singleton<PoolManager>
    {
        struct PoolObjDetail
        {
            public GameObject prefab;
            public IPoolable[] poolables;
        }

        Dictionary<GameObject, Queue<GameObject>> _pools = new();
        Dictionary<GameObject, List<GameObject>> _inUses = new();
        Dictionary<GameObject, PoolObjDetail> _objDetail = new();

        Dictionary<AsyncOperation, GameObject> _cacheOp = new();

        Transform _container;
        List<IPoolable> _getList = new();

        protected override void OnDestroy()
        {
            foreach (var pair in _pools)
            {
                pair.Value.Clear();
            }
            _pools.Clear();

            foreach (var pair in _inUses)
            {
                // outside manager, destroy
                foreach (var go in pair.Value)
                {
                    Destroy(go);
                }
                pair.Value.Clear();
            }
            _inUses.Clear();

            _objDetail.Clear();

            _cacheOp.Clear();
            _getList.Clear();

            base.OnDestroy();
        }

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
                GetPool(prefab, out var pool, out var inUse);
                if (pool == null || inUse == null) return;

                var count = pool.Count + inUse.Count;
                var container = GetContainer(prefab);
                var result = op.Result;

                for (var i = 0; i < result.Length; i++)
                {
                    var instance = result[i];
                    pool.Enqueue(instance);
                    instance.name = $"{prefab.name}_{count + i}";
                    instance.transform.SetParent(container);

                    CreatePoolableDetail(instance, prefab);

                    PoolableCallback(instance, onRecycle: true);
                }
            }
        }

        public GameObject Spawn(GameObject prefab, Transform parent = null)
        {
            GetPool(prefab, out var pool, out var inUse);
            if (pool == null || inUse == null) return null;

            if (pool.TryDequeue(out var result))
            {
                result.transform.SetParent(parent);
            }
            else
            {
                result = Instantiate(prefab, parent);

                var count = pool.Count + inUse.Count;
                result.name = $"{prefab.name}_{count}";

                CreatePoolableDetail(result, prefab);
            }

            if (inUse != null) inUse.Add(result);

            PoolableCallback(result, onSpawn: true);

            return result;
        }

        public void Recycle(GameObject instance)
        {
            var prefab = GetPrefab(instance);
#if POOL_DEBUG
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
#if POOL_DEBUG
                    Debug.LogWarning($"[PoolManager] Destroy instead recycle {instance}", instance);
#endif
                    Destroy(gameObject);
                }
                return;
            }

            pool.Enqueue(instance);
            instance.transform.SetParent(GetContainer(prefab));

            PoolableCallback(instance, onRecycle: true);
        }

        public void DestroyPool(GameObject prefab)
        {
            GetPool(prefab, out var pool, out var inUse);

            if (pool != null)
            {
                while (pool.TryDequeue(out var instance))
                {
                    _objDetail.Remove(instance);
                    if (instance) Destroy(instance);
                }
                pool.Clear();
                _pools.Remove(prefab);
            }

            if (inUse != null)
            {
                foreach (var instance in inUse)
                {
                    _objDetail.Remove(instance);
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
#if POOL_DEBUG
                Debug.LogWarning($"[PoolManager] GetPool failed by null object");
#endif
                return;
            }

            _pools.TryGetValue(prefab, out pool);
            _inUses.TryGetValue(prefab, out inUse);

#if POOL_DEBUG
            if (pool == null || inUse == null)
            {
                Debug.LogWarning($"[PoolManager] Pool is not init for {prefab}", prefab);
            }
#endif
        }

        GameObject GetPrefab(GameObject instance)
        {
            if (instance == null) return null;
            if (!_objDetail.TryGetValue(instance, out var detail))
            {
                return null;
            }
            return detail.prefab;
        }

        Transform GetContainer(GameObject prefab)
        {
            if (_container == null)
            {
                var go = new GameObject("Root");
                go.transform.SetParent(transform);
                go.SetActive(false);

                _container = go.transform;
            }

            if (prefab)
            {
#if POOL_DEBUG || UNITY_EDITOR
                var transform = _container.Find(prefab.name);
                if (transform == null)
                {
                    var go = new GameObject(prefab.name);
                    go.transform.SetParent(_container);
                    transform = go.transform;
                }

                return transform;
#endif
            }
            return _container;
        }

        #region IPoolable

        void CreatePoolableDetail(GameObject instance, GameObject prefab)
        {
            if (instance == null || prefab == null) return;
            if (_objDetail.ContainsKey(instance)) return;

            _getList.Clear();
            instance.GetComponents(_getList);

            _objDetail[instance] = new PoolObjDetail
            {
                prefab = prefab,
                poolables = _getList.Count > 0 ? _getList.ToArray() : null,
            };
        }

        void PoolableCallback(GameObject instance, bool onSpawn = false, bool onRecycle = false)
        {
            if (instance == null) return;

            var poolables = _objDetail[instance].poolables;
            if (poolables != null)
            {
                var l = poolables.Length;
                for (var i = 0; i < l; i++)
                {
                    if (onSpawn) poolables[i].OnRecycle();
                    if (onRecycle) poolables[i].OnRecycle();
                }
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        public Dictionary<GameObject, Queue<GameObject>> GetPools()
        {
            return _pools;
        }

        public Dictionary<GameObject, List<GameObject>> GetInUses()
        {
            return _inUses;
        }
#endif

        #endregion
    }
}
