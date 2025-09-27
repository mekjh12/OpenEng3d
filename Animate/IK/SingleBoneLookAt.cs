using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 단일 본이 특정 방향을 바라보도록 하는 Look At IK
    /// 머리, 눈, 척추 등에 사용된다
    /// </summary>
    public class SingleBoneLookAt
    {
        private Bone _bone;
        private Vertex3f _localForward;  // 본의 로컬 전방 벡터
        private Vertex3f _localUp;       // 본의 로컬 상향 벡터

        public SingleBoneLookAt(Bone bone, Vertex3f localForward = default, Vertex3f localUp = default)
        {
            _bone = bone;
            _localForward = localForward == default ? Vertex3f.UnitY : localForward.Normalized;
            _localUp = localUp == default ? Vertex3f.UnitZ : localUp.Normalized;
        }

        /// <summary>
        /// 특정 월드 위치를 바라보도록 설정
        /// </summary>
        /// <param name="worldTargetPosition">바라볼 월드 위치</param>
        /// <param name="model">모델변환행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="worldUpHint">월드 업 벡터 힌트</param>
        public Vertex3f LookAt(Vertex3f worldTargetPosition, Matrix4x4f model, Animator animator, Vertex3f worldUpHint = default)
        {
            // 현재 본의 월드 변환 가져오기
            Matrix4x4f currentWorldTransform = animator.GetRootTransform(_bone);
            Matrix4x4f finalWorldTransform = model * currentWorldTransform;
            Vertex3f currentWorldPosition = finalWorldTransform.Position;

            // 월드 업 벡터 설정
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 타겟 방향 계산
            Vertex3f toTarget = (worldTargetPosition - currentWorldPosition).Normalized;

            // Look At 회전 계산
            Matrix4x4f lookAtRotation = CalculateLookAtRotation(toTarget, worldUpHint, finalWorldTransform);

            // 본에 적용 (각도 제한 없음)
            _bone.BoneMatrixSet.LocalTransform = lookAtRotation;
            _bone.UpdateAnimatorTransforms(animator, true);

            return currentWorldPosition;
        }

        private Matrix4x4f CalculateLookAtRotation(Vertex3f worldTargetDirection, Vertex3f worldUpHint, Matrix4x4f finalWorldTransform)
        {
            // 로컬 공간으로 변환
            Matrix4x4f parentWorldTransform = Matrix4x4f.Identity;
            if (_bone.Parent != null)
            {
                parentWorldTransform = finalWorldTransform * _bone.BoneMatrixSet.LocalTransform.Inversed();
            }

            Matrix4x4f worldToParentLocal = parentWorldTransform.Inversed();
            Vertex4f localDir = worldToParentLocal * new Vertex4f(worldTargetDirection.x, worldTargetDirection.y, worldTargetDirection.z, 0);
            Vertex4f localUp = worldToParentLocal * new Vertex4f(worldUpHint.x, worldUpHint.y, worldUpHint.z, 0);

            Vertex3f targetDir = new Vertex3f(localDir.x, localDir.y, localDir.z).Normalized;
            Vertex3f upHint = new Vertex3f(localUp.x, localUp.y, localUp.z).Normalized;

            // From 좌표계 (원래 본의 로컬 축들)
            Vertex3f fromForward = _localForward;
            Vertex3f fromRight = fromForward.Cross(_localUp).Normalized;
            Vertex3f fromUp = fromRight.Cross(fromForward).Normalized;

            // To 좌표계 (목표 방향)
            Vertex3f toForward = targetDir;
            Vertex3f toRight = toForward.Cross(upHint).Normalized;
            if (toRight.Length() < 0.001f)
            {
                Vertex3f altUp = Math.Abs(toForward.Dot(Vertex3f.UnitX)) < 0.9f ? Vertex3f.UnitX : Vertex3f.UnitY;
                toRight = toForward.Cross(altUp).Normalized;
            }
            Vertex3f toUp = toRight.Cross(toForward).Normalized;

            // 변환: From → To
            Matrix4x4f fromMatrix = new Matrix4x4f(
                fromRight.x, fromRight.y, fromRight.z, 0,
                fromForward.x, fromForward.y, fromForward.z, 0,
                fromUp.x, fromUp.y, fromUp.z, 0,
                0, 0, 0, 1);

            Matrix4x4f toMatrix = new Matrix4x4f(
                toRight.x, toRight.y, toRight.z, 0,
                toForward.x, toForward.y, toForward.z, 0,
                toUp.x, toUp.y, toUp.z, 0,
                0, 0, 0, 1);

            // 최종 변환 = toMatrix * fromMatrix^-1
            Matrix4x4f transform = toMatrix * fromMatrix.Inversed();

            // 위치 유지
            Vertex3f pos = _bone.BoneMatrixSet.LocalTransform.Position;
            transform[3, 0] = pos.x;
            transform[3, 1] = pos.y;
            transform[3, 2] = pos.z;

            return transform;
        }

        /// <summary>
        /// 특정 방향 벡터를 바라보도록 설정
        /// </summary>
        public Vertex3f LookDirection(Vertex3f worldDirection, Matrix4x4f model, Animator animator, Vertex3f worldUpHint = default)
        {
            // 현재 본의 월드 변환 가져오기
            Matrix4x4f currentWorldTransform = animator.GetRootTransform(_bone);
            Matrix4x4f finalWorldTransform = model * currentWorldTransform;
            Vertex3f currentWorldPosition = finalWorldTransform.Position;

            // 월드 업 벡터 설정
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 정규화된 방향으로 Look At 계산
            Vertex3f normalizedDirection = worldDirection.Normalized;
            Matrix4x4f lookAtRotation = CalculateLookAtRotation(normalizedDirection, worldUpHint, finalWorldTransform);

            // 본에 적용 (각도 제한 없음)
            _bone.BoneMatrixSet.LocalTransform = lookAtRotation;
            _bone.UpdateAnimatorTransforms(animator, true);

            return currentWorldPosition;
        }
    }
}