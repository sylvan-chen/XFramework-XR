using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using XGame.Extensions;
using XGame.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XGame.Modules.SimpleDressup
{
    /// <summary>
    /// 简单换装系统主控制器
    /// </summary>
    /// <remarks>
    /// 负责协调整个换装流程：数据提取 → 图集生成 → UV重映射 → 网格合并 → 结果应用
    /// </remarks>
    public class SimpleDressupController : MonoBehaviour
    {
        [Header("基础配置")]
        [IntDropdown(256, 512, 1024, 2048, 4096)]
        [SerializeField] private int _atlasSize = 1024;
        [SerializeField] private bool _autoApplyOnStart = false;

        [Header("材质合并设置")]
        [SerializeField] private Material _baseMaterial;

        [Header("网格合并设置")]
        [SerializeField] private MeshCombiner.MeshCombineStrategy _meshCombineStrategy = MeshCombiner.MeshCombineStrategy.SingleSubmesh;

        [Header("骨骼数据")]
        [SerializeField] private Transform _rootBone;

        [Header("外观部件")]
        [SerializeField] private List<DressupOutlookItem> _outlookItems;

        // 核心组件
        private MeshCombiner _meshCombiner;                         // 网格合并器
        private Mesh _combinedMesh;                                 // 合并后的网格

        private MaterialCombiner _materialCombiner;                 // 材质合并器
        private Material _combinedMaterial;                         // 合并后的材质

        private SkinnedMeshRenderer _targetRenderer;

        // 骨骼数据
        private Transform[] _mainBones = new Transform[0];                // 主骨骼数组
        private Matrix4x4[] _bindPoses = new Matrix4x4[0];                // 绑定姿势矩阵
        private readonly Dictionary<string, Transform> _boneMap = new();  // 骨骼映射字典

        // 状态管理
        public bool IsInitialized { get; private set; } = false;
        public bool IsDressing { get; private set; } = false;

        /// <summary>
        /// 换装结果事件
        /// </summary>
        public System.Action<bool> OnDressupComplete;

#if UNITY_EDITOR
        public Material CombinedMaterial => _combinedMaterial;
#endif

        private void Awake()
        {
            Init();
        }

        private void Start()
        {
            if (_autoApplyOnStart)
            {
                ApplyOutlookAsync().Forget();
            }
        }

        private void OnDestroy()
        {
            // 清理临时创建的材质
            if (_combinedMaterial != null && ShouldDestroyMaterial(_combinedMaterial))
            {
                DestroyImmediate(_combinedMaterial);
            }

            // 清理临时创建的网格
            if (_combinedMesh != null && ShouldDestroyMesh(_combinedMesh))
            {
                DestroyImmediate(_combinedMesh);
            }
        }

        private bool ShouldDestroyMaterial(Material material)
        {
            if (material == null) return false;

#if UNITY_EDITOR
            // 在编辑器中，检查材质是否是资源文件
            return !AssetDatabase.Contains(material);
#else
            return material.name.Contains("(Instance)") || material.name.Contains("(Clone)");
#endif
        }

        private bool ShouldDestroyMesh(Mesh mesh)
        {
            if (mesh == null) return false;

#if UNITY_EDITOR
            // 在编辑器中，检查网格是否是资源文件
            return !AssetDatabase.Contains(mesh);
#else
            return mesh.name.Contains("(Instance)") || mesh.name.Contains("(Clone)");
#endif
        }

        private void Init()
        {
            // 创建和初始化核心组件
            _meshCombiner = new MeshCombiner();
            _materialCombiner = new MaterialCombiner();

            // 骨骼数据初始化
            _boneMap.Clear();
            _mainBones = _rootBone.GetComponentsInChildren<Transform>();
            _bindPoses = new Matrix4x4[_mainBones.Length];

            for (int i = 0; i < _mainBones.Length; i++)
            {
                var bone = _mainBones[i];
                if (bone == null) throw new System.ArgumentNullException($"Bone at index {i} is null in the hierarchy under {_rootBone.name}");

                // 骨骼绑定姿势
                _bindPoses[i] = _mainBones[i].worldToLocalMatrix * _rootBone.localToWorldMatrix;

                // 骨骼映射字典
                _boneMap[bone.name] = bone;
            }

            IsInitialized = true;
        }

        /// <summary>
        /// 应用当前的外观配置
        /// </summary>
        public async UniTask<bool> ApplyOutlookAsync()
        {
            if (!IsInitialized)
            {
                Log.Error("[SimpleDressupController] Controller not initialized.");
                return false;
            }

            if (IsDressing)
            {
                Log.Warning("[SimpleDressupController] Dressing in progress, ignoring duplicate call.");
                return false;
            }

            if (_outlookItems.Count == 0)
            {
                Log.Warning("[SimpleDressupController] No valid dressup items to apply.");
                return false;
            }

            IsDressing = true;

            Log.Debug($"[SimpleDressupController] Start dressing process - {_outlookItems.Count} items.");

            // 1. 验证所有部件
            ValidateOutlookItems();

            // 2. 提取合并单元
            var combineUnits = ExtractCombineUnits();
            if (combineUnits == null || combineUnits.Length == 0)
            {
                Log.Warning("[SimpleDressupController] No valid combine units found.");
                IsDressing = false;
                return false;
            }

            // 3. 合并材质
            bool atlasSuccess = CombineMaterials(combineUnits);
            if (!atlasSuccess)
            {
                Log.Error("[SimpleDressupController] Failed to generate texture atlas.");
                IsDressing = false;
                return false;
            }

            // 4. 重映射UV
            ApplyAtlasRectRemapping(combineUnits);

            // 5. 合并网格
            bool meshSuccess = await CombineMeshesAsync(combineUnits);
            if (!meshSuccess)
            {
                Log.Error("[SimpleDressupController] Failed to combine meshes.");
                IsDressing = false;
                return false;
            }

            // 6. 应用合并结果
            bool applySuccess = ApplyCombineResult();
            if (!applySuccess)
            {
                Log.Error("[SimpleDressupController] Failed to apply to target renderer.");
                IsDressing = false;
                return false;
            }

            Log.Debug("[SimpleDressupController] Dressup process completed.");

            OnDressupComplete?.Invoke(true);
            IsDressing = false;
            return true;
        }

        /// <summary>
        /// 验证所有外观部件
        /// </summary>
        private void ValidateOutlookItems()
        {
            var invalidItems = new List<DressupOutlookItem>();

            foreach (var item in _outlookItems)
            {
                // 初始化
                if (!item.IsValid)
                {
                    Log.Error($"[SimpleDressupController] Clothing item '{item.Renderer.name}' is invalid.");
                    invalidItems.Add(item);
                    continue;
                }
            }

            // 移除无效的部件
            foreach (var invalidItem in invalidItems)
            {
                _outlookItems.Remove(invalidItem);
            }
        }

        /// <summary>
        /// 提取合并单元
        /// </summary>
        private DressupCombineUnit[] ExtractCombineUnits()
        {
            var materialDatas = _materialCombiner.ExtractMaterialData(_outlookItems);
            var submeshDatas = _meshCombiner.ExtractSubmeshData(_outlookItems);

            if (materialDatas.Length > submeshDatas.Length)
            {
                Log.Error("[SimpleDressupController] Mismatch between material data and submesh data counts (Materials > Submeshes), " +
                    "there may be missing submesh data.");
                return null;
            }
            if (materialDatas.Length < submeshDatas.Length)
            {
                Log.Error("[SimpleDressupController] Mismatch between material data and submesh data counts (Materials < Submeshes), " +
                    "there may be missing material data.");
                return null;
            }

            var combineUnits = new DressupCombineUnit[materialDatas.Length];
            for (int i = 0; i < combineUnits.Length; i++)
            {
                combineUnits[i] = new DressupCombineUnit
                {
                    MaterialData = materialDatas[i],
                    SubmeshData = submeshDatas[i]
                };
            }

            return combineUnits;
        }

        /// <summary>
        /// 合并材质
        /// </summary>
        private bool CombineMaterials(DressupCombineUnit[] combineUnits)
        {
            _combinedMaterial = _materialCombiner.CombineMaterials(combineUnits, _atlasSize, _baseMaterial);

            if (_combinedMaterial == null)
            {
                Log.Error("[SimpleDressupController] Failed to generate atlas material.");
                return false;
            }

            Log.Debug("[SimpleDressupController] Successfully generated atlas material.");

            return _combinedMaterial != null;
        }

        /// <summary>
        /// 合并网格
        /// </summary>
        private async UniTask<bool> CombineMeshesAsync(DressupCombineUnit[] combineUnits)
        {
            _combinedMesh = await _meshCombiner.CombineMeshesAsync(combineUnits, _bindPoses, _meshCombineStrategy);

            if (_combinedMesh == null)
            {
                Log.Error("[SimpleDressupController] Failed to combine items.");
                return false;
            }

            Log.Debug($"[SimpleDressupController] Items combined successfully - {_outlookItems.Count} items → {_combinedMesh.subMeshCount} submeshes.");

            return true;
        }

        /// <summary>
        /// 应用图集UV重映射
        /// </summary>
        /// <param name="combineUnits">合并单元列表</param>
        public void ApplyAtlasRectRemapping(DressupCombineUnit[] combineUnits)
        {
            for (int i = 0; i < combineUnits.Length; i++)
            {
                var materialData = combineUnits[i].MaterialData;
                var subMeshData = combineUnits[i].SubmeshData;

                var originalUVs = subMeshData.UVs;
                if (originalUVs == null || originalUVs.Length == 0)
                {
                    Log.Warning($"[MaterialCombiner] SubmeshData in CombineUnit {i} has NO uv to remap.");
                    continue;
                }

                var targetRect = materialData.AtlasRect;
                var targetUVs = new Vector2[originalUVs.Length];

                for (int uvIndex = 0; uvIndex < originalUVs.Length; uvIndex++)
                {
                    var originalUV = originalUVs[uvIndex];
                    // 将原始 UV 映射到图集中的对应区域
                    targetUVs[uvIndex] = new Vector2(
                        targetRect.x + originalUV.x * targetRect.width,
                        targetRect.y + originalUV.y * targetRect.height
                    );
                }

                combineUnits[i].SubmeshData.UVs = targetUVs;
            }
        }

        /// <summary>
        /// 应用合并结果
        /// </summary>
        private bool ApplyCombineResult()
        {
            if (_targetRenderer == null)
            {
                _targetRenderer = new GameObject("CombinedRenderer").AddComponent<SkinnedMeshRenderer>();
                _targetRenderer.transform.SetParent(transform);
            }

            // 应用合并的网格
            _targetRenderer.sharedMesh = _combinedMesh;
            // 应用材质
            var atlasMaterials = new Material[_combinedMesh.subMeshCount];
            for (int i = 0; i < atlasMaterials.Length; i++)
            {
                atlasMaterials[i] = _combinedMaterial;
            }
            _targetRenderer.sharedMaterials = atlasMaterials;
            // 应用骨骼
            _targetRenderer.bones = _mainBones;
            // 设置根骨骼
            _targetRenderer.rootBone = _rootBone;

            // 计算合适的本地边界
            RecalculateLocalBounds(_targetRenderer);

            Log.Debug("[SimpleDressupController] Successfully applied to target renderer.");

            foreach (var item in _outlookItems)
            {
                if (item != null && item.Renderer != null)
                {
                    item.Renderer.enabled = false; // 禁用原部件的渲染器
                }
            }

            return true;
        }

        /// <summary>
        /// 重新计算合适的本地边界
        /// 基于所有原始部件的localBounds计算合并后的边界
        /// </summary>
        private void RecalculateLocalBounds(SkinnedMeshRenderer targetRenderer)
        {
            if (_outlookItems.Count == 0)
            {
                Log.Warning("[SimpleDressupController] No dressup items to calculate bounds from.");
                return;
            }

            // 计算所有部件的联合边界
            Bounds combinedBounds = new();
            bool firstBounds = true;

            foreach (var item in _outlookItems)
            {
                if (item?.Renderer != null)
                {
                    var itemBounds = item.Renderer.localBounds;

                    if (firstBounds)
                    {
                        combinedBounds = itemBounds;
                        firstBounds = false;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(itemBounds);
                    }
                }
            }

            if (!firstBounds)
            {
                targetRenderer.localBounds = combinedBounds;
            }
            else
            {
                Log.Warning("[SimpleDressupController] No valid bounds found from dressup items.");
            }
        }
    }
}
