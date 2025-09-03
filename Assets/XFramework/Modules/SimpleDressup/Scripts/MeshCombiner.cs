using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using XGame.Core;

namespace XGame.Modules.SimpleDressup
{
    /// <summary>
    /// 网格合并器
    /// </summary>
    public partial class MeshCombiner
    {
        #region 常量配置

        /// <summary>
        /// 每帧处理的三角形数量
        /// </summary>
        private const int TRIANGLE_PROCESS_COUNT_PER_FRAME = 1000;

        #endregion

        #region 数据结构/枚举

        /// <summary>
        /// 网格合并策略
        /// </summary>
        public enum MeshCombineStrategy
        {
            /// <summary>
            /// 单一子网格 - 所有子网格合并为一个子网格
            /// </summary>
            SingleSubmesh,

            /// <summary>
            /// 保留子网格 - 保留原有的子网格结构
            /// </summary>
            PreserveSubmeshes,
        }

        /// <summary>
        /// 合并网格信息
        /// </summary>
        private struct CombinedMeshInfo
        {
            public bool Success;
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public Vector4[] Tangents;
            public Vector2[] UVs;
            public BoneWeight[] BoneWeights;
            public int[] Triangles;
            public List<int>[] SubmeshToTriangles;
        }

        #endregion

        #region 公共接口

        /// <summary>
        /// 提取外观部件的子网格数据
        /// </summary>
        /// <param name="outlookItems">外观部件列表</param>
        /// <returns>子网格数据列表</returns>
        public DressupSubmeshData[] ExtractSubmeshData(IReadOnlyList<DressupOutlookItem> outlookItems)
        {
            var result = new List<DressupSubmeshData>();

            for (int i = 0; i < outlookItems.Count; i++)
            {
                var item = outlookItems[i];
                var submeshDatas = ExtractSubmeshData(item);
                result.AddRange(submeshDatas);
            }

            return result.ToArray();
        }

        /// <summary>
        /// 提取单个外观部件的子网格数据
        /// </summary>
        /// <param name="outlookItem">外观部件</param>
        /// <returns>子网格数据列表</returns>
        public DressupSubmeshData[] ExtractSubmeshData(DressupOutlookItem outlookItem)
        {
            var mesh = outlookItem.Renderer.sharedMesh;
            int submeshCount = outlookItem.SubmeshCount;

            var result = new DressupSubmeshData[submeshCount];

            for (int submeshIndex = 0; submeshIndex < submeshCount; submeshIndex++)
            {
                var submeshData = ExtractSubmeshDataInternal(mesh, submeshIndex);
                result[submeshIndex] = submeshData;
            }
            return result;
        }

        /// <summary>
        /// 根据子网格单元列表建立合并网格
        /// </summary>
        /// <param name="submeshUnits">子网格单元列表</param>
        /// <param name="bindPoses">绑定姿势矩阵数组</param>
        /// <returns>合并网格</returns>
        public async UniTask<Mesh> CombineMeshesAsync(DressupCombineUnit[] combineUnits, Matrix4x4[] bindPoses, MeshCombineStrategy strategy)
        {
            var combinedMeshInfo = await GenerateCombinedMeshInfoAsync(combineUnits, strategy);
            if (!combinedMeshInfo.Success)
            {
                Log.Error("[MeshCombiner] Failed to generate combined mesh info.");
                return null;
            }

            Log.Info($"[MeshCombiner] Successfully built mesh with {combineUnits.Length} submeshes, " +
                     $"{combinedMeshInfo.Vertices.Length} vertices, {combinedMeshInfo.Triangles.Length} triangle indices.");

            try
            {
                var mesh = new Mesh
                {
                    name = "CombinedMesh",
                    hideFlags = HideFlags.HideAndDontSave
                };

                // 设置顶点数据 
                mesh.SetVertices(combinedMeshInfo.Vertices);
                mesh.SetNormals(combinedMeshInfo.Normals);
                mesh.SetTangents(combinedMeshInfo.Tangents);
                mesh.SetUVs(0, combinedMeshInfo.UVs);

                var submeshToTriangles = combinedMeshInfo.SubmeshToTriangles;

                mesh.subMeshCount = submeshToTriangles.Length;
                for (int submeshIndex = 0; submeshIndex < submeshToTriangles.Length; submeshIndex++)
                {
                    mesh.SetTriangles(submeshToTriangles[submeshIndex], submeshIndex);
                }

                mesh.boneWeights = combinedMeshInfo.BoneWeights;
                mesh.bindposes = bindPoses;

                if (mesh.normals == null || mesh.normals.Length == 0)
                    mesh.RecalculateNormals();

                if (mesh.tangents == null || mesh.tangents.Length == 0)
                    mesh.RecalculateTangents();

                mesh.RecalculateBounds();

                return mesh;
            }
            catch (Exception e)
            {
                Log.Error($"[MeshCombiner] CreateFinalCombinedMesh error - {e.Message}");
                return null;
            }
        }

        #endregion

        #region 私有方法

        public DressupSubmeshData ExtractSubmeshDataInternal(Mesh mesh, int submeshIndex)
        {
            if (mesh == null) throw new System.ArgumentNullException(nameof(mesh));

            var sourceVertices = mesh.vertices;
            var sourceNormals = mesh.normals;
            var sourceTangents = mesh.tangents;
            var sourceUVs = mesh.uv;
            var sourceBoneWeights = mesh.boneWeights;
            var sourceSubtriangles = mesh.GetTriangles(submeshIndex);

            var data = new DressupSubmeshData()
            {
                Name = $"{mesh.name}_submesh_{submeshIndex}"
            };

            int sourceVertexCount = sourceVertices?.Length ?? 0;

            bool hasNormals = sourceNormals != null && sourceNormals.Length == sourceVertexCount;
            bool hasTangents = sourceTangents != null && sourceTangents.Length == sourceVertexCount;
            bool hasUV = sourceUVs != null && sourceUVs.Length == sourceVertexCount;
            bool hasBoneWeights = sourceBoneWeights != null && sourceBoneWeights.Length == sourceVertexCount;

            if (!hasNormals)
                Log.Debug($"[MeshCombiner] Source mesh missing normals, will recalculate.");
            if (!hasTangents)
                Log.Debug($"[MeshCombiner] Source mesh missing tangents, will recalculate.");
            if (!hasUV)
                Log.Warning($"[MeshCombiner] Source mesh missing UV coordinates, using default values. This may affect texture rendering.");
            if (!hasBoneWeights)
                Log.Warning($"[MeshCombiner] Source mesh missing bone weights, using default weights. This may affect skinning.");

            // 提取新旧顶点索引映射 (原索引 -> 0, 1, 2, ...)
            int usedVertexCount = 0;
            int[] newIndexToOld;
            Dictionary<int, int> oldIndexToNew;
            // 获取索引范围
            int maxVertexIndex = 0;
            for (int i = 0; i < sourceSubtriangles.Length; i++)
            {
                if (sourceSubtriangles[i] > maxVertexIndex)
                    maxVertexIndex = sourceSubtriangles[i];
            }
            // 根据索引范围使用不同提取算法
            if (maxVertexIndex < 10000)  // 小范围使用bool数组标记
            {
                var vertexUseFlags = new bool[maxVertexIndex + 1];

                for (int i = 0; i < sourceSubtriangles.Length; i++)
                {
                    int vertexIndex = sourceSubtriangles[i];
                    if (!vertexUseFlags[vertexIndex])
                    {
                        vertexUseFlags[vertexIndex] = true;
                        usedVertexCount++;
                    }
                }

                newIndexToOld = new int[usedVertexCount];
                oldIndexToNew = new Dictionary<int, int>(usedVertexCount);
                int newIndex = 0;
                for (int i = 0; i < vertexUseFlags.Length; i++)
                {
                    if (vertexUseFlags[i])
                    {
                        newIndexToOld[newIndex] = i;
                        oldIndexToNew[i] = newIndex;
                        newIndex++;
                    }
                }
            }
            else  // 大范围使用排序去重
            {
                System.Array.Sort(sourceSubtriangles);

                usedVertexCount = 1; // 包含第一个顶点
                for (int i = 1; i < sourceSubtriangles.Length; i++)
                {
                    if (sourceSubtriangles[i] != sourceSubtriangles[i - 1])
                        usedVertexCount++;
                }

                newIndexToOld = new int[usedVertexCount];
                oldIndexToNew = new Dictionary<int, int>(usedVertexCount);

                newIndexToOld[0] = sourceSubtriangles[0];
                oldIndexToNew[sourceSubtriangles[0]] = 0;

                int newIndex = 1;
                for (int i = 1; i < sourceSubtriangles.Length; i++)
                {
                    if (sourceSubtriangles[i] != sourceSubtriangles[i - 1])
                    {
                        newIndexToOld[newIndex] = sourceSubtriangles[i];
                        oldIndexToNew[sourceSubtriangles[i]] = newIndex;
                        newIndex++;
                    }
                }
            }

            data.Triangles = new int[sourceSubtriangles.Length];
            data.Vertices = new Vector3[usedVertexCount];
            data.Normals = new Vector3[usedVertexCount];
            data.Tangents = new Vector4[usedVertexCount];
            data.UVs = new Vector2[usedVertexCount];
            data.BoneWeights = new BoneWeight[usedVertexCount];

            // 复制顶点数据
            for (int newIndex = 0; newIndex < newIndexToOld.Length; newIndex++)
            {
                int oldIndex = newIndexToOld[newIndex];

                // 顶点位置
                data.Vertices[newIndex] = sourceVertices[oldIndex];

                // 法线数据（如果不存在或损坏，会在后续重新计算）
                if (hasNormals)
                    data.Normals[newIndex] = sourceNormals[oldIndex];

                // 切线数据（如果不存在或损坏，会在后续重新计算）
                if (hasTangents)
                    data.Tangents[newIndex] = sourceTangents[oldIndex];

                // UV坐标（无法自动计算，使用默认值，可能影响材质渲染效果）
                if (hasUV)
                    data.UVs[newIndex] = sourceUVs[oldIndex];
                else
                    data.UVs[newIndex] = Vector2.zero;

                // 骨骼权重（无法自动计算，使用默认值，可能影响蒙皮效果）
                if (hasBoneWeights)
                    data.BoneWeights[newIndex] = sourceBoneWeights[oldIndex];
                else
                    data.BoneWeights[newIndex] = new BoneWeight { weight0 = 1.0f, boneIndex0 = 0 };
            }

            // 三角形索引重映射
            for (int i = 0; i < sourceSubtriangles.Length; i++)
            {
                data.Triangles[i] = oldIndexToNew[sourceSubtriangles[i]];
            }

            return data;
        }

        private async UniTask<CombinedMeshInfo> GenerateCombinedMeshInfoAsync(DressupCombineUnit[] combineUnits, MeshCombineStrategy strategy)
        {
            var result = new CombinedMeshInfo { Success = false };

            if (combineUnits == null || combineUnits.Length == 0)
            {
                Log.Warning("[MeshCombiner] No submesh units to build mesh.");
                return result;
            }

            int totalVertexCount = 0;
            int totalTriangleCount = 0;

            for (int i = 0; i < combineUnits.Length; i++)
            {
                var data = combineUnits[i].SubmeshData;

                totalVertexCount += data.VertexCount;
                totalTriangleCount += data.TriangleIndexCount;
            }

            int submeshCount = strategy switch
            {
                MeshCombineStrategy.SingleSubmesh => 1,
                MeshCombineStrategy.PreserveSubmeshes => combineUnits.Length,
                _ => combineUnits.Length
            };

            result.Vertices = new Vector3[totalVertexCount];
            result.Normals = new Vector3[totalVertexCount];
            result.Tangents = new Vector4[totalVertexCount];
            result.UVs = new Vector2[totalVertexCount];
            result.BoneWeights = new BoneWeight[totalVertexCount];
            result.Triangles = new int[totalTriangleCount];
            result.SubmeshToTriangles = new List<int>[submeshCount];

            for (int i = 0; i < submeshCount; i++)
            {
                result.SubmeshToTriangles[i] = new List<int>();
            }

            int vertexOffset = 0;
            int triangleOffset = 0;
            int processedCount = 0;

            for (int unitIndex = 0; unitIndex < combineUnits.Length; unitIndex++)
            {
                var submeshData = combineUnits[unitIndex].SubmeshData;

                int vertexCount = submeshData.VertexCount;
                int triangleIndexCount = submeshData.TriangleIndexCount;

                Array.Copy(submeshData.Vertices, 0, result.Vertices, vertexOffset, vertexCount);
                Array.Copy(submeshData.Normals, 0, result.Normals, vertexOffset, vertexCount);
                Array.Copy(submeshData.Tangents, 0, result.Tangents, vertexOffset, vertexCount);
                Array.Copy(submeshData.UVs, 0, result.UVs, vertexOffset, vertexCount);
                Array.Copy(submeshData.BoneWeights, 0, result.BoneWeights, vertexOffset, vertexCount);

                var targetSubmeshIndex = strategy switch
                {
                    MeshCombineStrategy.SingleSubmesh => 0,
                    MeshCombineStrategy.PreserveSubmeshes => unitIndex,
                    _ => unitIndex
                };

                // 三角形索引重映射到新的顶点索引
                var unitTriangles = submeshData.Triangles;
                for (int i = 0; i < triangleIndexCount; i++)
                {
                    result.Triangles[triangleOffset + i] = unitTriangles[i] + vertexOffset;
                    result.SubmeshToTriangles[targetSubmeshIndex].Add(unitTriangles[i] + vertexOffset);

                    processedCount++;
                    if (processedCount >= TRIANGLE_PROCESS_COUNT_PER_FRAME)
                    {
                        processedCount = 0;
                        await UniTask.NextFrame();
                    }
                }

                vertexOffset += vertexCount;
                triangleOffset += triangleIndexCount;
            }

            result.Success = true;
            return result;
        }

        #endregion
    }
}
