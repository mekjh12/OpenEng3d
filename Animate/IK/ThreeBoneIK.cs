using OpenGL;
using System;

namespace Animate
{
    /// <summary>
    /// 아직 구현되지 않은 3본 IK 클래스 (미완성) : TwoBoneIK를 2번 사용 (불안정)
    /// </summary>
    public class ThreeBoneIK
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private readonly Bone _upperBone;
        private readonly Bone _middleBone;
        private readonly Bone _lowerBone;
        private readonly Bone _endBone;

        private readonly TwoBoneIK _mainBoneIK;
        private readonly TwoBoneIK _subBoneIK;

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        public Bone UpperBone => _upperBone;
        public Bone MiddleBone => _middleBone;
        public Bone LowerBone => _lowerBone;
        public Bone EndBone => _endBone;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// TwoBoneIK 생성자
        /// </summary>
        public ThreeBoneIK(Bone upperBone, Bone middleBone, Bone lowerBone, Bone endBone)
        {
            // 본 유효성 검사
            _upperBone = upperBone ?? throw new ArgumentNullException(nameof(upperBone));
            _middleBone = middleBone ?? throw new ArgumentNullException(nameof(middleBone));
            _lowerBone = lowerBone ?? throw new ArgumentNullException(nameof(lowerBone));
            _endBone = endBone ?? throw new ArgumentNullException(nameof(endBone));

            _mainBoneIK =new TwoBoneIK(upperBone, middleBone, lowerBone);
            _subBoneIK = new TwoBoneIK(middleBone, lowerBone, endBone);
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 3본 IK를 해결하여 끝점이 목표 위치에 도달하도록 한다
        /// </summary>
        /// <param name="targetPositionWorld">목표 월드 위치</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <returns>계산된 관절 위치들 [Root, Mid, End, MidAfter]</returns>
        public Vertex3f[] Solve(Vertex3f targetPositionWorld, Matrix4x4f modelMatrix, Animator animator)
        {
            _mainBoneIK.Solve(targetPositionWorld, modelMatrix, animator);
            _subBoneIK.Solve(targetPositionWorld, modelMatrix, animator);
            return new Vertex3f[] {  };
        }


    }
}