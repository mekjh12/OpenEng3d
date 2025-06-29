using OpenGL;

namespace Animate
{
    /// <summary>
    /// 정점별 뼈대 영향 정보를 저장하는 클래스
    /// <para>뼈 인덱스들 (최대 4개), 각 뼈의 가중치 값들 (최대 4개)을 포함합니다.</para>
    /// </summary>
    public class VertexBoneData
    {
        /// <summary>영향을 주는 뼈 인덱스들 (최대 4개)</summary>
        public Vertex4i BoneIndices { get; set; }

        /// <summary>각 뼈의 가중치 값들 (최대 4개)</summary>
        public Vertex4f BoneWeights { get; set; }

        public VertexBoneData()
        {
            BoneIndices = Vertex4i.Zero;
            BoneWeights = Vertex4f.Zero;
        }

        public VertexBoneData(Vertex4i indices, Vertex4f weights)
        {
            BoneIndices = indices;
            BoneWeights = weights;
        }
    }
}
