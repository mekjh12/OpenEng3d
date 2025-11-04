using OpenGL;
using System;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 단일 본 회전 IK 솔버<br/>
    /// Y축을 유지하면서 Z축이 월드 타겟을 향하도록 회전시킨다
    /// </summary>
    public class SingleBoneRotationIK
    {
        // -----------------------------------------------------------------------
        // 멤버 변수
        // -----------------------------------------------------------------------

        private readonly Bone _bone;
        private readonly Vertex3f _forward;
        private readonly Vertex3f _up;

        // 재사용 가능한 임시 변수들 (GC 압박 감소)
        private Matrix4x4f _boneWorldTransform;
        private Matrix4x4f _parentWorldTransform;
        private Matrix4x4f _newBoneWorldTransform;
        private Matrix4x4f _newBoneLocalTransform;

        private Vertex3f _boneWorldPos;
        private Vertex3f _oldX, _oldY, _oldZ;
        private Vertex3f _newX, _newY, _newZ;
        private Vertex3f _proj;
        private Vertex3f _targetDir;

        // -----------------------------------------------------------------------
        // 생성자
        // -----------------------------------------------------------------------
        public SingleBoneRotationIK(Bone bone, Vertex3f up = default, Vertex3f forward = default)
        {
            _bone = bone ?? throw new ArgumentNullException(nameof(bone));

            if (forward == default) forward = Vertex3f.UnitZ;
            if (up == default) up = Vertex3f.UnitY;

            // forward, up 직교성 검사
            if (Math.Abs(forward.Dot(up)) > 0.0001f)
                throw new ArgumentException("Forward and Up vectors must be orthogonal");

            // forward, up 단위 벡터화
            _forward = forward.Normalized;
            _up = up.Normalized;
        }

        // -----------------------------------------------------------------------
        // 정적 팩토리 메서드
        // -----------------------------------------------------------------------

        public static SingleBoneRotationIK Create(Bone bone, Vertex3f up = default, Vertex3f forward = default)
        {
            return new SingleBoneRotationIK(bone, up, forward);
        }

        // -----------------------------------------------------------------------
        // 핵심 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// IK를 계산하고 본에 적용한다
        /// </summary>
        /// <param name="worldTarget">월드 공간의 타겟 위치</param>
        /// <param name="modelMatrix">모델 변환 행렬</param>
        /// <param name="animator">애니메이터</param>
        /// <returns>회전 각도 (도)</returns>
        public float Solve(Vertex3f worldTarget, Matrix4x4f modelMatrix, Animator animator)
        {
            // 1. 현재 본의 월드 변환 가져오기
            _boneWorldTransform = animator.GetAnimatedWorldTransform(_bone, modelMatrix);
            _boneWorldPos = _boneWorldTransform.Position;

            // 2. 현재 본의 로컬 축과 스케일 추출
            ExtractAxesAndScales(out float sizeX, out float sizeY, out float sizeZ);

            // 3. 타겟을 Y축에 투영한 지점 계산
            _targetDir = worldTarget - _boneWorldPos;

            // 투영 점 계산
            _proj = _targetDir.Project(_oldY) + _boneWorldPos;

            // 4. 새로운 좌표계 구성 (Y축은 유지, Z축은 타겟 방향)
            ConstructNewCoordinateSystem(worldTarget, sizeX, sizeY, sizeZ);

            // 5. 새로운 월드 변환 행렬 생성
            _newBoneWorldTransform = Matrix4x4f.Identity.Frame(_newX, _newY, _newZ, _boneWorldPos);

            // 6. 부모의 월드 변환 구하기
            GetParentWorldTransform(modelMatrix, animator);

            // 7. 새로운 로컬 변환 계산: newLocal = parentWorld^(-1) * newWorld
            _newBoneLocalTransform = _parentWorldTransform.Inversed() * _newBoneWorldTransform;

            // 8. 적용
            _bone.BoneMatrixSet.LocalTransform = _newBoneLocalTransform;
            _bone.UpdateAnimatorTransforms(animator, isSelfIncluded: true);

            // 9. 회전량 계산 및 반환 (도)
            return CalculateRotationAngle();
        }

        // -----------------------------------------------------------------------
        // 내부 메서드
        // -----------------------------------------------------------------------

        /// <summary>
        /// 본의 현재 축과 스케일을 추출한다
        /// </summary>
        private void ExtractAxesAndScales(out float sizeX, out float sizeY, out float sizeZ)
        {
            if (_up == Vertex3f.UnitY && _forward == Vertex3f.UnitZ)
            {
                _oldX = _boneWorldTransform.Column0.xyz();
                _oldY = _boneWorldTransform.Column1.xyz();
                _oldZ = _boneWorldTransform.Column2.xyz();
            }
            if (_up == Vertex3f.UnitY && _forward == Vertex3f.UnitX)
            {
                _oldX = _boneWorldTransform.Column2.xyz();
                _oldY = _boneWorldTransform.Column1.xyz();
                _oldZ = -_boneWorldTransform.Column0.xyz();
            }
            else if (_up == Vertex3f.UnitZ && _forward == Vertex3f.UnitY)
            {
                _oldX = _boneWorldTransform.Column0.xyz();
                _oldY = _boneWorldTransform.Column2.xyz();
                _oldZ = -_boneWorldTransform.Column1.xyz();
            }
            else if (_up == Vertex3f.UnitZ && _forward == Vertex3f.UnitX)
            {
                _oldX = _boneWorldTransform.Column1.xyz();
                _oldY = _boneWorldTransform.Column2.xyz();
                _oldZ = _boneWorldTransform.Column0.xyz();
            }
            else if (_up == Vertex3f.UnitX && _forward == Vertex3f.UnitZ)
            {
                _oldX = -_boneWorldTransform.Column2.xyz();
                _oldY = _boneWorldTransform.Column0.xyz();
                _oldZ = _boneWorldTransform.Column1.xyz();
            }
            else if (_up == Vertex3f.UnitX && _forward == Vertex3f.UnitY)
            {
                _oldX = -_boneWorldTransform.Column1.xyz();
                _oldY = _boneWorldTransform.Column0.xyz();
                _oldZ = _boneWorldTransform.Column2.xyz();
            }

            sizeX = _oldX.Length();
            sizeY = _oldY.Length();
            sizeZ = _oldZ.Length();
        }

        /// <summary>
        /// 새로운 좌표계를 구성한다 (Y축 유지, Z축은 타겟 방향)
        /// </summary>
        private void ConstructNewCoordinateSystem(Vertex3f worldTarget, float sizeX, float sizeY, float sizeZ)
        {
            // Y축은 유지
            _newY = _oldY.Normalized * sizeY;

            // Z축은 타겟 방향
            _newZ = (worldTarget - _proj).Normalized * sizeZ;

            // Z가 너무 작으면 원래 축 유지
            if (_newZ.LengthSquared() < 0.000001f)
            {
                _newZ = _oldZ;
            }

            // X축은 Y와 Z의 외적
            _newX = _newY.Cross(_newZ).Normalized * sizeX;
        }

        /// <summary>
        /// 부모의 월드 변환을 가져온다
        /// </summary>
        private void GetParentWorldTransform(Matrix4x4f modelMatrix, Animator animator)
        {
            if (_bone.Parent == null)
            {
                _parentWorldTransform = modelMatrix;
            }
            else
            {
                _parentWorldTransform = modelMatrix * animator.GetRootTransform(_bone.Parent);
            }
        }

        /// <summary>
        /// 회전 각도를 계산한다 (도 단위)
        /// </summary>
        private float CalculateRotationAngle()
        {
            // 정규화된 벡터로 계산
            Vertex3f oldZNorm = _oldZ.Normalized;
            Vertex3f newZNorm = _newZ.Normalized;

            // 내적으로 각도 계산 (라디안)
            float dotProduct = Math.Max(-1f, Math.Min(1f, oldZNorm.Dot(newZNorm)));
            float angleRad = (float)Math.Acos(dotProduct);

            // 회전 방향 판별 (외적의 Y축 성분으로)
            float crossY = oldZNorm.Cross(newZNorm).Dot(_oldY.Normalized);
            float sign = crossY < 0 ? -1f : 1f;

            // 도 단위로 변환
            return sign * angleRad.ToDegree();
        }
    }
}