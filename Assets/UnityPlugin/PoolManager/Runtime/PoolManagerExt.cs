using UnityEngine;

namespace UnityPlugin.PoolManager
{
    public static class PoolManagerExt
    {
        #region GameObject

        public static void InitPool(this GameObject prefab, int poolSize = 0)
        {
            PoolManager.Instance?.InitPool(prefab, poolSize);
        }

        public static GameObject Spawn(this GameObject prefab, Transform parent = null)
        {
            return PoolManager.Instance?.Spawn(prefab, parent);
        }

        public static void Recycle(this GameObject prefab)
        {
            PoolManager.Instance?.Recycle(prefab);
        }

        #endregion

        #region Component

        public static void InitPool<T>(this T prefab, int poolSize = 0) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[PoolManager] null prefab for InitPool<T>");
                return;
            }

            PoolManager.Instance?.InitPool(prefab.gameObject, poolSize);
        }

        public static T Spawn<T>(this T prefab, Transform parent = null) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[PoolManager] null prefab for Spawn<T>");
                return null;
            }

            var go = PoolManager.Instance?.Spawn(prefab.gameObject, parent);
            return go?.GetComponent<T>();
        }

        public static void Recycle<T>(this T prefab) where T : Component
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[PoolManager] null prefab for Recycle<T>");
                return;
            }

            PoolManager.Instance?.Recycle(prefab.gameObject);
        }

        #endregion
    }
}
