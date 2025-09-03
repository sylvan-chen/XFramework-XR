using System;
using UnityEngine;
using System.Collections.Generic;
using XGame.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace XGame.Modules.SimpleDressup
{
    /// <summary>
    /// 外观类型
    /// </summary>
    [Serializable]
    public enum OutlookType
    {
        None = 0,
        Body = 1 << 0,      // 身体
        Face = 1 << 1,      // 脸部
        Hair = 1 << 2,      // 头发  
        Top = 1 << 3,       // 上衣
        Bottom = 1 << 4,    // 下衣
        Shoes = 1 << 5,     // 鞋子
        Gloves = 1 << 6,    // 手套
        Hat = 1 << 7,       // 帽子
        All = ~0            // 所有部位
    }

    /// <summary>
    /// 外观部件
    /// </summary>
    /// <remarks>
    /// 本质是对SkinnedMeshRenderer的封装
    /// </remarks>
    [Serializable]
    public class DressupOutlookItem
    {
        [SerializeField] private OutlookType _outlookType = OutlookType.None;
        [SerializeField] private SkinnedMeshRenderer _renderer;

        public OutlookType OutlookType => _outlookType;
        public SkinnedMeshRenderer Renderer => _renderer;

        public bool IsValid => _renderer.sharedMesh != null && _renderer.sharedMaterials != null && _renderer.sharedMaterials.Length > 0;
        public int SubmeshCount => _renderer.sharedMesh != null ? _renderer.sharedMesh.subMeshCount : 0;

        /// <summary>
        /// 按骨骼名字重映射到新的骨骼
        /// </summary>
        /// <param name="boneMap">骨骼映射字典</param>
        /// <returns>重映射是否成功</returns>
        public bool RemapBones(Dictionary<string, Transform> boneMap)
        {
            var bones = _renderer.bones;
            var rootBone = _renderer.rootBone;

            if (bones == null || bones.Length == 0 || rootBone == null)
            {
                Log.Warning("[DressupItem] Bones or RootBone is null or empty.");
                return false;
            }

            if (boneMap == null || boneMap.Count == 0)
            {
                Log.Warning("[DressupItem] Bone map is null or empty.");
                return false;
            }

            for (int i = 0; i < bones.Length; i++)
            {
                if (boneMap.TryGetValue(bones[i].name, out var targetBone))
                {
                    bones[i] = targetBone;
                }
                else
                {
                    Log.Warning($"[DressupItem] Bone '{bones[i].name}' not found in boneMap.");
                    return false;
                }
            }

            if (boneMap.TryGetValue(rootBone.name, out var targetRootBone))
            {
                rootBone = targetRootBone;
            }
            else
            {
                Log.Warning($"[DressupItem] Root bone '{rootBone.name}' not found in boneMap.");
                return false;
            }

            _renderer.bones = bones;
            _renderer.rootBone = rootBone;

            return true;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// 让_dressupType和_renderer两个字段并排显示
    /// </summary>
    [CustomPropertyDrawer(typeof(DressupOutlookItem))]
    public class DressupOutlookItemDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 绘制标签
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // 不缩进子属性
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // 计算每个字段的矩形区域
            var typeRect = new Rect(position.x, position.y, position.width * 0.3f, position.height);
            var rendererRect = new Rect(position.x + position.width * 0.3f + 5, position.y, position.width * 0.7f - 5, position.height);

            var dressupTypeProperty = property.FindPropertyRelative("_outlookType");
            var rendererProperty = property.FindPropertyRelative("_renderer");

            EditorGUI.PropertyField(typeRect, dressupTypeProperty, GUIContent.none);
            EditorGUI.PropertyField(rendererRect, rendererProperty, GUIContent.none);

            // 恢复缩进
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }
    }
#endif
}