using UnityEngine;

namespace XGame.Modules.SimpleDressup
{
    /// <summary>
    /// 子网格数据
    /// </summary>
    public class DressupSubmeshData
    {
        public string Name { get; set; }
        public int[] Triangles { get; set; }
        public Vector3[] Vertices { get; set; }
        public Vector3[] Normals { get; set; }
        public Vector4[] Tangents { get; set; }
        public Vector2[] UVs { get; set; }
        public BoneWeight[] BoneWeights { get; set; }

        public bool IsValid => Vertices?.Length > 0 && Triangles?.Length > 0;
        public int VertexCount => Vertices?.Length ?? 0;
        public int TriangleIndexCount => Triangles?.Length ?? 0;
    }
}