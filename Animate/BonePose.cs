using System;
using Assimp;
using OpenGL;
using ZetaExt;

namespace Animate
{
    /// <summary>
    /// 본(뼈대)의 변환 정보를 저장하는 클래스<br/>
    /// - 3D 공간에서 본의 위치(Position), 회전(Rotation), 크기(Scaling) 정보 관리<br/>
    /// - 애니메이션 키프레임에서 본의 상태를 표현하는 기본 단위
    /// </summary>
    public class BonePose
    {
        Vertex3f _scaling;   // 본의 스케일 (x, y, z 배율)
        Vertex3f _position;  // 본의 위치 (x, y, z 좌표)
        ZetaExt.Quaternion _rotation; // 본의 회전 (쿼터니언)

        public Vertex3f Scaling
        {
            get => _scaling;
            set => _scaling = value;
        }

        public Vertex3f Position
        {
            get => _position;
            set => _position = value;
        }

        public ZetaExt.Quaternion Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        /// <summary>위치만 지정하는 생성자 (기본: 회전 없음, 스케일 1.0)</summary>
        public BonePose(Vertex3f position)
        {
            _position = position;
            _rotation = new ZetaExt.Quaternion(Vertex3f.UnitX, 0); // 회전 없음
            _scaling = Vertex3f.One; // 스케일 1.0
        }

        /// <summary>위치와 회전을 지정하는 생성자 (기본: 스케일 1.0)</summary>
        public BonePose(Vertex3f position, ZetaExt.Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;
            _scaling = Vertex3f.One; // 스케일 1.0
        }

        /// <summary>Assimp Vector3D 타입으로 모든 변환 정보를 지정하는 생성자</summary>
        public BonePose(Vector3D position, ZetaExt.Quaternion rotation, Vector3D scaling)
        {
            _position = new Vertex3f((float)position.X, (float)position.Y, (float)position.Z);
            _rotation = rotation;
            _scaling = new Vertex3f((float)scaling.X, (float)scaling.Y, (float)scaling.Z);
        }

        /// <summary>기본 생성자 (원점, 회전 없음, 스케일 1.0)</summary>
        public BonePose()
        {
            _position = Vertex3f.Zero;
            _rotation = ZetaExt.Quaternion.Identity;
            _scaling = Vertex3f.One;
        }

        /// <summary>
        /// 본의 로컬 변환 행렬 생성<br/>
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
        /// 두 BonePose 간의 구면 선형 보간<br/>
        /// 키프레임 애니메이션에서 중간 포즈 계산에 사용
        /// </summary>
        /// <param name="frameA">시작 포즈</param>
        /// <param name="frameB">끝 포즈</param>
        /// <param name="progression">보간 진행률 (0.0 ~ 1.0)</param>
        /// <returns>보간된 BonePose</returns>
        public static BonePose InterpolateSlerp(BonePose frameA, BonePose frameB, float progression)
        {
            // null 처리: 하나만 있으면 그것을 사용
            if (frameA == null && frameB != null) frameA = frameB;
            if (frameA != null && frameB == null) frameB = frameA;

            // 위치와 회전을 각각 보간
            Vertex3f pos = InterpolateLerp(frameA.Position, frameB.Position, progression);
            ZetaExt.Quaternion rot = frameA.Rotation.Interpolate(frameB.Rotation, progression);

            return new BonePose(pos, rot);
        }

        /// <summary>두 3D 벡터 간의 선형 보간</summary>
        private static Vertex3f InterpolateLerp(Vertex3f start, Vertex3f end, float progression)
        {
            return start + (end - start) * progression;
        }

        /// <summary>BonePose의 모든 변환 정보를 복사하여 새 인스턴스 생성</summary>
        public BonePose Clone()
        {
            return new BonePose(_position, _rotation) { Scaling = _scaling };
        }

        /// <summary>지정된 변환 행렬에서 BonePose 생성</summary>
        public static BonePose FromMatrix(Matrix4x4f matrix)
        {
            var pose = new BonePose();
            pose._position = matrix.Position;
            pose._rotation = matrix.ToQuaternion();

            // 스케일 추출 (각 열벡터의 크기)
            pose._scaling = new Vertex3f(
                matrix.Column0.Vertex3f().Norm(),
                matrix.Column1.Vertex3f().Norm(),
                matrix.Column2.Vertex3f().Norm()
            );

            return pose;
        }

        public override string ToString()
        {
            return $"BonePose(Pos: {_position}, Rot: {_rotation}, Scale: {_scaling})";
        }
    }
}