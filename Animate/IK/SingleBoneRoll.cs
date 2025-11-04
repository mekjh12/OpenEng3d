using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    public class SingleBoneRoll
    {
        private readonly Bone _bone;
        private readonly LocalSpaceAxis _rollAxis;

        public SingleBoneRoll(Bone bone, LocalSpaceAxis rollAxis = LocalSpaceAxis.Y)
        {
            _bone = bone;
            _rollAxis = rollAxis;
        }

        public Matrix4x4f Solve(Vertex3f target, Matrix4x4f modelMatrix, Animator animator, float weight = 1.0f)
        {
            // 월드 공간 변환 계산
            Matrix4x4f worldTransform = modelMatrix * animator.GetRootTransform(_bone);
            Matrix4x4f animateToLocal = worldTransform.Inversed();

            // 타겟을 로컬 공간으로 변환
            Vertex3f localTarget = animateToLocal.Transform(target);

            // Roll 축에 따라 2D 투영점 계산
            Vertex2f projPoint = GetProjectedPoint(localTarget);

            // 회전 각도 계산 (-π ~ π)
            float angle = (float)Math.Atan2(projPoint.y, projPoint.x);

            // 가중치를 적용한 회전 행렬 생성
            Matrix4x4f rotation = Matrix4x4f.RotatedY(angle.ToDegree()) * weight;

            // 최종 변환 행렬 계산 및 본 업데이트
            Matrix4x4f finalMatrix = _bone.BoneMatrixSet.LocalTransform * rotation;
            _bone.UpdateBone(ref finalMatrix, animator, true);

            return rotation;
        }

        private Vertex2f GetProjectedPoint(Vertex3f localTarget)
        {
            if (_rollAxis == LocalSpaceAxis.Y)
                return new Vertex2f(localTarget.z, localTarget.x);

            if (_rollAxis == LocalSpaceAxis.X)
                return new Vertex2f(localTarget.y, localTarget.z);

            if (_rollAxis == LocalSpaceAxis.Z)
                return new Vertex2f(localTarget.x, localTarget.y);

            throw new ArgumentException($"지원하지 않는 Roll 축: {_rollAxis}");
        }
    }
}