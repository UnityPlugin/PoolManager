using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityPlugin
{
    [CustomEditor(typeof(PoolManager))]
    public class PoolManagerEditor : Editor
    {
        PoolManager _target;
        Dictionary<GameObject, Queue<GameObject>> _pools;
        Dictionary<GameObject, List<GameObject>> _inUses;

        Dictionary<GameObject, bool> _fold;

        void OnEnable()
        {
            _target = target as PoolManager;
            _pools = _target.GetPools();
            _inUses = _target.GetInUses();

            _fold = new();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            using (var poolsScope = IMGUIUtils.Foldout("Pools"))
            {
                if (poolsScope.fold)
                {
                    EditorGUILayout.LabelField(IMGUIUtils.GetGUIContent("Pool Size"), IMGUIUtils.GetGUIContent(_pools.Count.ToString()));

                    foreach (var pair in _pools)
                    {
                        IMGUIUtils.ObjectField("Prefab", pair.Key);

                        _fold.TryGetValue(pair.Key, out var fold);
                        if (IMGUIUtils.IsLastControlClick()) fold = !fold;

                        if (fold)
                        {
                            using (IMGUIUtils.Vertical(true))
                            {
                                using (var prefabPoolScope = IMGUIUtils.Foldout($"{pair.Key.name}_pool"))
                                {
                                    prefabPoolScope.name.text = $"In Pool : {pair.Value.Count}";
                                    if (prefabPoolScope.fold && pair.Value.Count > 0)
                                    {
                                        foreach (var p in pair.Value)
                                        {
                                            IMGUIUtils.ObjectField("", p);
                                        }
                                    }
                                }

                                using (var prefabUseScope = IMGUIUtils.Foldout($"{pair.Key.name}_use"))
                                {
                                    var inUseCount = 0;
                                    if (_inUses.TryGetValue(pair.Key, out var inUse))
                                    {
                                        inUseCount = inUse.Count;
                                    }

                                    prefabUseScope.name.text = $"In Use : {inUseCount}";
                                    if (prefabUseScope.fold && inUseCount > 0)
                                    {
                                        foreach (var p in inUse)
                                        {
                                            IMGUIUtils.ObjectField("", p);
                                        }
                                    }
                                }
                            }
                        }

                        _fold[pair.Key] = fold;
                    }
                }
            }
        }

    }
}
