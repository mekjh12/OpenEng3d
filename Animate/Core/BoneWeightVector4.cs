using System;
using OpenGL;

namespace Animate
{
    /// <summary>
    /// 정점별 뼈대 영향 정보를 저장하는 구조체
    /// <para>뼈 인덱스들 (최대 4개), 각 뼈의 가중치 값들 (최대 4개)을 포함합니다.</para>
    /// </summary>
    public readonly struct BoneWeightVector4 : IEquatable<BoneWeightVector4>
    {
        /// <summary>영향을 주는 뼈 인덱스들 (최대 4개)</summary>
        public readonly Vertex4i BoneIndices;

        /// <summary>각 뼈의 가중치 값들 (최대 4개)</summary>
        public readonly Vertex4f BoneWeights;

        /// <summary>뼈 인덱스와 가중치를 지정하는 생성자</summary>
        public BoneWeightVector4(Vertex4i indices, Vertex4f weights)
        {
            BoneIndices = indices;
            BoneWeights = weights;
        }

        /// <summary>개별 뼈 정보를 지정하는 생성자</summary>
        public BoneWeightVector4(int bone0, int bone1, int bone2, int bone3,
                              float weight0, float weight1, float weight2, float weight3)
        {
            BoneIndices = new Vertex4i(bone0, bone1, bone2, bone3);
            BoneWeights = new Vertex4f(weight0, weight1, weight2, weight3);
        }

        /// <summary>영향을 받지 않는 정점 (모든 가중치가 0)</summary>
        public static BoneWeightVector4 None => new BoneWeightVector4();

        /// <summary>첫 번째 뼈에만 완전히 영향받는 정점</summary>
        public static BoneWeightVector4 SingleBone(int boneIndex) =>
            new BoneWeightVector4(new Vertex4i(boneIndex, 0, 0, 0), new Vertex4f(1.0f, 0.0f, 0.0f, 0.0f));

        /// <summary>가중치가 정규화되어 있는지 확인 (합이 1.0에 가까운지)</summary>
        public bool IsNormalized
        {
            get
            {
                float sum = BoneWeights.x + BoneWeights.y + BoneWeights.z + BoneWeights.w;
                return Math.Abs(sum - 1.0f) < 1e-6f;
            }
        }

        /// <summary>가중치를 정규화한 새로운 VertexBoneData 반환</summary>
        public BoneWeightVector4 Normalized
        {
            get
            {
                float sum = BoneWeights.x + BoneWeights.y + BoneWeights.z + BoneWeights.w;
                if (sum <= 1e-6f) return None; // 모든 가중치가 0에 가까우면 None 반환

                float invSum = 1.0f / sum;
                return new BoneWeightVector4(BoneIndices,
                    new Vertex4f(BoneWeights.x * invSum, BoneWeights.y * invSum,
                                BoneWeights.z * invSum, BoneWeights.w * invSum));
            }
        }

        /// <summary>실제로 영향을 주는 뼈의 개수 (가중치가 0이 아닌 것들)</summary>
        public int ActiveBoneCount
        {
            get
            {
                int count = 0;
                if (BoneWeights.x > 1e-6f) count++;
                if (BoneWeights.y > 1e-6f) count++;
                if (BoneWeights.z > 1e-6f) count++;
                if (BoneWeights.w > 1e-6f) count++;
                return count;
            }
        }

        /// <summary>새로운 뼈 인덱스들로 VertexBoneData 생성</summary>
        public BoneWeightVector4 WithBoneIndices(Vertex4i indices)
        {
            return new BoneWeightVector4(indices, BoneWeights);
        }

        /// <summary>새로운 가중치들로 VertexBoneData 생성</summary>
        public BoneWeightVector4 WithBoneWeights(Vertex4f weights)
        {
            return new BoneWeightVector4(BoneIndices, weights);
        }

        public bool Equals(BoneWeightVector4 other)
        {
            return BoneIndices.Equals(other.BoneIndices) &&
                   BoneWeights.Equals(other.BoneWeights);
        }

        public override bool Equals(object obj)
        {
            return obj is BoneWeightVector4 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + BoneIndices.GetHashCode();
                hash = hash * 23 + BoneWeights.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(BoneWeightVector4 left, BoneWeightVector4 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoneWeightVector4 left, BoneWeightVector4 right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"VertexBoneData(Indices: {BoneIndices}, Weights: {BoneWeights})";
        }
    }
}