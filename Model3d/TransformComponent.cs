using Assimp;
using Common.Abstractions;
using Common.Mathematics;
using OpenGL;
using System;
using ZetaExt;

namespace Model3d
{
    /// <summary>
    /// 3D 엔티티의 변환(Transform)을 처리하는 컴포넌트입니다. <br/>
    /// 위치, 회전, 크기 등의 변환을 관리하며, 기본 바인딩 변환과 동적 자세 변환을 구분하여 처리합니다.<br/>
    /// 
    /// 주요 변환 구성요소:
    /// 1. Pose (_pose)
    ///    - 엔티티의 현재 자세와 위치를 나타내는 동적 변환
    ///    - Position: 월드 공간에서의 현재 위치
    ///    - Quaternion: 현재 회전 상태(쿼터니온)
    ///    - 실시간 변화하는 움직임과 회전을 처리 (Translate, Yaw, Pitch, Roll)
    /// 
    /// 2. Bind Matrix (_bind)
    ///    - 로컬 공간에서의 기본/초기 변환을 정의하는 정적 변환
    ///    - 주로 초기화 시 한 번 설정되며, 모델의 기본 방향이나 오프셋 조정에 사용
    ///    - LocalBindTransform 메서드를 통해 설정
    ///    - 변환 순서: Scale -> Rotation -> Translation
    /// 
    /// 사용 예시:
    /// 1. 모델이 기본적으로 90도 회전되어 있어야 하는 경우:
    ///    transform.LocalBindTransform(1, 1, 1, 90, 0, 0);  // _bind 설정
    /// 
    /// 2. 게임 실행 중 동적 변환:
    ///    transform.Translate(0, 1, 0);    // _pose 변경
    ///    transform.Yaw(45);               // _pose 변경
    /// 
    /// 이러한 구조의 장점:
    /// - 모델의 기본 방향/크기 조정(_bind)과 실제 게임플레이 중의 변환(_pose)을 분리
    /// - 애니메이션이나 물리 시뮬레이션에서는 _pose만 수정하면 됨
    /// - 모델 Import 시의 방향 문제 등을 _bind로 해결 가능
    /// 
    /// <remarks>
    /// 1. 모델 공간 (Model Space)
    ///    - 3D 모델링 도구(블렌더 등)에서 만들어진 원본 모델의 좌표계
    ///    - obj, dae 등의 3D 모델 파일이 가지고 있는 원본 좌표계
    ///    - 모델의 피봇 포인트(pivot point)가 원점(0,0,0)
    ///
    /// 2. 로컬 공간 (Local Space)  
    ///    - 게임 엔진이나 3D 응용프로그램에서 모델을 불러와서 배치하는 공간
    ///    - 각 객체가 독립적으로 가지는 자신만의 좌표계
    ///    - 객체의 위치, 회전, 크기 변환의 기준점
    ///
    /// 3. 월드 공간 (World Space)
    ///    - 3D 씬(Scene) 전체의 글로벌 좌표계
    ///    - 모든 객체들이 공유하는 공통 좌표계 
    ///    - 카메라, 조명 등의 기준이 되는 공간
    ///
    /// 변환 과정:
    /// Model Space -> Local Space -> World Space
    /// (모델 파일)    (객체 공간)    (전체 공간)
    ///
    /// 각 단계별 변환 행렬:
    /// - Model to Local: 모델 공간의 좌표를 로컬 공간으로 변환
    /// - Local to World: 로컬 공간의 좌표를 월드 공간으로 변환
    /// </remarks>
    /// </summary>
    public class TransformComponent : ITransformable
    {
        private Pose _pose;
        private Vertex3f _size;

        private bool _isMoved = true;          // [이동플래그] 이전 프레임에서 물체가 이용하였는지 유무, 처음 시작시 업데이트를 위해 true
        private Matrix4x4f _localBindMatrix;
        private Vertex3f _localRotation;
        private Vertex3f _localScaling;
        private Vertex3f _localPosition;

        /// <summary>
        /// 생성자
        /// </summary>
        public TransformComponent()
        {
            _pose = new Pose(Quaternion4.Identity, Vertex3f.Zero);
            _size = Vertex3f.One;
            _localBindMatrix = Matrix4x4f.Identity;
            _localRotation = Vertex3f.Zero;
            _localScaling = Vertex3f.One;
            _localPosition = Vertex3f.Zero;
        }

        /// <summary>
        /// 모델공간에서 로컬공간으로의 변환행렬
        /// </summary>
        public Matrix4x4f LocalBindMatrix
        {
            get => _localBindMatrix;
            set => _localBindMatrix = value;
        }

        /// <summary>
        /// 로컬공간에서 월드공간으로의 변환행렬
        /// </summary>
        public Matrix4x4f ModelMatrix
        {
            get
            {
                Matrix4x4f S = Matrix4x4F.Scaled(_size);
                Matrix4x4f R = _pose.Matrix4x4f;
                Matrix4x4f T = Matrix4x4f.Translated(_pose.Position.x, _pose.Position.y, _pose.Position.z);
                return T * R * S; // [순서 중요] 연산순서는 S->R->T순
            }
        }

        public Vertex3f Size
        {
            get => _size;
            set => _size = value;
        }

        public Vertex3f Position
        {
            get => _pose.Position;
            set => _pose.Position = value;
        }

        public Pose Pose
        {
            get => Pose; 
            set => Pose = value;
        }
        public bool IsMoved
        {
            get => _isMoved; 
            set => _isMoved = value;
        }

        public void LocalBindTransform(float sx = 1.0f, float sy = 1.0f, float sz = 1.0f,
            float rotx = 0, float roty = 0, float rotz = 0,
            float x = 0, float y = 0, float z = 0)
        {
            _localScaling = new Vertex3f(sx, sy, sz);
            _localRotation = new Vertex3f(rotx, roty, rotz);
            _localPosition = new Vertex3f(x, y, z);

            _localBindMatrix = Matrix4x4f.Translated(x, y, z) *
                    Matrix4x4f.RotatedX(rotx) *
                    Matrix4x4f.RotatedY(roty) *
                    Matrix4x4f.RotatedZ(rotz) *
                    Matrix4x4f.Scaled(sx, sy, sz);
        }

        public void Scale(float scaleX, float scaleY, float scaleZ)
        {
            _size.x = scaleX;
            _size.y = scaleY;
            _size.z = scaleZ;
        }

        public void Translate(float dx, float dy, float dz)
        {
            _pose.Position += new Vertex3f(dx, dy, dz);
        }

        public void Yaw(float deltaDegree)
        {
            Vertex3f up = -_pose.Matrix4x4f.Column2.Vertex3f(); // z 오른손 법칙
            Quaternion4 q = new Quaternion4(up, -deltaDegree);
            _pose.Quaternion = q * _pose.Quaternion;
        }

        public void Roll(float deltaDegree)
        {
            Vertex3f forward = _pose.Matrix4x4f.Column0.Vertex3f(); // y
            Quaternion4 q = new Quaternion4(forward, deltaDegree);
            _pose.Quaternion = q * _pose.Quaternion;
        }

        public void Pitch(float deltaDegree)
        {
            Vertex3f right = _pose.Matrix4x4f.Column1.Vertex3f(); // x
            Quaternion4 q = new Quaternion4(right, deltaDegree);
            _pose.Quaternion = q * _pose.Quaternion;
        }

        public void SetRollPitchAngle(float pitch, float yaw, float roll)
        {
            Vertex3f right = _pose.Matrix4x4f.Column1.Vertex3f();
            Vertex3f up = -_pose.Matrix4x4f.Column2.Vertex3f(); // z 오른손 법칙
            Vertex3f forward = _pose.Matrix4x4f.Column0.Vertex3f();

            Quaternion4 q1 = new Quaternion4(right, pitch);
            Quaternion4 q2 = new Quaternion4(up, yaw);
            Quaternion4 q3 = new Quaternion4(forward, roll);
            _pose.Quaternion = q3 * q1* q2 * Quaternion4.Identity;
        }
    }
}
