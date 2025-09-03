using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XGame.Core;

namespace XGame.Editor
{
    [CustomEditor(typeof(CachePoolDebugger))]
    internal class CachePoolInspector : InspectorBase
    {
        private readonly Dictionary<string, List<CacheCollectionInfo>> _cacheCollectionInfosDict = new();
        private readonly HashSet<string> _expandedFoldout = new();
        private bool _showFullTypeName = false;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Available in play mode only.", MessageType.Info);
                return;
            }

            // 选项：是否显示完整类型名
            var targetObject = target as CachePoolDebugger;

            _showFullTypeName = EditorGUILayout.Toggle("Show Full Type Name", _showFullTypeName);

            // 获取缓存池信息
            _cacheCollectionInfosDict.Clear();
            CacheCollectionInfo[] cacheCollectionInfoArray = CachePool.GetAllCacheCollectionInfos();
            foreach (CacheCollectionInfo cacheCollectionInfo in cacheCollectionInfoArray)
            {
                string assemblyName = cacheCollectionInfo.CacheType.Assembly.GetName().Name;
                if (!_cacheCollectionInfosDict.TryGetValue(assemblyName, out List<CacheCollectionInfo> cacheCollectionInfos))
                {
                    cacheCollectionInfos = new List<CacheCollectionInfo>();
                    _cacheCollectionInfosDict.Add(assemblyName, cacheCollectionInfos);
                }
                cacheCollectionInfos.Add(cacheCollectionInfo);
            }

            foreach (KeyValuePair<string, List<CacheCollectionInfo>> assemblyNameAndCacheCollectionInfosPair in _cacheCollectionInfosDict)
            {
                string assemblyName = assemblyNameAndCacheCollectionInfosPair.Key;
                List<CacheCollectionInfo> cacheCollectionInfos = assemblyNameAndCacheCollectionInfosPair.Value;
                // 每一个程序集对应一个折叠框
                bool isExpanded = _expandedFoldout.Contains(assemblyName);
                bool isExpandedByUser = EditorGUILayout.Foldout(isExpanded, assemblyName);
                if (isExpandedByUser != isExpanded)
                {
                    if (isExpandedByUser)
                    {
                        _expandedFoldout.Add(assemblyName);
                    }
                    else
                    {
                        _expandedFoldout.Remove(assemblyName);
                    }
                }
                if (isExpanded)
                {
                    // 垂直盒子中显示具体的缓存池信息
                    using (new EditorGUILayout.VerticalScope("box"))
                    {
                        // 居中样式
                        GUIStyle centeredStyle = new(GUI.skin.label)
                        {
                            alignment = TextAnchor.MiddleCenter
                        };
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(_showFullTypeName ? "Full Type Name" : "Type Name", EditorStyles.wordWrappedLabel, GUILayout.Width(200));
                            // EditorGUILayout.LabelField(_showFullTypeName ? "Full Type Name" : "Type Name", GUILayout.Width(500);
                            EditorGUILayout.LabelField("Unused", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Using", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Spawned", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Unspawned", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Created", centeredStyle, GUILayout.Width(80));
                            EditorGUILayout.LabelField("Discarded", centeredStyle, GUILayout.Width(80));
                        }
                        cacheCollectionInfos.Sort(CompareCacheCollectionInfo);
                        foreach (CacheCollectionInfo cacheCollectionInfo in cacheCollectionInfos)
                        {
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayout.Label(_showFullTypeName ? cacheCollectionInfo.CacheType.FullName : cacheCollectionInfo.CacheType.Name, EditorStyles.wordWrappedLabel, GUILayout.Width(200));
                                // EditorGUILayout.LabelField(_showFullTypeName ? cacheCollectionInfo.CacheType.FullName : cacheCollectionInfo.CacheType.Name, GUILayout.Width(500));
                                EditorGUILayout.LabelField(cacheCollectionInfo.UnusedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.UsingCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.SpawnedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.UnspawnedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.CreatedCount.ToString(), centeredStyle, GUILayout.Width(80));
                                EditorGUILayout.LabelField(cacheCollectionInfo.DiscardedCount.ToString(), centeredStyle, GUILayout.Width(80));
                            }
                        }
                    }
                    EditorGUILayout.Separator();
                }
            }
            Repaint();
        }

        private int CompareCacheCollectionInfo(CacheCollectionInfo a, CacheCollectionInfo b)
        {
            if (_showFullTypeName)
            {
                return a.CacheType.FullName.CompareTo(b.CacheType.FullName);
            }
            else
            {
                return a.CacheType.Name.CompareTo(b.CacheType.Name);
            }
        }
    }
}