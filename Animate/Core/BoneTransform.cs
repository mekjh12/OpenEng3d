using System;
using OpenGL;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 본(뼈대)의 변환 정보를 저장하는 구조체<br/>
    /// - 부모뼈공간(로컬공간)에서 본의 위치(Position), 회전(Rotation), 크기(Scaling) 정보 관리<br/>
    /// - 애니메이션 키프레임에서 본의 상태를 표현하는 기본 단위
    /// </summary>
    public readonly struct BoneTransform : IEquatable<BoneTransform>
    {
        readonly Vertex3f _scaling;   // 본의 스케일 (x, y, z 배율)
        readonly Vertex3f _position;  // 본의 위치 (x, y, z 좌표) 
        readonly ZetaExt.Quaternion _rotation; // 본의 회전 (쿼터니언)

        public Vertex3f Scaling => _scaling;
        public Vertex3f Position => _position;
        public ZetaExt.Quaternion Rotation => _rotation;

        /// <summary>위치만 지정하는 생성자 (기본: 회전 없음, 스케일 1.0)</summary>
        public BoneTransform(Vertex3f position)
        {
            _position = position;
            _rotation = new ZetaExt.Quaternion(Vertex3f.UnitX, 0); // 회전 없음
            _scaling = Vertex3f.One; // 스케일 1.0
        }

        /// <summary>위치와 회전을 지정하는 생성자 (기본: 스케일 1.0)</summary>
        public BoneTransform(Vertex3f position, ZetaExt.Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;
            _scaling = Vertex3f.One; // 스케일 1.0
        }

        /// <summary>모든 변환 요소를 지정하는 생성자</summary>
        public BoneTransform(Vertex3f position, ZetaExt.Quaternion rotation, Vertex3f scaling)
        {
            _position = position;
            _rotation = rotation;
            _scaling = scaling;
        }

        /// <summary>기본 BoneTransform (원점, 회전 없음, 스케일 1.0)</summary>
        public static BoneTransform Identity => new BoneTransform(
            Vertex3f.Zero,
            ZetaExt.Quaternion.Identity,
            Vertex3f.One
        );

        /// <summary>
        /// 본의 로컬 변환 행렬<br/>
        /// T(이동) * R(회전) * S(스케일) 순서로 변환 적용
        /// </summary>
        public Matrix4x4f LocalTransform
        {
            get
            {
                Matrix4x4f T = Matrix4x4f.Translated(_position.x, _position.y, _position.z);
                Matrix4x4f R = (Matrix4x4f)_rotation;
                Matrix4x4f S = Matrix4x4f.Scaled(_scaling.x, _scaling.y, _scaling.z);
                return T * R * S; // TRS 순서
            }
        }

        /// <summary>스케일을 변경한 새로운 BoneTransform 반환</summary>
        public BoneTransform WithScaling(Vertex3f scaling)
        {
            return new BoneTransform(_position, _rotation, scaling);
        }

        /// <summary>위치를 변경한 새로운 BoneTransform 반환</summary>
        public BoneTransform WithPosition(Vertex3f position)
        {
            return new BoneTransform(position, _rotation, _scaling);
        }

        /// <summary>회전을 변경한 새로운 BoneTransform 반환</summary>
        public BoneTransform WithRotation(ZetaExt.Quaternion rotation)
        {
            return new BoneTransform(_position, rotation, _scaling);
        }

        /// <summary>
        /// 두 변환 행렬 간의 구면 선형 보간 (SLERP)<br/>
        /// 애니메이션에서 부드러운 변환을 위해 사용
        /// </summary>
        /// <param name="frameA">시작 변환 행렬</param>
        /// <param name="frameB">끝 변환 행렬</param>
        /// <param name="progression">보간 진행률 (0.0 ~ 1.0)</param>
        /// <returns>보간된 변환 행렬</returns>
        public static Matrix4x4f InterpolateSlerp(Matrix4x4f frameA, Matrix4x4f frameB, float progression)
        {
            // 위치 보간 (선형)
            Vertex3f pos = InterpolateLerp(frameA.Position, frameB.Position, progression);

            // 회전 보간 (구면 선형 보간)
            ZetaExt.Quaternion qa = frameA.ToQuaternion();
            ZetaExt.Quaternion qb = frameB.ToQuaternion();
            ZetaExt.Quaternion rot = qa.Interpolate(qb, progression);

            // 회전 행렬 생성
            Matrix4x4f res = (Matrix4x4f)rot;

            // 원본 스케일 정보 보존하여 최종 행렬 구성
            Vertex3f c0 = res.Column0.Vertex3f() * frameA.Column0.Vertex3f().Norm();
            Vertex3f c1 = res.Column1.Vertex3f() * frameA.Column1.Vertex3f().Norm();
            Vertex3f c2 = res.Column2.Vertex3f() * frameA.Column2.Vertex3f().Norm();

            res = new Matrix4x4f(c0.x, c0.y, c0.z, 0,
                                c1.x, c1.y, c1.z, 0,
                                c2.x, c2.y, c2.z, 0,
                                pos.x, pos.y, pos.z, 1);
            return res;
        }

        /// <summary>
        /// 두 BoneTransform 간의 구면 선형 보간<br/>
        /// 키프레임 애니메이션에서 중간 포즈 계산에 사용
        /// </summary>
        /// <param name="frameA">시작 포즈</param>
        /// <param name="frameB">끝 포즈</param>
        /// <param name="progression">보간 진행률 (0.0 ~ 1.0)</param>
        /// <returns>보간된 BoneTransform</returns>
        public static BoneTransform InterpolateSlerp(BoneTransform frameA, BoneTransform frameB, float progression)
        {
            // 위치와 회전, 스케일을 각각 보간
            Vertex3f pos = InterpolateLerp(frameA.Position, frameB.Position, progression);
            ZetaExt.Quaternion rot = frameA.Rotation.Interpolate(frameB.Rotation, progression);
            Vertex3f scale = InterpolateLerp(frameA.Scaling, frameB.Scaling, progression);

            return new BoneTransform(pos, rot, scale);
        }

        /// <summary>
        /// 두 BoneTransform? 간의 구면 선형 보간<br/>
        /// null 처리가 필요한 경우 사용
        /// </summary>
        /// <param name="frameA">시작 포즈 (null 가능)</param>
        /// <param name="frameB">끝 포즈 (null 가능)</param>
        /// <param name="progression">보간 진행률 (0.0 ~ 1.0)</param>
        /// <returns>보간된 BoneTransform</returns>
        public static BoneTransform? InterpolateSlerp(BoneTransform? frameA, BoneTransform? frameB, float progression)
        {
            // null 처리: 하나만 있으면 그것을 사용
            if (frameA == null && frameB != null) return frameB;
            if (frameA != null && frameB == null) return frameA;
            if (frameA == null && frameB == null) return null;

            return InterpolateSlerp(frameA.Value, frameB.Value, progression);
        }

        /// <summary>두 3D 벡터 간의 선형 보간</summary>
        private static Vertex3f InterpolateLerp(Vertex3f start, Vertex3f end, float progression)
        {
            return start + (end - start) * progression;
        }

        /// <summary>지정된 변환 행렬에서 BoneTransform 생성</summary>
        public static BoneTransform FromMatrix(Matrix4x4f matrix)
        {
            var position = matrix.Position;
            var rotation = matrix.ToQuaternion();

            // 스케일 추출 (각 열벡터의 크기)
            var scaling = new Vertex3f(
                matrix.Column0.Vertex3f().Norm(),
                matrix.Column1.Vertex3f().Norm(),
                matrix.Column2.Vertex3f().Norm()
            );

            return new BoneTransform(position, rotation, scaling);
        }

        public bool Equals(BoneTransform other)
        {
            return _position.Equals(other._position) &&
                   _rotation.Equals(other._rotation) &&
                   _scaling.Equals(other._scaling);
        }

        public override bool Equals(object obj)
        {
            return obj is BoneTransform other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + _position.GetHashCode();
                hash = hash * 23 + _rotation.GetHashCode();
                hash = hash * 23 + _scaling.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(BoneTransform left, BoneTransform right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BoneTransform left, BoneTransform right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"BoneTransform(Pos: {_position}, Rot: {_rotation}, Scale: {_scaling})";
        }
    }
}