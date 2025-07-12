using OpenGL;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 뼈대의 변환 행렬들을 관리하는 클래스
    /// 애니메이션과 바인딩 포즈에 필요한 모든 변환 행렬을 포함한다.
    /// </summary>
    public class BoneTransforms
    {
        // 애니메이션 변환 행렬들
        Matrix4x4f _localTransform = Matrix4x4f.Identity;      // 부모 뼈 공간에서의 변환 행렬
        Matrix4x4f _animatedTransform = Matrix4x4f.Identity;   // 캐릭터 공간에서의 애니메이션 변환 행렬
        // LocalTransform이 변경되면 UpdateAnimatedFromLocal()를 통해 _animatedTransform이 업데이트됨

        // 바인딩 포즈 행렬들
        Matrix4x4f _localBindTransform = Matrix4x4f.Identity;          // 부모 뼈 공간에서의 바인딩 포즈 행렬
        Matrix4x4f _animatedBindPoseTransform = Matrix4x4f.Identity;   // 캐릭터 공간에서의 바인딩 포즈 변환 행렬
        Matrix4x4f _inverseBindPoseTransform = Matrix4x4f.Identity;    // 캐릭터 공간에서의 바인딩 포즈 역행렬

        /// <summary>
        /// 부모 뼈대 공간에서의 로컬 변환 행렬
        /// </summary>
        public Matrix4x4f LocalTransform
        {
            get => _localTransform;
            set => _localTransform = value;
        }

        /// <summary>
        /// 캐릭터 공간에서의 애니메이션 변환 행렬
        /// </summary>
        public Matrix4x4f AnimatedTransform
        {
            get => _animatedTransform;
            set => _animatedTransform = value;
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
        /// 캐릭터 공간에서의 바인딩 포즈 변환 행렬
        /// </summary>
        public Matrix4x4f AnimatedBindPoseTransform
        {
            get => _animatedBindPoseTransform;
            set => _animatedBindPoseTransform = value;
        }

        /// <summary>
        /// 캐릭터 공간에서의 바인딩 포즈 역행렬 (스키닝에 사용)
        /// </summary>
        public Matrix4x4f InverseBindPoseTransform
        {
            get => _inverseBindPoseTransform;
            set => _inverseBindPoseTransform = value;
        }

        /// <summary>
        /// 부모의 변환을 기반으로 로컬 변환을 역계산한다
        /// (캐릭터 공간 → 부모 뼈 공간 변환)
        /// </summary>
        public void UpdateLocalFromAnimated(Matrix4x4f parentAnimatedTransform)
        {
            _localTransform = parentAnimatedTransform.Inversed() * _animatedTransform;
        }

        /// <summary>
        /// 부모의 변환을 기반으로 애니메이션 변환을 계산한다
        /// (부모 뼈 공간 → 캐릭터 공간 변환)
        /// </summary>
        public void UpdateAnimatedFromLocal(Matrix4x4f parentAnimatedTransform)
        {
            _animatedTransform = parentAnimatedTransform * _localTransform;
        }

        /// <summary>
        /// 애니메이션 변환의 위치를 설정한다
        /// </summary>
        /// <param name="position">설정할 위치</param>
        public void SetAnimatedPosition(Vertex3f position)
        {
            _animatedTransform[3, 0] = position.x;
            _animatedTransform[3, 1] = position.y;
            _animatedTransform[3, 2] = position.z;
        }
    }
}