using Animate;
using OpenGL;
using System;
using ZetaExt;

namespace Animates
{
    internal class ThreeBoneRoll
    {
        private readonly Bone _firstBone;
        private readonly Bone _secondBone;
        private readonly Bone _thirdBone;
        private readonly LocalSpaceAxis _rollAxis;
        private readonly SingleBoneRoll _firstBoneRoll;
        private readonly SingleBoneRoll _secondBoneRoll;
        private readonly SingleBoneRoll _thirdBoneRoll;

        [Obsolete("이 클래스는 정상 작동하지 않습니다.")]
        public ThreeBoneRoll(Bone firstBone, Bone secondBone, Bone thirdBone, LocalSpaceAxis rollAxis = LocalSpaceAxis.Y)
        {
            _firstBone = firstBone;
            _secondBone = secondBone;
            _thirdBone = thirdBone;
            _rollAxis = rollAxis;
            _firstBoneRoll = new SingleBoneRoll(_firstBone, _rollAxis);
            _secondBoneRoll = new SingleBoneRoll(_secondBone, _rollAxis);
            _thirdBoneRoll = new SingleBoneRoll(_thirdBone, _rollAxis);
        }

        public void Solve(Vertex3f target, Matrix4x4f modelMatrix, Animator animator,
            float firstWeight = 0.33f, float secondWeight = 0.66f, float thirdWeight = 1.0f)
        {
            // 첫 번째 본 회전 적용
            Matrix4x4f rotation = _firstBoneRoll.Solve(target, modelMatrix, animator, firstWeight);

            // 두 번째 본에 역회전 적용 후 롤 계산
            rotation = _secondBone.BoneMatrixSet.LocalTransform * rotation.Inversed();
            _secondBone.UpdateBone(ref rotation, animator, true);
            rotation = _secondBoneRoll.Solve(target, modelMatrix, animator, secondWeight);

            // 세 번째 본에 역회전 적용 후 롤 계산
            rotation = _thirdBone.BoneMatrixSet.LocalTransform * rotation.Inversed();
            _thirdBone.UpdateBone(ref rotation, animator, true);
            _thirdBoneRoll.Solve(target, modelMatrix, animator, thirdWeight);
        }
    }
}