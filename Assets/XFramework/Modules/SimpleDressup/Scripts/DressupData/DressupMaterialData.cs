using UnityEngine;

namespace XGame.Modules.SimpleDressup
{
    internal enum TextureType
    {
        Base,
        Normal,
        Metallic,
        Occlusion,
        Emission
    }

    /// <summary>
    /// 材质数据
    /// </summary>
    public class DressupMaterialData
    {
        public string Name { get; set; }
        public Texture2D BaseMap { get; set; }
        public Texture2D NormalMap { get; set; }
        public Texture2D MetallicMap { get; set; }
        public Texture2D OcclusionMap { get; set; }
        public Texture2D EmissionMap { get; set; }
        public Rect AtlasRect { get; set; } = new(0, 0, 1, 1);

        internal Texture2D GetTexture(TextureType type)
        {
            return type switch
            {
                TextureType.Base => BaseMap,
                TextureType.Normal => NormalMap,
                TextureType.Metallic => MetallicMap,
                TextureType.Occlusion => OcclusionMap,
                TextureType.Emission => EmissionMap,
                _ => null
            };
        }
    }
}
