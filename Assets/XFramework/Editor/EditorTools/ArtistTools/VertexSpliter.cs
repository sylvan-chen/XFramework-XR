using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace XGame.Editor
{
    /// <summary>
    /// 顶点拆分工具
    /// </summary>
    public class VertexSpliter : EditorWindow
    {
        private List<string> _prefabDirs = new()
        {
            "Assets/Res/prefabs/avatar/top",
            "Assets/Res/prefabs/avatar/bottom",
            "Assets/Res/prefabs/avatar/suit",
            "Assets/Res/prefabs/avatar/shoesl",
            "Assets/Res/prefabs/avatar/shoesr",
        };

        private string _savePath = "Assets/Res/splited_meshes/";

        private bool _isAutoApply = false;
        private bool _skipExist = false;
        private bool _showClearSettings = false;

        private readonly List<GameObject> _prefabs = new();
        private bool _isProcessing = false;
        private Vector2 _scrollPosition;
        private Vector2 _pathScrollPosition;

        private bool _checkNormal = true;
        private bool _checkBoneWeight = true;
        private bool _checkVertexColorGray = false;

        [MenuItem("Tools/美术工具/顶点拆分工具")]
        public static void ShowWindow()
        {
            var window = GetWindow<VertexSpliter>("顶点拆分工具");
            window.minSize = new Vector2(600, 800);
            // window.CollectPrefabs();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("顶点拆分工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"目标预制体目录:");

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_pathScrollPosition, GUILayout.Height(150)))
            {
                _pathScrollPosition = scrollView.scrollPosition;

                for (int i = 0; i < _prefabDirs.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _prefabDirs[i] = EditorGUILayout.TextField($"目录 {i + 1}:", _prefabDirs[i]);

                        if (GUILayout.Button("浏览", GUILayout.Width(50)))
                        {
                            string selectedPath = EditorUtility.OpenFolderPanel("选择预制体目录", _prefabDirs[i], "");
                            if (!string.IsNullOrEmpty(selectedPath))
                            {
                                // 转换为相对路径
                                string relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                                _prefabDirs[i] = relativePath;
                            }
                        }

                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            _prefabDirs.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("添加目录"))
                {
                    _prefabDirs.Add("Assets/");
                }
                if (GUILayout.Button("重置为默认"))
                {
                    _prefabDirs = new List<string>()
                    {
                        "Assets/Res/prefabs/avatar/top",
                        "Assets/Res/prefabs/avatar/bottom",
                        "Assets/Res/prefabs/avatar/suit",
                        "Assets/Res/prefabs/avatar/shoesl",
                        "Assets/Res/prefabs/avatar/shoesr",
                    };
                }
            }

            if (GUILayout.Button("收集预制体"))
            {
                CollectPrefabs();
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                _savePath = EditorGUILayout.TextField("保存路径:", _savePath);

                if (GUILayout.Button("浏览", GUILayout.Width(50)))
                {
                    string selectedPath = EditorUtility.OpenFolderPanel("选择保存路径", _savePath, "");
                    if (!string.IsNullOrEmpty(selectedPath))
                    {
                        // 转换为相对路径
                        string relativePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                        if (!relativePath.EndsWith("/"))
                        {
                            relativePath += "/";
                        }
                        _savePath = relativePath;
                    }
                }
            }

            EditorGUILayout.Space();

            _showClearSettings = EditorGUILayout.Foldout(_showClearSettings, "网格清理", EditorStyles.boldFont);
            if (_showClearSettings)
            {
                EditorGUI.indentLevel++;

                _checkNormal = EditorGUILayout.Toggle("检查法线完整性", _checkNormal);
                _checkBoneWeight = EditorGUILayout.Toggle("检查骨骼权重完整性", _checkBoneWeight);
                _checkVertexColorGray = EditorGUILayout.Toggle("检查顶点色是否为灰度", _checkVertexColorGray);

                if (GUILayout.Button("检查并清理网格资源"))
                {
                    ValidateMeshAssets();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"找到 {_prefabs.Count} 个预制体:");

            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition, GUILayout.Height(250)))
            {
                _scrollPosition = scrollView.scrollPosition;

                foreach (var prefab in _prefabs)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);

                    var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                    EditorGUILayout.LabelField($"({renderers.Length} 个渲染器)", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            _isAutoApply = EditorGUILayout.Toggle("处理后自动应用到预制体", _isAutoApply);

            _skipExist = EditorGUILayout.Toggle("跳过已存在的网格", _skipExist);

            EditorGUILayout.Space();

            using (new EditorGUI.DisabledGroupScope(_isProcessing || _prefabs.Count == 0))
            {
                if (GUILayout.Button("开始批量处理", GUILayout.Height(30)))
                {
                    ProcessAllPrefabs();
                }
            }

            if (_isProcessing)
            {
                EditorGUILayout.HelpBox("正在处理中，请稍候...", MessageType.Info);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("还原所有预制体网格", GUILayout.Height(30)))
            {
                RevertAllMeshes();
            }
        }

        private void CollectPrefabs()
        {
            _prefabs.Clear();

            foreach (var dir in _prefabDirs)
            {
                if (!Directory.Exists(dir))
                {
                    Debug.LogWarning($"路径不存在: {dir}");
                    return;
                }

                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { dir });

                foreach (string guid in prefabGuids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                    if (prefab != null)
                    {
                        var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                        if (renderers.Length > 0)
                        {
                            _prefabs.Add(prefab);
                        }
                    }
                }

                Debug.Log($"从 '{dir}' 找到 {_prefabs.Count} 个预制体");
            }
        }

        private void ProcessAllPrefabs()
        {
            _isProcessing = true;
            int processedCount = 0;
            int totalCount = _prefabs.Count;

            try
            {
                if (!Directory.Exists(_savePath))
                {
                    Directory.CreateDirectory(_savePath);
                    AssetDatabase.Refresh();
                }

                foreach (var prefab in _prefabs)
                {
                    EditorUtility.DisplayProgressBar("批量网格分离",
                        $"正在处理: {prefab.name} ({processedCount + 1}/{totalCount})", (float)processedCount / totalCount);

                    ProcessPrefab(prefab);
                    processedCount++;
                }

                EditorUtility.DisplayDialog("完成", $"批量处理完成！\n成功处理了 {processedCount}/{totalCount} 个预制体。", "确定");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"批量分离网格失败: {ex.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isProcessing = false;
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private void ProcessPrefab(GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMesh == null)
                {
                    Debug.LogWarning($"预制体 {prefab.name} 的渲染器没有共享网格，跳过处理。");
                    continue;
                }

                var splitedMesh = SplitMesh(renderer.sharedMesh);
                if (splitedMesh != null)
                {
                    SaveMeshAsAsset(splitedMesh);

                    if (_isAutoApply) renderer.sharedMesh = splitedMesh;

                    Debug.Log($"已处理: {prefab.name}.{renderer.name}.{renderer.sharedMesh.name} -> {splitedMesh.name}");
                }
            }
        }

        public Mesh SplitMesh(Mesh originalMesh)
        {
            if (originalMesh == null)
            {
                Debug.LogError("No mesh found on the SkinnedMeshRenderer!");
                return null;
            }

            var newMesh = new Mesh()
            {
                name = $"{originalMesh.name}_splited",
            };

            var originalVertices = originalMesh.vertices;
            var originalNormals = originalMesh.normals;
            var originalTangents = originalMesh.tangents;
            var originalUVs = originalMesh.uv;
            var originalBonesPerVertex = originalMesh.GetBonesPerVertex().ToArray();
            var originalBoneWeights = originalMesh.GetAllBoneWeights().ToArray();
            var originalColors = originalMesh.colors;

            // 分析顶点使用情况
            var vertexSubMeshUsage = new Dictionary<int, HashSet<int>>();
            for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; subMeshIndex++)
            {
                var subTriangles = originalMesh.GetTriangles(subMeshIndex);
                Debug.Log($"分析子网格 {subMeshIndex}，索引数量：{subTriangles.Length}");
                if (!vertexSubMeshUsage.ContainsKey(subMeshIndex))
                {
                    vertexSubMeshUsage[subMeshIndex] = new HashSet<int>();
                }
                foreach (var vertexIndex in subTriangles)
                {
                    vertexSubMeshUsage[subMeshIndex].Add(vertexIndex);
                }
            }

            Debug.Log($"子网格数量：{originalMesh.subMeshCount}, 各子网格使用顶点数量：{string.Join(", ", vertexSubMeshUsage.Select(kvp => kvp.Value.Count))}");

            var newVertices = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var newTangents = new List<Vector4>();
            var newUVs = new List<Vector2>();
            var newColors = new List<Color>();
            var newBonesPerVertex = new List<byte>();
            var newBoneWeights = new List<BoneWeight1>();
            var newSubTriangles = new List<int>[originalMesh.subMeshCount];

            var originalIndexToNewIndex = new Dictionary<int, Dictionary<int, int>>();

            // 预先计算骨骼权重的起始索引
            var boneWeightStartIndex = new int[originalMesh.vertexCount + 1];
            int currentBoneWeightIndex = 0;
            for (int i = 0; i < originalMesh.vertexCount; i++)
            {
                boneWeightStartIndex[i] = currentBoneWeightIndex;
                currentBoneWeightIndex += originalBonesPerVertex[i];
            }
            boneWeightStartIndex[originalMesh.vertexCount] = currentBoneWeightIndex;

            foreach (var kvp in vertexSubMeshUsage)
            {
                int submeshIndex = kvp.Key;
                var usedBySubMeshes = kvp.Value;


                foreach (int originalIndex in usedBySubMeshes)
                {
                    int newIndex = newVertices.Count;

                    newVertices.Add(originalVertices[originalIndex]);

                    if (originalNormals != null && originalNormals.Length > originalIndex)
                        newNormals.Add(originalNormals[originalIndex]);

                    if (originalTangents != null && originalTangents.Length > originalIndex)
                        newTangents.Add(originalTangents[originalIndex]);

                    if (originalUVs != null && originalUVs.Length > originalIndex)
                        newUVs.Add(originalUVs[originalIndex]);

                    if (originalColors != null && originalColors.Length > originalIndex)
                        newColors.Add(originalColors[originalIndex]);
                    else
                        newColors.Add(Color.white);

                    if (!originalIndexToNewIndex.ContainsKey(originalIndex))
                        originalIndexToNewIndex[originalIndex] = new Dictionary<int, int>();
                    originalIndexToNewIndex[originalIndex][submeshIndex] = newIndex;

                    // 处理骨骼信息
                    if (originalBonesPerVertex != null && originalBonesPerVertex.Length > originalIndex)
                    {
                        var boneCount = originalBonesPerVertex[originalIndex];

                        newBonesPerVertex.Add(boneCount);

                        // 复制这个顶点的所有骨骼权重
                        int startIndex = boneWeightStartIndex[originalIndex];
                        for (int boneIdx = 0; boneIdx < boneCount; boneIdx++)
                        {
                            if (startIndex + boneIdx < originalBoneWeights.Length)
                            {
                                newBoneWeights.Add(originalBoneWeights[startIndex + boneIdx]);
                            }
                        }
                    }
                }
            }

            Debug.Log($"源顶点数：{originalMesh.vertexCount}，新顶点数：{newVertices.Count}");

            // 重新构建三角形索引
            for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; subMeshIndex++)
            {
                newSubTriangles[subMeshIndex] = new List<int>();
                var originalTriangles = originalMesh.GetTriangles(subMeshIndex);

                for (int i = 0; i < originalTriangles.Length; i++)
                {
                    int originalVertexIndex = originalTriangles[i];
                    int newVertexIndex = originalIndexToNewIndex[originalVertexIndex][subMeshIndex];
                    newSubTriangles[subMeshIndex].Add(newVertexIndex);
                }
            }

            newMesh.SetVertices(newVertices);
            if (newNormals.Count > 0)
                newMesh.SetNormals(newNormals);
            if (newTangents.Count > 0)
                newMesh.SetTangents(newTangents);
            if (newUVs.Count > 0)
                newMesh.SetUVs(0, newUVs);

            newMesh.SetColors(newColors);
            newMesh.subMeshCount = originalMesh.subMeshCount;

            for (int subMeshIndex = 0; subMeshIndex < originalMesh.subMeshCount; subMeshIndex++)
            {
                newMesh.SetTriangles(newSubTriangles[subMeshIndex], subMeshIndex);
            }

            newMesh.bindposes = originalMesh.bindposes;

            var newBonesPerVertexArray = new NativeArray<byte>(newBonesPerVertex.ToArray(), Allocator.Persistent);
            var newBoneWeightsArray = new NativeArray<BoneWeight1>(newBoneWeights.ToArray(), Allocator.Persistent);

            newMesh.SetBoneWeights(newBonesPerVertexArray, newBoneWeightsArray);

            newBonesPerVertexArray.Dispose();
            newBoneWeightsArray.Dispose();

            newMesh.RecalculateBounds();

            return newMesh;
        }

        private void SaveMeshAsAsset(Mesh mesh)
        {
            string fileName = $"{mesh.name}.asset";
            // 如果 fileName 存在，增加后缀
            int index = 1;
            while (File.Exists(Path.Combine(_savePath, fileName)))
            {
                if (_skipExist) return;

                fileName = $"{mesh.name}_{index++}.asset";
            }
            string fullPath = Path.Combine(_savePath, fileName);

            // 保存网格资源
            AssetDatabase.CreateAsset(mesh, fullPath);
        }

        private void ValidateMeshAssets()
        {
            if (!Directory.Exists(_savePath))
            {
                EditorUtility.DisplayDialog("错误", $"保存路径不存在: {_savePath}", "确定");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog("确认清理",
                $"将检查并清理路径 '{_savePath}' 下的网格资源。\n\n" +
                "确定要继续吗？",
                "确定", "取消");

            if (!confirmed) return;

            int totalCount = 0;
            int deletedCount = 0;
            var deletedFiles = new List<string>();
            var fileNameToIssues = new Dictionary<string, List<string>>();

            try
            {
                // 获取_savePath下所有资源
                string[] meshGuids = AssetDatabase.FindAssets("t:Mesh", new[] { _savePath });
                totalCount = meshGuids.Length;

                if (totalCount == 0)
                {
                    EditorUtility.DisplayDialog("完成",
                        $"在路径 '{_savePath}' 下没有找到任何网格文件。",
                        "确定");
                    return;
                }

                EditorUtility.DisplayProgressBar("检查网格资源", "正在检查...", 0f);

                for (int i = 0; i < meshGuids.Length; i++)
                {
                    string guid = meshGuids[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = Path.GetFileName(path);
                    Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                    EditorUtility.DisplayProgressBar("检查网格资源",
                        $"正在检查: {Path.GetFileName(path)} ({i + 1}/{totalCount})",
                        (float)i / totalCount);

                    var fileIssues = new List<string>();

                    // 检查网格是否损坏
                    if (mesh == null)
                    {
                        fileIssues.Add("网格损坏或无法加载");
                        Debug.Log($"网格资源损坏或无法加载: {path}");
                        AssetDatabase.DeleteAsset(path);
                        deletedFiles.Add(fileName);
                        deletedCount++;
                        continue;
                    }

                    bool shouldDelete = false;

                    // 检查顶点数据完整性
                    if (mesh.vertexCount == 0)
                    {
                        fileIssues.Add("网格顶点数据为空");
                        Debug.Log($"网格顶点数据为空: {path}");
                        shouldDelete = true;
                    }
                    else if (mesh.triangles == null || mesh.triangles.Length == 0)
                    {
                        fileIssues.Add("网格三角形数据为空");
                        Debug.Log($"网格三角形数据为空: {path}");
                        shouldDelete = true;
                    }

                    // 检查子网格完整性
                    if (!shouldDelete && mesh.subMeshCount > 0)
                    {
                        for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                        {
                            var triangles = mesh.GetTriangles(subMeshIndex);
                            if (triangles.Length == 0)
                            {
                                fileIssues.Add($"子网格 {subMeshIndex} 没有三角形数据");
                                Debug.Log($"子网格 {subMeshIndex} 没有三角形数据: {path}");
                                shouldDelete = true;
                            }
                            else if (triangles.Length % 3 != 0)
                            {
                                fileIssues.Add($"子网格 {subMeshIndex} 三角形索引数不是3的倍数");
                                Debug.Log($"子网格 {subMeshIndex} 三角形索引数不是3的倍数: {path}");
                                shouldDelete = true;
                            }
                        }
                    }

                    // 检查法线
                    if (!shouldDelete && _checkNormal)
                    {
                        Vector3[] normals = mesh.normals;
                        if (normals == null || normals.Length == 0)
                        {
                            fileIssues.Add("法线数据为空");
                            Debug.Log($"法线数据为空: {path}");
                            shouldDelete = true;
                        }
                        if (normals != null && normals.Length > 0 && normals.Length != mesh.vertexCount)
                        {
                            fileIssues.Add("法线数量与顶点数量不匹配");
                            Debug.Log($"法线数量与顶点数量不匹配: {path}");
                            shouldDelete = true;
                        }
                    }

                    // 检查骨骼权重
                    if (!shouldDelete && _checkBoneWeight)
                    {
                        BoneWeight[] boneWeights = mesh.boneWeights;
                        if (boneWeights == null || boneWeights.Length == 0)
                        {
                            fileIssues.Add("骨骼权重为空");
                            Debug.Log($"骨骼权重为空: {path}");
                            shouldDelete = true;
                        }
                        if (boneWeights != null && boneWeights.Length > 0)
                        {
                            if (boneWeights.Length != mesh.vertexCount)
                            {
                                fileIssues.Add("骨骼权重数量与顶点数量不匹配");
                                Debug.Log($"骨骼权重数量与顶点数量不匹配: {path}");
                                shouldDelete = true;
                            }
                            else
                            {
                                // 检查权重总和
                                for (int w = 0; w < boneWeights.Length; w++)
                                {
                                    float weightSum = boneWeights[w].weight0 + boneWeights[w].weight1 +
                                                    boneWeights[w].weight2 + boneWeights[w].weight3;
                                    if (Mathf.Abs(weightSum - 1.0f) > 0.01f)
                                    {
                                        fileIssues.Add($"顶点 {w} 的骨骼权重总和不等于 1 (实际: {weightSum:F3})");
                                        Debug.Log($"顶点 {w} 的骨骼权重总和不等于 1 (实际: {weightSum:F3}): {path}");
                                        shouldDelete = true;
                                        break; // 只报告第一个错误，避免太多日志
                                    }
                                }
                            }
                        }
                    }

                    // 检查顶点色是否为灰度
                    if (!shouldDelete && _checkVertexColorGray)
                    {
                        Color[] vertexColors = mesh.colors;
                        if (vertexColors != null && vertexColors.Length > 0)
                        {
                            bool allGray = true;
                            foreach (Color color in vertexColors)
                            {
                                float r = color.r;
                                float g = color.g;
                                float b = color.b;
                                if (r != g || r != b || g != b)
                                {
                                    allGray = false;
                                    break;
                                }
                            }
                            if (!allGray)
                            {
                                fileIssues.Add("顶点色不为灰度");
                                Debug.Log($"顶点色不为灰度: {path}");
                                shouldDelete = true;
                            }
                        }
                    }

                    // 执行删除或记录修复
                    if (shouldDelete)
                    {
                        AssetDatabase.DeleteAsset(path);
                        deletedFiles.Add(fileName);
                        deletedCount++;
                    }

                    if (fileIssues.Count > 0)
                    {
                        fileNameToIssues[fileName] = fileIssues;
                    }
                }

                AssetDatabase.Refresh();

                // 显示结果
                ShowValidationResults(totalCount, deletedCount, deletedFiles, fileNameToIssues);

                Debug.Log($"网格资源验证完成 - 检查: {totalCount}, 删除: {deletedCount}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"验证网格资源时发生错误: {ex.Message}");
                EditorUtility.DisplayDialog("错误",
                    $"验证过程中发生错误:\n{ex.Message}",
                    "确定");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private void ShowValidationResults(int totalCount, int deletedCount, List<string> deletedFiles, Dictionary<string, List<string>> fileNameToIssues)
        {
            string resultMessage = $"检查完成！\n\n" +
                                   $"总共检查: {totalCount} 个网格文件\n" +
                                   $"删除文件: {deletedCount} 个\n";

            if (deletedCount > 0)
            {
                resultMessage += $"\n删除的文件:\n";
                foreach (string fileName in deletedFiles)
                {
                    resultMessage += $"• {fileName}\n";
                }
            }

            if (fileNameToIssues.Count > 0)
            {
                resultMessage += "\n存在问题的文件:\n";
                foreach (var kvp in fileNameToIssues.Take(5))
                {
                    resultMessage += $"\n{kvp.Key}:\n";
                    foreach (var issue in kvp.Value)
                    {
                        resultMessage += $"  - {issue}\n";
                    }
                }
                if (fileNameToIssues.Count > 5)
                {
                    resultMessage += $"\n...还有 {fileNameToIssues.Count - 5} 个问题文件未显示 (详见控制台日志)";
                }
            }

            EditorUtility.DisplayDialog(deletedCount > 0 ? "清理完成" : "检查完成",
                resultMessage, "确定");
        }

        /// <summary>
        /// 复原所有服装的网格为分离之前
        /// </summary>
        private void RevertAllMeshes()
        {
            if (_prefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "没有收集到任何预制体，请先点击\"收集预制体\"按钮。", "确定");
                return;
            }

            int totalRenderers = 0;
            int affectedPrefabs = 0;

            foreach (var prefab in _prefabs)
            {
                var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                if (renderers.Length > 0)
                {
                    totalRenderers += renderers.Length;
                    affectedPrefabs++;
                }
            }

            // 确认对话框
            bool confirmed = EditorUtility.DisplayDialog("确认还原",
                $"将还原以下预制体的网格到原始状态：\n\n" +
                $"• 预制体数量: {affectedPrefabs} 个\n" +
                $"• 渲染器数量: {totalRenderers} 个\n\n" +
                "此操作将撤销所有网格修改，恢复到分离前的状态。\n\n" +
                "确定要继续吗？",
                "确定还原", "取消");

            if (!confirmed) return;

            int processedPrefabs = 0;
            int processedRenderers = 0;
            int revertedRenderers = 0;
            var revertedPrefabs = new List<string>();

            try
            {
                foreach (var prefab in _prefabs)
                {
                    EditorUtility.DisplayProgressBar("还原网格",
                        $"正在处理: {prefab.name} ({processedPrefabs + 1}/{affectedPrefabs})",
                        (float)processedPrefabs / affectedPrefabs);

                    var renderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                    bool prefabChanged = false;
                    foreach (var renderer in renderers)
                    {
                        processedRenderers++;

                        var so = new SerializedObject(renderer);
                        var sp = so.FindProperty("m_Mesh");

                        if (sp == null)
                        {
                            Debug.LogWarning($"未找到网格属性: {renderer.name}");
                            continue;
                        }

                        PrefabUtility.RevertPropertyOverride(sp, InteractionMode.UserAction);
                        revertedRenderers++;
                        prefabChanged = true;
                        Debug.Log($"已还原网格: {renderer.name} (预制体: {prefab.name})");
                    }
                    if (prefabChanged)
                    {
                        revertedPrefabs.Add(prefab.name);
                    }
                    processedPrefabs++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // 显示结果
                string resultMessage = $"网格还原完成！\n\n" +
                                      $"处理的预制体: {processedPrefabs} 个\n" +
                                      $"处理的渲染器: {processedRenderers} 个\n" +
                                      $"成功还原的渲染器: {revertedRenderers} 个\n";

                if (revertedPrefabs.Count > 0)
                {
                    resultMessage += $"\n已修改的预制体:\n";
                    foreach (string prefabName in revertedPrefabs.Take(10)) // 最多显示10个
                    {
                        resultMessage += $"• {prefabName}\n";
                    }
                    if (revertedPrefabs.Count > 10)
                    {
                        resultMessage += $"... 还有 {revertedPrefabs.Count - 10} 个预制体";
                    }
                }
                else
                {
                    resultMessage += "\n没有发现需要还原的网格覆盖。";
                }

                EditorUtility.DisplayDialog(revertedRenderers > 0 ? "还原完成" : "还原完成",
                    resultMessage, "确定");

                Debug.Log($"网格还原操作完成 - 处理预制体: {processedPrefabs}, 还原渲染器: {revertedRenderers}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"还原网格时发生错误: {ex.Message}");
                EditorUtility.DisplayDialog("错误",
                    $"还原过程中发生错误:\n{ex.Message}",
                    "确定");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
