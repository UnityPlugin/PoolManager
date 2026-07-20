using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityPlugin.Bridge;

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

            _fold = new Dictionary<GameObject, bool>();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            using (var poolsScope = IMGUIUtils.Foldout("Pools"))
            {
                if (poolsScope.fold)
                {
                    EditorGUILayout.LabelField(IMGUIUtils.GetGUIContent("Pool Size"), IMGUIUtils.GetGUIContent(_pools.Count.ToString()));

                    var sb = UnityGenericPool<StringBuilder>.Get();
                    try
                    {
                        foreach (var pair in _pools)
                        {
                            sb.Clear();
                            sb.Append("Prefab_").Append(pair.Key.GetInstanceID());
                            var key = sb.ToString();
                            var label = IMGUIUtils.GetGUIContent(key);

                            var inPoolCount = pair.Value.Count;
                            var inUseCount = 0;
                            if (_inUses.TryGetValue(pair.Key, out var inUse)) inUseCount = inUse.Count;

                            sb.Clear();
                            sb.Append(pair.Key.name)
                            .Append('(').Append(inPoolCount + inUseCount).Append(')');
                            label.text = sb.ToString();

                            IMGUIUtils.ObjectField(key, pair.Key);
                            _fold.TryGetValue(pair.Key, out var fold);
                            if (IMGUIUtils.IsLastControlClick()) fold = !fold;

                            if (fold)
                            {
                                using (IMGUIUtils.Vertical(true))
                                {
                                    sb.Clear();
                                    sb.Append(pair.Key.name).Append("_pool");
                                    using (var prefabPoolScope = IMGUIUtils.Foldout(sb.ToString()))
                                    {
                                        sb.Clear();
                                        sb.Append("In Pool : ").Append(inPoolCount);

                                        prefabPoolScope.name.text = sb.ToString();
                                        if (prefabPoolScope.fold && inPoolCount > 0)
                                        {
                                            foreach (var p in pair.Value)
                                            {
                                                IMGUIUtils.ObjectField("", p);
                                            }
                                        }
                                    }

                                    sb.Clear();
                                    sb.Append(pair.Key.name).Append("_use");
                                    using (var prefabUseScope = IMGUIUtils.Foldout(sb.ToString()))
                                    {
                                        sb.Clear();
                                        sb.Append("In Use : ").Append(inUseCount);
                                        prefabUseScope.name.text = sb.ToString();
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
                    finally
                    {
                        UnityGenericPool<StringBuilder>.Release(sb);
                    }
                }
            }
        }

    }
}
