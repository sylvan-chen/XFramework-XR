using UnityEngine;

namespace XGame.Extensions
{
    public static class CameraExtensions
    {
        /// <summary>
        /// 排除指定层级的渲染
        /// </summary>
        /// <param name="layerName">层级名称</param>
        public static void ExcludeLayer(this Camera cam, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"Layer '{layerName}' 不存在");
                return;
            }

            cam.cullingMask &= ~(1 << layer);
        }

        /// <summary>
        /// 包含指定层级的渲染
        /// </summary>
        /// <param name="layerName">层级名称</param>
        public static void IncludeLayer(this Camera cam, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                Debug.LogWarning($"Layer '{layerName}' 不存在");
                return;
            }

            cam.cullingMask |= 1 << layer;
        }
    }
}