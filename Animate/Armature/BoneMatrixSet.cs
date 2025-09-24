using OpenGL;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 뼈대의 변환 행렬들을 관리하는 클래스
    /// <code>
    /// - 애니메이션과 바인딩 포즈에 필요한 모든 변환 행렬을 포함한다.
    /// - 바인딩이란? T 또는 A 포즈에 해당하는 캐릭터의 기본 자세에서 캐릭터 공간에서 각 본이 변환되어지는 행렬을 이야기한다.
    /// - 바인딩 행렬의 역행렬은 캐릭터 공간의 본의 위치를 부모 뼈대 공간으로 변환하는 데 사용된다.
    /// </code>
    /// </summary>
    public class BoneMatrixSet
    {
        // 애니메이션 변환 행렬들
        Matrix4x4f _localTransform = Matrix4x4f.Identity;      // 부모 뼈 공간에서의 변환 행렬

        // 바인딩 포즈 행렬들
        Matrix4x4f _localBindTransform = Matrix4x4f.Identity;           // 부모 뼈 공간에서의 바인딩 포즈 행렬
        Matrix4x4f _inverseBindPoseTransform = Matrix4x4f.Identity;     // 캐릭터 공간에서의 바인딩 포즈 역행렬

        // 성능 향샹을 위한 변환 행렬들
        Matrix4x4f _bindPoseTransform = Matrix4x4f.Identity; // 바인딩 포즈 변환 행렬 (캐릭터 공간에서)

        // 속성
        public Vertex3f Pivot => _bindPoseTransform.Position;

        /// <summary>
        /// 부모 뼈대 공간에서의 바인딩 포즈 위치
        /// </summary>
        public Vertex3f LocalBindPosition => _localBindTransform.Position;


        /// <summary>
        /// 부모 뼈대 공간에서의 로컬 변환 행렬
        /// </summary>
        public Matrix4x4f LocalTransform
        {
            get => _localTransform;
            set => _localTransform = value;
        }

        /// <summary>
        /// 부모 뼈대 공간에서의 바인딩 포즈 변환 행렬
        /// </summary>
        public Matrix4x4f LocalBindTransform
        {
            get => _localBindTransform;
            set => _localBindTransform = value;
        }

        /// <summary>
        /// 캐릭터 공간에서의 바인딩 포즈 역행렬 (스키닝에 사용)
        /// </summary>
        public Matrix4x4f InverseBindPoseTransform
        {
            get => _inverseBindPoseTransform;
            set
            {
                _inverseBindPoseTransform = value;
                _bindPoseTransform = value.Inversed();
            }
        }

    }
}