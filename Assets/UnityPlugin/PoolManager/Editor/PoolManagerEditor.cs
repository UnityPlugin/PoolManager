using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityPlugin.Bridge;
using UnityPlugin.EditorUtils;

namespace UnityPlugin.PoolManager
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

            using (var poolsScope = IMGUI.Foldout("Pools"))
            {
                if (poolsScope.fold)
                {
                    EditorGUILayout.LabelField(IMGUI.GetC("Pool Size"), IMGUI.TmpC(_pools.Count.ToString()));

                    var index = 0;
                    var sb = UnityGenericPool<StringBuilder>.Get();
                    try
                    {
                        foreach (var pair in _pools)
                        {
                            sb.Clear()
                            .Append("Prefab_").Append(pair.Key.GetInstanceID());
                            var key = sb.ToString();
                            var label = IMGUI.GetC(key);

                            var inPoolCount = pair.Value.Count;
                            var inUseCount = 0;
                            if (_inUses.TryGetValue(pair.Key, out var inUse)) inUseCount = inUse.Count;

                            sb.Clear()
                            .Append(index).Append(" : ").Append(pair.Key.name).Append(' ')
                            .Append('(').Append(inPoolCount + inUseCount).Append(')');
                            label.text = sb.ToString();

                            IMGUI.ObjectField(key, pair.Key);
                            _fold.TryGetValue(pair.Key, out var fold);
                            if (IMGUI.IsLastControlClick()) fold = !fold;

                            if (fold)
                            {
                                using (IMGUI.Vertical(true))
                                {
                                    sb.Clear()
                                    .Append(pair.Key.name).Append("_pool");
                                    using (var prefabPoolScope = IMGUI.Foldout(sb.ToString()))
                                    {
                                        sb.Clear()
                                        .Append("In Pool : ").Append(inPoolCount);

                                        prefabPoolScope.name.text = sb.ToString();
                                        if (prefabPoolScope.fold && inPoolCount > 0)
                                        {
                                            foreach (var p in pair.Value)
                                            {
                                                IMGUI.ObjectField("", p);
                                            }
                                        }
                                    }

                                    sb.Clear();
                                    sb.Append(pair.Key.name).Append("_use");
                                    using (var prefabUseScope = IMGUI.Foldout(sb.ToString()))
                                    {
                                        sb.Clear();
                                        sb.Append("In Use : ").Append(inUseCount);
                                        prefabUseScope.name.text = sb.ToString();
                                        if (prefabUseScope.fold && inUseCount > 0)
                                        {
                                            foreach (var p in inUse)
                                            {
                                                IMGUI.ObjectField("", p);
                                            }
                                        }
                                    }
                                }
                            }

                            _fold[pair.Key] = fold;
                            index++;
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
