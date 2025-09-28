using OpenGL;
using System;
using ZetaExt;
using Quaternion = ZetaExt.Quaternion;

namespace Animate
{
    /// <summary>
    /// 단일 본 Look At IK 시스템
    /// <br/>
    /// 하나의 본이 월드 공간의 특정 위치를 바라보도록 회전시키는 IK 구현이다.
    /// 머리, 눈, 척추 등 단일 관절 회전에 적합하다.
    /// 
    /// <code>
    /// 사용 예시:
    /// var headLookAt = new SingleBoneLookAt(headBone, Vertex3f.UnitY, Vertex3f.UnitZ);
    /// headLookAt.LookAt(targetPosition, modelMatrix, animator);
    /// </code>
    /// </summary>
    public class SingleBoneLookAt
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private readonly Bone _bone;               // 제어할 본
        private readonly Vertex3f _localForward;   // 본의 로컬 전방 벡터 (바라보는 방향)
        private readonly Vertex3f _localUp;        // 본의 로컬 상향 벡터 (업 방향)

        // -----------------------------------------------------------------------
        // 속성
        // -----------------------------------------------------------------------

        /// <summary>본의 로컬 전방 벡터</summary>
        public Vertex3f LocalForward => _localForward;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------

        /// <summary>
        /// SingleBoneLookAt 생성자
        /// </summary>
        /// <param name="bone">제어할 본</param>
        /// <param name="localForward">본의 로컬 전방 벡터 (기본: Y축)</param>
        /// <param name="localUp">본의 로컬 상향 벡터 (기본: Z축)</param>
        /// <exception cref="ArgumentException">Forward와 Up 벡터가 평행한 경우</exception>
        public SingleBoneLookAt(Bone bone, Vertex3f localForward = default, Vertex3f localUp = default)
        {
            _bone = bone ?? throw new ArgumentNullException(nameof(bone));
            _localForward = (localForward == default ? Vertex3f.UnitY : localForward).Normalized;
            _localUp = (localUp == default ? Vertex3f.UnitZ : localUp).Normalized;

            // Forward와 Up 벡터가 평행하면 안 됨
            if (Math.Abs(_localForward.Dot(_localUp)) > 0.99f)
                throw new ArgumentException("Local Forward와 Up 벡터는 평행하지 않아야 한다.");
        }

        // -----------------------------------------------------------------------
        // 공개 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 본에 지정된 회전을 적용한다
        /// </summary>
        /// <param name="animator">애니메이터</param>
        /// <param name="rotation">적용할 회전 (쿼터니언)</param>
        public void Rotate(Animator animator, Quaternion rotation)
        {
            // 현재 로컬 변환에 회전 적용
            _bone.BoneMatrixSet.LocalTransform *= (Matrix4x4f)rotation;

            // 애니메이터 변환 업데이트
            _bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);
        }

        /// <summary>
        /// 본이 월드 공간의 특정 위치를 바라보도록 회전시킨다
        /// </summary>
        /// <param name="worldTargetPosition">바라볼 월드 위치</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="worldUpHint">월드 업 벡터 힌트 (기본: Z축)</param>
        public void LookAt(Vertex3f worldTargetPosition, Matrix4x4f modelMatrix, Animator animator,
                          Vertex3f worldUpHint = default)
        {
            var rotationInfo = CalculateRotation(worldTargetPosition, modelMatrix, animator, worldUpHint);

            // 계산된 변환 행렬을 본에 적용
            _bone.BoneMatrixSet.LocalTransform = rotationInfo.Matrix;
            _bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);
        }

        /// <summary>
        /// Look At에 필요한 회전 정보를 계산한다 (실제 적용하지 않음)
        /// </summary>
        /// <param name="worldTargetPosition">바라볼 월드 위치</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <param name="worldUpHint">월드 업 벡터 힌트</param>
        /// <returns>회전 정보 (쿼터니언과 행렬)</returns>
        public RotationInfo CalculateRotation(Vertex3f worldTargetPosition, Matrix4x4f modelMatrix,
                                            Animator animator, Vertex3f worldUpHint = default)
        {
            // 현재 본의 월드 변환 계산
            var currentWorldTransform = animator.GetRootTransform(_bone);
            var finalWorldTransform = modelMatrix * currentWorldTransform;
            var currentWorldPosition = finalWorldTransform.Position;

            // 기본 월드 업 벡터 설정
            worldUpHint = worldUpHint == default ? Vertex3f.UnitZ : worldUpHint.Normalized;

            // 타겟 방향 벡터 계산
            var targetDirection = (worldTargetPosition - currentWorldPosition).Normalized;

            // Look At 변환 행렬 생성
            var lookAtMatrix = CreateLocalSpaceTransform(targetDirection, worldUpHint, finalWorldTransform);

            return new RotationInfo(lookAtMatrix.ToQuaternion(), lookAtMatrix);
        }

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 좌표계 변환을 통해 Look At 로컬 변환 행렬을 생성한다
        /// <br/>
        /// From 좌표계(원래 본의 로컬 축) → To 좌표계(목표 방향 기준)로 변환하는 행렬을 계산한다.
        /// </summary>
        /// <param name="worldTargetDirection">월드 공간에서의 목표 방향</param>
        /// <param name="worldUpHint">월드 공간에서의 업 벡터 힌트</param>
        /// <param name="finalWorldTransform">본의 최종 월드 변환</param>
        /// <returns>새로운 로컬 변환 행렬</returns>
        private Matrix4x4f CreateLocalSpaceTransform(Vertex3f worldTargetDirection, Vertex3f worldUpHint,
                                                   Matrix4x4f finalWorldTransform)
        {
            // 1단계: 월드 방향을 부모 로컬 공간으로 변환
            Matrix4x4f parentWorldTransform = _bone.Parent == null ?
                Matrix4x4f.Identity : finalWorldTransform * _bone.BoneMatrixSet.LocalTransform.Inversed();

            Matrix4x4f worldToParentLocal = parentWorldTransform.Inversed();
            Vertex4f localDir = worldToParentLocal * new Vertex4f(worldTargetDirection.x, worldTargetDirection.y, worldTargetDirection.z, 0);
            Vertex4f localUp = worldToParentLocal * new Vertex4f(worldUpHint.x, worldUpHint.y, worldUpHint.z, 0);

            Vertex3f targetDir = new Vertex3f(localDir.x, localDir.y, localDir.z).Normalized;
            Vertex3f upHint = new Vertex3f(localUp.x, localUp.y, localUp.z).Normalized;

            // 2단계: From 좌표계 구성 (원래 본의 로컬 축들)
            Vertex3f fromForward = _localForward;
            Vertex3f fromRight = fromForward.Cross(_localUp).Normalized;
            Vertex3f fromUp = fromRight.Cross(fromForward).Normalized;

            // 3단계: To 좌표계 구성 (목표 방향 기준)
            Vertex3f toForward = targetDir;
            Vertex3f toRight = toForward.Cross(upHint).Normalized;

            // Right 벡터 검증 및 보정
            if (toRight.Length() < 0.001f)
            {
                Vertex3f altUp = Math.Abs(toForward.Dot(Vertex3f.UnitX)) < 0.9f ?
                    Vertex3f.UnitX : Vertex3f.UnitY;
                toRight = toForward.Cross(altUp).Normalized;
            }
            Vertex3f toUp = toRight.Cross(toForward).Normalized;

            // 4단계: 좌표계 변환 행렬 생성
            Matrix4x4f fromBasis = new Matrix4x4f(
                fromRight.x, fromRight.y, fromRight.z, 0,
                fromForward.x, fromForward.y, fromForward.z, 0,
                fromUp.x, fromUp.y, fromUp.z, 0,
                0, 0, 0, 1);

            Matrix4x4f toBasis = new Matrix4x4f(
                toRight.x, toRight.y, toRight.z, 0,
                toForward.x, toForward.y, toForward.z, 0,
                toUp.x, toUp.y, toUp.z, 0,
                0, 0, 0, 1);

            // 5단계: From → To 변환 = toBasis * fromBasis^(-1)
            Matrix4x4f basisTransform = toBasis * fromBasis.Inversed();

            // 6단계: 원래 위치 보존
            Vertex3f originalPosition = _bone.BoneMatrixSet.LocalTransform.Position;
            basisTransform[3, 0] = originalPosition.x;
            basisTransform[3, 1] = originalPosition.y;
            basisTransform[3, 2] = originalPosition.z;

            return basisTransform;
        }

        // -----------------------------------------------------------------------
        // 내부 구조체
        // -----------------------------------------------------------------------

        /// <summary>
        /// Look At 회전 정보를 담는 구조체
        /// </summary>
        public readonly struct RotationInfo
        {
            /// <summary>회전을 나타내는 쿼터니언</summary>
            public readonly Quaternion Quaternion;

            /// <summary>회전을 나타내는 4x4 행렬</summary>
            public readonly Matrix4x4f Matrix;

            public RotationInfo(Quaternion quaternion, Matrix4x4f matrix)
            {
                Quaternion = quaternion;
                Matrix = matrix;
            }
        }
    }
}