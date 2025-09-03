using System.Collections.Generic;
using UnityEngine;
using XGame.Core;

namespace XGame.Modules.SimpleDressup
{
    /// <summary>
    /// 材质合并器
    /// </summary>
    public class MaterialCombiner
    {
        #region 常量定义

        /// <summary>
        /// 每帧处理的像素数量
        /// </summary>
        public const int PIXEL_PROCESS_COUNT_PER_FRAME = 15000;

        /// <summary>
        /// 纹理图集的格式
        /// </summary>
        public const TextureFormat TEXTURE_FORMAT = TextureFormat.RGBA32;

        #endregion

        #region 字段/属性

        /// <summary>
        /// 缓存材质到材质数据的映射，避免重复提取
        /// </summary>
        private readonly Dictionary<Material, DressupMaterialData> _materialToData = new();

        #endregion

        #region 数据结构

        private struct AtlasInfo
        {
            public bool IsValid;
            public Texture2D BaseAtlas;
            public Texture2D NormalAtlas;
            public Texture2D MetallicAtlas;
            public Texture2D OcclusionAtlas;
            public Texture2D EmissionAtlas;
        }

        #endregion

        #region 公开接口

        /// <summary>
        /// 从外观物品中提取材质数据
        /// </summary>
        /// <remarks>
        /// 只提取有效的材质（多于子网格的材质略过）
        /// </remarks>
        /// <param name="outlookItems">外观物品列表</param>
        /// <returns>材质数据数组</returns>
        public DressupMaterialData[] ExtractMaterialData(IReadOnlyList<DressupOutlookItem> outlookItems)
        {
            var materialDatas = new List<DressupMaterialData>();

            foreach (var item in outlookItems)
            {
                var materials = item.Renderer.sharedMaterials;
                int submeshCount = item.Renderer.sharedMesh.subMeshCount;
                if (materials.Length < submeshCount)
                {
                    Log.Warning($"[MaterialCombiner] Outlook item ({item.OutlookType}) has fewer materials than submeshes. " +
                                $"Materials: {materials.Length}, Submeshes: {submeshCount}");
                }

                // 只提取有效的材质（多于子网格的材质略过）
                for (int materialIndex = 0; materialIndex < materials.Length && materialIndex < submeshCount; materialIndex++)
                {
                    var material = materials[materialIndex];
                    if (material == null) continue;

                    // 重复材质直接复用
                    if (_materialToData.TryGetValue(material, out var existingData))
                    {
                        materialDatas.Add(existingData);
                        Log.Debug($"[MaterialCombiner] Reusing existing material data for '{material.name}'");
                        continue;
                    }

                    var data = new DressupMaterialData
                    {
                        Name = material.name,
                        BaseMap = ExtractTexture(material, TextureType.Base),
                        NormalMap = ExtractTexture(material, TextureType.Normal),
                        MetallicMap = ExtractTexture(material, TextureType.Metallic),
                        OcclusionMap = ExtractTexture(material, TextureType.Occlusion),
                        EmissionMap = ExtractTexture(material, TextureType.Emission)
                    };

                    _materialToData[material] = data;
                    materialDatas.Add(data);
                    Log.Debug($"[MaterialCombiner] Extracted new material data for '{material.name}'");
                }
            }

            return materialDatas.ToArray();
        }

        /// <summary>
        /// 合并材质
        /// </summary>
        /// <param name="combineUnits">合并单元列表</param>
        /// <param name="atlasSize">图集大小</param>
        /// <param name="baseMaterial">基础材质</param>
        /// <returns>纹理图集</returns>
        public Material CombineMaterials(DressupCombineUnit[] combineUnits, int atlasSize, Material baseMaterial)
        {
            try
            {
                // 打包基础纹理图集
                var baseAtlas = PackTextures(combineUnits, atlasSize, TextureType.Base);
                if (baseAtlas == null)
                {
                    Log.Error("[MaterialCombiner] Failed to create base atlas, no valid base textures found.");
                    return null;
                }
                // 打包其他纹理图集
                var normalAtlas = PackTextures(combineUnits, atlasSize, TextureType.Normal);
                var metallicAtlas = PackTextures(combineUnits, atlasSize, TextureType.Metallic);
                var occlusionAtlas = PackTextures(combineUnits, atlasSize, TextureType.Occlusion);
                var emissionAtlas = PackTextures(combineUnits, atlasSize, TextureType.Emission);

                // 构建图集材质
                var atlasInfo = new AtlasInfo
                {
                    IsValid = false,
                    BaseAtlas = baseAtlas,
                    NormalAtlas = normalAtlas,
                    MetallicAtlas = metallicAtlas,
                    OcclusionAtlas = occlusionAtlas,
                    EmissionAtlas = emissionAtlas
                };

                var combinedMaterial = BuildAtlasMaterialFromBaseMaterial(atlasInfo, baseMaterial);

                Log.Debug($"[MaterialCombiner] Successfully combine material from {combineUnits.Length} materials.");
                return combinedMaterial;
            }
            catch (System.Exception ex)
            {
                Log.Error($"[MaterialCombiner] Failed to combined material: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 内部实现

        private Texture2D ExtractTexture(Material material, TextureType type)
        {
            Texture2D map = null;

            switch (type)
            {
                case TextureType.Base:
                    if (material.HasTexture("_BaseMap"))
                    {
                        map = material.GetTexture("_BaseMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_MainTex"))
                    {
                        map = material.GetTexture("_MainTex") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_BaseColorMap"))
                    {
                        map = material.GetTexture("_BaseColorMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_Albedo"))
                    {
                        map = material.GetTexture("_Albedo") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_Diffuse"))
                    {
                        map = material.GetTexture("_Diffuse") as Texture2D;
                        break;
                    }
                    break;
                case TextureType.Normal:
                    if (material.HasTexture("_BumpMap"))
                    {
                        map = material.GetTexture("_BumpMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_NormalMap"))
                    {
                        map = material.GetTexture("_NormalMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_DetailNormalMap"))
                    {
                        map = material.GetTexture("_DetailNormalMap") as Texture2D;
                        break;
                    }
                    break;
                case TextureType.Metallic:
                    if (material.HasTexture("_MetallicGlossMap"))
                    {
                        map = material.GetTexture("_MetallicGlossMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_MetallicMap"))
                    {
                        map = material.GetTexture("_MetallicMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_SpecGlossMap"))
                    {
                        map = material.GetTexture("_SpecGlossMap") as Texture2D;
                        break;
                    }
                    break;
                case TextureType.Occlusion:
                    if (material.HasTexture("_OcclusionMap"))
                    {
                        map = material.GetTexture("_OcclusionMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_AOMap"))
                    {
                        map = material.GetTexture("_AOMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_AmbientOcclusionMap"))
                    {
                        map = material.GetTexture("_AmbientOcclusionMap") as Texture2D;
                        break;
                    }
                    break;
                case TextureType.Emission:
                    if (material.HasTexture("_EmissionMap"))
                    {
                        map = material.GetTexture("_EmissionMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_EmissiveMap"))
                    {
                        map = material.GetTexture("_EmissiveMap") as Texture2D;
                        break;
                    }
                    else if (material.HasTexture("_EmissionColorMap"))
                    {
                        map = material.GetTexture("_EmissionColorMap") as Texture2D;
                        break;
                    }
                    break;
                default:
                    map = null;
                    break;
            }

            return map;
        }

        private Texture2D PackTextures(DressupCombineUnit[] combineUnits, int atlasSize, TextureType textureType)
        {
            var uniqueTextures = new List<Texture2D>();
            var textureToRectIndex = new Dictionary<Texture2D, int>();  // 纹理到图集坐标的映射
            var unitIndexToRectIndex = new int[combineUnits.Length];    // 单元索引到图集坐标的映射

            for (int unitIndex = 0; unitIndex < combineUnits.Length; unitIndex++)
            {
                var materialData = combineUnits[unitIndex].MaterialData;

                var texture = materialData.GetTexture(textureType);

                if (texture == null)
                {
                    if (textureType == TextureType.Base)
                    {
                        Log.Error($"[MaterialCombiner] Material data '{materialData.Name}' has no base texture.");
                        return null; // 基础纹理不能为空
                    }

                    // 标记为无纹理
                    unitIndexToRectIndex[unitIndex] = -1;
                    continue;
                }

                // 尝试复用现有的图集坐标
                if (textureToRectIndex.TryGetValue(texture, out int existingRectIndex))
                {
                    unitIndexToRectIndex[unitIndex] = existingRectIndex;
                }
                else
                {
                    int newIndex = uniqueTextures.Count;
                    textureToRectIndex[texture] = newIndex;
                    unitIndexToRectIndex[unitIndex] = newIndex;
                    uniqueTextures.Add(texture);
                }
            }

            if (uniqueTextures.Count == 0)
            {
                return null;
            }

            var atlas = new Texture2D(atlasSize, atlasSize, TEXTURE_FORMAT, false)
            {
                name = $"CombinedAtlas_{atlasSize}_{textureType}"
            };

            var atlasRects = atlas.PackTextures(uniqueTextures.ToArray(), 2, atlasSize);

            for (int unitIndex = 0; unitIndex < combineUnits.Length; unitIndex++)
            {
                int rectIndex = unitIndexToRectIndex[unitIndex];
                if (rectIndex < 0) continue; // 无纹理的材质跳过

                var atlasRect = atlasRects[rectIndex];

                if (textureType == TextureType.Base)
                {
                    // 保存基础图集坐标
                    combineUnits[unitIndex].MaterialData.AtlasRect = atlasRect;
                }

                else
                {
                    // 其他图集验证与基础图集一致
                    if (atlasRect != combineUnits[unitIndex].MaterialData.AtlasRect)
                    {
                        Log.Warning($"[MaterialCombiner] Atlas rect mismatch for {textureType} in '{combineUnits[unitIndex].MaterialData.Name}'. " +
                               $"Expected: {combineUnits[unitIndex].MaterialData.AtlasRect}, Got: {atlasRect}");
                    }
                }
            }

            return atlas;
        }

        private Material BuildAtlasMaterialFromBaseMaterial(AtlasInfo atlasInfo, Material baseMaterial)
        {
            if (baseMaterial == null)
            {
                Log.Error("[MaterialCombiner] Base material is null, cannot build new material.");
                return null;
            }

            var resultMaterial = Object.Instantiate(baseMaterial);
            resultMaterial.name = $"{baseMaterial.name}_Atlas";
#if UNITY_EDITOR
            resultMaterial.hideFlags = HideFlags.DontSave;
#else
            resultMaterial.hideFlags = HideFlags.HideAndDontSave;
#endif

            ApplyTexturesToMaterial(resultMaterial, atlasInfo.BaseAtlas, atlasInfo.NormalAtlas, atlasInfo.MetallicAtlas, atlasInfo.OcclusionAtlas, atlasInfo.EmissionAtlas);

            return resultMaterial;
        }

        private Material BuildAtlasMaterialWithShader(AtlasInfo atlasInfo, Shader shader)
        {
            if (shader == null)
            {
                Log.Error("[MaterialCombiner] Shader is null, cannot build new material.");
                return null;
            }

            var resultMaterial = new Material(shader)
            {
                name = $"{shader.name}_Atlas",
#if UNITY_EDITOR
                hideFlags = HideFlags.DontSave,
#else
                hideFlags = HideFlags.HideAndDontSave,
#endif
            };

            ApplyTexturesToMaterial(resultMaterial, atlasInfo.BaseAtlas, atlasInfo.NormalAtlas, atlasInfo.MetallicAtlas, atlasInfo.OcclusionAtlas, atlasInfo.EmissionAtlas);

            return resultMaterial;
        }

        private void ApplyTexturesToMaterial(Material material, Texture2D baseAtlas, Texture2D normalAtlas, Texture2D metallicAtlas, Texture2D occlusionAtlas, Texture2D emissionAtlas)
        {
            if (baseAtlas != null)
                SetTextureProperty(material, baseAtlas, "_BaseMap", "_MainTex", "_BaseColorMap", "_Albedo", "_Diffuse");

            if (normalAtlas != null)
                SetTextureProperty(material, normalAtlas, "_BumpMap", "_NormalMap", "_DetailNormalMap");

            if (metallicAtlas != null)
                SetTextureProperty(material, metallicAtlas, "_MetallicGlossMap", "_MetallicMap", "_SpecGlossMap");

            if (occlusionAtlas != null)
                SetTextureProperty(material, occlusionAtlas, "_OcclusionMap", "_AOMap", "_AmbientOcclusionMap");

            if (emissionAtlas != null)
                SetTextureProperty(material, emissionAtlas, "_EmissionMap", "_EmissiveMap", "_EmissionColorMap");
        }

        private void SetTextureProperty(Material material, Texture texture, params string[] propertyNames)
        {
            for (int i = 0; i < propertyNames.Length - 1; i++)
            {
                if (material.HasTexture(propertyNames[i]))
                {
                    material.SetTexture(propertyNames[i], texture);
                    return;
                }
            }
        }

        #endregion
    }
}
